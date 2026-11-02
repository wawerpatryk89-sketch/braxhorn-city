// LUDZIE (build 14): ciało postaci Rocketbox — Standard (PBR) z przebarwieniem UBRAŃ (dyspozytor 01:28: różne odcienie,
// bez klonów). Alfa tekstury koloru = maska ubrania (1) / skóry (0), liczona w CI (unity-tools/claude/fetch_rocketbox.py).
// Przebarwienie zachowuje cieniowanie tkaniny: jasność piksela × kolor odcienia. _Odcien i _OdcienSila ustawia
// MaterialPropertyBlock (BraxWyglad) — jeden materiał na postać, bez kopii.
Shader "Braxhorn/LudzieUbranie"
{
    Properties
    {
        _MainTex ("Kolor (A = maska ubrania)", 2D) = "white" {}
        _BumpMap ("Normalne", 2D) = "bump" {}
        _Glossiness ("Gladkosc", Range(0, 1)) = .3
        _Odcien ("Odcien ubran", Color) = (1, 1, 1, 1)
        _OdcienSila ("Sila odcienia", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex, _BumpMap;
        half _Glossiness, _OdcienSila;
        fixed4 _Odcien;
        struct Input { float2 uv_MainTex; };
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            half y = dot(c.rgb, half3(.299, .587, .114));
            half3 nowy = saturate(_Odcien.rgb * y * 1.7);
            o.Albedo = lerp(c.rgb, nowy, saturate(c.a * _OdcienSila));
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            o.Smoothness = _Glossiness;
            o.Metallic = 0;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Standard"
}
