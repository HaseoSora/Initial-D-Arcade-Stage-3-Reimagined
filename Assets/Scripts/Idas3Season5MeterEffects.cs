using UnityEngine;
using Layer=Idas3ArcadeMeterCatalog.Layer;
using Curve=Idas3ArcadeMeterCatalog.Curve;

// Season 5 material adapters use original textures/parameters and the shared
// presentation clock. Stripped shader graphs are reconstructed, not emulated.
internal sealed partial class Idas3ImportedMeter
{
    static float LoopProgress(Curve curve,float seconds){
        float length=Idas3MeterAnimationState.DurationSeconds(curve);
        if(length<=0)return 0;
        float start=curve.playbackStart,end=curve.playbackEnd;
        if(end<=start){start=curve.animationStart;end=curve.animationEnd;}
        float tick=Mathf.Lerp(start,end,Mathf.Repeat(seconds,length)/length);
        return curve.animationEnd>curve.animationStart?Mathf.Clamp01((tick-curve.animationStart)/(curve.animationEnd-curve.animationStart)):0;
    }
    float AnimatedScalar(Layer layer,string name,float fallback,Idas3ArcadeHud.Telemetry data,float seconds,bool shifts){
        float result=Scalar(layer,name,fallback);
        if(layer.curves!=null)foreach(var curve in layer.curves)
            if(curve.parameter==name&&(curve.owner==null||string.IsNullOrEmpty(curve.owner.name)||curve.owner.name==layer.name)&&Progress(curve,data,seconds,shifts,out var progress))
                result=Evaluate(curve,progress);
        return result;
    }
    void ApplySeason5Material(ref Idas3ArcadeHud.Sprite sprite,Layer layer,Idas3ArcadeHud.Telemetry data,float seconds,float percentage,bool shifts){
        string parent=layer.materialParent;
        if(Contains(parent,"M_Shadow_Ball.")){
            sprite.materialEffect=8;
            sprite.effectParams=new Vector4(Scalar(layer,"S Radius",.2f),Scalar(layer,"M Radius",.3f),Scalar(layer,"L Radius",.5f),Scalar(layer,"Diamond",5));
            sprite.effectParams2=new Vector4(Scalar(layer,"S Density",.6f),Scalar(layer,"M Density",.5f),Scalar(layer,"L Density",.6f),1);
        }else if(Contains(parent,"M_MaterEyesMoveing.")){
            sprite.materialEffect=9;sprite.effectTex1=BoundTexture(layer,"DistortionUVTex");
            sprite.effectParams=new Vector4(AnimatedScalar(layer,"CenterPos_U",0,data,seconds,shifts),AnimatedScalar(layer,"CenterPos_V",0,data,seconds,shifts),
                Scalar(layer,"TexSize",.75f),Scalar(layer,"DistortionPower",.5f));
        }else if(Contains(parent,"M_MaterGradRotate.")){
            sprite.materialEffect=10;sprite.texture=BoundTexture(layer,"Textures");sprite.effectTex1=BoundTexture(layer,"Tex_Mask");sprite.effectTex2=BoundTexture(layer,"Tex_Grad");
            sprite.effectParams=new Vector4(seconds*Scalar(layer,"RotationSpeed",.1f),0,0,0);
        }else if(Contains(parent,"M_Aura.")){
            sprite.materialEffect=11;sprite.texture=BoundTexture(layer,"Tex_Base00");sprite.effectTex1=BoundTexture(layer,"Tex_Base01");
            sprite.effectTex2=BoundTexture(layer,"Tex_Mask");sprite.effectTex3=BoundTexture(layer,"Tex_Distortion");
            sprite.effectColor1=MaterialColor(layer,"Color",Color.white);
            sprite.effectParams=new Vector4(seconds*Scalar(layer,"Speed",.1f),Scalar(layer,"Power",2),Scalar(layer,"UTiling",1),Scalar(layer,"VTiling",1));
        }else if(Contains(parent,"M_GaugeAnm.")){
            sprite.materialEffect=6;sprite.gaugeMode=0;sprite.texture=BoundTexture(layer,"GaugeDesign");
            sprite.effectTex1=BoundTexture(layer,"GaugeGrad");sprite.effectTex2=BoundTexture(layer,"GaugeMask");
            sprite.effectParams=new Vector4(Mathf.Clamp01(percentage),Scalar(layer,"RotationValue",0)*Mathf.Deg2Rad,0,0);
        }else if(Contains(parent,"M_CircleMaskbyTexture.")){
            sprite.materialEffect=12;sprite.gaugeMode=0;sprite.texture=BoundTexture(layer,"Base");sprite.effectTex1=BoundTexture(layer,"Mask");
            sprite.effectParams=new Vector4(AnimatedScalar(layer,"Angle",0,data,seconds,shifts),0,0,0);
        }else if(Contains(parent,"M_MaskCircleTex.")){
            sprite.materialEffect=13;
        }else if(Contains(parent,"M_Aura03.")){
            sprite.materialEffect=14;sprite.texture=BoundTexture(layer,"Mask_Outline");sprite.effectTex1=BoundTexture(layer,"Noise");sprite.effectTex2=BoundTexture(layer,"Distorsion");
            sprite.effectColor1=MaterialColor(layer,"Color2",Color.white);
            sprite.effectParams=new Vector4(Scalar(layer,"Radial_U_size",10),Scalar(layer,"Radial_V_size",3),seconds*Scalar(layer,"Speed X",.15f),seconds*Scalar(layer,"Speed Y",-.1f));
            sprite.effectParams2=new Vector4(Scalar(layer,"MaskRadius_Inner",.3f),Scalar(layer,"MaskDensity_inner",3),0,0);
        }else if(Contains(parent,"M_Distortion.")){
            sprite.materialEffect=15;sprite.texture=BoundTexture(layer,"Tex_BaseImage");sprite.effectTex1=BoundTexture(layer,"Tex_Distortion");sprite.effectTex2=BoundTexture(layer,"Tex_Distortion02");
            sprite.effectParams=new Vector4(seconds,Scalar(layer,"Ammount",.09f),0,0);
        }else if(Contains(parent,"M_Team_CassIcon_Effect_Scroll.")){
            sprite.materialEffect=16;
            sprite.effectParams=new Vector4(seconds,Scalar(layer,"ScrollSpeed",.6f),Scalar(layer,"Period",3),Scalar(layer,"NumberOfScrolls",1));
        }else if(Contains(parent,"M_Meter98_Rotation.")){
            // Authored center sits at the feet. Reconstruct a small sinusoidal
            // sway from retained Width/Speed instead of spinning the character.
            float angle=Mathf.Sin(seconds*Scalar(layer,"Speed",.4f)*Mathf.PI*2)*Scalar(layer,"Width",2);
            var pivot=new Vector3(layer.width*Scalar(layer,"CenterX",.5f),layer.height*Scalar(layer,"CenterY",1),0);
            sprite.transform*=Matrix4x4.Translate(pivot)*Matrix4x4.Rotate(Quaternion.Euler(0,0,angle))*Matrix4x4.Translate(-pivot);
            sprite.materialEffect=17; // full original quad is reserved in layout bounds
        }else if(Contains(parent,"M_UpDownLoop.")){
            float cycle=Mathf.Sin((seconds*Scalar(layer,"Speed",.25f)+Scalar(layer,"Random Seed",0))*Mathf.PI*2);
            float offset=Scalar(layer,"offset Position",0)+cycle*Scalar(layer,"Scale",.025f);
            sprite.sampleMotion=new Vector4(offset*Scalar(layer,"Vector_X",0),offset*Scalar(layer,"Vector_Y",1),Scalar(layer,"Angle",0),0);
        }
    }
}
