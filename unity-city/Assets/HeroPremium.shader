Shader "Braxhorn/HeroPremium"
{
 Properties{_MainTex("Original Brax colour",2D)="white"{} _BumpMap("Original normal",2D)="bump"{} _Orm("Original AO roughness metal",2D)="white"{} _BumpScale("Normal strength",Range(0,2))=.75 _Color("Tint",Color)=(1,1,1,1)}
 SubShader{
  Tags{"RenderType"="Opaque"}
  CGPROGRAM
  #pragma target 3.0
  #pragma surface surf Standard fullforwardshadows
  sampler2D _MainTex,_BumpMap,_Orm;float _BumpScale;fixed4 _Color;
  struct Input{float2 uv_MainTex;float2 uv_BumpMap;};
  void surf(Input IN,inout SurfaceOutputStandard o){
   float3 c=tex2D(_MainTex,IN.uv_MainTex).rgb*_Color.rgb;float3 orm=tex2D(_Orm,IN.uv_MainTex).rgb;
   o.Albedo=c;float3 n=UnpackNormal(tex2D(_BumpMap,IN.uv_BumpMap));n.xy*=_BumpScale;o.Normal=normalize(n);
   o.Metallic=min(.22,orm.b*.26);o.Smoothness=1-clamp(orm.g*.84+.10,.46,.91);o.Occlusion=lerp(1,orm.r,.75);
   float green=saturate((c.g-c.r*1.2)*8)*saturate((c.g-c.b*1.4)*8);
   o.Emission=c*(.035+green*.055);o.Alpha=1;
  }
  ENDCG
 }
 Fallback "Standard"
}
