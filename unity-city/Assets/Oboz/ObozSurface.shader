Shader "Braxhorn/ObozSurface"
{
 Properties {
  _MainTex("Original colour",2D)="white"{}
  _BumpMap("Original normal",2D)="bump"{}
  _RoughnessTex("Original roughness / ORM",2D)="white"{}
  _MetallicTex("Original metallic",2D)="black"{}
  _OcclusionTex("Original AO",2D)="white"{}
  _PackedORM("Roughness in green channel",Float)=0
  _RoughnessFactor("Original roughness factor",Range(0,1))=1
  _MetallicFactor("Original metal factor",Range(0,1))=1
 }
 SubShader {
  Tags {"RenderType"="Opaque"}
  CGPROGRAM
  #pragma target 3.0
  #pragma surface surf Standard fullforwardshadows
  sampler2D _MainTex,_BumpMap,_RoughnessTex,_MetallicTex,_OcclusionTex;
  float _PackedORM,_RoughnessFactor,_MetallicFactor;
  struct Input {float2 uv_MainTex;};
  void surf(Input IN,inout SurfaceOutputStandard o) {
   float4 rough=tex2D(_RoughnessTex,IN.uv_MainTex);
   o.Albedo=tex2D(_MainTex,IN.uv_MainTex).rgb;
   o.Normal=UnpackNormal(tex2D(_BumpMap,IN.uv_MainTex));
   o.Smoothness=1-saturate(lerp(rough.r,rough.g,_PackedORM)*_RoughnessFactor);
   o.Metallic=tex2D(_MetallicTex,IN.uv_MainTex).r*_MetallicFactor;
   o.Occlusion=tex2D(_OcclusionTex,IN.uv_MainTex).r;
   o.Alpha=1;o.Emission=0;
  }
  ENDCG
 }
 Fallback "Standard"
}
