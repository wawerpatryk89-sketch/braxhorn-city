// BRAXHORN R2 — niebo: panorama 360° (jak Skybox/Panoramic: to samo mapowanie i obrót) z podbitym nasyceniem błękitu.
// Nasycenie liczone w przestrzeni liniowej wokół luminancji: c' = Y + (c - Y) * _Saturation — chmury (szare/białe,
// c ≈ Y) zostają białe, błękit robi się żywszy. Wartość z pomiaru zdjęć z gry (build 2: błękit sRGB 109/126/152,
// nasycenie 0.28) → cel nasycenie 0.50 (sRGB 87/127/176) → _Saturation 2.1. Nic nie świeci (to niebo, tło).
Shader "Braxhorn/OsiedleNiebo"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Panorama (2D, równoodległościowa)", 2D) = "grey" {}
        _Exposure ("Ekspozycja", Range(0, 8)) = 1.05
        _Rotation ("Obrót (stopnie)", Range(0, 360)) = 40
        _Saturation ("Nasycenie (mnożnik wokół luminancji)", Range(0, 4)) = 2.1
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            half _Exposure, _Saturation;
            float _Rotation;
            struct appdata_t { float4 vertex : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex : SV_POSITION; float3 texcoord : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };

            float3 RotateAroundYInDegrees (float3 vertex, float degrees)
            {
                float alpha = degrees * UNITY_PI / 180.0;
                float sina, cosa; sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float3(mul(m, vertex.xz), vertex.y).xzy;
            }
            inline float2 ToRadialCoords (float3 coords)
            {
                float3 n = normalize(coords);
                float latitude = acos(n.y);
                float longitude = atan2(n.z, n.x);
                float2 sphereCoords = float2(longitude, latitude) * float2(0.5 / UNITY_PI, 1.0 / UNITY_PI);
                return float2(0.5, 1.0) - sphereCoords;
            }
            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 rotated = RotateAroundYInDegrees(v.vertex.xyz, _Rotation);
                o.vertex = UnityObjectToClipPos(rotated);
                o.texcoord = v.vertex.xyz;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                half3 c = tex2D(_MainTex, ToRadialCoords(i.texcoord)).rgb;
                half y = dot(c, half3(.2126, .7152, .0722));
                c = max(0, y + (c - y) * _Saturation) * _Exposure;
                return half4(c, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
