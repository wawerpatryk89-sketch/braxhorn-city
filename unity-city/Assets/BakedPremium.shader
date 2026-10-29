Shader "Braxhorn/BakedPremium"
{
 Properties
 {
  _MainTex("Original albedo and branding",2D)="white"{}
  _Color("Original colour",Color)=(1,1,1,1)
  _VertexColor("Original baked lighting",Float)=1
  _Cull("Cull",Float)=2
  _NormalMap("Surface relief",2D)="bump"{}
  _NormalStrength("Relief strength",Range(0,2))=0
  _NormalRepeat("Relief repeat",Float)=1
  _RoughnessMap("Linear surface roughness",2D)="white"{}
  _RoughnessRepeat("Roughness repeat",Float)=1
  _RoughnessInfluence("Roughness variation",Range(0,1))=0
  _Exposure("Material exposure",Float)=1.1
  _Specular("Material highlight",Range(0,.3))=.01
  _Smoothness("Material smoothness",Range(0,1))=.15
 }
 SubShader
 {
  Tags { "RenderType"="Opaque" "Queue"="Geometry" }
  Cull [_Cull]
  Pass
  {
   CGPROGRAM
   #pragma target 3.0
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex,_NormalMap,_RoughnessMap;
   float4 _MainTex_ST,_Color;
   float _VertexColor,_NormalStrength,_NormalRepeat,_Exposure,_Specular,_Smoothness;
   float _RoughnessRepeat,_RoughnessInfluence;
   struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 light:TEXCOORD1;float3 wp:TEXCOORD2;float3 wn:TEXCOORD3;};
   v2f vert(appdata v)
   {v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);o.light=lerp(float3(1,1,1),v.color.rgb,_VertexColor);o.wp=mul(unity_ObjectToWorld,v.vertex).xyz;o.wn=UnityObjectToWorldNormal(v.normal);return o;}
   fixed4 frag(v2f i):SV_Target
   {
    float3 N=normalize(i.wn);float3 originalN=N;
    if(_NormalStrength>.001)
    {
     float2 uv=i.uv*_NormalRepeat;
     float3 a=ddx(i.wp),b=ddy(i.wp);float2 u=ddx(uv),v=ddy(uv);
     float3 p=cross(b,N),q=cross(N,a),T=p*u.x+q*v.x,B=p*u.y+q*v.y;
     float inv=rsqrt(max(max(dot(T,T),dot(B,B)),1e-10));
     float3 n=UnpackNormal(tex2D(_NormalMap,uv));n.xy*=_NormalStrength;
     N=normalize(T*inv*n.x+B*inv*n.y+N*n.z);
    }
    float3 L=normalize(float3(-.32,.84,.45));
    float relief=clamp(1+(dot(N,L)-dot(originalN,L))*.70,.58,1.32);
    float3 V=normalize(_WorldSpaceCameraPos-i.wp),H=normalize(L+V);
    // Preserve the original baked illumination; data roughness controls only
    // the restrained local highlight, without adding white haze to black mats.
    float roughness=tex2D(_RoughnessMap,i.uv*_RoughnessRepeat).r;
    float smoothness=lerp(_Smoothness,1-roughness,_RoughnessInfluence);
    float spec=pow(saturate(dot(N,H)),lerp(12,160,smoothness))*_Specular;
    spec*=lerp(1,1-roughness*.65,_RoughnessInfluence);
    float3 c=max(0,tex2D(_MainTex,i.uv).rgb*_Color.rgb*i.light*_Exposure*relief+spec);
    c=saturate((c*(2.51*c+.03))/(c*(2.43*c+.59)+.14));
    return float4(c,1);
   }
   ENDCG
  }
 }
}
