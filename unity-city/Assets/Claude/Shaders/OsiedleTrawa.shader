// BRAXHORN R2 — trawa osiedla: fotoskan Grass004 rzutowany w metrach świata (XZ),
// dwie skale (detal + duże plamy koloru), łagodne przebarwienia (miejscami suchsza)
// i pasy od koszenia jak na zadbanym trawniku. Nic nie świeci.
Shader "Braxhorn/OsiedleTrawa"
{
    Properties
    {
        _MainTex ("Kolor", 2D) = "white" {}
        _BumpMap ("Normalna (GL)", 2D) = "bump" {}
        _MetallicGlossMap ("Metal (R) / Gładkość (A)", 2D) = "black" {}
        _OcclusionMap ("AO", 2D) = "white" {}
        _Color ("Odcień", Color) = (1,1,1,1)
        _Dry ("Odcień suchszy (mnożnik liniowy, bez konwersji sRGB)", Vector) = (1.12,1.02,.78,1)
        _Tile ("Metry na kafel", Float) = 2.5
        _MacroTile ("Metry na dużą plamę", Float) = 29
        _MacroMix ("Siła plam", Range(0,1)) = .45
        _DryMix ("Siła przebarwień", Range(0,1)) = .35
        _Stripe ("Szerokość pasa koszenia (m)", Float) = 2.4
        _StripeMix ("Siła pasów", Range(0,.5)) = .09
        _BumpScale ("Siła wypukłości", Range(0,2)) = 1.1
        _Gloss ("Gładkość (mnożnik)", Range(0,1)) = .14
        _MacroMean ("Średnia jasność tekstury (liniowa, .3/.59/.11)", Float) = .1387
        _DryMean ("Średnia zieleń tekstury (liniowa)", Float) = .1631
        _DryThr ("Próg suchszych plam (× średnia)", Float) = 1.45
        _DryGain ("Narastanie suchszych plam", Float) = 2.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex, _BumpMap, _MetallicGlossMap, _OcclusionMap;
        fixed4 _Color; float4 _Dry;
        float _Tile, _MacroTile, _MacroMix, _DryMix, _Stripe, _StripeMix, _BumpScale, _Gloss, _MacroMean, _DryMean, _DryThr, _DryGain;
        struct Input { float3 worldPos; };
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 w = IN.worldPos.xz;
            // detal obrócony o 0.6 rad, żeby kafel nie układał się w linie z ulicami
            float2 r = float2(w.x * .825 - w.y * .565, w.x * .565 + w.y * .825);
            float2 uv = r / _Tile;
            fixed4 c = tex2D(_MainTex, uv);
            fixed3 c2 = tex2D(_MainTex, w / (_Tile * 2.7) + float2(.31, .77)).rgb;
            c.rgb = lerp(c.rgb, (c.rgb + c2) * .5, .35);
            fixed3 macro = tex2D(_MainTex, w / _MacroTile + float2(.37, .11)).rgb;
            float lum = dot(macro, float3(.3, .59, .11));
            // plamy względem średniej tekstury (średnio ×1, bez przyciemniania całego trawnika)
            c.rgb *= 1 + (min(lum / _MacroMean, 2.5) - 1) * _MacroMix;
            // suchsze plamy tam, gdzie duża skala jest jaśniejsza od średniej (~20% powierzchni)
            float dry = saturate((tex2D(_MainTex, w / (_MacroTile * 2.3) + float2(.71, .29)).g / _DryMean - _DryThr) * _DryGain);
            c.rgb = lerp(c.rgb, c.rgb * _Dry.rgb, dry * _DryMix);
            float stripe = step(.5, frac(w.x / (_Stripe * 2)));
            c.rgb *= 1 + (stripe - .5) * 2 * _StripeMix;
            o.Albedo = c.rgb * _Color.rgb;
            o.Normal = UnpackScaleNormal(tex2D(_BumpMap, uv), _BumpScale);
            o.Metallic = 0;
            // trawa jest matowa: mała gładkość, inaczej pod niebo (Fresnel) daleki trawnik bieleje
            o.Smoothness = tex2D(_MetallicGlossMap, uv).a * _Gloss;
            o.Occlusion = tex2D(_OcclusionMap, uv).r;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
