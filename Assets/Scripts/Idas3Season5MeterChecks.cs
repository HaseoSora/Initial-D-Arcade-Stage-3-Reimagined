using System;
using System.Collections.Generic;
using UnityEngine;
using Meter=Idas3ArcadeMeterCatalog.Meter;
using Layer=Idas3ArcadeMeterCatalog.Layer;
using Sprite=Idas3ArcadeHud.Sprite;
using Telemetry=Idas3ArcadeHud.Telemetry;

// Exercise recovered source layers through the production compositor. These
// checks establish behavior and clock stability, not original shader parity.
public static class Idas3Season5MeterChecks
{
    public static string RunChecks(){
        int checks=0,resident=Idas3ImportedMeter.ResidentTextureCount;
        void Check(bool ok,string message){++checks;if(!ok)throw new InvalidOperationException("Season 5 meters: "+message);}
        var data=new Telemetry{size=40,version=3,flags=1,gear=3,rpm=4500,revLimit=8500,speedKmh=123,throttle=.6f,brake=.2f};
        Meter Source(int id){var value=Idas3ArcadeMeterCatalog.Get(id+2);Check(value!=null&&value.id==id,"missing source "+id);return value;}
        Layer Find(int id,string name){var value=Array.Find(Source(id).layers,l=>l.name==name);Check(value!=null,"missing layer "+id+"/"+name);return value;}
        Meter Isolate(int id,string name)=>new Meter{id=id,layers=new[]{Find(id,name)}};
        List<Sprite> Compose(Idas3ImportedMeter renderer,Meter meter,float now){
            var result=new List<Sprite>();renderer.Compose(result,meter,new Idas3GameOptions.Values{hudMeterStyle=meter.id+2},data,now);return result;
        }
        float Difference(Sprite a,Sprite b){
            float difference=Vector4.Distance(a.effectParams,b.effectParams)+Vector4.Distance(a.effectParams2,b.effectParams2)+Vector4.Distance(a.sampleMotion,b.sampleMotion);
            difference+=Vector4.Distance((Vector4)a.color,(Vector4)b.color)+Vector4.Distance(new Vector4(a.uv.x,a.uv.y,a.uv.width,a.uv.height),new Vector4(b.uv.x,b.uv.y,b.uv.width,b.uv.height));
            for(int i=0;i<16;++i)difference+=Mathf.Abs(a.transform[i]-b.transform[i]);return difference;
        }
        // Every new stable ID and gear cell must remain selectable, including
        // neutral. Cooked placeholders must never become player options.
        for(int id=90;id<=117;++id){
            if(id==113||id==114){Check(!Idas3ArcadeMeterCatalog.IsValidStyle(id+2),"placeholder is selectable");continue;}
            var meter=Source(id);var gear=Array.Find(meter.layers,l=>l.name=="GearRate01"||l.name=="GearRate");
            Check(gear!=null&&gear.role=="gear","missing gear readout for "+id);
            var isolated=new Meter{id=id,layers=new[]{gear}};
            for(int number=0;number<=6;++number)using(var renderer=new Idas3ImportedMeter()){
                data.gear=number;var sprites=Compose(renderer,isolated,0);Check(sprites.Count==1,"gear missing "+id+"/"+number);
                var uv=sprites[0].uv;
                Check(Mathf.Abs(uv.x-number%gear.atlasCols/(float)gear.atlasCols)<.00001f&&Mathf.Abs(uv.y-(1-(number/gear.atlasCols+1)/(float)gear.atlasRows))<.00001f,"wrong gear cell "+id+"/"+number);
            }
        }
        data.gear=3;
        // The Kazuma face lacks the usual _A suffix. Its explicit source frame
        // registration, rather than filename guessing, must select each scale.
        var face=Find(115,"CenterMeter");
        foreach(int rpm in new[]{8000,9000,10000,13000})foreach(string day in new[]{"A","B"})
            Check(Array.Exists(face.textureVariants,v=>v.maxRpm==rpm&&v.day==day),"Kazuma scale missing "+rpm+day);
        Check(Find(90,"CenterPin").texture.EndsWith("T_Meter10_BasePoint_A",StringComparison.Ordinal),"Pop Team Epic constructor needle was lost");
        Check(Find(98,"CenterPin").texture.EndsWith("T_Meter98_PointRmp_A",StringComparison.Ordinal),"Chibi constructor needle was lost");

        // Isolated motion cannot pass because unrelated speed digits changed.
        foreach(var test in new[]{(91,"Rev_Normal_1"),(93,"BaseTexture_1"),(98,"Chara"),(104,"EyeMove"),(104,"Aura01"),(104,"Cricle01"),(112,"Aura01")}){
            var meter=Isolate(test.Item1,test.Item2);Sprite reference=default;
            foreach(int fps in new[]{120,30,60})using(var renderer=new Idas3ImportedMeter()){
                List<Sprite> sprites=null;
                for(int frame=0;frame<=fps*5/4;++frame)sprites=Compose(renderer,meter,frame/(float)fps);
                sprites=Compose(renderer,meter,1.25f);
                Check(sprites.Count==1,"ambient layer absent: "+test);var actual=sprites[0];
                if(fps==120)reference=actual;else Check(Difference(actual,reference)<.0001f,"frame-dependent motion: "+test);
                sprites=Compose(renderer,meter,1.25f);Check(Difference(actual,sprites[0])<.0001f,"motion advances while paused: "+test);
                bool moved=false;
                for(int frame=1;frame<=120;++frame){sprites=Compose(renderer,meter,1.25f+frame/60f);moved|=Difference(actual,sprites[0])>.001f;}
                Check(moved,"motion frozen: "+test);
                var rewind=Compose(renderer,meter,0)[0];
                using(var fresh=new Idas3ImportedMeter())Check(Difference(rewind,Compose(fresh,meter,0)[0])<.0001f,"seek kept old state: "+test);
            }
        }
        // All three original eye clips must play, one at a time, with their
        // individual source playback lengths and stable phases after seeking.
        var eyeMeter=Source(104);var clips=new List<Idas3ArcadeMeterCatalog.Curve>();
        foreach(var curve in Find(104,"EyeMove").curves)if(curve.animation.Contains("Eye_Loop")&&!clips.Exists(c=>c.animation==curve.animation))clips.Add(curve);
        clips.Sort((a,b)=>string.CompareOrdinal(a.animation,b.animation));Check(clips.Count==3,"eye clips missing");
        float start=0;var state=new Idas3MeterAnimationState();
        foreach(var clip in clips){
            float length=Idas3MeterAnimationState.DurationSeconds(clip);state.Update(eyeMeter,data,start+length*.5f);
            foreach(var candidate in clips)Check(state.TryProgress(candidate,out _)==(candidate==clip),"eye playlist overlap/gap");
            start+=length;
        }
        var dice=Isolate(110,"GearDice_Blur");
        using(var renderer=new Idas3ImportedMeter()){
            Check(Compose(renderer,dice,0).Count==0,"dice rolls without gear change");
            data.gear=4;Compose(renderer,dice,1);var a=Compose(renderer,dice,1.05f);var b=Compose(renderer,dice,1.10f);
            Check(a.Count==1&&b.Count==1&&a[0].uv!=b[0].uv,"dice flipbook is frozen");
            Check(Compose(renderer,dice,4).Count==0,"dice event never ends");
        }
        Check(Idas3ImportedMeter.ResidentTextureCount==resident,"texture leases leaked");
        return "PASS "+checks+" Season 5 meter checks: 26 IDs, all gear cells, constructor frames/needles, ambient/eye/dice animations, pause/seek and 30/60/120 FPS.";
    }
}
