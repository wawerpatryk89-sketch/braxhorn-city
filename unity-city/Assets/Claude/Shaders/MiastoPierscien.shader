// BRAXHORN CITY — PIERŚCIEŃ HORYZONTU 3D (panorama, pkt 7 / 7b / 15d): archipelag wysp na morzu, wzgórza i góry za miastem
// jako prawdziwe bryły 3D (cieniowane słońcem sceny), ustawiane przy kamerze w skali (BraxPierscien) — kąt widzenia jak z
// prawdziwych 2–9 km, bez paralaksy (nie da się dopłynąć). Głębia zapisana tuż przed płaszczyzną daleką: wszystko ze sceny
// rysuje się PRZED pierścieniem, niebo nie zamaluje pierścienia; kolejność wewnątrz pierścienia zachowana (odległość w skali).
// Kolor bazowy z wierzchołków (rgb), mgiełka odległości w alfie wierzchołka (zapieczona przy buildzie). Nic nie świeci.
// 3.11 (właściciel 12:26: „ma być zajebisty widok z dachu wieżowca… piąty raz mówię”): zamiast płaskich plam koloru —
// SZCZEGÓŁ TERENU liczony w prawdziwych metrach (współrzędne siatki): szachownica pól uprawnych z różnymi uprawami i miedzami,
// rzędy upraw, kępy koron drzew w lasach (ciemne i jasne korony, prześwity), skała ze żłobieniami, śnieg na szczytach gór,
// piasek plaż; oświetlenie z mikrorzeźbą (gradient szumu = wypukłości koron i pól) i rozproszeniem nieba; mgiełka jak dotąd.
Shader "Braxhorn/MiastoPierscien"
{
    Properties
    {
        _Mgla ("Kolor mgiełki horyzontu", Color) = (.70, .76, .83, 1)
        _Slonce ("Kierunek do słońca", Vector) = (-.45, .78, -.42, 0)
        _Jasnosc ("Jasność", Float) = 1
        _Szczegol ("Siła szczegółu terenu", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Geometry+450" "RenderType"="Opaque" "IgnoreProjector"="True" }
        Cull Back ZWrite On ZTest LEqual
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            half4 _Mgla; float4 _Slonce; half _Jasnosc; half _Szczegol;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; half3 n : TEXCOORD0; float3 op : TEXCOORD1; };

            float hash21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p); f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i), b = hash21(i + float2(1, 0)), c = hash21(i + float2(0, 1)), d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            float fbm(float2 p) { float s = 0, a = .5; for (int k = 0; k < 4; k++) { s += a * vnoise(p); p = p * 2.03 + 17.1; a *= .5; } return s; }

            v2f vert (appdata v)
            {
                v2f o;
                float3 w = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = UnityObjectToClipPos(v.vertex);
                // głębia: tuż przed płaszczyzną daleką, porządek wg odległości w pierścieniu (0…900 m)
                float dd = saturate(distance(w, _WorldSpaceCameraPos.xyz) / 900.0);
                #if defined(UNITY_REVERSED_Z)
                o.pos.z = o.pos.w * (1e-5 + 9e-5 * (1.0 - dd));
                #else
                o.pos.z = o.pos.w * (1.0 - 1e-5 - 9e-5 * (1.0 - dd));
                #endif
                o.color = v.color;
                o.n = UnityObjectToWorldNormal(v.normal);
                o.op = v.vertex.xyz;   // prawdziwe metry siatki (element skalowany dopiero w pierścieniu)
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                half3 baza = i.color.rgb; float2 p = i.op.xz; float h = i.op.y;
                // rozmiar piksela w metrach terenu — drobny szczegół gaśnie, zanim zacząłby migotać (bez aliasingu w ruchu)
                float3 dpx = ddx(i.op), dpy = ddy(i.op); float px = max(length(dpx), length(dpy));
                float drob = saturate(1.6 - px / 4.0), sred = saturate(1.6 - px / 14.0), duzy = saturate(1.6 - px / 45.0);
                half3 nrm = normalize(i.n);
                float lum = dot(baza, half3(.3, .59, .11));
                bool ziel = baza.g > baza.r * 1.04 && baza.g > baza.b * 1.08;
                bool piasek = baza.r > .6 && baza.r > baza.g && baza.g > baza.b;
                bool skala = !ziel && !piasek && abs(baza.r - baza.g) < .06 && abs(baza.g - baza.b) < .07;
                half3 c = baza; float wys = 0;   // wysokość mikrorzeźby (do cieniowania)
                if (ziel && lum < .24)
                {   // LAS: kępy koron (12 m), większe płaty (70 m), prześwity
                    float kor = fbm(p / 11.0), plat = fbm(p / 70.0 + 3.1);
                    float kepa = smoothstep(.35, .75, kor);
                    kepa = lerp(.5, kepa, drob);
                    c = baza * lerp(.62, 1.28, kepa) * lerp(.85, 1.12, lerp(.5, plat, sred));
                    c = lerp(c, half3(.10, .16, .08), smoothstep(.62, .78, 1.0 - kor) * .35 * drob);   // cienie między koronami
                    wys = kepa * drob + plat * .4 * sred;
                }
                else if (ziel)
                {   // POLA I ŁĄKI: szachownica działek (obrócona siatka ~160 × 90 m), różne uprawy, miedze, rzędy
                    float a = .42; float2 q = float2(p.x * cos(a) - p.y * sin(a), p.x * sin(a) + p.y * cos(a));
                    float2 dz = q / float2(160.0, 90.0); float2 id = floor(dz + float2(fbm(q / 400.0) * .6, 0)); float2 fr = frac(dz);
                    float u = hash21(id);
                    half3 uprawa = u < .25 ? half3(.36, .48, .20) : u < .45 ? half3(.55, .55, .26) : u < .6 ? half3(.62, .52, .30)
                                 : u < .75 ? half3(.30, .42, .18) : u < .87 ? half3(.47, .38, .24) : half3(.42, .54, .26);
                    c = lerp(baza, uprawa, .78 * duzy);
                    float rzedy = lerp(.5, sin((u > .5 ? q.x : q.y) * 2.1) * .5 + .5, saturate(1.6 - px / 1.2));   // rzędy upraw ~3 m
                    c *= lerp(.92, 1.05, rzedy) * lerp(.9, 1.08, lerp(.5, fbm(q / 25.0), sred));
                    float miedza = (1.0 - smoothstep(.0, .025, min(min(fr.x, 1 - fr.x) * 1.8, min(fr.y, 1 - fr.y)))) * sred;
                    c = lerp(c, half3(.24, .34, .15), miedza * .8);                            // miedze z krzakami
                    float zag = step(.88, hash21(id + 7.7)) * smoothstep(.45, .75, fbm(q / 9.0)) * duzy;   // zagajniki śródpolne
                    c = lerp(c, half3(.14, .22, .10), zag);
                    wys = zag * .8 + miedza * .3;
                }
                else if (piasek) { c = baza * lerp(.9, 1.07, lerp(.5, fbm(p / 6.0), drob)); wys = fbm(p / 15.0) * .2 * sred; }
                else if (skala)
                {   // SKAŁA: żłobienia i warstwy, porosty
                    float z = lerp(.5, fbm(float2(p.x / 18.0, h / 4.0)), sred); c = baza * lerp(.7, 1.2, z);
                    c = lerp(c, half3(.32, .36, .26), smoothstep(.6, .8, fbm(p / 30.0)) * .3);
                    wys = z * 1.2;
                }
                // ŚNIEG na szczytach gór (wyżej niż ~620 m, mniej na stromych zboczach)
                float snieg = smoothstep(560.0, 720.0, h + fbm(p / 120.0) * 120.0) * smoothstep(.45, .75, nrm.y);
                c = lerp(c, half3(.93, .94, .96), snieg);
                // oświetlenie: słońce + niebo + mikrorzeźba (gradient wysokości szczegółu w ekranie)
                float dwx = ddx(wys), dwy = ddy(wys);
                float3 gr = (dwx * dpx / max(dot(dpx, dpx), 1e-4) + dwy * dpy / max(dot(dpy, dpy), 1e-4)) * 6.0 * _Szczegol;
                half3 n2 = normalize(nrm - half3(gr.x, 0, gr.z));
                half ndl = saturate(dot(n2, normalize(_Slonce.xyz)));
                half sky = .5 + .5 * n2.y;
                c = lerp(baza, c, _Szczegol);
                half3 col = c * (0.40 * sky + 0.70 * ndl) * _Jasnosc;
                col = lerp(col, _Mgla.rgb, i.color.a);
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
