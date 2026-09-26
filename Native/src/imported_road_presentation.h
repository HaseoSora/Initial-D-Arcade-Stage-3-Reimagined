#pragma once
#include "math_types.h"

namespace idas3 {
// The D3 contact solver can oscillate a few centimetres on the long, steep
// converted Stage 8 road strips. Stable road-relative presentation follows
// actual elevation without changing the driving/collision state itself.
class ImportedRoadPresentation {
public:
    void reset(){ready_=false;}
    bool ready()const{return ready_;}
    Vec3 position()const{return position_;}
    float pitch()const{return pitch_;}
    float roll()const{return roll_;}
    // Sadamine's grounded presentation is surface-owned: no portion of the
    // solver's repeating heave/pitch/roll reaches the car or camera. The 2 cm
    // anchor cancels OriginalCarBodyPosition's model-origin subtraction.
    void anchor(Vec3 actor,float pitch,float roll,float roadHeight,Vec3 normal,float yaw,bool grounded,double seconds){
        const bool eligible=grounded&&std::isfinite(roadHeight)&&normal.y>.6f&&std::abs(actor.y-roadHeight)<.25f;
        if(!eligible){update(actor,pitch,roll,roadHeight,false,seconds);return;}
        const float along=dot(normal,forward(yaw)),across=dot(normal,right(yaw));
        const float roadPitch=std::atan2(along,normal.y);
        const float roadRoll=std::atan2(-across,std::sqrt(along*along+normal.y*normal.y));
        const bool snap=!ready_||seconds<=0||seconds>.25||length(actor-lastActor_)>10;
        const float response=snap?1.f:float(-std::expm1(-18.0*seconds));
        pitch_=lerpAngle(pitch_,roadPitch,response);roll_=lerpAngle(roll_,roadRoll,response);
        offset_=.02f;lastActor_=actor;position_=actor;position_.y=roadHeight+offset_;ready_=true;
    }
    void update(Vec3 actor,float pitch,float roll,float roadHeight,bool grounded,double seconds){
        const float offset=actor.y-roadHeight;
        const bool eligible=grounded&&std::isfinite(roadHeight)&&std::abs(offset)<.25f;
        const bool snap=!ready_||!eligible||seconds<=0||seconds>.25||length(actor-lastActor_)>10;
        if(snap){offset_=offset;pitch_=wrapAngle(pitch);roll_=wrapAngle(roll);}
        else{
            offset_+=(offset-offset_)*float(-std::expm1(-12.0*seconds));
            const float response=float(-std::expm1(-18.0*seconds));
            pitch_=lerpAngle(pitch_,pitch,response);roll_=lerpAngle(roll_,roll,response);
        }
        lastActor_=actor;position_=actor;
        if(eligible)position_.y=roadHeight+offset_;
        ready_=eligible;
    }
private:
    bool ready_=false;
    Vec3 position_{},lastActor_{};
    float offset_=0,pitch_=0,roll_=0;
};
}
