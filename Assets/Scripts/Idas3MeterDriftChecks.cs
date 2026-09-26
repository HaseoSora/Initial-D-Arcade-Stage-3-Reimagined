using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using Sprite=Idas3ArcadeHud.Sprite;
using Telemetry=Idas3ArcadeHud.Telemetry;

// Explicit diagnostics; never run during ordinary gameplay.
public static class Idas3MeterDriftChecks
{
    static Telemetry Signal(int level,float opacity=1)=>new Telemetry{size=40,version=3,
        flags=9u|((uint)level<<8),gear=3,speedKmh=100,rpm=5000,revLimit=8500,driftOpacity=opacity};
    static Vector4 Light(List<Sprite> sprites){
        var result=Vector4.zero;
        foreach(var s in sprites)result+=new Vector4(s.color.r*s.color.a,s.color.g*s.color.a,s.color.b*s.color.a,s.color.a);
        return result;
    }
    public static void Run(){
        Debug.Log(RunChecks());
        Debug.Log(Idas3HalloweenMeterChecks.RunChecks());
        Debug.Log(Idas3MeterSignalChecks.RunChecks());
        RenderPreviews(Path.GetFullPath("Verification/drift-grades-20260926/previews"));
    }
    public static string RunChecks(){
        int checks=0,families=0,unlit=0;
        void Check(bool ok,string message){++checks;if(!ok)throw new InvalidOperationException("Drift grades: "+message);}
        int resident=Idas3ImportedMeter.ResidentTextureCount;
        for(int index=1;index<Idas3ArcadeMeterCatalog.Count;++index){
            int style=Idas3ArcadeMeterCatalog.StyleAt(index);
            var meter=Idas3ArcadeMeterCatalog.Get(style);if(meter==null)continue;
            bool authored=Array.Exists(meter.layers,l=>l.role=="drift"&&string.IsNullOrEmpty(l.disabledReason));
            var options=new Idas3GameOptions.Values{hudMeterStyle=style};
            var sprites=new List<Sprite>();var colors=new Vector4[4];
            using(var renderer=new Idas3ImportedMeter()){
                // With a steady initial gear this production pass contains only
                // the authored drift layers, so changes cannot hide in other art.
                renderer.Compose(sprites,meter,options,Signal(0,0),0,true);
                Check(renderer.DriftSpriteCount==0&&sprites.Count==0,meter.name+" lights while inactive");
                for(int level=0;level<4;++level){
                    sprites.Clear();renderer.Compose(sprites,meter,options,Signal(level),level+1,true);
                    Check(sprites.Count==renderer.DriftSpriteCount,meter.name+" mixed unrelated effects into a steady-gear sample");
                    Check(authored?renderer.DriftSpriteCount>0:renderer.DriftSpriteCount==0,meter.name+" missing/added grade "+level);
                    colors[level]=Light(sprites);
                    foreach(var s in sprites)Check(s.texture&&s.color.a>0&&s.color.a<=1&&float.IsFinite(s.color.r+s.color.g+s.color.b),meter.name+" invalid lamp sprite");
                    if(authored&&level>0)Check(Vector4.Distance(colors[level],colors[level-1])>.01f,meter.name+" did not change authored color "+level);
                }
                sprites.Clear();renderer.Compose(sprites,meter,options,Signal(1),10,true);float full=Light(sprites).w;
                var fading=Signal(1,.5f);fading.flags&=~8u;
                sprites.Clear();renderer.Compose(sprites,meter,options,fading,10,true);float half=Light(sprites).w;
                Check(!authored||half>0&&half<full,meter.name+" release fade did not preserve authored alpha");
                sprites.Clear();renderer.Compose(sprites,meter,options,Signal(1,0),11,true);
                Check(sprites.Count==0,meter.name+" lamps remain after release");
            }
            if(authored)++families;else ++unlit;
        }
        Check(families>=82,"catalog drift coverage shrank");
        Check(unlit>=5,"designs without a drift light were not covered");
        // Evaluate the same elapsed red-slide time through production composition
        // at different render rates; include Halloween's shared moving parent.
        foreach(int style in new[]{1,2,6,68,70,85}){
            var meter=Idas3ArcadeMeterCatalog.Get(style);var options=new Idas3GameOptions.Values{hudMeterStyle=style};
            Vector4 reference=default;
            foreach(int rate in new[]{240,30,60,120})using(var renderer=new Idas3ImportedMeter()){
                var sprites=new List<Sprite>();
                for(int frame=0;frame<=rate;++frame){sprites.Clear();renderer.Compose(sprites,meter,options,Signal(3),(float)frame/rate,true);}
                var actual=Light(sprites);
                if(rate==240)reference=actual;else Check(Vector4.Distance(reference,actual)<.0001f,meter.name+" blink depends on FPS");
                sprites.Clear();renderer.Compose(sprites,meter,options,Signal(3),1,true);
                Check(Vector4.Distance(actual,Light(sprites))<.0001f,meter.name+" blink advanced while paused");
                bool changed=false;
                for(int frame=1;frame<=60;++frame){sprites.Clear();renderer.Compose(sprites,meter,options,Signal(3),1+frame/60f,true);changed|=Vector4.Distance(actual,Light(sprites))>.01f;}
                Check(changed,meter.name+" recovered Blink is frozen");
                sprites.Clear();renderer.Compose(sprites,meter,options,Signal(3),0,true);var rewound=Light(sprites);
                using(var fresh=new Idas3ImportedMeter()){
                    sprites.Clear();fresh.Compose(sprites,meter,options,Signal(3),0,true);
                    Check(Vector4.Distance(rewound,Light(sprites))<.0001f,meter.name+" seek retained stale animation phase");
                }
            }
        }
        Check(Idas3ArcadeHud.DriftLevel(new Telemetry{version=2,flags=3u<<8})==1,"legacy binary telemetry lost its green fallback");
        Check(Idas3ImportedMeter.ResidentTextureCount==resident,"diagnostics leaked texture leases");
        return "PASS "+checks+" graded drift meter checks; "+families+" authored lamp styles, "+unlit+" unlit styles; all four colors, fade, pause, seek and 30/60/120/240 FPS.";
    }
    public static void RenderPreviews(string output){
        Directory.CreateDirectory(output);
        var host=new GameObject("Graded drift GPU preview");var camera=host.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=0;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.03f,.04f,.05f);camera.allowHDR=camera.allowMSAA=false;
        var target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;
        var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);var commands=new CommandBuffer();camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque,commands);
        try{
            foreach(int style in new[]{1,2,68,70,85})using(var renderer=new Idas3ArcadeHud()){
                var options=new Idas3GameOptions.Values{hudMeterStyle=style};
                for(int level=0;level<4;++level){
                    commands.Clear();renderer.Build(options,Signal(level),1280,720,level+1,true,out _);renderer.Render(commands,1280,720);camera.Render();
                    var previous=RenderTexture.active;
                    try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();}finally{RenderTexture.active=previous;}
                    File.WriteAllBytes(Path.Combine(output,style+"-"+level+".png"),pixels.EncodeToPNG());
                }
            }
        }finally{camera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque,commands);commands.Dispose();camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(pixels);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(host);}
        Debug.Log("Graded drift GPU previews: "+output);
    }
}
