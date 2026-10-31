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
        _GroundTex ("Tekstura trawnika (ta sama co trawnik)", 2D) = "white" {}
        _Ground ("Kolor z trawnika pod kępą (0/1)", Float) = 0
        _GroundColor ("Odcień trawnika", Color) = (.90,1,.88,1)
        _Dry ("Odcień suchszy", Color) = (1.12,1.02,.78,1)
        _Tile ("Metry na kafel trawnika", Float) = 3.5
        _MacroTile ("Metry na plamę", Float) = 29
        _MacroMix ("Siła plam", Range(0,1)) = .45
        _DryMix ("Siła przebarwień", Range(0,1)) = .35
        _Stripe ("Pas koszenia (m)", Float) = 2.4
        _StripeMix ("Siła pasów", Range(0,.5)) = .09
        _TipLight ("Jaśniejsze końcówki", Range(0,1)) = .32
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
        fixed4 _Color, _GroundColor, _Dry;
        float _Ground, _Tile, _MacroTile, _MacroMix, _DryMix, _Stripe, _StripeMix, _TipLight, _Wind, _FadeStart, _FadeEnd;
        // Dokładnie ten sam kolor co shader trawnika (Braxhorn/OsiedleTrawa) w danym punkcie — kępa wyrasta z trawnika.
        fixed3 GroundAt(float2 w)
        {
            float2 r = float2(w.x * .825 - w.y * .565, w.x * .565 + w.y * .825);
            fixed3 c = tex2D(_GroundTex, r / _Tile).rgb;
            fixed3 c2 = tex2D(_GroundTex, w / (_Tile * 2.7) + float2(.31, .77)).rgb;
            c = lerp(c, (c + c2) * .5, .35);
            fixed3 macro = tex2D(_GroundTex, w / _MacroTile + float2(.37, .11)).rgb;
            c *= lerp(1, saturate(dot(macro, float3(.3, .59, .11)) * 2.2), _MacroMix);
            float dry = saturate((tex2D(_GroundTex, w / (_MacroTile * 2.3) + float2(.71, .29)).g - .32) * 4);
            c = lerp(c, c * _Dry.rgb, dry * _DryMix);
            float stripe = step(.5, frac(w.x / (_Stripe * 2)));
            c *= 1 + (stripe - .5) * 2 * _StripeMix;
            return c * _GroundColor.rgb;
        }
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
            if (_Ground > .5)
            {
                // źdźbło = kolor trawnika pod nim, końcówki jaśniejsze, lekkie zróżnicowanie z tekstury źdźbeł
                float var = saturate(dot(c.rgb, float3(.3, .59, .11)) * 2.6);
                o.Albedo = GroundAt(IN.worldPos.xz) * lerp(.96, 1 + _TipLight, IN.top) * lerp(.9, 1.1, var);
            }
            else o.Albedo = c.rgb * _Color.rgb * lerp(.78, 1, IN.top);
            o.Alpha = c.a * (1 - IN.fade);
        }
        ENDCG
    }
    FallBack "Legacy Shaders/Transparent/Cutout/VertexLit"
}
