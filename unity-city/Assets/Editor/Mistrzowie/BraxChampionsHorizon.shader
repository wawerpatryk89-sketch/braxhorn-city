// Owner 10.11 19:44-19:51: "I panoramy nie widzę tylko niebo", "daj panoramę w tle". Source template (Editor folder, never in
// a player build): BraxChampionsHorizon copies it to PlayableGenerated under the player name "Braxhorn/MistrzowieHorizon".
// The far landscape round the region (coastal hills, headlands, mountain ridges 2-13 km away): albedo in the vertex colour
// (sRGB), lit like CityStaticPBR's fixed city sun without shadows + a sky/ground ambient, and the scene's exponential-squared
// fog (BraxChampionsHorizon.Fog: the colour of the HDR sky at the horizon) as the haze of distance - the ridges pale into the
// sky exactly as the far sea does (the region sea shader takes the same fog). Opaque, no textures, one pass.
Shader "Braxhorn/MistrzowieHorizonSource"
{
    Properties
    {
        _SunDirection ("Sun direction", Vector) = (.35,.8,.45,0)
        _SunColor ("Sun colour", Color) = (1,.97,.92,1)
        _SunStrength ("Sun strength", Float) = 1.05
        _SkyAmbient ("Ambient from the sky", Color) = (.55,.63,.71,1)
        _GroundAmbient ("Ambient from the ground", Color) = (.30,.33,.28,1)
    }
    SubShader
    {
        Tags { "Queue"="Geometry+10" "RenderType"="Opaque" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 3.0
            #include "UnityCG.cginc"

            float4 _SunDirection;
            half4 _SunColor, _SkyAmbient, _GroundAmbient;
            half _SunStrength;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; half4 color : COLOR; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                half3 n : TEXCOORD0;
                half3 albedo : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            half3 Linear(half3 c)
            {
                #ifdef UNITY_COLORSPACE_GAMMA
                return c;
                #else
                return GammaToLinearSpace(c);
                #endif
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.n = UnityObjectToWorldNormal(v.normal);
                o.albedo = Linear(v.color.rgb);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half3 n = normalize(i.n);
                half ndl = saturate(dot(n, normalize(_SunDirection.xyz)));
                half3 ambient = lerp(Linear(_GroundAmbient.rgb), Linear(_SkyAmbient.rgb), n.y * .5h + .5h);
                half3 col = i.albedo * (Linear(_SunColor.rgb) * ndl * _SunStrength + ambient);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return half4(col, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
