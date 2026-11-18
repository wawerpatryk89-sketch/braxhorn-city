// MIASTO R1 (18.11, właściciel: „Nie zanurkujesz… widać nic”; 13:04 „tak” — przeniesione z gałęzi Mistrzów,
// Braxhorn/ChampionsPodWodaEkran): zasłona ekranu, gdy kamera jest pod lustrem morza w strefie kąpieliska (BraxMiastoNurek) —
// kolor wody morskiej oświetlonej z góry (jaśniej ku górze obrazu, głębiej ku dołowi), powolne falowanie światła, ciemniejsze
// rogi. Rysowana wprost w przestrzeni ekranu (kwadrat −1..1) — cały ekran przy każdym polu widzenia kamery. Lekka (10–18 %):
// pochłanianie światła po drodze przez wodę liczy objętość wody (Braxhorn/MiastoWodaObjetosc), zasłona to tylko odcień i falowanie.
Shader "Braxhorn/MiastoPodWodaEkran"
{
    Properties
    {
        _Top ("Water towards the surface", Color) = (0.30,0.62,0.62,0.10)
        _Bottom ("Water towards the floor", Color) = (0.05,0.24,0.28,0.18)
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
