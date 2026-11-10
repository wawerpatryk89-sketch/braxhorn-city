// Owner 10.11 08:26: "flagi, żeby powiewały, przed areną, premium". Source template (Editor folder, never in a
// player build): BraxChampionsArenaArrival.FlagMaterial copies it to PlayableGenerated under the player name
// "Braxhorn/MistrzowieFlagMobile". Two-sided alpha-cut banner, waves in the vertex shader (no CPU, no script):
// TEXCOORD1 = (u from the pole 0..1, t from the top bar 0..1); the pole edge and the top bar stay still.
// Lighting like CityStaticPBR's fixed city sun without shadows + flat ambient, plus a little self light.
Shader "Braxhorn/MistrzowieFlagSource"
{
    Properties
    {
        _MainTex ("Banner (RGB), swallowtail cut (A)", 2D) = "white" {}
        _Cutoff ("Alpha cutoff", Range(0,1)) = .5
        _WaveAmp ("Wave amplitude (object units)", Float) = .22
        _WaveLen ("Waves across the banner", Float) = 1.3
        _WaveSpeed ("Wave speed", Float) = 2.3
        _SunDirection ("Sun direction", Vector) = (.35,.8,.45,0)
        _SunColor ("Sun colour", Color) = (1,.96,.9,1)
        _Ambient ("Ambient", Color) = (.50,.53,.58,1)
        _Emission ("Self light", Range(0,1)) = .14
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True" "DisableBatching"="True" }
        Cull Off
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half _Cutoff, _WaveAmp, _WaveLen, _WaveSpeed, _Emission;
            float4 _SunDirection;
            half4 _SunColor, _Ambient;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float2 wave : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 n : TEXCOORD1;
                UNITY_FOG_COORDS(2)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float u = v.wave.x;
                float t = v.wave.y;
                float w = u * sqrt(t);
                float3 wp = mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz;
                float ph = _Time.y * _WaveSpeed - (u * .8 + t * .6) * _WaveLen * 6.2832 + (wp.x + wp.z) * .37;
                float d = (sin(ph) + .35 * sin(ph * 2.3 + 1.7)) * _WaveAmp * w;
                float slope = (cos(ph) + .8 * cos(ph * 2.3 + 1.7)) * _WaveAmp * w * _WaveLen * 3.3;
                float4 p = v.vertex;
                p.xyz += v.normal * d;
                // banner object frame: x = across the banner, y = up, normal = +-z
                float3 n = normalize(v.normal - float3(slope, 0, 0) * sign(v.normal.z));
                o.pos = UnityObjectToClipPos(p);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.n = UnityObjectToWorldNormal(n);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i, fixed facing : VFACE) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                clip(c.a - _Cutoff);
                float3 n = normalize(i.n) * (facing > 0 ? 1.0 : -1.0);
                half ndl = saturate(dot(n, normalize(_SunDirection.xyz)));
                half3 col = c.rgb * (_Ambient.rgb + _SunColor.rgb * ndl * .85) + c.rgb * _Emission;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
    Fallback "Legacy Shaders/Transparent/Cutout/Diffuse"
}
