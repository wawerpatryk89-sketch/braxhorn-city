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
  _GroundColor("Gravel colour",2D)="white"{}
  _GroundNormal("Gravel normal",2D)="bump"{}
  _GroundRoughness("Gravel roughness",2D)="white"{}
  _GroundAO("Gravel occlusion",2D)="white"{}
  _GroundUVScale("Gravel relative tiling",Float)=2
 }
 SubShader {
  Tags {"RenderType"="Opaque"}
  CGPROGRAM
  #pragma target 3.0
  #pragma surface surf Standard fullforwardshadows
  #pragma shader_feature_local _ OBOZ_GROUND_BLEND
  sampler2D _MainTex,_BumpMap,_RoughnessTex,_MetallicTex,_OcclusionTex;
  float _PackedORM,_RoughnessFactor,_MetallicFactor;
  #if defined(OBOZ_GROUND_BLEND)
  sampler2D _GroundColor,_GroundNormal,_GroundRoughness,_GroundAO;
  float _GroundUVScale;
  #endif
  struct Input {float2 uv_MainTex; float4 color : COLOR;};
  void surf(Input IN,inout SurfaceOutputStandard o) {
   float4 rough=tex2D(_RoughnessTex,IN.uv_MainTex);
   o.Albedo=tex2D(_MainTex,IN.uv_MainTex).rgb;
   o.Normal=UnpackNormal(tex2D(_BumpMap,IN.uv_MainTex));
   o.Smoothness=1-saturate(lerp(rough.r,rough.g,_PackedORM)*_RoughnessFactor);
   o.Metallic=tex2D(_MetallicTex,IN.uv_MainTex).r*_MetallicFactor;
   o.Occlusion=tex2D(_OcclusionTex,IN.uv_MainTex).r;
   #if defined(OBOZ_GROUND_BLEND)
   float blend=saturate(IN.color.r);
   float2 gravelUV=IN.uv_MainTex*_GroundUVScale;
   o.Albedo=lerp(o.Albedo,tex2D(_GroundColor,gravelUV).rgb,blend);
   o.Normal=normalize(lerp(o.Normal,UnpackNormal(tex2D(_GroundNormal,gravelUV)),blend));
   o.Smoothness=lerp(o.Smoothness,1-tex2D(_GroundRoughness,gravelUV).r,blend);
   o.Occlusion=lerp(o.Occlusion,tex2D(_GroundAO,gravelUV).r,blend);
   o.Metallic=lerp(o.Metallic,0,blend);
   #endif
   o.Alpha=1;o.Emission=0;
  }
  ENDCG
 }
 Fallback "Standard"
}

