// BRAXHORN MIASTO MISTRZÓW — morze przy plaży (właściciel 23.11 19:12, zdjęcia plaży z GTA: "chcę taki piasek i fale na
// morzu i woda jak prawdziwa"; 19:27 "fale niech się wylewają na plażę jak normalnie").
// Wszystko z pola brzegu (BraxChampionsMorzeBrzeg, tekstura RGHalf): R = odległość od linii wody (+ nad morzem, - nad
// piaskiem), G = wysokość dna (piasek tam, gdzie jest; dalej spadek plaży, coraz głębiej).
// - fale idą do brzegu wzdłuż linii wody (co 20 j., co 7 s), łamią się w pianę w strefie przyboju (do ~50 j. od brzegu),
//   każda inaczej i w kawałkach; za grzbietem piana rozpada się w koronkę; przy brzegu przechodzi w spieniony wał;
// - fala wbiega na piasek: woda przy brzegu podnosi się do 0,27 j. i opada (wierzchołki morza i płycizny nad mokrym
//   piaskiem), linia wody sama przesuwa się po plaży o ok. 10 j.; na czele biała, poszarpana warga piany;
// - woda jest przezroczysta: przez płyciznę widać piasek dna (z załamaniem i światłem tańczącym po dnie), z głębokością
//   czerwień ginie - turkus, potem błękit, daleko granat; płytka woda nad piaskiem ma kolor piasku, więc linia wody nie
//   migocze (zasada 16.11);
// - powierzchnia: niebo z sondy odbicia (Fresnel), słońce na zmarszczkach (zmarszczki drobniejsze od piksela gasną).
// Jeden przebieg, nieprzezroczyste (bez sortowania, bez przezroczystości), mgła jak reszta świata.
// To samo morze wokół wyspy z willą (23.11, BraxChampionsWyspaMorze): jej własne pole brzegu, fale ze wszystkich stron.
// Kołysanie otwartej wody gaśnie przy brzegu prostokąta pola: tam morze tego pola styka się z płaskim dalekim morzem
// (BraxChampionsHorizon) krawędź w krawędź, bez schodka.
// 24.11 morze przy prywatnej plaży willi nad morzem (BraxChampionsNabrzezeMorze): jego pole kończy się po obu bokach na
// brzegu dalekiego lądu, więc przybój i wbieganie gasną w pasie _Boki przy bokach x pola (0 = bez: plaża, wyspa).
Shader "Braxhorn/ChampionsMorze"
{
    Properties
    {
        _Brzeg ("Brzeg: odległość od linii wody, dno (RGHalf)", 2D) = "black" {}
        _BrzegRect ("Brzeg: x0, z0, 1/szer., 1/dł. (j.)", Vector) = (0,0,1,1)
        _Poziom ("Poziom morza (y)", Float) = -2.589
        _Szum ("Szum: koronka (R), gładki (G), płaty (B)", 2D) = "gray" {}
        _Dno ("Dno: mokry piasek", 2D) = "gray" {}
        _DnoTint ("Dno: mnożnik (liniowo)", Vector) = (3,3.3,4,1)
        _DnoKafel ("Dno: j. na kafel", Float) = 2.589
        _Plytka ("Płycizna (liniowo)", Vector) = (.22,.74,.62,1)
        _Srednia ("Średnia głębia (liniowo)", Vector) = (.04,.46,.46,1)
        _Gleboka ("Głębia (liniowo)", Vector) = (.02,.16,.27,1)
        _Pochlanianie ("Pochłanianie na j. głębi (RGB)", Vector) = (1.7,.38,.34,0)
        _Metnosc ("Mętność na j. głębi", Float) = .9
        _Odbicie ("Odbicie nieba", Range(0,1)) = .55
        _Fala ("Fala: długość, okres, wbieganie, zasięg wbiegania", Vector) = (20,7,.27,22)
        _Przyboj ("Przybój: od, do (j. od brzegu)", Vector) = (52,36,0,0)
        _WzdluzZ ("Fale różne wzdłuż z (wyspa: brzeg z każdej strony)", Float) = 0
        _Boki ("Przybój gaśnie przy bokach x pola (j.; 0 = bez)", Float) = 0
        _SkyHorizon ("Niebo przy horyzoncie bez sondy", Color) = (0.62,0.70,0.78,1)
        _SkyZenith ("Niebo w zenicie bez sondy", Color) = (0.36,0.52,0.72,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+10" "IgnoreProjector"="True" }
        LOD 150
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            ZWrite On
            Cull Back
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            sampler2D _Brzeg, _Szum, _Dno;
            float4 _BrzegRect, _DnoTint, _Plytka, _Srednia, _Gleboka, _Pochlanianie, _Fala, _Przyboj;
            float _Poziom, _DnoKafel, _Metnosc, _Odbicie, _WzdluzZ, _Boki;
            half4 _SkyHorizon, _SkyZenith;

            struct appdata { float4 vertex : POSITION; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 world : TEXCOORD0;
                float run : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            // the field: distance to the waterline and the bed; outside the field's rectangle (the far sea round the
            // region) the distance grows on by the way out, so the open sea is deep there
            float2 Pole(float2 w, float lod)
            {
                float2 uv = (w - _BrzegRect.xy) * _BrzegRect.zw;
                float2 f = tex2Dlod(_Brzeg, float4(uv, 0, lod)).rg;
                float2 out2 = max(abs(uv - .5) - .5, 0) / _BrzegRect.zw;
                f.x += max(out2.x, out2.y);
                return f;
            }
            float Wzdluz(float x) { return sin(x * .0131) * .35 + sin(x * .0047 + 1.7) * .55; }
            // each wave different along the shore: the beach runs along x; an island's shore runs every way
            float Faza(float2 w) { return Wzdluz(w.x) + _WzdluzZ * Wzdluz(w.y * 1.13 + 77); }
            // how far inside the field's rectangle (world units, negative outside): the open water's swell dies out at its
            // rim, so where this sea meets the flat far sea (or the island's water the far sea) the edge stays flat
            float Rant(float2 w)
            {
                float2 uv = (w - _BrzegRect.xy) * _BrzegRect.zw;
                float2 r = min(uv, 1 - uv) / _BrzegRect.zw;
                return smoothstep(9, 60, min(r.x, r.y));
            }
            // the surf and the run-up die out toward the field's two x sides over _Boki units (a shore field that ends on
            // land either side: the seafront villa's); 1 everywhere when _Boki is 0
            float Boki(float2 w)
            {
                float u = (w.x - _BrzegRect.x) * _BrzegRect.z;
                return _Boki > 0 ? smoothstep(0, _Boki, min(u, 1 - u) / _BrzegRect.z) : 1;
            }

            v2f vert(appdata v)
            {
                v2f o;
                float3 w = mul(unity_ObjectToWorld, v.vertex).xyz;
                float d = Pole(w.xz, 0).x;
                float p = frac(d / _Fala.x + _Time.y / _Fala.y + Faza(w.xz));
                // the run-up: the water at the shore rises fast after a wave comes in and drains slowly
                float run = smoothstep(0, .10, p) * (1 - smoothstep(.10, .80, p));
                float env = 1 - smoothstep(0, _Fala.w, d);
                env *= Boki(w.xz);
                float swell = (sin(dot(w.xz, float2(.034, .021)) - _Time.y * .65) * .06
                             + sin(dot(w.xz, float2(-.013, .041)) - _Time.y * .92) * .04) * smoothstep(15, 60, d) * Rant(w.xz);
                w.y += _Fala.z * env * run + swell;
                o.world = w;
                o.run = run * env;
                o.pos = mul(UNITY_MATRIX_VP, float4(w, 1));
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            half3 Niebo(float3 r)
            {
                half4 enc = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, r, 2.5);
                half3 probe = DecodeHDR(enc, unity_SpecCube0_HDR);
                half3 grad = lerp(_SkyHorizon.rgb, _SkyZenith.rgb, saturate(r.y));
                half ok = step(0.0001h, dot(probe, half3(.2126h, .7152h, .0722h)));
                return lerp(grad, probe, ok);
            }

            half4 frag(v2f i) : SV_Target
            {
                // full precision for world positions, time and derivatives on GLES3
                float2 w = i.world.xz;
                float3 eye = _WorldSpaceCameraPos.xyz - i.world;
                float dist = max(length(eye), .001);
                float3 V = eye / dist;
                float2 f = Pole(w, 0);
                float d = f.x;
                float depth = max(i.world.y - f.y, 0);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float3 sun = _LightColor0.rgb, amb = ShadeSH9(float4(0, 1, 0, 1));
                float ndl = saturate(L.y);
                // toward the open sea: the field's slope (the waves' fronts are across it)
                float e = 1.5;
                float2 g = float2(Pole(w + float2(e, 0), 0).x - Pole(w - float2(e, 0), 0).x, Pole(w + float2(0, e), 0).x - Pole(w - float2(0, e), 0).x);
                float2 toSea = normalize(g + 1e-5);
                float4 n38 = tex2D(_Szum, w / 38), n13 = tex2D(_Szum, w / 13 + .5);
                float wig = (n38.g - .5) * .10 + (n13.g - .5) * .05;
                float ph0 = d / _Fala.x + _Time.y / _Fala.y + Faza(w) + wig;
                float p = frac(ph0), idx = floor(ph0);
                // the surface: the breaking waves' faces in the surf zone and ripples (finer than a pixel: gone)
                float surf = smoothstep(55, 30, d) * smoothstep(-2, 6, d);
                float boki = Boki(w); surf *= boki;
                float face = smoothstep(0, .08, p) - smoothstep(.08, .55, p) * .85;
                float2 slope = -toSea * (face * surf * .18) * (.7 + .3 * n38.g);
                float3 ph = float3(dot(w, float2(.6, .8)) * 2.1, dot(w, float2(.958, -.287)) * 1.6, dot(w, float2(-.21, .978)) * 1.15)
                          - _Time.y * float3(1.73, 1.21, .87);
                float3 filt = 1 - smoothstep(1, 2.8, fwidth(ph));
                float3 sm = cos(ph) * float3(.035, .03, .026) * filt * (1 - smoothstep(40, 160, dist));
                slope += sm.x * float2(.6, .8) + sm.y * float2(.958, -.287) + sm.z * float2(-.21, .978);
                float3 N = normalize(float3(-slope.x, 1, -slope.y));
                // the bed through the water: wet sand, bent by the surface, the light dancing on it, red lost with depth
                float2 refr = w + N.xz * depth * 1.2;
                float3 bed = tex2D(_Dno, refr / _DnoKafel).rgb * _DnoTint.rgb;
                float c1 = tex2D(_Szum, refr / 3.1 + _Time.y * float2(.021, .013)).r;
                float c2 = tex2D(_Szum, refr / 3.7 - _Time.y * float2(.017, -.019)).r;
                float caus = min(c1, c2) * smoothstep(0, .05, depth) * exp(-depth * 1.1) * (1 - smoothstep(30, 90, dist));
                bed *= (sun * ndl + amb) * (1 + caus * 1.6);
                float3 sc = lerp(_Plytka.rgb, _Srednia.rgb, smoothstep(.6, 3, depth));
                sc = lerp(sc, _Gleboka.rgb, smoothstep(3, 16, depth)) * (sun * ndl + amb) * .87;
                float3 T = exp(-depth * _Pochlanianie.rgb);
                float murk = 1 - exp(-depth * _Metnosc);
                float3 water = lerp(bed * T, sc, murk);
                water *= 1 - max(face, 0) * surf * .12;
                float F = .02 + .98 * pow(1 - saturate(dot(N, V)), 5);
                float3 col = lerp(water, min(Niebo(reflect(-V, N)), 1.3), F * _Odbicie);
                col += sun * pow(saturate(dot(normalize(L + V), N)), 220) * 1.5;
                // foam: the breaking crests running to the shore, the whitewash behind them, the run-up's sheet and lip
                float4 n23 = tex2D(_Szum, w / 2.3), n51 = tex2D(_Szum, w / 5.1 + float2(.37, .11)), n11 = tex2D(_Szum, w / 11);
                float lace = n23.r, lace2 = n51.r, blob = n11.b;
                // (a fixed mip: idx jumps at each crest, a derivative there would pick the wrong mip on a one-pixel line)
                float seg = tex2Dlod(_Szum, float4(w.x / 70 + idx * .37, idx * .61 + w.y / 90, 0, 2)).g;
                float power = smoothstep(.30, .62, seg);
                float zone = smoothstep(_Przyboj.x, _Przyboj.y, d) * smoothstep(-1, 3, d);
                zone *= boki;
                float crest = smoothstep(0, .02, p) * (1 - smoothstep(.07, .24, p)) * (.75 + .25 * smoothstep(.2, .6, lace + blob * .5));
                float pat = lace * .55 + lace2 * .45 + blob * .35;
                float near = 1 - smoothstep(4, 18, d);
                float th = .22 + p * (1.1 - near * .45);
                float wash = (1 - smoothstep(.05, .7, p)) * smoothstep(th, th + .25, pat);
                float foam = zone * max(power, near * .85) * (crest * (.70 + .30 * blob) + wash * .95);
                float edge = (1 - smoothstep(0, .03, depth)) * smoothstep(-.001, .004, depth);
                float edgeLace = edge * smoothstep(.15, .6, lace + blob * .5);
                float thin = (1 - smoothstep(0, .07, depth)) * i.run;
                float lipW = .010 + .022 * n51.g;
                float front = (1 - smoothstep(0, lipW, depth)) * smoothstep(.15, .5, i.run) * smoothstep(.05, .45, lace * .7 + blob * .6);
                foam = saturate(foam + edgeLace + thin * smoothstep(.15, .6, lace * .8 + blob * .5) * .85 + front * .9) * (1 - smoothstep(120, 260, dist));
                float3 foamC = float3(.96, .97, .95) * (sun * (.6 + .4 * ndl) + amb * .8);
                col = lerp(col, foamC, foam);
                half4 result = half4(col, 1);
                UNITY_APPLY_FOG(i.fogCoord, result);
                return result;
            }
            ENDCG
        }
    }
    Fallback Off
}
