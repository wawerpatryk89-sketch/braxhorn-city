// BRAXHORN MIASTO MISTRZÓW — trawa 3D (właściciel 23.11: "żeby ta trawa nie była płaska", "długa, nie jest krótka", "to jest
// dobra trawa"). Karta z prawdziwych roślin Poly Haven Grass Bermuda 01 (CC0, trawa_bermuda.py), w kępach po trzy karty
// skrzyżowane co 60°, rysowana instancjami przy kamerze (BraxChampionsTrawa3D).
// - światło jak trawnik pod nią: normalna kępy do góry (siatka), bez ciemnych odwrotów kart;
// - kolor trawnika: te same plamy, suchsze miejsca i pasy koszenia co płaski trawnik (ChampionsGrass), liczone raz na kępę
//   z jej nasady, więc z daleka trawa 3D przechodzi w płaski trawnik bez szwu;
// - cień przy nasadzie (źdźbła zasłaniają się nawzajem), lekki wiatr na czubkach;
// - z odległością kępy maleją w ziemię, każda na swojej odległości (rzadziej im dalej) — bez przeskoków i bez migotania
//   (mipmapy karty zachowują pokrycie: BraxChampionsTrawaKartaImport).
Shader "Braxhorn/ChampionsTrawaKarta"
{
    Properties
    {
        _MainTex ("Karta (RGBA)", 2D) = "white" {}
        _Color ("Odcień (do zieleni trawnika)", Color) = (1,1,1,1)
        _Cutoff ("Próg alfa", Range(0,1)) = .5
        _Zasieg ("Zasięg (j. gry)", Float) = 15
        _Blisko ("Pełna gęstość do (j. gry)", Float) = 7
        _Pas ("Pas zaniku (j. gry)", Float) = 3
        _Nasada ("Cień przy nasadzie", Range(0,1)) = .55
        _NasadaPas ("Wysokość cienia (1 / część karty)", Float) = 1.6
        _Wiatr ("Wiatr: wychylenie czubka (j. gry)", Float) = .025
        _Gloss ("Gładkość", Range(0,1)) = .12
        _LawnTex ("Trawnik: zdjęcie (plamy)", 2D) = "gray" {}
        _Dry ("Odcień suchszy", Vector) = (1.12,1.02,.78,1)
        _MacroTile ("Metry na dużą plamę", Float) = 29
        _MacroMix ("Siła plam", Range(0,1)) = .45
        _MacroMean ("Średnia jasność zdjęcia", Float) = .1387
        _DryMix ("Siła przebarwień", Range(0,1)) = .35
        _DryMean ("Średnia zieleń zdjęcia", Float) = .1631
        _DryThr ("Próg suchszych plam", Float) = 1.45
        _DryGain ("Narastanie suchszych plam", Float) = 2.5
        _Stripe ("Szerokość pasa koszenia", Float) = 1.8
        _StripeMix ("Siła pasów", Range(0,.5)) = .045
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True" }
        LOD 200
        Cull Off
        CGPROGRAM
        // one pass a clump: the sun (with its shadows) and the sky; the street lamps light it per vertex (noforwardadd) -
        // a cut-out drawn again for every lamp in reach would double the grass's cost at night
        #pragma surface surf Standard vertex:vert alphatest:_Cutoff noforwardadd nolightmap nodynlightmap nodirlightmap nometa exclude_path:deferred exclude_path:prepass
        #pragma multi_compile_instancing
        #pragma target 3.0
        sampler2D _MainTex, _LawnTex;
        fixed4 _Color; float4 _Dry;
        float _Zasieg, _Blisko, _Pas, _Nasada, _NasadaPas, _Wiatr, _Gloss;
        float _MacroTile, _MacroMix, _MacroMean, _DryMix, _DryMean, _DryThr, _DryGain, _Stripe, _StripeMix;
        struct Input { float2 uv_MainTex; float3 trawnik; };

        // stały los kępy z miejsca jej nasady (bez sinusa: ten sam wynik na każdym telefonie)
        float Los(float2 p)
        {
            float3 p3 = frac(float3(p.xyx) * .1031);
            p3 += dot(p3, p3.yzx + 33.33);
            return frac((p3.x + p3.y) * p3.z);
        }

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            float3 root = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
            float los = Los(root.xz);
            // zanik: kępa maleje w ziemię między swoim końcem a pasem przed nim; koniec od _Blisko+_Pas (rzadko) do _Zasieg
            float d = distance(root, _WorldSpaceCameraPos.xyz);
            float koniec = lerp(_Zasieg, _Blisko + _Pas, los);
            float f = saturate((koniec - d) / _Pas);
            // wiatr: czubki (wysokość^2), każda kępa w swojej fazie
            float t = v.texcoord.y * v.texcoord.y;
            float faza = _Time.y * 1.6 + los * 6.2832 + root.x * .21 + root.z * .17;
            v.vertex.xz += float2(sin(faza), sin(faza * .83 + 1.3)) * _Wiatr * t;
            v.vertex.xyz *= f;
            // ton trawnika pod kępą: plamy, suchsze miejsca, pasy koszenia (jak ChampionsGrass, z mipmapy uśredniającej)
            float2 w = root.xz;
            float lum = dot(tex2Dlod(_LawnTex, float4(w / _MacroTile + float2(.37, .11), 0, 5)).rgb, float3(.3, .59, .11));
            float k = 1 + (min(lum / _MacroMean, 2.5) - 1) * _MacroMix;
            float dry = saturate((tex2Dlod(_LawnTex, float4(w / (_MacroTile * 2.3) + float2(.71, .29), 0, 5)).g / _DryMean - _DryThr) * _DryGain);
            float3 c = k * lerp(float3(1, 1, 1), _Dry.rgb, dry * _DryMix);
            float stripe = step(.5, frac(w.x / (_Stripe * 2)));
            o.trawnik = c * (1 + (stripe - .5) * 2 * _StripeMix);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 k = tex2D(_MainTex, IN.uv_MainTex);
            float nasada = lerp(_Nasada, 1, saturate(IN.uv_MainTex.y * _NasadaPas));
            o.Albedo = k.rgb * _Color.rgb * IN.trawnik * nasada;
            o.Alpha = k.a;
            o.Metallic = 0;
            o.Smoothness = _Gloss;
            o.Occlusion = 1;
        }
        ENDCG
    }
    FallBack Off
}
