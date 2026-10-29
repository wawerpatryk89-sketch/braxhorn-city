Shader "Braxhorn/HeroPremium"
{
 Properties{_MainTex("Original Brax colour",2D)="white"{} _BumpMap("Original normal",2D)="bump"{} _Orm("Original AO roughness metal",2D)="white"{} _BumpScale("Normal strength",Range(0,2))=.75 _Color("Tint",Color)=(1,1,1,1)}
 SubShader{
  Tags{"RenderType"="Opaque"}
  CGPROGRAM
  #pragma target 3.0
  #pragma surface surf StandardSpecular fullforwardshadows
  sampler2D _MainTex,_BumpMap,_Orm;float _BumpScale;fixed4 _Color;
  struct Input{float2 uv_MainTex;float2 uv_BumpMap;};
  void surf(Input IN,inout SurfaceOutputStandardSpecular o){
   float3 c=tex2D(_MainTex,IN.uv_MainTex).rgb*_Color.rgb;float3 orm=tex2D(_Orm,IN.uv_MainTex).rgb;
   o.Albedo=c;float3 n=UnpackNormal(tex2D(_BumpMap,IN.uv_BumpMap));n.xy*=_BumpScale*.5;o.Normal=normalize(n);
   o.Specular=lerp(float3(.008,.008,.008),float3(.025,.025,.025),saturate(orm.b));
   o.Smoothness=clamp((1-orm.g)*.17+.055,.055,.22);o.Occlusion=lerp(1,orm.r,.60);
   float green=saturate((c.g-c.r*1.2)*8)*saturate((c.g-c.b*1.4)*8);
   o.Emission=c*(.035+green*.055);o.Alpha=1;
  }
  ENDCG
 }
 Fallback "Standard (Specular setup)"
}
