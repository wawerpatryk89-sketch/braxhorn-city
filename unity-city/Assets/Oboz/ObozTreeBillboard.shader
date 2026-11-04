Shader "Braxhorn/ObozTreeBillboard"
{
 Properties { _MainTex("Four rendered source views",2D)="white"{} _Cutoff("Leaf alpha",Range(0,1))=.35 }
 SubShader {
  Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "DisableBatching"="True" }
  Cull Off ZWrite On
  Pass {
   Tags { "LightMode"="ForwardBase" }
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #pragma multi_compile_instancing
   #include "UnityCG.cginc"
   #include "Lighting.cginc"
   sampler2D _MainTex;float _Cutoff;
   struct appdata { float4 vertex:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct v2f { float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;UNITY_FOG_COORDS(1) };
   v2f vert(appdata v) {
    UNITY_SETUP_INSTANCE_ID(v);v2f o;
    float3 origin=mul(unity_ObjectToWorld,float4(0,0,0,1)).xyz;
    float3 toward=_WorldSpaceCameraPos-origin;toward.y=0;toward=normalize(toward+float3(0,0,.00001));
    float3 right=float3(toward.z,0,-toward.x);
    float sx=length(mul((float3x3)unity_ObjectToWorld,float3(1,0,0)));
    float sy=length(mul((float3x3)unity_ObjectToWorld,float3(0,1,0)));
    float3 p=origin+right*v.vertex.x*sx+float3(0,v.vertex.y*sy,0);
    o.vertex=UnityWorldToClipPos(p);
    float3 localView=mul(unity_WorldToObject,float4(_WorldSpaceCameraPos,1)).xyz;
    float turn=atan2(localView.x,localView.z)/6.283185307;
    float frame=fmod(floor(turn*4+4.5),4);
    // glTFast mirrors the source X axis; source-render frames use that same
    // mirrored view convention and need a horizontal pixel mirror.
    o.uv=float2((frame+1-v.uv.x)*.25,v.uv.y);
    UNITY_TRANSFER_FOG(o,o.vertex);return o;
   }
   fixed4 frag(v2f i):SV_Target {
    fixed4 c=tex2D(_MainTex,i.uv);clip(c.a-_Cutoff);
    // Captures contain source albedo, with no permanently baked day light.
    float3 illumination=max(0,ShadeSH9(float4(0,1,0,1)))+_LightColor0.rgb*saturate(_WorldSpaceLightPos0.y);
    c.rgb*=illumination;UNITY_APPLY_FOG(i.fogCoord,c);return c;
   }
   ENDCG
  }
 }
}
