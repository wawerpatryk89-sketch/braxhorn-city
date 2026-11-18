// MIASTO R1 (18.11, właściciel: „Nie zanurkujesz… i widać nic”; 13:04 „tak” — nurkowanie): WODA JAKO OBJĘTOŚĆ pod lustrem,
// widziana tylko spod wody. Odwrócone pudło akwenu dookoła kąpieliska (BraxNabrzezePlaza) — piksel za końcem dna (otwarta woda)
// traci światło po drodze przez wodę od kamery: przepuszczalność T = exp(−k · droga) na kanał (te same współczynniki pochłaniania
// co shader wody), w miejsce straconego światła — światło rozproszone w wodzie. Samo dno tłumi shader terenu (NabrzezeTeren,
// _PodWodaLustro). Dwa przebiegi: 1) mnożenie obrazu przez T (Blend Zero SrcColor), 2) dodanie rozproszonego światła × (1 − T)
// (Blend One One). Gdy kamera jest nad lustrem — nic (piksel odrzucony; z góry i tak zasłania to nieprzezroczysta powierzchnia
// wody). Bez mgły nad wodą.
Shader "Braxhorn/MiastoWodaObjetosc"
{
    Properties
    {
        _Lustro ("Water surface y", Float) = -1.7
        _Pochlanianie ("Light attenuation per metre (RGB)", Vector) = (0.45,0.09,0.075,0)
        _Rozproszenie ("Light scattered by the water (linear)", Vector) = (0.016,0.083,0.112,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent+5" "RenderType"="Transparent" "IgnoreProjector"="True" }
        ZWrite Off
        Cull Off
        CGINCLUDE
        #include "UnityCG.cginc"
        float _Lustro;
        float4 _Pochlanianie, _Rozproszenie;
        struct appdata { float4 vertex : POSITION; };
        struct v2f { float4 pos : SV_POSITION; float3 world : TEXCOORD0; };
        v2f vert(appdata v)
        {
            v2f o;
            o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
            o.pos = mul(UNITY_MATRIX_VP, float4(o.world, 1));
            return o;
        }
        float3 Przepuszczalnosc(float3 world)
        {
            if (_WorldSpaceCameraPos.y >= _Lustro) discard;
            float droga = length(world - _WorldSpaceCameraPos.xyz);
            return exp(-_Pochlanianie.rgb * droga);
        }
        ENDCG
        Pass
        {
            Blend Zero SrcColor
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragT
            half4 fragT(v2f i) : SV_Target { return half4(Przepuszczalnosc(i.world), 1); }
            ENDCG
        }
        Pass
        {
            Blend One One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragS
            half4 fragS(v2f i) : SV_Target { return half4(_Rozproszenie.rgb * (1.0 - Przepuszczalnosc(i.world)), 1); }
            ENDCG
        }
    }
    Fallback Off
}
