Shader "Braxhorn/ObozWaterParticle"
{
 Properties { _Color("Water or steam",Color)=(.65,.8,.85,.3) }
 SubShader {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" }
  Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
  Pass {
   Tags { "LightMode"="ForwardBase" }
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #include "UnityCG.cginc"
   #include "Lighting.cginc"
   fixed4 _Color;
   struct appdata { float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR; };
   struct v2f { float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;UNITY_FOG_COORDS(1) };
   v2f vert(appdata v) { v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;UNITY_TRANSFER_FOG(o,o.vertex);return o; }
   fixed4 frag(v2f i):SV_Target {
    fixed4 c=_Color*i.color;float2 d=i.uv*2-1;c.a*=pow(saturate(1-dot(d,d)),2);clip(c.a-.005);
    c.rgb*=max(0,ShadeSH9(float4(0,1,0,1)))+_LightColor0.rgb*saturate(_WorldSpaceLightPos0.y);
    UNITY_APPLY_FOG(i.fogCoord,c);return c;
   }
   ENDCG
  }
 }
}
