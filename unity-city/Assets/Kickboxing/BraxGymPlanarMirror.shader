Shader "Braxhorn/GymPlanarMirror"
{
    Properties
    {
        _ReflectionTex("Live planar reflection", 2D) = "black" {}
        _Tint("Neutral glass tint", Color) = (0.97, 0.985, 0.98, 1)
        _BackingColor("Glass backing", Color) = (0.055, 0.062, 0.060, 1)
        _Reflectance("Mirror reflectance", Range(0, 1)) = 0.96
        [HideInInspector] _ReflectionReady("Reflection ready", Float) = 0
        [HideInInspector] _MirrorPlane("Plane", Vector) = (0, 0, 1, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        // Handles imported or generated plane winding. The physical front side
        // is enforced by _MirrorPlane, independent of the mesh winding.
        Cull Off
        ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _ReflectionTex;
            float4x4 _ReflectionTextureMatrix;
            float4 _MirrorPlane;
            fixed4 _Tint, _BackingColor;
            half _Reflectance, _ReflectionReady;

            struct appdata { float4 vertex : POSITION; };
            struct v2f
            {
                float4 position : SV_POSITION;
                float4 reflectionPosition : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                float4 worldPosition = mul(unity_ObjectToWorld, v.vertex);
                o.reflectionPosition = mul(_ReflectionTextureMatrix, worldPosition);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                clip(dot(_MirrorPlane, float4(_WorldSpaceCameraPos, 1)) - 0.001);
                float w = max(i.reflectionPosition.w, 0.00001);
                float2 uv = i.reflectionPosition.xy / w;
                half inside = step(0.00001, i.reflectionPosition.w)
                    * step(0, uv.x) * step(uv.x, 1) * step(0, uv.y) * step(uv.y, 1);
                fixed3 reflected = tex2D(_ReflectionTex, saturate(uv)).rgb * _Tint.rgb;
                fixed3 colour = lerp(_BackingColor.rgb, reflected, _Reflectance * _ReflectionReady * inside);
                return fixed4(colour, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
