using System;
using System.Collections.Generic;
using UnityEngine;
using Meter=Idas3ArcadeMeterCatalog.Meter;
using Curve=Idas3ArcadeMeterCatalog.Curve;

// Native grades select artwork. Recovered curves supply its motion and light;
// only the strongest grade loops Blink. These are presentation rules, not physics.
internal sealed class Idas3MeterDriftAnimation
{
    readonly Dictionary<Curve,int> modes=new Dictionary<Curve,int>();
    Meter meter;
    bool ready,blinking;
    float seconds,phaseStarted,opacity;
    int level;
    static bool Has(string name,string part)=>name!=null&&name.IndexOf(part,StringComparison.OrdinalIgnoreCase)>=0;
    static int Mode(Curve c)=>!Has(c.animation,"DriftLamp")?0:Has(c.animation,"InOut")?1:Has(c.animation,"Blink")?2:Has(c.animation,"Stay")?4:0;
    internal void Reset(){meter=null;modes.Clear();ready=blinking=false;seconds=phaseStarted=opacity=0;level=0;}
    internal void Update(Meter next,Idas3ArcadeHud.Telemetry data,float now){
        if(next==null||data.version<3||(data.flags&1)==0||float.IsNaN(now)||float.IsInfinity(now)){Reset();return;}
        if(meter!=next){
            Reset();meter=next;
            foreach(var layer in next.layers)if(layer?.curves!=null)foreach(var curve in layer.curves){
                if(Mode(curve)==0)continue;
                int available=0;
                foreach(var other in layer.curves)
                    if(other.property==curve.property&&other.parameter==curve.parameter&&other.owner?.name==curve.owner?.name)
                        available|=Mode(other);
                modes[curve]=available;
            }
        }
        int nextLevel=Idas3ArcadeHud.DriftLevel(data);
        bool blink=(data.flags&8)!=0&&nextLevel==3;
        if(!ready||now<seconds||blink!=blinking||nextLevel!=level||opacity<=0)phaseStarted=now;
        level=nextLevel;
        blinking=blink;opacity=Idas3ArcadeHud.LampOpacity(data);seconds=now;ready=true;
    }
    internal bool TryProgress(Curve curve,out float progress){
        progress=0;if(!ready||!modes.TryGetValue(curve,out int available))return false;
        // The lantern's entry/exit rotation has its own recovered event clock.
        if(curve.owner?.name=="Cantera"&&curve.property=="Rotation"&&Mode(curve)==1)return false;
        int wanted=opacity<.9999f?1:blinking?2:4;
        if((available&wanted)==0)wanted=4;
        // Street has entry/exit and blink tracks but no Stay track. Its
        // green/orange/red widgets start transparent in the editor. Hold the
        // completed entry keys for a steady slide instead of that hidden state.
        if(wanted==4&&(available&4)==0&&(available&1)!=0){
            progress=1;return Mode(curve)==1;
        }
        if(Mode(curve)!=wanted)return false;
        if(wanted==1){
            progress=opacity;
            // A short lamp fade can share a longer animation with a moving
            // parent (Halloween). Map opacity to the fade channel's own keys,
            // not the whole animation's duration, or half fade is already full.
            if((curve.property=="Color.A"||curve.property=="RenderOpacity")&&curve.times?.Length>1&&curve.animationEnd>curve.animationStart)
                progress=Mathf.Clamp01((Mathf.Lerp(curve.times[0],curve.times[curve.times.Length-1],opacity)-curve.animationStart)/(curve.animationEnd-curve.animationStart));
            return true;
        }
        float duration=Idas3MeterAnimationState.DurationSeconds(curve);
        if(duration<=0){progress=1;return wanted==4;}
        float rate=curve.ticksPerSecond>0?curve.ticksPerSecond:24000;
        float begin=curve.playbackEnd>curve.playbackStart?curve.playbackStart:curve.animationStart;
        float span=curve.animationEnd-curve.animationStart;
        progress=span>0?Mathf.Clamp01((begin+Mathf.Repeat(seconds-phaseStarted,duration)*rate-curve.animationStart)/span):0;
        return true;
    }
}
