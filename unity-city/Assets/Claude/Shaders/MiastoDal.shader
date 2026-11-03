// BRAXHORN CITY — teren za granicą miasta (wzgórza, dolina, pola, las, skarpy, brzeg) i dalekie obiekty (wiadukt
// za granicą). Wykonawca „horyzont”, 2.11. Tryb 0 = teren: trawa (fotoskan Grass004) z barwą makro (ta sama tekstura
// co ziemia w tle nieba — z odległością teren ma dokładnie barwę tła), skała na stromych miejscach i przy brzegu
// (kolor wierzchołka R, rzut trójplanarny), ziemia ze ściółką pod lasem (G), suchsze pole (B). Tryb 1 = obiekt
// (beton wiaduktu, mur): tekstura trójplanarna. Światło jak w CityStaticPBR (to samo słońce i niebo miasta, bez świateł
// Unity, bez cieni w czasie rzeczywistym). Z odległością: mgiełka powietrza (to samo prawo co w tle), a przed końcem
// zasięgu kamery trybu grafiki teren przechodzi DOKŁADNIE w tło w tym kierunku (MiastoTlo) — brzeg zasięgu niewidoczny.
Shader "Braxhorn/MiastoDal"
{
    Properties
    {
        _Tryb ("Tryb: 0 teren, 1 obiekt", Float) = 0
        _MainTex ("Trawa / tekstura obiektu", 2D) = "white" {}
        _Color ("Odcień", Color) = (.9, 1, .88, 1)
        _Tile ("Kafel (m)", Float) = 3.2
        _RockTex ("Skała", 2D) = "grey" {}
        _RockColor ("Odcień skały", Color) = (.8, .78, .74, 1)
        _RockTile ("Kafel skały (m)", Float) = 4.5
        _SoilTex ("Ziemia ze ściółką", 2D) = "grey" {}
        _SoilColor ("Odcień ziemi", Color) = (.72, .68, .6, 1)
        _SoilTile ("Kafel ziemi (m)", Float) = 2.5
        _FieldColor ("Odcień pola", Color) = (1.08, 1.0, .72, 1)
        _MakroUdzial ("Udział barwy makro z bliska", Range(0, 1)) = .55
        _MglaOd ("Przejście w tło od (ułamek końca)", Float) = .6
        _MglaDo ("Przejście w tło najdalej (m)", Float) = 470
        _NieboTex ("Niebo", 2D) = "grey" {}
        _NieboObrot ("Obrót nieba", Float) = 0
        _NieboEkspozycja ("Ekspozycja", Float) = 1
        _NieboNasycenie ("Nasycenie", Float) = 1.3
        _NieboChmury ("Ruch chmur", Float) = .0035
        _SloKierunek ("Kierunek do słońca", Vector) = (-.45, .78, -.42, 0)
        _Pasmo ("Góry", 2D) = "black" {}
        _PasmoJest ("Góry włączone", Float) = 0
        _PasmoSrodek ("Środek walców", Vector) = (17, 0, 49, 0)
        _PasmoPromien ("Promień gór", Float) = 2600
        _PasmoU0 ("u0", Float) = -.23
        _PasmoGora ("Góra", Float) = 35
        _PasmoDol ("Dół", Float) = -10
        _PasmoJasnosc ("Jasność gór", Float) = 1
        _PasmoNasycenie ("Nasycenie gór", Float) = 1.15
        _PasmoMgla ("Mgiełka gór", Float) = .18
        _Grzbiety ("Grzbiety", 2D) = "black" {}
        _GrzbietyR ("R1 R2 H1 H2", Vector) = (850, 1450, 160, 320)
        _GrzbietKolor1 ("Grzbiet 1", Color) = (.11, .17, .08, 1)
        _GrzbietKolor2 ("Grzbiet 2", Color) = (.10, .15, .09, 1)
        _ZiemiaMakro ("Ziemia makro", 2D) = "grey" {}
        _ZiemiaMakroSkala ("Makro: m na kafel", Float) = 1600
        _ZiemiaTrawa ("Trawa tła", 2D) = "grey" {}
        _ZiemiaTrawaSkala ("Trawa tła: m", Float) = 3.2
        _ZiemiaOdcien ("Odcień trawy tła", Color) = (.9, 1, .85, 1)
        _Brzeg1 ("Brzeg 1", Vector) = (84, 150, 100, 118)
        _Brzeg2 ("Brzeg 2", Vector) = (460, -340, .9, -1.7)
        _MorzeKolor ("Morze", Color) = (.035, .085, .11, 1)
        _MgielkaKolor ("Mgiełka", Color) = (.62, .68, .76, 1)
        _MgielkaGestosc ("Mgiełka gęstość", Float) = .00042
        _MgielkaNieboUdzial ("Mgiełka: niebo", Float) = .6
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "MiastoTlo.cginc"
            sampler2D _MainTex, _RockTex, _SoilTex;
            half4 _Color, _RockColor, _SoilColor, _FieldColor;
            float _Tile, _RockTile, _SoilTile, _Tryb, _MglaOd, _MglaDo;
            half _MakroUdzial;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float3 wp : TEXCOORD0; float3 n : TEXCOORD1; half4 col : COLOR; };
            v2f vert (appdata v)
            {
                v2f o;
                o.wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.n = UnityObjectToWorldNormal(v.normal);
                o.pos = mul(UNITY_MATRIX_VP, float4(o.wp, 1));
                o.col = v.color;
                return o;
            }
            half3 Lin(half3 c) { return GammaToLinearSpace(c); }
            // 3.11 (właściciel: „zajebisty widok z dachu wieżowca”): szachownica pól z daleka — działki ~160 × 90 m z różnymi
            // uprawami, miedze z krzakami, zagajniki śródpolne; gaśnie z bliska (tam trawa) i przy rozmiarze piksela (bez migotania)
            float md_hash(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float md_noise(float2 p)
            {
                float2 i = floor(p), f = frac(p); f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(md_hash(i), md_hash(i + float2(1, 0)), f.x), lerp(md_hash(i + float2(0, 1)), md_hash(i + float2(1, 1)), f.x), f.y);
            }
            half3 md_pola(half3 g, float2 p, float px, float d)
            {
                float a = .42; float2 q = float2(p.x * cos(a) - p.y * sin(a), p.x * sin(a) + p.y * cos(a));
                float2 dz = q / float2(160.0, 90.0); float2 id = floor(dz + float2(md_noise(q / 400.0) * .6, 0)); float2 fr = frac(dz);
                float u = md_hash(id);
                half3 t = u < .25 ? half3(.92, 1.05, .82) : u < .45 ? half3(1.18, 1.08, .70) : u < .6 ? half3(1.25, 1.0, .72)
                        : u < .75 ? half3(.82, .98, .78) : u < .87 ? half3(1.05, .86, .66) : half3(1.0, 1.1, .9);
                float zas = smoothstep(120.0, 420.0, d) * saturate(1.6 - px / 45.0);
                half3 c = g * lerp(half3(1, 1, 1), t, zas * .85);
                float sred = saturate(1.6 - px / 14.0);
                float miedza = (1.0 - smoothstep(.0, .025, min(min(fr.x, 1 - fr.x) * 1.8, min(fr.y, 1 - fr.y)))) * sred * zas;
                c = lerp(c, c * half3(.62, .78, .58), miedza * .8);
                float zag = step(.88, md_hash(id + 7.7)) * smoothstep(.45, .75, md_noise(q / 9.0)) * zas;
                c = lerp(c, c * half3(.45, .62, .42), zag);
                return c;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.n);
                float3 wp = i.wp;
                float3 gx = ddx(wp), gy = ddy(wp);
                float3 v = wp - _WorldSpaceCameraPos;
                float d = max(length(v), 1e-3);
                float3 dir = v / d;
                float3 an = abs(n); an /= (an.x + an.y + an.z);
                half3 alb;
                if (_Tryb < 0.5)
                {
                    // trawa (szczegół) z barwą makro; z odległością sama barwa makro (= ziemia w tle nieba)
                    half3 tr = tex2Dgrad(_MainTex, wp.xz / _Tile, gx.xz / _Tile, gy.xz / _Tile).rgb * _Color.rgb;
                    half4 m = tex2Dgrad(_ZiemiaMakro, wp.xz / _ZiemiaMakroSkala, gx.xz / _ZiemiaMakroSkala, gy.xz / _ZiemiaMakroSkala);
                    half lum = max(dot(tr, half3(.3, .59, .11)), 1e-3);
                    half lumM = max(dot(m.rgb, half3(.3, .59, .11)), 1e-3);
                    // z bliska: jasność i szczegół trawy, barwa przesunięta w stronę barwy makro (plamy łąki / pola)
                    half3 blisko = lerp(tr, lum * (m.rgb / lumM), _MakroUdzial * 0.5);
                    blisko *= lerp(0.85, 1.15, saturate(lumM * 4.0));
                    half3 g = lerp(blisko, m.rgb, smoothstep(70.0, 260.0, d));
                    g = lerp(g, g * _FieldColor.rgb, i.col.b);
                    g = md_pola(g, wp.xz, max(length(gx), length(gy)), d);
                    half3 c = g;
                    if (i.col.g > 0.01)
                    {
                        half3 so = tex2Dgrad(_SoilTex, wp.xz / _SoilTile, gx.xz / _SoilTile, gy.xz / _SoilTile).rgb * _SoilColor.rgb;
                        c = lerp(c, so, i.col.g);
                    }
                    if (i.col.r > 0.01)
                    {
                        half3 r = (tex2Dgrad(_RockTex, wp.zy / _RockTile, gx.zy / _RockTile, gy.zy / _RockTile).rgb * an.x
                                 + tex2Dgrad(_RockTex, wp.xz / _RockTile, gx.xz / _RockTile, gy.xz / _RockTile).rgb * an.y
                                 + tex2Dgrad(_RockTex, wp.xy / _RockTile, gx.xy / _RockTile, gy.xy / _RockTile).rgb * an.z) * _RockColor.rgb;
                        c = lerp(c, r, i.col.r);
                    }
                    alb = c;
                }
                else
                {
                    alb = (tex2Dgrad(_MainTex, wp.zy / _Tile, gx.zy / _Tile, gy.zy / _Tile).rgb * an.x
                         + tex2Dgrad(_MainTex, wp.xz / _Tile, gx.xz / _Tile, gy.xz / _Tile).rgb * an.y
                         + tex2Dgrad(_MainTex, wp.xy / _Tile, gx.xy / _Tile, gy.xy / _Tile).rgb * an.z) * _Color.rgb * i.col.rgb;
                }
                // światło jak CityStaticPBR (słońce miasta 2.4, niebo/ziemia 0.65)
                float3 sun = normalize(_SloKierunek.xyz);
                half nl = saturate(dot(n, sun));
                half3 amb = lerp(Lin(half3(.42, .40, .36)), Lin(half3(.60, .68, .78)), n.y * .5 + .5) * .65;
                half3 c = alb * (Lin(half3(1, .98, .94)) * (2.4 / 3.14159) * nl + amb);
                // mgiełka powietrza (to samo prawo co w tle) i przejście w tło przed końcem zasięgu kamery
                c = lerp(c, MT_Mgielka(dir), MT_MgielkaIle(d));
                float koniec = min(_ProjectionParams.z * .95, _MglaDo), poczatek = koniec * _MglaOd;
                half k = smoothstep(poczatek, koniec, d);
                if (k > 0.001) c = lerp(c, MiastoTlo(_WorldSpaceCameraPos, dir, MT_KatPiksela()), k);
                return half4(c, 1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
