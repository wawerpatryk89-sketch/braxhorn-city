// BRAXHORN R2 — kępy trawy 3D (warstwa 2 trawnika, zlecenie „trawa” 31.10): instancje jednej siatki
// (3 skrzyżowane płaszczyzny, 6 trójkątów) z kartami kęp Poly Haven (grass_medium_01, atlas 2K z alfą).
// Dwa typy w jednym materiale, rozpoznawane po wysokości instancji (skala macierzy = wysokość w m):
// murawa (~6.5 cm) i łąka (26–40 cm). Kolor nasady = kolor podłoża w tym miejscu (ta sama tekstura i parametry
// co trawnik), ku czubkom kolor karty; cień u nasady (AO). Gęstość z odległością: pełna do Near, połowa do Mid,
// zero na Far (osobno murawa i łąka) — kępa „wrasta” w ziemię płynnie (bez pstryknięcia). Wiatr tylko łąka.
// Odbiera cienie, nie rzuca. Nic nie świeci.
Shader "Braxhorn/OsiedleKepyInst"
{
    Properties
    {
        _MainTex ("Atlas kęp (RGBA)", 2D) = "white" {}
        _Cutoff ("Próg alfy", Range(0,1)) = .45
        _TipTint ("Odcień kart (mnożnik liniowy)", Vector) = (1,1,1,1)
        _GroundTex ("Trawnik: albedo", 2D) = "white" {}
        _GroundTint ("Trawnik: odcień (liniowo)", Vector) = (1,1,1,1)
        _Tile ("Trawnik: metry na kafel", Float) = 2
        _MacroTile ("Trawnik: metry na plamę", Float) = 29
        _MacroMix ("Trawnik: siła plam", Float) = .45
        _MacroMean ("Trawnik: średnia jasność", Float) = .1387
        _Dry ("Trawnik: odcień suchszy (liniowo)", Vector) = (1.12,1.02,.78,1)
        _DryMix ("Trawnik: siła przebarwień", Float) = .35
        _DryMean ("Trawnik: średnia zieleń", Float) = .1631
        _DryThr ("Trawnik: próg suchszych plam", Float) = 1.45
        _DryGain ("Trawnik: narastanie plam", Float) = 2.5
        _Stripe ("Trawnik: pas koszenia (m)", Float) = 1.8
        _StripeMix ("Trawnik: siła pasów", Float) = .045
        _BaseAO ("Cień u nasady", Range(0,1)) = .55
        _Wind ("Wiatr (m)", Float) = .04
        _Near ("Murawa: pełna gęstość do (m)", Float) = 10
        _Mid ("Murawa: połowa gęstości do (m)", Float) = 18
        _Far ("Murawa: koniec kęp (m)", Float) = 22
        _NearL ("Łąka: pełna gęstość do (m)", Float) = 12
        _MidL ("Łąka: połowa gęstości do (m)", Float) = 25
        _FarL ("Łąka: koniec kęp (m)", Float) = 30
        _LakaH ("Granica wysokości łąka/murawa (m)", Float) = .15
        _C0 ("Karta 0 (u, v, w, h)", Vector) = (0,0,1,1)
        _C1 ("Karta 1", Vector) = (0,0,1,1)
        _C2 ("Karta 2", Vector) = (0,0,1,1)
        _C3 ("Karta 3", Vector) = (0,0,1,1)
        _C4 ("Karta 4", Vector) = (0,0,1,1)
        _Cells ("Liczba kart", Float) = 5
        _AtlasAspect ("Atlas: szerokość / wysokość", Float) = 2
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True" }
        LOD 200
        Cull Off
        CGPROGRAM
        // addshadow: głębia kamery (cienie ekranowe) z tym samym kształtem kęp co w obrazie
        #pragma surface surf Standard vertex:vert alphatest:_Cutoff fullforwardshadows addshadow
        #pragma multi_compile_instancing
        #pragma instancing_options assumeuniformscaling
        #pragma target 3.5
        sampler2D _MainTex, _GroundTex;
        float4 _TipTint, _GroundTint, _C0, _C1, _C2, _C3, _C4, _Dry;
        float _Tile, _MacroTile, _MacroMix, _MacroMean, _BaseAO, _Wind, _Near, _Mid, _Far, _NearL, _MidL, _FarL, _LakaH, _Cells, _AtlasAspect;
        float _DryMix, _DryMean, _DryThr, _DryGain, _Stripe, _StripeMix;
        struct Input { float2 cardUV; float3 ground; float h; };   // bez przedrostka „uv” (surface shader traktuje uvX jako UV tekstury X)

        float Hash(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }

        // kolor trawnika pod nasadą kępy: stały dla całej kępy, więc liczony w wierzchołkach (tex2Dlod), nie w pikselach —
        // 5 próbek na wierzchołek zamiast 5 na piksel. Poziom mip 4 (2048/16 = 128 tekseli na kafel 2 m = 1.6 cm na teksel) = uśredniony kolor
        // miejsca, bez migotania pojedynczych tekseli między sąsiednimi kępami.
        float3 GroundAt(float2 w)
        {
            const float L = 4;
            float2 rr = float2(w.x * .825 - w.y * .565, w.x * .565 + w.y * .825);
            float3 c = tex2Dlod(_GroundTex, float4(rr / _Tile, 0, L)).rgb;
            float3 c2 = tex2Dlod(_GroundTex, float4(w / (_Tile * 2.7) + float2(.31, .77), 0, L)).rgb;
            c = lerp(c, (c + c2) * .5, .35);
            float lum = dot(tex2Dlod(_GroundTex, float4(w / _MacroTile + float2(.37, .11), 0, L)).rgb, float3(.3, .59, .11));
            c *= 1 + (min(lum / _MacroMean, 2.5) - 1) * _MacroMix;
            // te same suche plamy i pasy koszenia co OsiedleTrawa (nasada kępy = kolor trawnika pod nią)
            float dry = saturate((tex2Dlod(_GroundTex, float4(w / (_MacroTile * 2.3) + float2(.71, .29), 0, L)).g / _DryMean - _DryThr) * _DryGain);
            c = lerp(c, c * _Dry.rgb, dry * _DryMix);
            c *= 1 + (step(.5, frac(w.x / (_Stripe * 2))) - .5) * 2 * _StripeMix;
            return c * _GroundTint.rgb;
        }

        void vert (inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            float3 root = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
            float pick = Hash(root.xz), rank = Hash(root.zx + 17.31);
            float c = floor(pick * _Cells * .9999);
            float4 r = c < .5 ? _C0 : c < 1.5 ? _C1 : c < 2.5 ? _C2 : c < 3.5 ? _C3 : _C4;
            // szerokość karty = wysokość × proporcje komórki atlasu (siatka ma szerokość 1 i wysokość 1)
            float aspect = r.z / max(r.w, 1e-4) * _AtlasAspect;
            v.vertex.xz *= aspect;
            // wysokość kępy w metrach = skala instancji (siatka ma wysokość 1); łąka > _LakaH, murawa poniżej
            float sc = max(length(unity_ObjectToWorld._m00_m10_m20), 1e-3);
            bool laka = sc > _LakaH;
            float n0 = laka ? _NearL : _Near, n1 = laka ? _MidL : _Mid, n2 = laka ? _FarL : _Far;
            // gęstość z odległością (od kamery w poziomie): 1 → 0.5 → 0; ranga kępy decyduje, czy jeszcze rośnie
            float d = distance(root.xz, _WorldSpaceCameraPos.xz);
            float dens = d < n0 ? 1 : d < n1 ? lerp(1, .5, (d - n0) / (n1 - n0)) : lerp(.5, 0, saturate((d - n1) / (n2 - n1)));
            float grow = saturate((dens - rank) / .08);
            float t = v.texcoord.y;
            v.vertex.y *= grow;
            // wiatr tylko na łące (zlecenie: murawa bez wiatru), tylko czubki; _Wind w metrach świata, więc w układzie obiektu / skala
            float sway = laka ? (sin(_Time.y * 1.7 + root.x * .6 + root.z * .4) + .5 * sin(_Time.y * 3.1 + root.z * 1.1)) * _Wind / sc * t * t : 0;
            v.vertex.x += sway; v.vertex.z += sway * .6;
            v.normal = normalize(mul(float3(0, 1, 0), (float3x3)unity_ObjectToWorld));   // normalna świata = w górę (światło jak trawnik); M = s·R, więc Mᵀ·up ∝ R⁻¹·up — bez unity_WorldToObject (instancje z assumeuniformscaling)
            o.cardUV = r.xy + v.texcoord.xy * r.zw;
            #if !defined(UNITY_PASS_SHADOWCASTER)
            o.ground = GroundAt(root.xz);   // przebieg głębi/cieni potrzebuje tylko alfy karty
            #endif
            o.h = t;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.cardUV);
            float3 ground = IN.ground;
            float3 card = c.rgb * _TipTint.rgb;
            float k = smoothstep(0, .55, IN.h);
            o.Albedo = lerp(ground, card, k) * lerp(_BaseAO, 1, smoothstep(0, .35, IN.h));
            o.Metallic = 0; o.Smoothness = .08; o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Legacy Shaders/Transparent/Cutout/VertexLit"
}
