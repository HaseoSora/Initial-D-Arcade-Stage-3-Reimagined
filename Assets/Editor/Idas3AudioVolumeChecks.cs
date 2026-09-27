using System;
using System.IO;
using System.Reflection;
using UnityEngine;

public static class Idas3AudioVolumeChecks
{
    sealed class Platform:Idas3GameOptions.IPlatform {
        public int Width=>1280;public int Height=>720;public int DisplayMode=>0;public double Now=>0;
        public Idas3GameOptions.ResolutionChoice[] Resolutions=>new[]{new Idas3GameOptions.ResolutionChoice(1280,720)};
        public void Apply(Idas3GameOptions.Values before,Idas3GameOptions.Values after,bool display){}
    }
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    static float[] Volumes(Idas3GameOptions.Values v)=>new[]{v.masterVolume,v.musicVolume,v.engineVolume,v.tireVolume,v.effectsVolume};
    public static void RunAndBuild(){Run();Idas3Build.RebuildWindowsPlayer();}
    public static void Run(){
        string proof=Path.GetFullPath("Verification/audio-volume");Directory.CreateDirectory(proof);
        string root=Path.Combine(proof,"preferences-"+Guid.NewGuid().ToString("N"));
        var go=new GameObject("Isolated volume checks");
        try{
            var options=new Idas3GameOptions(new Platform());options.Initialize(root);
            foreach(float v in Volumes(options.Current))Check(v==1,"Default volume changed");
            var menu=go.AddComponent<Idas3PauseMenu>();menu.Initialize(options);menu.OpenAttractOptions();menu.SelectTab(0);
            for(int row=0;row<5;++row){
                menu.NavigateHorizontal(1);Check(Math.Abs(Volumes(options.Draft)[row]-1.05f)<.001,"Cannot adjust beyond 100%");
                for(int i=0;i<45;++i)menu.NavigateHorizontal(1);
                Check(Volumes(options.Draft)[row]==2,"Audio row does not stop at 200%");
                menu.NavigateHorizontal(-1);Check(Math.Abs(Volumes(options.Draft)[row]-1.95f)<.001,"Audio step is not 5%");
                for(int i=0;i<45;++i)menu.NavigateHorizontal(-1);
                Check(Volumes(options.Draft)[row]==0,"Audio row cannot mute");
                for(int i=0;i<45;++i)menu.NavigateHorizontal(1);
                menu.Navigate(1);
            }
            Check(options.ApplyDraft(),"Boosted settings failed to save: "+options.LastError);
            var reload=new Idas3GameOptions(new Platform());reload.Initialize(root);
            foreach(float v in Volumes(reload.Current))Check(v==2,"Reload capped a boosted setting");
            options.BeginEdit();options.Draft.masterVolume=.65f;options.Draft.musicVolume=1;
            Check(options.ApplyDraft(),"Could not save legacy-range levels");reload.Initialize(root);
            Check(reload.Current.masterVolume==.65f&&reload.Current.musicVolume==1,"Existing saved volume changed meaning");
            var invalid=Idas3GameOptions.Normalize(new Idas3GameOptions.Values{masterVolume=9,musicVolume=-1,engineVolume=float.NaN,tireVolume=float.PositiveInfinity,effectsVolume=1.5f});
            Check(invalid.masterVolume==2&&invalid.musicVolume==0&&invalid.engineVolume==1&&invalid.tireVolume==1&&invalid.effectsVolume==1.5f,"Invalid audio bounds/fallback");
            var preview=go.AddComponent<Idas3MusicPreview>();
            var refresh=typeof(Idas3MusicPreview).GetMethod("RefreshGain",BindingFlags.NonPublic|BindingFlags.Instance);
            var filter=typeof(Idas3MusicPreview).GetMethod("OnAudioFilterRead",BindingFlags.NonPublic|BindingFlags.Instance);
            foreach(float volume in new[]{0f,.5f,1f,1.5f,2f}){
                preview.Gain=volume;preview.GameGain=4;refresh.Invoke(preview,null);
                var pcm=new[]{.05f,-.05f,.2f,-.2f};filter.Invoke(preview,new object[]{pcm,2});
                Check(Math.Abs(pcm[0]-.05f*volume*4)<1e-6&&Math.Abs(pcm[1]+.05f*volume*4)<1e-6,"Preview boost/mute does not honor all gain controls");
                Check(pcm[2]<=1&&pcm[3]>=-1,"Preview output exceeds PCM bounds");
            }
            // Exercise persistence through the production Sound Room reader/writer.
            string previewRoot=Path.Combine(root,"preview");Directory.CreateDirectory(previewRoot);
            File.WriteAllText(Path.Combine(previewRoot,"sound-room.json"),"{\"volume\":2,\"favorites\":[]}");
            var room=go.AddComponent<Idas3RaceMusicMenu>();
            typeof(Idas3RaceMusicMenu).GetMethod("ConfigurePlayer",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(room,new object[]{previewRoot});
            var loaded=go.transform.Find("Sound Room Preview").GetComponent<Idas3MusicPreview>();
            Check(loaded.Gain==2,"Saved preview gain was capped at 100%");
            File.WriteAllText(Path.Combine(proof,"settings-checks.json"),"{\"passed\":true,\"maximumPercent\":200,\"defaultPercent\":100,\"audioRows\":5,\"previewBoost\":true,\"savedValuesPreserved\":true}");
            Debug.Log("PASS 0-200% audio: five menu rows, 5% input steps, persistence, legacy levels, preview gain and output bounds");
        }finally{UnityEngine.Object.DestroyImmediate(go);}
    }
}
