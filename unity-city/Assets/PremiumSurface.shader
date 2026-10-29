Shader "Braxhorn/PremiumSurface"
{
 Properties{
  _MainTex("Original colour",2D)="white"{} _Color("Original tint",Color)=(1,1,1,1)
  _BumpMap("Relief",2D)="bump"{} _BumpScale("Relief strength",Float)=0
  _DetailMap("Micro relief",2D)="bump"{} _DetailScale("Micro repeat",Float)=1 _DetailStrength("Micro strength",Float)=0
  _UseBaked("Original baked lighting",Float)=1 _Smoothness("Smoothness",Range(0,1))=.2 _Metallic("Metallic",Range(0,1))=0
  _Exposure("Exposure",Float)=1 _Cull("Cull",Float)=2 _Glass("Frosted glass",Float)=0
 }
 SubShader{
  Tags{"RenderType"="Opaque" "Queue"="Geometry"}
  Cull [_Cull]
  Pass{
   Tags{"LightMode"="ForwardBase"}
   CGPROGRAM
   #pragma target 3.0
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fwdbase
   #include "UnityCG.cginc"
   #include "Lighting.cginc"
   #include "AutoLight.cginc"
   sampler2D _MainTex,_BumpMap,_DetailMap;
   float4 _MainTex_ST,_Color;float _BumpScale,_DetailScale,_DetailStrength,_UseBaked,_Smoothness,_Metallic,_Exposure,_Glass;
   struct appdata{float4 vertex:POSITION;float3 normal:NORMAL;float4 tangent:TANGENT;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct v2f{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 worldPos:TEXCOORD1;float3 normal:TEXCOORD2;float4 tangent:TEXCOORD3;float3 baked:TEXCOORD4;SHADOW_COORDS(5)};
   v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);o.worldPos=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.tangent=float4(UnityObjectToWorldDir(v.tangent.xyz),v.tangent.w*unity_WorldTransformParams.w);o.baked=v.color.rgb;TRANSFER_SHADOW(o);return o;}
   float3 film(float3 x){return saturate((x*(2.51*x+.03))/(x*(2.43*x+.59)+.14));}
   fixed4 frag(v2f i):SV_Target{
    float3 N=normalize(i.normal);float3 T=normalize(i.tangent.xyz+float3(.000001,0,0));float3 B=normalize(cross(N,T))*i.tangent.w;
    float3 n=UnpackNormal(tex2D(_BumpMap,i.uv));n.xy*=_BumpScale;
    float3 d=UnpackNormal(tex2D(_DetailMap,i.uv*_DetailScale));n.xy+=d.xy*_DetailStrength;
    float3 mapped=normalize(T*n.x+B*n.y+N*max(.3,n.z));
    float3 L=normalize(float3(-.45,.85,.35)),V=normalize(_WorldSpaceCameraPos-i.worldPos),H=normalize(L+V);
    float relief=clamp(1+(dot(mapped,L)-dot(N,L))*.72,.62,1.38);
    float3 base=tex2D(_MainTex,i.uv).rgb*_Color.rgb;
    float3 illumination=lerp((.56+.23*saturate(N.y)+.25*saturate(dot(N,L))).xxx,max(i.baked,.14),_UseBaked);
    float shadow=lerp(.62,1,SHADOW_ATTENUATION(i));
    float gloss=lerp(14,140,_Smoothness);float highlight=pow(saturate(dot(mapped,H)),gloss)*(_Smoothness*.17+.012);
    float fresnel=pow(1-saturate(dot(mapped,V)),5);
    float3 spec=lerp(float3(.035,.035,.035),max(base,.12),_Metallic)*(highlight*4+fresnel*.12);
    float3 c=base*illumination*relief*shadow+spec;
    if(_Glass>.5){float g=saturate((i.worldPos.y-4.6)/2);float streak=pow(saturate(sin((i.worldPos.z+i.worldPos.y*.6)*2.3)),20)*.12;c=lerp(float3(.07,.12,.15),float3(.31,.39,.42),g)+streak+fresnel*.16;}
    c=max(c*_Exposure,0);float lum=dot(c,float3(.2126,.7152,.0722));float3 preserve=c*(film(lum.xxx).x/max(.001,lum));
    return float4(lerp(film(c),saturate(preserve),.32),1);
   }
   ENDCG
  }
  UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
 }
 Fallback "Diffuse"
}
