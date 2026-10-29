Shader "Braxhorn/CitySignalLens"
{
    Properties { _Color ("Visible diffuser color", Color) = (0.8,0.02,0.01,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata_base v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                // The lens itself is visible in daylight. No light, bloom,
                // transparent halo or surrounding glow is generated.
                float rib = .94 + .06 * cos(i.uv.x * 90.0) * cos(i.uv.y * 90.0);
                return fixed4(_Color.rgb * rib, 1);
            }
            ENDCG
        }
    }
}
