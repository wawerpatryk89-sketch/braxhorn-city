// The veil over the screen while the camera is under a Mistrzowie pool's surface (BraxChampionsPodWoda): the colour of the
// water lit from above (lighter at the top of the picture, deeper at the bottom), a slow shimmer, darker corners. Drawn
// straight in clip space (a quad -1..1), so it covers the screen whatever the camera's field of view.
Shader "Braxhorn/ChampionsPodWodaEkran"
{
    Properties
    {
        _Top ("Water towards the surface", Color) = (0.20,0.62,0.78,0.42)
        _Bottom ("Water towards the floor", Color) = (0.02,0.20,0.36,0.58)
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Top, _Bottom;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = float4(v.vertex.x, v.vertex.y * _ProjectionParams.x, .5, 1);
                o.uv = v.uv;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y;
                half4 c = lerp(_Bottom, _Top, i.uv.y);
                half shimmer = .5h + .5h * sin(i.uv.x * 23.0 + sin(i.uv.y * 17.0 + t * 1.3) * 2.0 + t * .9);
                c.rgb += shimmer * shimmer * .05h * i.uv.y;
                half2 d = i.uv - .5h;
                c.a = saturate(c.a + dot(d, d) * .55h);
                return c;
            }
            ENDCG
        }
    }
    FallBack Off
}
