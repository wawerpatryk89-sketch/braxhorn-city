// BRAXHORN R2 — kępy: źdźbła trawy i kwiaty rabat (dwie skrzyżowane karty na kępę).
// Wycięcie alfą, obie strony, normalna „w górę” (kępy świecą jak trawnik pod nimi, bez ciemnych boków),
// kolor trawy brany z tej samej plamistej tekstury co trawnik (kępy pasują do podłoża), lekki wiatr,
// z odległością kępy się obniżają i znikają płynnie (bez widocznej granicy). Nic nie świeci.
Shader "Braxhorn/OsiedleKepki"
{
    Properties
    {
        _MainTex ("Atlas kęp (RGBA)", 2D) = "white" {}
        _Color ("Odcień", Color) = (1,1,1,1)
        _Cutoff ("Próg alfy", Range(0,1)) = .45
        _GroundTex ("Tekstura trawnika (kolor plam)", 2D) = "white" {}
        _MacroTile ("Metry na plamę", Float) = 29
        _MacroMix ("Wpływ trawnika", Range(0,1)) = 0
        _Wind ("Wiatr (m)", Float) = .035
        _FadeStart ("Początek zaniku (m)", Float) = 16
        _FadeEnd ("Koniec zaniku (m)", Float) = 30
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True" }
        LOD 200
        Cull Off
        CGPROGRAM
        #pragma surface surf Lambert alphatest:_Cutoff addshadow vertex:vert
        #pragma target 3.0
        sampler2D _MainTex, _GroundTex;
        fixed4 _Color;
        float _MacroTile, _MacroMix, _Wind, _FadeStart, _FadeEnd;
        struct Input { float2 uv_MainTex; float3 worldPos; float fade; float top; };

        void vert (inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
            float d = distance(wp.xz, _WorldSpaceCameraPos.xz);
            float f = saturate((d - _FadeStart) / max(_FadeEnd - _FadeStart, .01));
            float top = v.texcoord.y;
            float t = _Time.y;
            float sway = (sin(t * 1.9 + wp.x * .7 + wp.z * .45) + .5 * sin(t * 3.3 + wp.z * 1.3 + wp.x * .2)) * _Wind * top;
            v.vertex.x += sway; v.vertex.z += sway * .6;
            v.vertex.y -= top * f * .08;
            v.normal = float3(0, 1, 0);
            o.fade = f; o.top = top;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            fixed3 g = tex2D(_GroundTex, IN.worldPos.xz / _MacroTile + float2(.37, .11)).rgb;
            float lum = dot(g, float3(.3, .59, .11));
            float macro = lerp(1, saturate(lum * 2.4) * 1.15, _MacroMix);
            o.Albedo = c.rgb * _Color.rgb * macro * lerp(.72, 1, IN.top);
            o.Alpha = c.a * (1 - IN.fade);
        }
        ENDCG
    }
    FallBack "Legacy Shaders/Transparent/Cutout/VertexLit"
}
