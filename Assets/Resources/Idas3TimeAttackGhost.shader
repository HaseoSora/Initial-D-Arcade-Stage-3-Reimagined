// Recovered Arcade ghost opacity: saved Opacity=0/FallOffSoftness=0
// evaluates to 1-texture.a. RGB uses the recovered tint with unlit presentation.
Shader "InitialD/Time Attack Ghost" {
 Properties { _MainTex("Source texture",2D)="white"{} _Tint("Source tint",Color)=(1,1,1,1) }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent"}
  Pass {
   Cull Back ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
   CGPROGRAM
   #pragma target 4.5
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   StructuredBuffer<float4> _IdasFrameWords; uint _IdasView; float4 _IdasDepthProjection;
   sampler2D _MainTex; float4 _Tint;
   struct Input {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
   struct Output {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
   Output vert(Input v){
    Output o;uint b=_IdasView*23;
    float4x4 vp=float4x4(_IdasFrameWords[b],_IdasFrameWords[b+1],_IdasFrameWords[b+2],_IdasFrameWords[b+3]);
    o.pos=mul(mul(unity_ObjectToWorld,v.vertex),vp);o.pos.y*=_ProjectionParams.x;
    #if defined(UNITY_REVERSED_Z)
    o.pos.z=_IdasDepthProjection.w!=0?_IdasDepthProjection.x-_IdasDepthProjection.y*o.pos.w:o.pos.w-o.pos.z;
    #endif
    o.uv=v.uv;return o;
   }
   float4 frag(Output i):SV_Target {float4 c=tex2D(_MainTex,i.uv);return float4(c.rgb*_Tint.rgb,1-c.a);}
   ENDCG
  }
 }
}
