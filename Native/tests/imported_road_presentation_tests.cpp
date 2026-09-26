#include "imported_road_presentation.h"
#include <iostream>
#include <stdexcept>
using namespace idas3;
static unsigned checks=0;
static void check(bool value,const char* text){++checks;if(!value)throw std::runtime_error(text);}
int main()try{
    ImportedRoadPresentation pose;
    float rawEnergy=0,filteredEnergy=0,angleEnergy=0;
    // A 15% downhill at 144km/h, with the repeating 7.5Hz contact residual
    // measured on Sadamine. Following world Y would lag far above this road.
    for(int i=0;i<600;++i){
        float t=i/60.f,road=740.f-6*t,noise=.035f*std::sin(2*pi*7.5f*t);
        Vec3 actor{10,road+.025f+noise,40*t};
        pose.update(actor,.15f+noise*.2f,noise*.1f,road,true,1.0/60);
        check(pose.ready(),"Grounded pose unavailable");
        check(pose.position().x==actor.x&&pose.position().z==actor.z,"Presentation changed horizontal motion");
        check(std::abs(pose.position().y-road-.025f)<.04f,"Presentation lags the descending road");
        if(i>120){rawEnergy+=noise*noise;float f=pose.position().y-road-.025f;filteredEnergy+=f*f;angleEnergy+=(pose.pitch()-.15f)*(pose.pitch()-.15f);}
    }
    check(filteredEnergy<rawEnergy*.12f,"Vertical contact chatter is not attenuated");
    check(angleEnergy<rawEnergy*.04f*.22f,"Pitch chatter is not attenuated");
    auto actor=Vec3{2,8,4};
    pose.update(actor,0,0,7,true,1.0/60);
    check(!pose.ready()&&length(pose.position()-actor)==0,"Airborne motion was flattened");
    pose.update(actor,.1f,.2f,8,false,1.0/60);
    check(!pose.ready()&&length(pose.position()-actor)==0,"Missing road contact was hidden");
    pose.update(actor,.1f,.2f,8,true,0);
    check(pose.ready()&&length(pose.position()-actor)==0,"Restart retained old position lag");
    actor={100,200,300};pose.update(actor,-.1f,-.2f,200,true,1.0/60);
    check(length(pose.position()-actor)==0&&std::abs(pose.pitch()+.1f)<.00001f,"Teleport retained old pose");
    pose.update(actor,2*pi-.001f,0,200,true,0);
    pose.update(actor,.001f,0,200,true,1.0/60);
    check(std::abs(pose.pitch())<.002f,"Angle wrap rotated the body");
    pose.reset();check(!pose.ready(),"Reset retained road presentation");
    ImportedRoadPresentation anchored,quiet;
    for(int i=0;i<600;++i){
        const float t=i/60.f,road=740-6*t,noise=.1f*std::sin(2*pi*7.5f*t);
        Vec3 normal=normalized(Vec3{.03f,1,.15f});
        anchored.anchor({10,road+.025f+noise,40*t},noise,noise,road,normal,0,true,1./60);
        quiet.anchor({10,road+.025f,40*t},0,0,road,normal,0,true,1./60);
        check(anchored.position().y==road+.02f,"Anchoring retained vertical solver motion");
        check(anchored.pitch()==quiet.pitch()&&anchored.roll()==quiet.roll(),"Anchoring retained angular solver motion");
        check(anchored.position().x==10&&anchored.position().z==40*t,"Anchoring changed horizontal movement");
        const auto up=right(0)*(-std::sin(anchored.roll()))+Vec3{0,std::cos(anchored.pitch())*std::cos(anchored.roll()),0}+forward(0)*(std::sin(anchored.pitch())*std::cos(anchored.roll()));
        check(length(up-normal)<.00001f,"Anchored body does not align with the banked road");
    }
    anchored.anchor({2,8,4},.1f,.2f,7,{0,1,0},0,true,1./60);
    check(!anchored.ready()&&anchored.position().y==8,"Anchoring flattened airborne motion");
    anchored.anchor({2,8,4},.1f,.2f,8,{0,1,0},0,false,1./60);
    check(!anchored.ready(),"Anchoring fabricated missing contact");
    anchored.anchor({100,200,300},.1f,.2f,200,{0,1,0},0,true,1./60);
    check(anchored.ready()&&anchored.position().y==200.02f&&std::abs(anchored.pitch())<.00001f,"Anchoring retained old pose after a teleport");
    std::cout<<"PASS "<<checks<<" imported presentation checks; filtered mode RMS ratio "<<std::sqrt(filteredEnergy/rawEnergy)<<"; anchored mode retained solver heave/tilt: 0\n";
}catch(const std::exception& e){std::cerr<<e.what()<<'\n';return 1;}
