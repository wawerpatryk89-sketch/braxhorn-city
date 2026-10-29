Shader "Braxhorn/HeroPremium"
{
 Properties
 {
  _MainTex("Original Brax colour",2D)="white"{}
  _BumpMap("Original normal",2D)="bump"{}
  _Orm("Original AO roughness metal",2D)="white"{}
  _BumpScale("Normal strength",Range(0,2))=.60
  _RoughnessFloor("Matte cloth and fur",Range(0,1))=.78
  _MetallicScale("Source metal response",Range(0,1))=0
  _OcclusionStrength("Source contact detail",Range(0,1))=.8
  _Color("Tint",Color)=(1,1,1,1)
 }
 SubShader{
  Tags{"RenderType"="Opaque"}
  CGPROGRAM
  #pragma target 3.0
  #pragma surface surf Standard fullforwardshadows
  sampler2D _MainTex,_BumpMap,_Orm;
  float _BumpScale,_RoughnessFloor,_MetallicScale,_OcclusionStrength;fixed4 _Color;
  struct Input{float2 uv_MainTex;float2 uv_BumpMap;};
  void surf(Input IN,inout SurfaceOutputStandard o){
   float3 c=tex2D(_MainTex,IN.uv_MainTex).rgb*_Color.rgb;float3 orm=tex2D(_Orm,IN.uv_MainTex).rgb;
   o.Albedo=c;float3 n=UnpackNormal(tex2D(_BumpMap,IN.uv_BumpMap));n.xy*=_BumpScale;o.Normal=normalize(n);
   // Keep all three original ORM channels. A roughness floor restrains the
   // plasticky highlight; the original logo is lit, never self-illuminated.
   o.Metallic=saturate(orm.b*_MetallicScale);
   o.Smoothness=1-clamp(orm.g,_RoughnessFloor,.96);
   o.Occlusion=lerp(1,orm.r,_OcclusionStrength);
   o.Emission=0;o.Alpha=1;
  }
  ENDCG
 }
 Fallback "Standard"
}
