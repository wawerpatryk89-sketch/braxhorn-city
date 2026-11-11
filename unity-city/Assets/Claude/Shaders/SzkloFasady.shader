// BRAXHORN CITY — SZKŁO FASAD WIEŻOWCÓW (9.11, właściciel z tarasu Tower: „taka chujnia”, „czarne bryły budynków”, „każdy
// budynek w nowej części miasta ma być premium”). Dotąd szkło nowych dzielnic i wieżowców CENTRUM było ciemnym metalem
// z szerokim odbiciem stałego koloru (z daleka czarne bloki), a okna „pokój za szybą” zależały od sond odbić Unity.
// Teraz jedna szyba elewacyjna, liczona w całości tutaj (bez sond, bez świateł Unity — działa tak samo na każdym telefonie):
//  • ODBICIE PRAWDZIWEGO NIEBA I HORYZONTU miasta (to samo tło co Braxhorn/MiastoNiebo: chmury, słońce, mgiełka) z Fresnelem
//    szkła z powłoką (F0 ≈ 0.08) — patrząc wzdłuż elewacji lustro nieba, na wprost widać wnętrze;
//  • WNĘTRZE za szybą: pokoje biurowe w siatce kondygnacji (interior mapping: tylna ściana, sufit z pasem światła, podłoga),
//    każdy pokój inny (jasność, żaluzje), część z zapalonym światłem — budynek „żyje”, nie jest płaską taflą;
//  • SIATKA ELEWACJI: słupki aluminiowe co moduł i pas międzykondygnacyjny (szkło emaliowane), wygładzane z odległością
//    (fwidth) — bez migotania i mory na telefonie;
//  • odblask słońca na szybie (kierunek słońca miasta), perspektywa powietrzna jak w CityStaticPBR.
// Cienie rzuca przebiegiem ShadowCaster z CityStaticPBR. Parametry tła (niebo, mgiełka) kopiuje krok horyzontu (UstawTlo).
Shader "Braxhorn/SzkloFasady"
{
    Properties
    {
        _Tint ("Barwa szkła (w głąb)", Color) = (.20, .30, .36, 1)
        _F0 ("Odbicie na wprost (F0)", Range(0, .3)) = .08
        _Odbicie ("Siła odbicia", Range(0, 1.5)) = 1
        _CellW ("Moduł elewacji (m)", Float) = 3.0
        _CellH ("Kondygnacja (m)", Float) = 3.8
        _Floor0 ("Poziom pierwszej kondygnacji (m)", Float) = .3
        _Depth ("Głębokość pokoju (m)", Float) = 5
        _Rama ("Szerokość słupka (m)", Float) = .08
        _RamaKolor ("Kolor słupków (aluminium)", Color) = (.62, .65, .68, 1)
        _Pas ("Pas międzykondygnacyjny (m)", Float) = .7
        _PasKolor ("Kolor pasa (szkło emaliowane)", Color) = (.17, .21, .25, 1)
        // ARCHITEKTURA ELEWACJI (10.11, właściciel po podglądzie: „jak to tak ma wyglądać, to na pewno nie” — sama tafla z siatką
        // wyglądała jak komputerowe pudło): typ 0 = ściana kurtynowa (pas międzykondygnacyjny ze szkła emaliowanego),
        // 1 = pasy okien w okładzinie (pas pełny: aluminium / kamień, cień pod pasem), 2 = filary i okna w kamieniu
        _Typ ("Typ elewacji (0 kurtynowa, 1 pasy, 2 filary)", Float) = 0
        _Filar ("Filar: szerokość (m)", Float) = 0
        _FilarCo ("Filar co ile modułów (0 = brak)", Float) = 0
        _FilarKolor ("Kolor filarów", Color) = (.62, .64, .66, 1)
        _Tafla ("Nierówność tafli (odbicie każdej szyby inne)", Range(0, .06)) = .018
        _Wnetrze ("Jasność wnętrz", Range(0, 1)) = .55
        _Swiatla ("Udział pokoi ze światłem", Range(0, 1)) = .35
        // 11.11 (właściciel: okrągły wieżowiec „brzydki”): bryła zaokrąglona — siatka elewacji z UV2 (x = metry po obwodzie
        // przekroju, liczone przy buildzie), nie z rzutu na płaszczyznę stycznej (na łuku dawało faliste, rozjechane okna)
        _SiatkaUV ("Siatka z UV2 (1 = bryła zaokrąglona)", Float) = 0
        // tło (niebo, mgiełka) — kopiowane z materiału nieba krokiem horyzontu (BraxRegionHoryzont.UstawTlo)
        [NoScaleOffset] _NieboTex ("Niebo (panorama 2:1)", 2D) = "grey" {}
        _NieboObrot ("Obrót nieba (rad)", Float) = 0
        _NieboEkspozycja ("Ekspozycja", Float) = 1
        _NieboNasycenie ("Nasycenie", Float) = 1.3
        _NieboChmury ("Ruch chmur (rad/s)", Float) = .0035
        _SloKierunek ("Kierunek do słońca", Vector) = (-.45, .78, -.42, 0)
        _SloObraz ("Słońce na obrazie nieba", Vector) = (0, 0, 0, 0)
        _SloTarcza ("Tarcza słońca", Vector) = (0.99994, 0, 0, 0)
        _MgielkaKolor ("Mgiełka", Color) = (.70, .76, .83, 1)
        _MgielkaGestosc ("Mgiełka: gęstość (1/m)", Float) = .00042
        _MgielkaNieboUdzial ("Mgiełka: udział koloru nieba", Range(0, 1)) = .6
        [HideInInspector] _Transparent ("(cienie) przezroczyste", Float) = 0
        [HideInInspector] _AlphaClip ("(cienie) maska alfa", Float) = 0
        [HideInInspector] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "SZKLO"
            Tags { "LightMode"="ForwardBase" }
            Cull Back
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "MiastoTlo.cginc"

            half4 _Tint, _RamaKolor, _PasKolor, _FilarKolor;
            half _F0, _Odbicie, _Wnetrze, _Swiatla, _Typ, _Tafla, _SiatkaUV;
            float _CellW, _CellH, _Floor0, _Depth, _Rama, _Pas, _Filar, _FilarCo;
            half4 _BraxMgielkaKolor;
            float _BraxMgielkaGestosc;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv2 : TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; float3 wp : TEXCOORD0; float3 n : TEXCOORD1; float2 uv2 : TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.n = UnityObjectToWorldNormal(v.normal);
                o.uv2 = v.uv2;
                return o;
            }

            float Hash(float3 p) { return frac(sin(dot(p, float3(12.9898, 78.233, 37.719))) * 43758.5453); }

            // linia siatki wygładzona: 1 na słupku, 0 na szkle; przy pod-pikselowych słupkach — średnie pokrycie (bez mory)
            half Linia(float x, float szer, float fw)
            {
                float d = abs(frac(x + .5) - .5);           // odległość do najbliższej linii (w modułach)
                float w = szer * .5;
                half ostra = 1 - smoothstep(w - fw, w + fw, d);
                return lerp(ostra, saturate(szer), saturate(fw * 2.5 - .25));
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.n);
                float3 V = normalize(i.wp - _WorldSpaceCameraPos);
                float kat = MT_KatPiksela();
                // oś elewacji (pozioma, styczna do szyby) i współrzędne w modułach
                float3 Tg = abs(n.y) < .9 ? normalize(cross(float3(0, 1, 0), n)) : float3(1, 0, 0);
                bool luk = _SiatkaUV > .5;
                float u = (luk ? i.uv2.x : dot(i.wp, Tg)) / _CellW, v = (i.wp.y - _Floor0) / _CellH;
                float fu = fwidth(u), fv = fwidth(v);
                // ---- wnętrze: pokój za szybą (tylna ściana, sufit z lampą, podłoga) ----
                float cu = floor(u), cv = floor(v);
                float3 p = float3(frac(u), frac(v), 0);
                float3 rd = float3(dot(V, Tg) / _CellW, V.y / _CellH, max(dot(V, -n) / _Depth, 1e-3));
                float tx = rd.x > 0 ? (1 - p.x) / rd.x : -p.x / min(rd.x, -1e-5);
                float ty = rd.y > 0 ? (1 - p.y) / rd.y : -p.y / min(rd.y, -1e-5);
                float tz = 1 / rd.z;
                float t = min(min(tx, ty), tz);
                float3 h = saturate(p + rd * t);
                float lico = luk ? 0 : floor(dot(i.wp, n) * .5);           // na łuku normalna się obraca — pokój = sama komórka siatki
                float id = Hash(float3(cu, cv, lico));
                half zapalone = step(id, _Swiatla);
                half3 sciana = lerp(half3(.62, .62, .60), half3(.70, .67, .62), frac(id * 7.31));
                half3 room;
                if (t == tz) room = sciana * .85;
                else if (t == tx) room = sciana * .7;
                else if (rd.y < 0) room = half3(.30, .30, .31);                                   // wykładzina
                else room = lerp(half3(.80, .80, .79), half3(1.4, 1.36, 1.25), zapalone * step(abs(h.x - .5), .18));   // sufit + pas światła
                room *= lerp(1, .45, h.z);
                half zaluzje = step(.78, frac(id * 13.7));
                room = lerp(room, half3(.70, .70, .68) * (.85 + .15 * step(.5, frac(p.y * 30))), zaluzje * step(.55, p.y));
                room *= _Wnetrze * lerp(.55, 1.15, zapalone);
                half3 szyba = room * _Tint.rgb * 1.6 + _Tint.rgb * .08;
                // ---- odbicie: niebo i horyzont miasta (to samo tło co niebo gry) ----
                // każda tafla szkła jest minimalnie inaczej pochylona (jak w prawdziwej ścianie kurtynowej): odbicie chmur łamie się
                // na podziałach, elewacja nie jest jednym lustrem; z daleka (tafla < 4 px) nierówność wygasa — bez migotania
                float idT = Hash(float3(cu + 3.7, cv + 9.1, lico));
                float3 nT = normalize(n + (Tg * (idT - .5) + float3(0, 1, 0) * (frac(idT * 31.7) - .5)) * (_Tafla * saturate(1.25 - max(fu, fv) * 5)));
                float3 R = reflect(V, nT);
                half3 tlo = R.y > 0 ? MT_Niebo(R, kat * 8) : MT_Mgielka(R) * lerp(.85, .55, saturate(-R.y * 4));
                half cosv = saturate(dot(-V, n));
                half F = _F0 + (1 - _F0) * pow(1 - cosv, 5);
                half3 kolor = lerp(szyba, tlo * _Odbicie, saturate(F));
                // odblask słońca
                float3 L = normalize(_SloKierunek.xyz);
                kolor += half3(1, .96, .88) * pow(saturate(dot(R, L)), 600) * 2.5;
                // światło na częściach pełnych (okładzina, filary, słupki): słońce + niebo
                half ndl = saturate(dot(n, L));
                half swiatlo = .55 + .45 * ndl;
                half3 pelne = half3(.42, .45, .50) + half3(1.0, .96, .88) * (ndl * .85);
                // ---- pas międzykondygnacyjny ----
                float pv = frac(v);
                half pas = (1 - smoothstep(_Pas / _CellH - fv, _Pas / _CellH + fv, pv));
                pas = lerp(pas, _Pas / _CellH, saturate(fv * 2.5 - .25));
                half pelnyPas = step(.5, _Typ);
                // okładzina wystaje przed szybę: cień pod pasem wyższej kondygnacji (góra okna) — głębia elewacji
                half cienPasa = pelnyPas * smoothstep(1 - .16 * (3.4 / _CellH), 1, pv) * (1 - saturate(fv * 4));
                kolor *= 1 - .42 * cienPasa;
                half3 pasSzklo = _PasKolor.rgb * .9 + tlo * (F * .6 + .05);
                // okładzina: płyty z fugą co moduł i cienką linią kapinosu u dołu pasa
                half fuga = Linia(u, .02 / _CellW, fu) * .28 + (1 - smoothstep(0, .05 / _CellH + fv, pv)) * .25 * (1 - saturate(fv * 4));
                half3 pasPelny = _PasKolor.rgb * pelne * (1 - fuga);
                kolor = lerp(kolor, lerp(pasSzklo, pasPelny, pelnyPas), pas * step(.001, _Pas));
                // ---- słupki co moduł (aluminium) — tylko w polu szyby ----
                half slupek = Linia(u, _Rama / _CellW, fu) * step(.001, _Rama) * (1 - pas * pelnyPas);
                kolor = lerp(kolor, _RamaKolor.rgb * swiatlo + tlo * .12, slupek);
                // ---- filary co kilka modułów: pełne, wysunięte — rzucają wąski cień na szybę po stronie od słońca ----
                if (_FilarCo > .5)
                {
                    float uf = u / _FilarCo, ff = fu / _FilarCo;
                    float df = (frac(uf + .5) - .5) * _FilarCo * _CellW;        // metry od osi najbliższego filara (ze znakiem wzdłuż Tg)
                    half filar = Linia(uf, _Filar / (_CellW * _FilarCo), ff);
                    half strona = dot(L, Tg);
                    half cienF = (1 - smoothstep(_Filar * .5, _Filar * .5 + .55, -df * sign(strona))) * step(0, -df * sign(strona)) * saturate(abs(strona) * 2.5) * (1 - saturate(ff * _FilarCo * 4));
                    kolor *= 1 - .38 * cienF * (1 - filar);
                    half fugaF = (1 - smoothstep(0, .03 / _CellH + fv, abs(pv - _Pas / _CellH))) * .22 * (1 - saturate(fv * 4));
                    kolor = lerp(kolor, _FilarKolor.rgb * pelne * (1 - fugaF), filar);
                }
                // ---- perspektywa powietrzna (jak CityStaticPBR) ----
                if (_BraxMgielkaGestosc > 0)
                {
                    float odl = distance(_WorldSpaceCameraPos.xyz, i.wp);
                    kolor = lerp(kolor, _BraxMgielkaKolor.rgb, saturate(1.0 - exp(-odl * _BraxMgielkaGestosc)));
                }
                return half4(kolor, 1);
            }
            ENDCG
        }
        UsePass "Braxhorn/CityStaticPBR/SHADOWCASTER"
    }
    Fallback Off
}
