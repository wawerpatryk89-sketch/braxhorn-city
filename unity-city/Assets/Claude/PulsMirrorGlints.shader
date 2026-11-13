Shader "Braxhorn/PulsMirrorGlints"
{
    SubShader
    {
        Tags { "Queue"="Transparent+1" "RenderType"="Transparent" }
        Pass
        {
            Blend One One
            ZWrite Off
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct input { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct varying { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; half3 glow : COLOR; };
            varying vert(input v)
            {
                varying o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                float3 view = normalize(WorldSpaceViewDir(v.vertex));
                float3 face = UnityObjectToWorldNormal(v.normal);
                float3 key = normalize(float3(1, .36, -.53));
                float3 fill = normalize(float3(.85, .45, .68));
                half ice = pow(saturate(dot(face, normalize(key + view))), 75);
                half amber = pow(saturate(dot(face, normalize(fill + view))), 90);
                o.glow = 1.65 * ice * half3(.72, .91, 1) + 1.4 * amber * half3(1, .76, .48);
                return o;
            }
            fixed4 frag(varying i) : SV_Target
            {
                half edge = max(abs(i.uv.x), abs(i.uv.y));
                return fixed4(i.glow * (1 - smoothstep(.7, 1, edge)), 0);
            }
            ENDCG
        }
    }
}
