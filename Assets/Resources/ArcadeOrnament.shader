Shader "Idas3/Actual Ornament" {
 Properties { _MainTex("Texture",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _Cutoff("Alpha cutoff",Float)=0 _Cull("Cull",Float)=2 _ZWrite("Depth",Float)=1
 _SpecularTex("Recovered specular",2D)="black"{} _NormalTex("Recovered normal",2D)="bump"{} _MaterialMaps("Material maps",Float)=0 _NormalMap("Normal map",Float)=0 _SpecularStrength("Specular",Float)=1 _Roughness("Roughness",Float)=.6 }
 SubShader { Tags {"RenderType"="Transparent"} Pass {
  Cull [_Cull] ZWrite [_ZWrite] ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
  HLSLPROGRAM
  #pragma vertex vert
  #pragma fragment frag
  #pragma target 3.0
  #include "UnityCG.cginc"
  sampler2D _MainTex,_SpecularTex,_NormalTex;float4 _Color;float _Cutoff,_MaterialMaps,_NormalMap,_SpecularStrength,_Roughness;
  struct app {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;};
  struct data {float4 pos:SV_POSITION;float3 normal:TEXCOORD1;float2 uv:TEXCOORD0;float3 world:TEXCOORD2;};
  data vert(app v){data o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.uv=v.uv;o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
  float4 frag(data i):SV_Target {float4 c=tex2D(_MainTex,i.uv)*_Color;clip(c.a-max(_Cutoff,.002));
   float3 n=normalize(i.normal),light=normalize(float3(-.4,.65,-1));
   if(_NormalMap>.5){
    float3 dp1=ddx(i.world),dp2=ddy(i.world);float2 duv1=ddx(i.uv),duv2=ddy(i.uv);
    float3 p1=cross(dp2,n),p2=cross(n,dp1),t=p1*duv1.x+p2*duv2.x,b=p1*duv1.y+p2*duv2.y;
    float norm=rsqrt(max(max(dot(t,t),dot(b,b)),1e-12));float3 mapped=tex2D(_NormalTex,i.uv).xyz*2-1;
    // Source maps stay ordinary linear RGB; no Unity normal-map repacking.
    n=normalize(t*norm*mapped.x+b*norm*mapped.y+n*mapped.z);
   }
   float lighting=.72+.28*abs(dot(n,light));c.rgb*=lighting;
   if(_MaterialMaps>.5){
    float3 view=normalize(_WorldSpaceCameraPos-i.world),halfway=normalize(light+view);
    float highlight=pow(saturate(abs(dot(n,halfway))),lerp(96,8,saturate(_Roughness)));
    c.rgb+=tex2D(_SpecularTex,i.uv).rgb*highlight*_SpecularStrength*.16;
   }
   return c;}
  ENDHLSL
 } }
}
