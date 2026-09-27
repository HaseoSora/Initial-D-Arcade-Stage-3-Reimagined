using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

// A separate 2D source auditions excerpts without changing the native race choice.
[RequireComponent(typeof(AudioSource))]
public sealed class Idas3MusicPreview : MonoBehaviour
{
    AudioSource source;
    AudioClip pendingClip;
    Coroutine loading;
    UnityWebRequest request;
    int generation;
    bool audible,paused;
    volatile float outputGain=.7f;
    public int TrackId {get;private set;}=int.MinValue;
    public bool Loading {get;private set;}
    public bool Playing=>source!=null&&source.isPlaying;
    public float Duration=>source!=null&&source.clip!=null?source.clip.length:0;
    public float Position=>source!=null&&source.clip!=null?source.time:0;
    public float Gain {get;set;}=.7f;
    public float GameGain {get;set;}=1;
    public float[] Waveform {get;private set;}
    public string Error {get;private set;}
    internal float NormalizationGain {get;private set;}=1;
    public event Action<bool> AudibilityChanged;

    void Awake(){source=GetComponent<AudioSource>();source.playOnAwake=false;source.loop=false;source.spatialBlend=0;source.ignoreListenerPause=true;source.volume=1;}
    void Update(){RefreshGain();SetAudible(Playing);}
    void RefreshGain(){
        float gain=float.IsNaN(Gain)||float.IsInfinity(Gain)?.7f:Mathf.Clamp(Gain,0,Idas3GameOptions.MaximumVolume);
        float game=float.IsNaN(GameGain)||float.IsInfinity(GameGain)?1:Mathf.Clamp(GameGain,0,Idas3GameOptions.MaximumVolume*Idas3GameOptions.MaximumVolume);
        outputGain=gain*game;
    }
    // AudioSource.volume is capped at one. Apply the user's gain to decoded
    // PCM here so preview, Music and Master boosts all work on the audio thread.
    void OnAudioFilterRead(float[] data,int channels){
        float gain=outputGain;
        for(int i=0;i<data.Length;++i)data[i]=Mathf.Clamp(data[i]*gain,-1,1);
    }
    void SetAudible(bool value){if(audible==value)return;audible=value;AudibilityChanged?.Invoke(value);}
    public void Toggle(Idas3RaceMusicMenu.Entry track)
    {
        if(TrackId==track.id&&Duration>0){
            if(Playing){source.Pause();paused=true;}
            else if(paused){source.UnPause();paused=false;}
            else{source.time=0;source.Play();}
            SetAudible(Playing);return;
        }
        Stop();if(string.IsNullOrEmpty(track.previewPath)&&string.IsNullOrEmpty(track.pcmPath))return;
        TrackId=track.id;Waveform=track.waveform;Loading=true;int ticket=generation;
        loading=StartCoroutine(Load(track,ticket));
    }
    public void Seek(float seconds){if(Duration>0)source.time=Mathf.Clamp(seconds,0,Mathf.Max(0,Duration-.025f));}
    public void Stop()
    {
        ++generation;if(loading!=null){StopCoroutine(loading);loading=null;}
        if(request!=null){request.Abort();request.Dispose();request=null;}
        if(source!=null){source.Stop();var old=source.clip;source.clip=null;if(old!=null)Destroy(old);}
        if(pendingClip!=null){Destroy(pendingClip);pendingClip=null;}
        NormalizationGain=1;
        Loading=false;paused=false;TrackId=int.MinValue;Error=null;Waveform=null;SetAudible(false);
    }
    IEnumerator Load(Idas3RaceMusicMenu.Entry track,int ticket)
    {
        AudioClip clip=null;
        if(!string.IsNullOrEmpty(track.pcmPath)){
            // Read at most thirty seconds from our imported PCM cache off the main thread.
            var task=Task.Run(()=>ReadPcm(track));
            while(!task.IsCompleted)yield return null;
            if(ticket!=generation)yield break;
            if(task.IsFaulted||task.Result.Length==0){Fail("Preview unavailable.");yield break;}
            var samples=task.Result;clip=AudioClip.Create("Custom music preview",samples.Length/track.pcmChannels,track.pcmChannels,track.pcmRate,false);
            clip.SetData(samples,0);Waveform=Envelope(samples);
        }else{
            try{request=UnityWebRequestMultimedia.GetAudioClip(new Uri(track.previewPath).AbsoluteUri,AudioType.MPEG);request.timeout=15;}
            catch(Exception){Fail("Preview unavailable.");yield break;}
            yield return request.SendWebRequest();
            if(ticket!=generation)yield break;
            if(request.result!=UnityWebRequest.Result.Success){request.Dispose();request=null;Fail("Preview unavailable.");yield break;}
            clip=DownloadHandlerAudioClip.GetContent(request);request.Dispose();request=null;
        }
        if(ticket!=generation){if(clip!=null)Destroy(clip);yield break;}
        if(clip==null){Fail("Preview unavailable.");yield break;}
        // Normalize decoded PCM off the main thread using the same meter as race playback.
        // Retain the clip here so Stop also releases it if this coroutine is cancelled.
        pendingClip=clip;
        var pcm=new float[clip.samples*clip.channels];
        if(!clip.GetData(pcm,0)){Destroy(pendingClip);pendingClip=null;Fail("Preview unavailable.");yield break;}
        int rate=clip.frequency,channels=clip.channels;
        var normalize=Task.Run(()=>Idas3Native.Idas3NormalizeMusicPreview(pcm,pcm.Length,rate,channels));
        while(!normalize.IsCompleted)yield return null;
        if(ticket!=generation)yield break;
        if(normalize.IsFaulted||normalize.Result<=0){Destroy(pendingClip);pendingClip=null;Fail("Preview unavailable.");yield break;}
        NormalizationGain=normalize.Result;
        if(!clip.SetData(pcm,0)){Destroy(pendingClip);pendingClip=null;Fail("Preview unavailable.");yield break;}
        pendingClip=null;Loading=false;loading=null;
        source.clip=clip;RefreshGain();source.Play();SetAudible(true);
    }
    void Fail(string error){Loading=false;loading=null;Error=error;SetAudible(false);}
    static float[] ReadPcm(Idas3RaceMusicMenu.Entry track)
    {
        if(track.pcmRate<8000||track.pcmRate>48000||track.pcmChannels<1||track.pcmChannels>2)throw new InvalidDataException();
        using(var file=File.OpenRead(track.pcmPath))using(var reader=new BinaryReader(file)){
            int count=(int)Math.Min(file.Length/2,(long)track.pcmRate*track.pcmChannels*30);
            count-=count%track.pcmChannels;var data=new float[count];
            for(int i=0;i<count;++i)data[i]=reader.ReadInt16()/32768f;return data;
        }
    }
    static float[] Envelope(float[] data)
    {
        var result=new float[96];float peak=.001f;
        for(int i=0;i<result.Length;++i){double sum=0;int n=0;for(int j=data.Length*i/96;j<data.Length*(i+1)/96;j+=12){sum+=data[j]*data[j];++n;}result[i]=(float)Math.Sqrt(sum/Math.Max(1,n));peak=Math.Max(peak,result[i]);}
        for(int i=0;i<result.Length;++i)result[i]=Math.Max(.055f,result[i]/peak);return result;
    }
    void OnDisable(){Stop();}
    void OnDestroy(){Stop();}
}
