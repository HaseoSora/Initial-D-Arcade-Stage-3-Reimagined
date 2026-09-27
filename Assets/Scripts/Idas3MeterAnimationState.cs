using System;
using System.Collections.Generic;
using Meter=Idas3ArcadeMeterCatalog.Meter;
using Curve=Idas3ArcadeMeterCatalog.Curve;

// Event clocks follow observed telemetry transitions and elapsed time, never
// render-frame counts. Source key times and playback ranges are retained.
internal sealed class Idas3MeterAnimationState
{
    Meter meter;
    bool ready,gearEvent;
    int gear;
    double seconds,gearStarted;
    readonly List<Curve> eyes=new List<Curve>();
    double eyeDuration;

    static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
    static bool Contains(string value,string part)=>value!=null&&value.IndexOf(part,StringComparison.OrdinalIgnoreCase)>=0;
    internal void Reset(){meter=null;ready=false;gearEvent=false;gear=0;seconds=0;gearStarted=0;eyes.Clear();eyeDuration=0;}

    internal void Update(Meter next,Idas3ArcadeHud.Telemetry telemetry,float now){
        bool valid=next!=null&&(telemetry.flags&1)!=0&&telemetry.gear>=0&&telemetry.gear<=6&&Finite(now);
        if(!valid){Reset();return;}
        if(!ready||!ReferenceEquals(meter,next)||now<seconds){
            Reset();meter=next;ready=true;gearEvent=false;gear=telemetry.gear;seconds=now;
            // The cooked eye widget registers three completion-driven loops.
            // A stable 00/01/02 playlist preserves all keys and avoids random
            // changes on pause/seek or when a preview is recreated.
            foreach(var layer in next.layers)if(layer?.curves!=null)foreach(var curve in layer.curves)
                if(Contains(curve.animation,"Eye_Loop")&&!eyes.Exists(c=>c.animation==curve.animation)&&DurationSeconds(curve)>0)eyes.Add(curve);
            eyes.Sort((a,b)=>string.CompareOrdinal(a.animation,b.animation));
            foreach(var curve in eyes)eyeDuration+=DurationSeconds(curve);
            return;
        }
        // Frozen presentation can still receive authoritative telemetry. Track
        // its gear without creating a pulse now or when the clock resumes.
        if(gear!=telemetry.gear){if(now>seconds){gearEvent=true;gearStarted=now;}gear=telemetry.gear;}
        seconds=now;
    }

    static double TicksPerSecond(Curve curve)=>Finite(curve.ticksPerSecond)&&curve.ticksPerSecond>0?curve.ticksPerSecond:24000;
    static void Playback(Curve curve,out double begin,out double end){
        begin=curve.playbackStart;end=curve.playbackEnd;
        if(!Finite(begin)||!Finite(end)||end<=begin){begin=curve.animationStart;end=curve.animationEnd;}
    }
    internal static float DurationSeconds(Curve curve){
        if(curve==null)return 0;
        Playback(curve,out var begin,out var end);
        return Finite(begin)&&Finite(end)&&end>begin?(float)((end-begin)/TicksPerSecond(curve)):0;
    }
    internal bool TryProgress(Curve curve,out float progress){
        progress=0;if(!ready||curve==null)return false;
        Playback(curve,out var begin,out var end);
        double rate=TicksPerSecond(curve),duration=(end-begin)/rate;
        if(!Finite(duration)||duration<=0)return false;
        double elapsed;
        if(Contains(curve.animation,"Gear_Change")){
            if(!gearEvent)return false;
            elapsed=seconds-gearStarted;
            if(elapsed<0||elapsed>=duration)return false;
        }else if(Contains(curve.animation,"Eye_Loop")){
            if(eyeDuration<=0)return false;
            elapsed=seconds%eyeDuration;if(elapsed<0)elapsed+=eyeDuration;
            Curve selected=null;
            foreach(var clip in eyes){double length=DurationSeconds(clip);if(elapsed<length){selected=clip;break;}elapsed-=length;}
            if(selected==null||selected.animation!=curve.animation)return false;
        }else if(Contains(curve.animation,"LED")){
            // Ambient loop phase is stable at a given timestamp, including
            // preview renderer recreation and different display frame rates.
            elapsed=seconds%duration;if(elapsed<0)elapsed+=duration;
        }else return false;
        double keyBegin=curve.animationStart,keyEnd=curve.animationEnd;
        if(!Finite(keyBegin)||!Finite(keyEnd)||keyEnd<=keyBegin)return false;
        progress=(float)Math.Max(0,Math.Min(1,(begin+elapsed*rate-keyBegin)/(keyEnd-keyBegin)));
        return true;
    }
}
