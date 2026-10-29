Shader "Braxhorn/GymDetailPBR"
{
 Properties
 {
  _MainTex("Original colour and branding",2D)="white"{}
  _Color("Surface tint",Color)=(1,1,1,1)
  _BumpMap("Material grain",2D)="bump"{}
  _BumpScale("Grain strength",Range(0,2))=.45
  _RoughnessMap("Linear material roughness",2D)="gray"{}
  _Smoothness("Finish smoothness",Range(0,1))=.2
  _Metallic("Metal content",Range(0,1))=0
 }
 SubShader
 {
  Tags {"RenderType"="Opaque"}
  CGPROGRAM
  #pragma target 3.0
  #pragma surface surf Standard fullforwardshadows
  sampler2D _MainTex,_BumpMap,_RoughnessMap;
  fixed4 _Color;
  half _BumpScale,_Smoothness,_Metallic;
  struct Input {float2 uv_MainTex;float2 uv_BumpMap;float2 uv_RoughnessMap;};
  void surf(Input IN,inout SurfaceOutputStandard o)
  {
   o.Albedo=tex2D(_MainTex,IN.uv_MainTex).rgb*_Color.rgb;
   half3 normal=UnpackNormal(tex2D(_BumpMap,IN.uv_BumpMap));
   normal.xy*=_BumpScale;o.Normal=normalize(normal);
   half roughness=tex2D(_RoughnessMap,IN.uv_RoughnessMap).r;
   o.Smoothness=saturate(_Smoothness*(1+(.75-roughness)*.8));
   o.Metallic=_Metallic;o.Occlusion=1;o.Emission=0;o.Alpha=1;
  }
  ENDCG
 }
 Fallback "Standard"
}
