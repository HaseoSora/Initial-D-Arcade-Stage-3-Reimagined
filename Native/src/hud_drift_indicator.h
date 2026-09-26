#pragma once
#include "math_types.h"
#include <algorithm>
#include <cmath>

namespace idas3 {

// Presentation-only, calibrated HUD behavior; not a source-game handling rule.
// Velocity is actual world motion in metres/second. Heading points along
// (+sin(yaw), 0, +cos(yaw)); vertical motion does not contribute to slip/speed.
class HudDriftIndicator {
public:
    struct Input {
        Vec3 velocity{};
        float yaw=0;
        bool grounded=true,wallContact=false,active=true,paused=false,discontinuity=false;
    };
    void reset(){drifting_=false;opacity_=slipDegrees_=0;entry_=release_=inhibit_=0;level_=candidate_=0;levelReady_=false;levelTime_=0;}
    bool drifting()const{return drifting_;}
    float opacity()const{return float(opacity_);}
    float slipDegrees()const{return slipDegrees_;}
    unsigned level()const{return level_;} // blue, green, orange, red; retained during fade-out

    void advance(float dt,const Input& input){
        // Explicit ownership changes win over pause; a paused valid race freezes
        // every timer and visual, including a wall-rearm cooldown.
        if(!input.active||input.discontinuity){reset();return;}
        if(input.paused||!std::isfinite(dt)||dt<=0)return;
        if(!std::isfinite(input.velocity.x)||!std::isfinite(input.velocity.y)||
           !std::isfinite(input.velocity.z)||!std::isfinite(input.yaw)){reset();return;}

        const double sine=std::sin(double(input.yaw)),cosine=std::cos(double(input.yaw));
        const double forward=double(input.velocity.x)*sine+double(input.velocity.z)*cosine;
        const double lateral=std::abs(double(input.velocity.x)*cosine-double(input.velocity.z)*sine);
        const double speed=std::hypot(double(input.velocity.x),double(input.velocity.z));
        slipDegrees_=speed>0?float(std::atan2(lateral,forward)*(180.0/double(pi))):0;
        double remaining=dt;
        if(!input.grounded||input.wallContact){
            // Keep the cooldown full through contact/air time. Entry qualification
            // only starts after a continuous clear interval, never during a push.
            drifting_=false;entry_=release_=0;inhibit_=InhibitSeconds;
            fade(remaining,false);return;
        }
        if(speed<25.0/3.6||forward<=0||slipDegrees_>65){
            drifting_=false;entry_=release_=0;inhibit_=std::max(0.0,inhibit_-remaining);
            fade(remaining,false);return;
        }
        if(inhibit_>0){
            const double blocked=std::min(remaining,inhibit_);
            inhibit_-=blocked;remaining-=blocked;fade(blocked,false);
            if(remaining<=0)return;
        }
        if(!drifting_){
            if(slipDegrees_>=8&&lateral>=1){
                const double qualifying=std::min(remaining,EntrySeconds-entry_);
                entry_+=qualifying;remaining-=qualifying;fade(qualifying,false);
                if(entry_>=EntrySeconds){drifting_=true;entry_=release_=0;fade(remaining,true);}
            }else{entry_=0;fade(remaining,false);}
        }else if(slipDegrees_<=5){
            const double qualifying=std::min(remaining,ReleaseSeconds-release_);
            release_+=qualifying;remaining-=qualifying;fade(qualifying,true);
            if(release_>=ReleaseSeconds){drifting_=false;release_=0;fade(remaining,false);}
        }else{release_=0;fade(remaining,true);}
    }
private:
    // Consume the pre-transition and post-transition portions independently so
    // the same elapsed driving interval has the same fade at 30/60/120 Hz.
    void fade(double seconds,bool visible){
        if(visible){
            unsigned wanted=levelReady_?level_:0;
            while(wanted<3&&slipDegrees_>=LevelThresholds[wanted])++wanted;
            while(wanted>0&&slipDegrees_<LevelThresholds[wanted-1]-1.5f)--wanted;
            if(!levelReady_){level_=candidate_=wanted;levelReady_=true;levelTime_=0;}
            else if(wanted==level_){candidate_=level_;levelTime_=0;}
            else{
                if(candidate_!=wanted){candidate_=wanted;levelTime_=0;}
                levelTime_+=seconds;
                if(levelTime_>=.08){level_=wanted;levelTime_=0;}
            }
        }else{candidate_=level_;levelTime_=0;}
        opacity_=std::clamp(opacity_+(visible?seconds/FadeInSeconds:-seconds/FadeOutSeconds),0.0,1.0);
        if(opacity_==0&&!visible){level_=candidate_=0;levelReady_=false;}
    }
    // Adapted to D3 body-slip ranges, not recovered Arcade grade thresholds.
    // Entry remains eight degrees; deeper sustained slides promote the color.
    static constexpr float LevelThresholds[]{10.f,13.f,17.f};
    static constexpr double EntrySeconds=.12f,ReleaseSeconds=.18f;
    static constexpr double InhibitSeconds=.25f,FadeInSeconds=.10f,FadeOutSeconds=.20f;
    bool drifting_=false;
    double opacity_=0,entry_=0,release_=0,inhibit_=0;
    float slipDegrees_=0;
    unsigned level_=0,candidate_=0;
    bool levelReady_=false;
    double levelTime_=0;
};
}
