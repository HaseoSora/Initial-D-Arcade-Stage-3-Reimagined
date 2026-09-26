#include "hud_drift_indicator.h"
#include <array>
#include <iostream>
#include <limits>
#include <stdexcept>

namespace {
using namespace idas3;
using Input=HudDriftIndicator::Input;
unsigned checks=0;
void require(bool value,const char* message){++checks;if(!value)throw std::runtime_error(message);}
bool near(float a,float b,float tolerance=1e-5f){return std::abs(a-b)<=tolerance;}
Input motion(float speed,float slip,float yaw=0,float vertical=0){
    Input input;input.yaw=yaw;input.velocity=forward(yaw+slip*pi/180.f)*speed;input.velocity.y=vertical;return input;
}
struct Sample {bool drifting;float opacity,slip;unsigned level;};
Sample sample(const HudDriftIndicator& indicator){return {indicator.drifting(),indicator.opacity(),indicator.slipDegrees(),indicator.level()};}
void finite(const HudDriftIndicator& indicator){
    require(std::isfinite(indicator.opacity())&&indicator.opacity()>=0&&indicator.opacity()<=1,"Opacity escaped [0,1]");
    require(std::isfinite(indicator.slipDegrees())&&indicator.slipDegrees()>=0&&indicator.slipDegrees()<=180,"Slip escaped [0,180]");
    require(indicator.level()<4,"Drift grade escaped four authored colors");
}
void same(const HudDriftIndicator& indicator,Sample before,const char* message){
    require(indicator.drifting()==before.drifting&&indicator.opacity()==before.opacity&&indicator.slipDegrees()==before.slip&&indicator.level()==before.level,message);
}
void lit(HudDriftIndicator& indicator){indicator.reset();indicator.advance(.35f,motion(25,15));require(indicator.drifting()&&near(indicator.opacity(),1),"Sustained drift did not light indicator");}
void phase(HudDriftIndicator& indicator,const Input& input,double seconds,unsigned rate){
    while(seconds>1e-9){const double step=std::min(seconds,1.0/rate);indicator.advance(float(step),input);finite(indicator);seconds-=step;}
}
std::array<Sample,15> trace(unsigned rate){
    HudDriftIndicator indicator;
    const Input drift=motion(25,14),grip=motion(25,4),band=motion(25,6);
    auto wall=drift;wall.wallContact=true;auto air=drift;air.grounded=false;auto inactive=drift;inactive.active=false;
    const std::array<Input,15> inputs={drift,drift,band,grip,band,grip,drift,drift,wall,drift,drift,air,drift,inactive,drift};
    const std::array<double,15> seconds={.09,.065,.041,.11,.05,.215,.06,.08,.02,.29,.13,.02,.4,.01,.17};
    std::array<Sample,15> result{};
    for(unsigned i=0;i<result.size();++i){phase(indicator,inputs[i],seconds[i],rate);result[i]=sample(indicator);}
    return result;
}
}

int main()try{
    HudDriftIndicator indicator;
    require(!indicator.drifting()&&indicator.opacity()==0&&indicator.slipDegrees()==0,"Fresh indicator is visible");
    for(float direction:{-1.f,1.f}){
        indicator.reset();const auto drift=motion(25,direction*14,1.3f);
        indicator.advance(.11f,drift);require(!indicator.drifting()&&indicator.opacity()==0,"Short slip spike armed indicator");
        indicator.advance(.02f,drift);require(indicator.drifting()&&near(indicator.opacity(),.1f),"Entry delay/fade does not consume the threshold remainder");
        require(near(indicator.slipDegrees(),14,1e-4f),"Left/right world slip projection is incorrect");
        indicator.advance(.10f,drift);require(near(indicator.opacity(),1),"Fade in did not settle");
    }

    // Turning the vehicle and its actual travel direction together is grip,
    // even across +/-pi and even with substantial vertical road movement.
    indicator.reset();
    for(unsigned i=0;i<900;++i){
        const float yaw=wrapAngle(2.8f+float(i)*.035f);
        indicator.advance(1.f/60,motion(30,0,yaw,40*std::sin(float(i))));
        require(!indicator.drifting()&&indicator.opacity()==0&&indicator.slipDegrees()<.0001f,"A gripping corner or heading wrap was labeled drift");
    }
    indicator.advance(10,motion(0,0,0,100));require(!indicator.drifting(),"Vertical speed bypassed minimum planar speed");
    for(const auto& input:{motion(24.f/3.6f,20),motion(25.1f/3.6f,8.1f),motion(25,66),motion(25,110),motion(25,180)}){
        indicator.reset();indicator.advance(2,input);finite(indicator);
        require(!indicator.drifting()&&indicator.opacity()==0,"Low speed, insufficient lateral speed, spin or reverse armed drift");
    }
    indicator.reset();indicator.advance(.3f,motion(25.2f/3.6f,60));
    require(indicator.drifting(),"Minimum speed incorrectly used forward component instead of actual planar speed");
    indicator.reset();indicator.advance(.3f,motion(25,64.5f));require(indicator.drifting(),"Valid forward slide rejected below spin cutoff");

    lit(indicator);indicator.advance(.8f,motion(25,6));require(indicator.drifting(),"Hysteresis band released active drift");
    indicator.advance(.17f,motion(25,4));require(indicator.drifting()&&near(indicator.opacity(),1),"Brief grip spike released drift");
    indicator.advance(.02f,motion(25,4));require(!indicator.drifting()&&near(indicator.opacity(),.95f),"Release delay/fade does not consume the threshold remainder");
    indicator.advance(.2f,motion(25,4));require(indicator.opacity()==0,"Released indicator never faded out");
    indicator.reset();indicator.advance(.1f,motion(25,14));indicator.advance(.01f,motion(25,6));indicator.advance(.03f,motion(25,14));
    require(!indicator.drifting(),"Entry qualification accumulated across a grip interruption");
    indicator.advance(.10f,motion(25,14));require(indicator.drifting(),"Continuous slip did not restart entry qualification");
    lit(indicator);
    for(unsigned i=0;i<20;++i){indicator.advance(.1f,motion(25,4));indicator.advance(.04f,motion(25,6));require(indicator.drifting(),"Release qualification accumulated across hysteresis-band recovery");}

    auto wall=motion(25,15);wall.wallContact=true;auto air=wall;air.wallContact=false;air.grounded=false;
    for(const auto& blocked:{motion(0,0),motion(24.f/3.6f,20),motion(25,66),motion(25,180),wall,air}){
        lit(indicator);indicator.advance(.02f,blocked);
        require(!indicator.drifting()&&near(indicator.opacity(),.9f),"Invalid driving state did not immediately disarm and begin fade");
        indicator.advance(.3f,blocked);require(indicator.opacity()==0,"Invalid driving state retained a visible indicator");
    }
    for(const auto& blocked:{wall,air}){
        lit(indicator);indicator.advance(.3f,blocked);
        indicator.advance(.24f,motion(25,15));require(!indicator.drifting()&&indicator.opacity()==0,"Contact/airborne cooldown was bypassed");
        indicator.advance(.015f,motion(25,15));indicator.advance(.10f,motion(25,15));
        require(!indicator.drifting(),"Entry qualified during the contact cooldown");
        indicator.advance(.02f,motion(25,15));require(indicator.drifting()&&near(indicator.opacity(),.05f),"Contact recovery lost the entry/fade boundary");
    }

    lit(indicator);const auto frozen=sample(indicator);auto paused=air;paused.paused=true;
    indicator.advance(10,paused);same(indicator,frozen,"Pause changed classification, slip, opacity or hard-state ownership");
    indicator.reset();indicator.advance(.08f,motion(25,15));paused=motion(25,0);paused.paused=true;indicator.advance(10,paused);
    indicator.advance(.03f,motion(25,15));require(!indicator.drifting(),"Pause advanced the entry timer");
    indicator.advance(.02f,motion(25,15));require(indicator.drifting(),"Pause discarded valid partial entry");
    lit(indicator);indicator.advance(.1f,motion(25,0));paused=motion(25,15);paused.paused=true;indicator.advance(10,paused);
    indicator.advance(.07f,motion(25,0));require(indicator.drifting(),"Pause advanced the release timer");
    indicator.advance(.02f,motion(25,0));require(!indicator.drifting(),"Pause discarded partial release");
    lit(indicator);indicator.advance(.01f,wall);const auto blockedSample=sample(indicator);paused=motion(25,15);paused.paused=true;
    indicator.advance(10,paused);same(indicator,blockedSample,"Pause changed cooldown fade");
    indicator.advance(.3f,motion(25,15));require(!indicator.drifting(),"Pause consumed wall rearm cooldown");
    indicator.advance(.08f,motion(25,15));require(indicator.drifting(),"Wall cooldown did not resume after pause");

    const float nan=std::numeric_limits<float>::quiet_NaN(),inf=std::numeric_limits<float>::infinity();
    lit(indicator);const auto valid=sample(indicator);
    for(float dt:{0.f,-1.f,nan,inf}){indicator.advance(dt,motion(0,0));same(indicator,valid,"Invalid interval mutated presentation");}
    for(unsigned field=0;field<4;++field)for(float invalid:{nan,inf,-inf}){
        lit(indicator);auto input=motion(25,15);
        if(field==0)input.velocity.x=invalid;else if(field==1)input.velocity.y=invalid;else if(field==2)input.velocity.z=invalid;else input.yaw=invalid;
        indicator.advance(.01f,input);finite(indicator);
        require(!indicator.drifting()&&indicator.opacity()==0&&indicator.slipDegrees()==0,"Invalid motion/yaw left stale drift visible");
        indicator.advance(.1f,motion(25,15));require(!indicator.drifting(),"Invalid motion retained partial entry qualification");
    }
    for(unsigned reason=0;reason<3;++reason){
        lit(indicator);auto input=motion(25,15);input.paused=true;
        if(reason==0)input.active=false;else if(reason==1)input.discontinuity=true;else indicator.reset();
        indicator.advance(0,input);require(!indicator.drifting()&&indicator.opacity()==0&&indicator.slipDegrees()==0,"Reset/inactive/discontinuity retained the prior drift while paused");
        indicator.advance(.1f,motion(25,15));require(!indicator.drifting(),"Reset retained an entry timer");
    }
    indicator.reset();auto huge=motion(25,15);huge.velocity.x=huge.velocity.z=std::numeric_limits<float>::max();
    indicator.advance(std::numeric_limits<float>::max(),huge);finite(indicator);
    indicator.advance(std::numeric_limits<float>::max(),motion(25,0));finite(indicator);require(!indicator.drifting()&&indicator.opacity()==0,"Large finite timestep overflowed timers/fade");

    for(unsigned rate:{30u,60u,120u,240u})for(float direction:{-1.f,1.f}){
        indicator.reset();
        const float angles[]{9,11,14,18};
        for(unsigned grade=0;grade<4;++grade){
            phase(indicator,motion(25,direction*angles[grade],1.7f),.35,rate);
            require(indicator.drifting()&&indicator.level()==grade,"Sustained slide selected wrong grade");
        }
        // Noise inside each release band cannot chatter between colors.
        for(unsigned grade=3;grade>0;--grade){
            const float edge=grade==3?17.f:grade==2?13.f:10.f;
            for(unsigned n=0;n<10;++n){
                phase(indicator,motion(25,direction*(edge-.7f)),.03,rate);
                phase(indicator,motion(25,direction*(edge+.2f)),.03,rate);
                require(indicator.level()==grade,"Color flickered inside hysteresis band");
            }
            phase(indicator,motion(25,direction*(edge-1.7f)),.05,rate);
            require(indicator.level()==grade,"Color demoted on a brief slip dip");
            phase(indicator,motion(25,direction*(edge-1.7f)),.04,rate);
            require(indicator.level()==grade-1,"Sustained recovery did not lower grade");
        }
        phase(indicator,motion(25,18),.05,rate);
        require(indicator.level()==0,"Brief deep-slip spike promoted grade");
        const auto beforePause=sample(indicator);auto stopped=motion(25,0);stopped.paused=true;
        indicator.advance(10,stopped);same(indicator,beforePause,"Pause advanced grade timer");
        phase(indicator,motion(25,18),.04,rate);
        require(indicator.level()==3,"Grade timer did not resume after pause");
        indicator.advance(.02f,wall);
        require(!indicator.drifting()&&indicator.level()==3&&indicator.opacity()>0,"Contact fade changed color before fading out");
        phase(indicator,wall,.25,rate);
        require(indicator.opacity()==0&&indicator.level()==0,"Finished fade retained the previous grade");
        phase(indicator,motion(25,9),.7,rate);
        require(indicator.drifting()&&indicator.level()==0,"New shallow drift inherited a deep grade");
        indicator.reset();require(indicator.level()==0,"Reset retained grade");
    }

    const auto reference=trace(240);float worst=0;
    for(unsigned rate:{30u,60u,120u}){
        const auto actual=trace(rate);
        for(unsigned i=0;i<actual.size();++i){
            worst=std::max(worst,std::abs(actual[i].opacity-reference[i].opacity));
            require(actual[i].drifting==reference[i].drifting&&near(actual[i].opacity,reference[i].opacity,2e-5f)&&near(actual[i].slip,reference[i].slip),"Classification/fade differs across frame rates");
        }
    }
    // A single large valid interval and small substeps consume the same
    // classification transition and fade, including partial re-entry fades.
    HudDriftIndicator coarse,fine;
    for(const auto& input:{motion(25,15),motion(25,0),wall,motion(25,15),motion(25,15)}){
        coarse.advance(.29f,input);phase(fine,input,double(.29f),1000);
        require(coarse.drifting()==fine.drifting()&&near(coarse.opacity(),fine.opacity(),2e-5f),"Coarse interval does not preserve transition timing");
    }
    std::cout<<"PASS "<<checks<<" HUD drift presentation checks; worst 30/60/120/240 Hz opacity difference "<<worst<<'\n';return 0;
}catch(const std::exception& error){std::cerr<<"FAIL: "<<error.what()<<'\n';return 1;}
