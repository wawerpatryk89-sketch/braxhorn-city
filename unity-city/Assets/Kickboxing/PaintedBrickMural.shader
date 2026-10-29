Shader "Braxhorn/PaintedBrickMural"
{
    Properties
    {
        _MainTex ("Approved BRAXHORN painted artwork", 2D) = "white" {}
        _BumpMap ("Original brick pores and mortar relief", 2D) = "bump" {}
        _PaintOpacity ("Dry-pigment opacity", Range(0,1)) = .87
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+1" "RenderType" = "Transparent" }
        LOD 250
        Cull Back
        ZWrite Off
        CGPROGRAM
        #pragma surface surf Standard alpha:fade noforwardadd
        #pragma target 3.0
        sampler2D _MainTex;
        sampler2D _BumpMap;
        half _PaintOpacity;
        struct Input { float2 uv_MainTex; float3 worldPos; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_MainTex;
            // The paint fades into the EXISTING exposed brick. No placard
            // perimeter, fake raised frame, studio backdrop or wall replacement.
            half edgeX = smoothstep(0.0, .09, uv.x)
                       * smoothstep(0.0, .09, 1.0 - uv.x);
            half edgeY = smoothstep(.02, .21, uv.y)
                       * smoothstep(0.0, .15, 1.0 - uv.y);
            float2 brickUV = IN.worldPos.xy / 2.2;
            fixed4 underlying = tex2D(_BumpMap, brickUV);
            half relief = saturate(underlying.b);
            fixed4 pigment = tex2D(_MainTex, uv);
            o.Albedo = pigment.rgb * lerp(.82, 1.00, relief);
            o.Normal = UnpackNormal(underlying);
            o.Metallic = 0;
            o.Smoothness = .06; // dried matte wall paint, never a glossy poster
            o.Occlusion = 1;
            o.Alpha = _PaintOpacity * edgeX * edgeY * lerp(.80, 1.0, relief);
        }
        ENDCG
    }
    Fallback "Transparent/Diffuse"
}
