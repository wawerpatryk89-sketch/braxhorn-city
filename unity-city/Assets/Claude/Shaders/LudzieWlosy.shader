// LUDZIE (build 14): włosy, rzęsy i inne karty z przezroczystością postaci Rocketbox — test alfa (bez sortowania,
// tani na telefonie), obie strony karty widoczne (Cull Off), cień z alfą.
Shader "Braxhorn/LudzieWlosy"
{
    Properties
    {
        _MainTex ("Kolor (A = krycie)", 2D) = "white" {}
        _BumpMap ("Normalne", 2D) = "bump" {}
        _Cutoff ("Prog alfa", Range(0, 1)) = .45
        _Glossiness ("Gladkosc", Range(0, 1)) = .35
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True" }
        LOD 200
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard alphatest:_Cutoff addshadow
        #pragma target 3.0
        sampler2D _MainTex, _BumpMap;
        half _Glossiness;
        struct Input { float2 uv_MainTex; };
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            o.Albedo = c.rgb;
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            o.Smoothness = _Glossiness;
            o.Metallic = 0;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Legacy Shaders/Transparent/Cutout/VertexLit"
}
