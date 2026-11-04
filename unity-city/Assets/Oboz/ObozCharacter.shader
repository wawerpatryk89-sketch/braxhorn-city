Shader "Braxhorn/ObozCharacter"
{
 Properties {
  _MainTex("Original colour",2D)="white"{}
  _BumpMap("Original normal",2D)="bump"{}
  _RoughnessTex("Original roughness",2D)="white"{}
  _MetallicTex("Original metallic",2D)="black"{}
  _BumpScale("Normal strength",Range(0,2))=.85
  _RoughnessFloor("Roughness floor",Range(0,1))=.42
  _MetallicScale("Metallic response",Range(0,1))=.30
  _DarkHands("GORR hand correction",Range(0,1))=0
 }
 SubShader {
  Tags {"RenderType"="Opaque"}
  CGPROGRAM
  #pragma target 3.0
  #pragma surface surf Standard fullforwardshadows
  sampler2D _MainTex,_BumpMap,_RoughnessTex,_MetallicTex;
  float _BumpScale,_RoughnessFloor,_MetallicScale,_DarkHands;
  struct Input {float2 uv_MainTex;float2 uv_BumpMap;float4 color:COLOR;};
  void surf(Input IN,inout SurfaceOutputStandard o) {
   o.Albedo=tex2D(_MainTex,IN.uv_MainTex).rgb;
   // Derived vertex alpha comes only from hand/finger bone weights. A warm
   // colour mask preserves the original blue and white wrist wraps.
   float warm=smoothstep(.05,.20,(o.Albedo.r-o.Albedo.g)/max(o.Albedo.r,.001))
             *smoothstep(.02,.15,(o.Albedo.g-o.Albedo.b)/max(o.Albedo.g,.001));
   float hand=smoothstep(.05,.65,IN.color.a)*warm*_DarkHands;
   float charcoal=dot(o.Albedo,float3(.2126,.7152,.0722))*.04+.015;
   o.Albedo=lerp(o.Albedo,charcoal.xxx,hand);
   float3 n=UnpackNormal(tex2D(_BumpMap,IN.uv_BumpMap));n.xy*=_BumpScale;
   o.Normal=normalize(n);
   o.Metallic=saturate(tex2D(_MetallicTex,IN.uv_MainTex).r*_MetallicScale);
   o.Smoothness=1-clamp(tex2D(_RoughnessTex,IN.uv_MainTex).r,_RoughnessFloor,.96);
   o.Metallic*=1-hand;o.Smoothness=lerp(o.Smoothness,.22,hand);
   o.Occlusion=1;o.Emission=0;o.Alpha=1;
  }
  ENDCG
 }
 Fallback "Standard"
}
