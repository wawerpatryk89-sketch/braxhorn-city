Shader "Braxhorn/BakedPremium"
{
 Properties { _MainTex("Original texture",2D)="white"{} _Color("Original color",Color)=(1,1,1,1) _VertexColor("Baked vertex light",Float)=1 _Cull("Cull",Float)=2 }
 SubShader
 {
  Tags { "RenderType"="Opaque" "Queue"="Geometry" }
  Cull [_Cull]
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex;float4 _MainTex_ST;float4 _Color;float _VertexColor;
   struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 light:TEXCOORD1;};
   v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);o.light=lerp(float3(1,1,1),v.color.rgb,_VertexColor);return o;}
   fixed4 frag(v2f i):SV_Target
   {
    float3 c=max(0,tex2D(_MainTex,i.uv).rgb*_Color.rgb*i.light*1.1);
    c=saturate((c*(2.51*c+.03))/(c*(2.43*c+.59)+.14));
    return float4(c,1);
   }
   ENDCG
  }
 }
}
