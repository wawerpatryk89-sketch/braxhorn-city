// BRAXHORN build 14 — wnętrze tunelu do miasta. Światło policzone przy budowie w kolorach wierzchołków (dzień wpadający
// przez portal, plamy światła lamp tunelowych co kilka metrów, jasny wylot daleko), więc wnętrze jest ciemne w każdym
// trybie grafiki (także bez cieni na NISKIEJ — słońce nie „wpada” do tunelu). Tekstura × kolor wierzchołka × jasność.
// Bez mgły sceny (mgła rozjaśniałaby ciemne wnętrze). Oprawy lamp: ta sama technika z dużą jasnością (jasny materiał,
// nie emisja). Nic nie oświetla otoczenia.
Shader "Braxhorn/OsiedleTunel"
{
    Properties
    {
        _MainTex ("Tekstura", 2D) = "white" {}
        _Color ("Odcień", Color) = (1, 1, 1, 1)
        _Jasnosc ("Jasność", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_ST;
            fixed4 _Color; half _Jasnosc;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            v2f vert (appdata v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = TRANSFORM_TEX(v.uv, _MainTex); o.color = v.color; return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed3 c = tex2D(_MainTex, i.uv).rgb * _Color.rgb * i.color.rgb * _Jasnosc;
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
    FallBack "Unlit/Texture"
}
