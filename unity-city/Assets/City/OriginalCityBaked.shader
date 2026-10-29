Shader "Braxhorn/OriginalCityBaked"
{
 Properties {
  _MainTex("Original colour",2D)="white"{} _Color("Original tint",Color)=(1,1,1,1)
  _VertexColor("Source vertex lighting",Float)=0
  _OriginalLight("Original ground lightmap",2D)="white"{}
  _UseLight("Ground lightmap enabled",Float)=0 _LightGain("Original light gain",Float)=1
  _Cull("Cull",Float)=2 _Cutoff("Alpha cutoff",Float)=0
  _SrcBlend("Source blend",Float)=1 _DstBlend("Destination blend",Float)=0 _ZWrite("Depth write",Float)=1
 }
 SubShader {
  Tags {"RenderType"="Opaque"}
  Cull [_Cull] Blend [_SrcBlend] [_DstBlend] ZWrite [_ZWrite]
  Pass {
   CGPROGRAM
   #pragma target 3.0
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex,_OriginalLight;float4 _MainTex_ST,_Color;
   float _VertexColor,_UseLight,_LightGain,_Cutoff;
   struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;float2 uvLight:TEXCOORD1;float4 color:COLOR;};
   struct v2f{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float2 uvLight:TEXCOORD1;float3 light:TEXCOORD2;};
   v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);o.uvLight=v.uvLight;o.light=lerp(float3(1,1,1),v.color.rgb,_VertexColor);return o;}
   fixed4 frag(v2f i):SV_Target{
    float4 c=tex2D(_MainTex,i.uv)*_Color;clip(c.a-_Cutoff);
    float3 light=i.light*lerp(float3(1,1,1),tex2D(_OriginalLight,i.uvLight).rgb*_LightGain,_UseLight);
    c.rgb=max(c.rgb*light,0);c.rgb=saturate((c.rgb*(2.51*c.rgb+.03))/(c.rgb*(2.43*c.rgb+.59)+.14));return c;
   }
   ENDCG
  }
 }
}
