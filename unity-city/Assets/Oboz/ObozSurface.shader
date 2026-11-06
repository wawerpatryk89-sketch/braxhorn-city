Shader "Braxhorn/ObozSurface"
{
 Properties {
  _MainTex("Original colour",2D)="white"{}
  _BumpMap("Original normal",2D)="bump"{}
  _RoughnessTex("Original roughness / ORM",2D)="white"{}
  _MetallicTex("Original metallic",2D)="black"{}
  _OcclusionTex("Original AO",2D)="white"{}
  _PackedRA("Pilot asphalt: R roughness, G AO (linear 4K)",2D)="white"{}
  _PackedORM("Roughness in green channel",Float)=0
  _RoughnessFactor("Original roughness factor",Range(0,1))=1
  _MetallicFactor("Original metal factor",Range(0,1))=1
  _GroundColor("Gravel colour",2D)="white"{}
  _GroundNormal("Gravel normal",2D)="bump"{}
  _GroundRoughness("Gravel roughness",2D)="white"{}
  _GroundAO("Gravel occlusion",2D)="white"{}
  _GroundUVScale("Gravel relative tiling",Float)=2
  _LawnColor("Lawn colour",2D)="white"{}
  _LawnNormal("Lawn normal",2D)="bump"{}
  _LawnRoughness("Lawn roughness",2D)="white"{}
  _LawnAO("Lawn occlusion",2D)="white"{}
  _LawnUVScale("Lawn relative tiling",Float)=2.142857
  _MudColor("Owner mud forest 2K colour",2D)="white"{}
  _MudNormal("Owner mud forest 2K normal",2D)="bump"{}
  _MudRoughness("Owner mud forest 2K roughness",2D)="white"{}
  _MudUVScale("Mud relative tiling",Float)=1
 }
 SubShader {
  Tags {"RenderType"="Opaque"}
  CGPROGRAM
  #pragma target 3.0
  #pragma surface surf Standard fullforwardshadows
  #pragma shader_feature_local _ OBOZ_GROUND_BLEND
  #pragma shader_feature_local _ OBOZ_PACKED_RA
  sampler2D _MainTex,_BumpMap,_RoughnessTex,_MetallicTex,_OcclusionTex,_PackedRA;
  float _PackedORM,_RoughnessFactor,_MetallicFactor;
  #if defined(OBOZ_GROUND_BLEND)
  sampler2D _GroundColor,_GroundNormal,_GroundRoughness,_GroundAO;
  float _GroundUVScale;
  sampler2D _LawnColor,_LawnNormal,_LawnRoughness,_LawnAO;
  float _LawnUVScale;
  sampler2D _MudColor,_MudNormal,_MudRoughness;
  float _MudUVScale;
  #endif
  struct Input {float2 uv_MainTex; float4 color : COLOR;};
  void surf(Input IN,inout SurfaceOutputStandard o) {
   #if defined(OBOZ_PACKED_RA) && !defined(OBOZ_GROUND_BLEND)
   // For pilot clean_asphalt: independent original roughness.r and AO.r
   // are stored in the R and G channels of one linear packed map.
   float2 packedRA = tex2D(_PackedRA,IN.uv_MainTex).rg;
   float4 rough = float4(packedRA.r,0,0,0);
   #else
   float4 rough=tex2D(_RoughnessTex,IN.uv_MainTex);
   #endif
   o.Albedo=tex2D(_MainTex,IN.uv_MainTex).rgb;
   o.Normal=UnpackNormal(tex2D(_BumpMap,IN.uv_MainTex));
   o.Smoothness=1-saturate(lerp(rough.r,rough.g,_PackedORM)*_RoughnessFactor);
   #if defined(OBOZ_GROUND_BLEND)
   // Terrain's forest source is ORM: AO already lives in rough.r.
   // Avoid two redundant terrain samplers on mobile hardware.
   o.Metallic=0;
   o.Occlusion=rough.r;
   float lawn=saturate(IN.color.g);
   float2 lawnUV=IN.uv_MainTex*_LawnUVScale;
   o.Albedo=lerp(o.Albedo,tex2D(_LawnColor,lawnUV).rgb,lawn);
   o.Normal=normalize(lerp(o.Normal,UnpackNormal(tex2D(_LawnNormal,lawnUV)),lawn));
   o.Smoothness=lerp(o.Smoothness,1-tex2D(_LawnRoughness,lawnUV).r,lawn);
   o.Occlusion=lerp(o.Occlusion,tex2D(_LawnAO,lawnUV).r,lawn);
   float blend=saturate(IN.color.r);
   float2 gravelUV=IN.uv_MainTex*_GroundUVScale;
   o.Albedo=lerp(o.Albedo,tex2D(_GroundColor,gravelUV).rgb,blend);
   o.Normal=normalize(lerp(o.Normal,UnpackNormal(tex2D(_GroundNormal,gravelUV)),blend));
   o.Smoothness=lerp(o.Smoothness,1-tex2D(_GroundRoughness,gravelUV).r,blend);
   o.Occlusion=lerp(o.Occlusion,tex2D(_GroundAO,gravelUV).r,blend);
   // Niebieska maska: ubity ziemny obóz, polana walk i rzeczywiste
   // mokre błoto przy workach; czerwony tartan ma osobną geometrię.
   float mud=saturate(IN.color.b);
   float2 mudUV=IN.uv_MainTex*_MudUVScale;
   o.Albedo=lerp(o.Albedo,tex2D(_MudColor,mudUV).rgb,mud);
   o.Normal=normalize(lerp(o.Normal,UnpackNormal(tex2D(_MudNormal,mudUV)),mud));
   o.Smoothness=lerp(o.Smoothness,1-tex2D(_MudRoughness,mudUV).r,mud);
   #else
   o.Metallic=tex2D(_MetallicTex,IN.uv_MainTex).r*_MetallicFactor;
   #if defined(OBOZ_PACKED_RA)
   o.Occlusion=packedRA.g;
   #else
   o.Occlusion=tex2D(_OcclusionTex,IN.uv_MainTex).r;
   #endif
   #endif
   o.Alpha=1;o.Emission=0;
  }
  ENDCG
 }
 Fallback "Standard"
}

