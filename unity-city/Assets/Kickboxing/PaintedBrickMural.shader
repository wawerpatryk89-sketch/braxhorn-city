Shader "Braxhorn/PaintedBrickMural"
{
    Properties
    {
        _MainTex ("Approved BRAXHORN flat mural, 1536x614", 2D) = "white" {}
        _BrickTex ("Original brick albedo", 2D) = "white" {}
        _BumpMap ("Original brick pores and mortar normal", 2D) = "bump" {}
        _PaintOpacity ("Dry pigment coverage", Range(0,1)) = .92
    }
    SubShader
    {
        Tags { "Queue"="Geometry+1" "RenderType"="Opaque" }
        LOD 225
        Cull Back
        ZWrite On
        ZTest LEqual
        CGPROGRAM
        #pragma surface surf Standard noforwardadd
        #pragma target 3.0
        sampler2D _MainTex;
        sampler2D _BrickTex;
        sampler2D _BumpMap;
        half _PaintOpacity;
        struct Input { float2 uv_MainTex; float2 uv2_BrickTex; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            half3 pigment = tex2D(_MainTex,IN.uv_MainTex).rgb;
            half3 brick = tex2D(_BrickTex,IN.uv2_BrickTex).rgb;
            float2 edgeUv = min(IN.uv_MainTex,1.0-IN.uv_MainTex);
            float physicalEdge = min(edgeUv.x*2.5016287,edgeUv.y);
            float hashSeed=dot(floor(IN.uv2_BrickTex*83.0),float2(12.9898,78.233));
            half noise=frac(sin(hashSeed)*43758.5453);
            half roughEdge=saturate((physicalEdge+noise*.014-.028)*31.0);
            half coat=saturate(_PaintOpacity*roughEdge);
            // The wall is still the same brick. Pigment modifies albedo only;
            // shared brick pores and mortar remain visible under the painting.
            o.Albedo=lerp(brick,pigment,coat);
            o.Normal=UnpackNormal(tex2D(_BumpMap,IN.uv2_BrickTex));
            o.Metallic=0;
            o.Smoothness=.12;
            o.Occlusion=1.0;
            // Neutral reflectance lift without introducing a green light source.
            o.Emission=pigment*(.10*coat);
            o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Standard"
}
