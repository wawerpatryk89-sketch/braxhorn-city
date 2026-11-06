Shader "Braxhorn/ObozGrassMedium"
{
    Properties
    {
        _MainTex("Owner Grass Medium 2K diffuse atlas",2D)="white"{}
        _AlphaTex("Owner Grass Medium 16-bit alpha atlas",2D)="white"{}
        _BumpMap("Owner Grass Medium normal atlas",2D)="bump"{}
        _RoughnessTex("Owner Grass Medium roughness atlas",2D)="white"{}
        _Cutoff("Owner alpha threshold",Range(0,1))=.35
    }
    SubShader
    {
        Tags {"Queue"="AlphaTest" "RenderType"="TransparentCutout"}
        Cull Off
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows addshadow
        sampler2D _MainTex,_AlphaTex,_BumpMap,_RoughnessTex;
        float _Cutoff;
        struct Input {float2 uv_MainTex;};
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float2 uv=IN.uv_MainTex;
            clip(tex2D(_AlphaTex,uv).r-_Cutoff);
            o.Albedo=tex2D(_MainTex,uv).rgb;
            o.Normal=UnpackNormal(tex2D(_BumpMap,uv));
            o.Smoothness=1-saturate(tex2D(_RoughnessTex,uv).r);
            o.Metallic=0;
            o.Occlusion=1;
            o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Transparent/Cutout/Diffuse"
}
