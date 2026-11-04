Shader "Braxhorn/ObozStream"
{
 Properties
 {
  _Color("Water tint",Color)=(0.176,0.353,0.353,0.72)
  _Smoothness("Surface smoothness",Range(0,1))=0.88
  _RippleStrength("Ripple normal strength",Range(0,0.3))=0.055
 }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent"}
  LOD 200
  ZWrite Off
  CGPROGRAM
  #pragma target 3.0
  #pragma surface surf Standard alpha:fade
  fixed4 _Color;
  half _Smoothness, _RippleStrength;
  struct Input { float3 worldPos; };
  void surf(Input IN, inout SurfaceOutputStandard o)
  {
   // Two small travelling ripples, in world metres, without grab passes
   // or per-frame material instances. Standard lighting uses scene probes.
   float2 p=IN.worldPos.xz;
   float t=_Time.y;
   float a=dot(p,float2(1.7,3.1))-t*1.25;
   float b=dot(p,float2(-4.3,2.3))-t*1.8;
   float2 slope=float2(cos(a)+0.45*cos(b),sin(a*0.7)+0.35*sin(b));
   o.Normal=normalize(float3(slope*_RippleStrength,1));
   o.Albedo=_Color.rgb;
   o.Metallic=0;
   o.Smoothness=_Smoothness;
   o.Occlusion=1;
   o.Alpha=_Color.a;
  }
  ENDCG
 }
 Fallback "Transparent/Diffuse"
}
