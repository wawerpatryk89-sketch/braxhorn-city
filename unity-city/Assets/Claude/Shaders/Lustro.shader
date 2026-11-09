// BRAXHORN CITY — LUSTRO (10.11, właściciel: „lustro ma być lustrem, a nie naklejką”). Tafla pokazuje teksturę odbicia rysowaną
// przez BraxLustro (kamera odbita w płaszczyźnie lustra) we współrzędnych ekranu — odbicie jest geometrycznie prawdziwe.
// Gdy odbicia nie ma (_OdbicieJest = 0: kamera daleko albo błąd), tafla pokazuje odbicie sondy otoczenia (jak szkło miasta),
// przyciemnione — nigdy czarna ani różowa. Lekki srebrzysty odcień i ciemniejsza krawędź (faza szkła).
Shader "Braxhorn/Lustro"
{
    Properties
    {
        _Tint ("Odcień tafli", Color) = (.93, .95, .96, 1)
        [NoScaleOffset] _OdbicieTex ("Odbicie (BraxLustro)", 2D) = "grey" {}
        _OdbicieJest ("Odbicie narysowane (0/1)", Float) = 0
        _Faza ("Faza krawędzi (udział UV)", Range(0, .1)) = .012
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Cull Back
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _OdbicieTex;
            half4 _Tint;
            half _OdbicieJest, _Faza;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; float4 ekran : TEXCOORD0; float3 wp : TEXCOORD1; float3 n : TEXCOORD2; float2 uv : TEXCOORD3; UNITY_VERTEX_OUTPUT_STEREO };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.ekran = ComputeScreenPos(o.pos);
                o.wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.n = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float2 suv = i.ekran.xy / max(i.ekran.w, 1e-4);
                half3 odb = tex2D(_OdbicieTex, suv).rgb;
                // zapas: sonda otoczenia (niebo / wnętrze) w kierunku odbicia
                float3 V = normalize(i.wp - _WorldSpaceCameraPos);
                float3 R = reflect(V, normalize(i.n));
                half4 env = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, R, 1);
                half3 sonda = DecodeHDR(env, unity_SpecCube0_HDR) * .8;
                half3 c = lerp(sonda, odb, saturate(_OdbicieJest)) * _Tint.rgb;
                float2 kr = min(i.uv, 1 - i.uv);
                half faza = 1 - smoothstep(0, max(_Faza, 1e-4), min(kr.x, kr.y));
                c *= 1 - .35 * faza;
                return half4(c, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
