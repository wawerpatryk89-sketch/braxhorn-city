// BRAXHORN CITY — TŁO ŚWIATA MIASTA (wykonawca „horyzont”, 2.11): to, co widać za ostatnią siatką w danym kierunku.
// Wspólne dla nieba (Braxhorn/MiastoNiebo) i dalekiego terenu (MiastoDal, MiastoDalLas — przechodzą w to tło z odległością).
//
// Dlaczego (zgłoszenia właściciela 13.1, zdjęcia d5c529ee, 37257bab, z30): niebo Poly Haven „Kloofendal … Pure Sky” ma
// w dolnej połowie ODBITE niebo — wszędzie, gdzie za granicą miasta nie było siatki, gracz widział „nieskończone lustro”
// z chmurami (i chodził po nim po niewidzialnej podłodze oryginału). Tu dolna półkula NIGDY nie pokazuje obrazu nieba:
// każdy promień w dół trafia w ziemię (płaszczyzna lądu y = 0 albo morza y = −1.7 wg linii brzegu z BraxMiastoGranice),
// cieniowaną jak prawdziwy teren / woda, z mgiełką powietrza do horyzontu. Dziura w świecie = ziemia albo morze
// w dobrej perspektywie, nigdy lustro ani pustka.
//
// Plany głębi (od bliskiego do dalekiego), wszystkie liczone geometrycznie z pozycji kamery (paralaksa jak w świecie):
//  1) ziemia (pola, łąki, kępy lasu z tekstury makro; trawa z bliska) albo morze (kolor głębi + odbicie nieba, fresnel);
//  2) grzbiet 1 — zalesione wzgórza na walcu R1 (wysokość z profilu _Grzbiety.r), światło słońca od zbocza;
//  3) grzbiet 2 — wyższe, bardziej zamglone pasmo na walcu R2 (_Grzbiety.g);
//  4) góry ze zdjęcia 360° (Poly Haven „Alps Field”, CC0, ten sam pas co na osiedlu) na walcu R3; od strony morza
//     pas zgnieciony do zera (_Grzbiety.b) — góry schodzą cyplami do wody, na wschodzie otwarty horyzont morza;
//  5) niebo z chmurami (Kloofendal, CC0), słońce obrazu ustawione na kierunek słońca miasta, chmury powoli płyną.
// Próbkowanie nieba z jawnym gradientem (tex2Dgrad) — bez szwu mip na łączeniu panoramy i bez „krawędzi pudełka”.
#ifndef BRAXHORN_MIASTO_TLO
#define BRAXHORN_MIASTO_TLO

#include "UnityCG.cginc"

#define MT_PI 3.14159265
#define MT_2PI 6.2831853

sampler2D _NieboTex;
float _NieboObrot;                 // radiany: obrót obrazu nieba wokół pionu (słońce obrazu → słońce miasta)
half _NieboEkspozycja, _NieboNasycenie;
float _NieboChmury;                // radiany na sekundę: ruch górnej części nieba
float4 _SloKierunek;               // kierunek DO słońca (świat)
// NIEBO (C / M8, właściciel 2.11: „w dzień nisko przy horyzoncie duża żółta tarcza z teksturą jak księżyc”): słońce ZDJĘCIA nieba
// (nisko, przepalone, z pierścieniami kompresji JPG = „kratery”) jest wymazane z obrazu — także z warstwy płynących chmur,
// która obracała je po niebie (blada „druga tarcza”, film09) — a słońce miasta rysowane jest jako czysta tarcza bez tekstury
// w kierunku światła sceny (_SloKierunek). _SloObraz: xyz = kierunek słońca na obrazie (rama obrazu), w = cos promienia maski
// (0 = bez maski); _SloTarcza: x = cos promienia tarczy, y = jasność tarczy (0 = bez tarczy).
float4 _SloObraz;
float4 _SloTarcza;

sampler2D _Pasmo;                  // pas gór ze zdjęcia (RGBA, A = ziemia)
float4 _PasmoSrodek;               // xyz: środek walców (środek prostokąta miasta, y = 0)
float _PasmoPromien, _PasmoU0, _PasmoGora, _PasmoDol, _PasmoJest;
half _PasmoJasnosc, _PasmoNasycenie, _PasmoMgla;

sampler2D _Grzbiety;               // 2048×1 liniowa: R = grzbiet 1 (×H1), G = grzbiet 2 (×H2), B = skala pasa gór (0 = morze)
float4 _GrzbietyR;                 // x = R1, y = R2 (m), z = H1, w = H2 (m)
half4 _GrzbietKolor1, _GrzbietKolor2;

sampler2D _ZiemiaMakro;            // RGB = barwa terenu z daleka (pola, łąki, las), A = las
float _ZiemiaMakroSkala;           // metrów na powtórzenie
sampler2D _ZiemiaTrawa;            // trawa z bliska
float _ZiemiaTrawaSkala;
half4 _ZiemiaOdcien;

float4 _Brzeg1;                    // x = brzeg na południu, y = brzeg na północy, z/w = przejście cypla (z0, z1)
float4 _Brzeg2;                    // x = łuk północ od z, y = łuk południe od z, z = nachylenie łuku, w = poziom morza
half4 _MorzeKolor;                 // głębia morza z daleka
half4 _MgielkaKolor;
float _MgielkaGestosc;             // 1/m
half _MgielkaNieboUdzial;          // ile koloru horyzontu nieba w mgiełce (0..1)

inline float MT_Brzeg(float z)
{
    float t = saturate((z - _Brzeg1.z) / max(_Brzeg1.w - _Brzeg1.z, 1.0));
    t = t * t * (3.0 - 2.0 * t);
    return lerp(_Brzeg1.x, _Brzeg1.y, t) + (max(0.0, z - _Brzeg2.x) + max(0.0, _Brzeg2.y - z)) * _Brzeg2.z;
}

inline half3 MT_Nasyc(half3 c, half s)
{
    half y = dot(c, half3(.2126, .7152, .0722));
    return max(0, y + (c - y) * s);
}

// współrzędne panoramy nieba dla kierunku świata (jak Skybox/Panoramic) + gradienty analityczne (rozmiar piksela)
inline float2 MT_NieboUV(float3 d, float obrot)
{
    float lon = atan2(d.z, d.x) - obrot;
    float lat = asin(clamp(d.y, -1.0, 1.0));
    return float2(frac(0.5 - lon / MT_2PI), 0.5 + lat / MT_PI);
}

inline half3 MT_NieboProbka(float3 d, float obrot, float katPiksela)
{
    float2 uv = MT_NieboUV(d, obrot);
    float c = max(sqrt(saturate(1.0 - d.y * d.y)), 0.03);
    float du = katPiksela / (MT_2PI * c), dv = katPiksela / MT_PI;
    half3 kol = tex2Dgrad(_NieboTex, uv, float2(du, 0), float2(0, dv)).rgb;
    // Android: zdjęcia 360° nie zawsze mają identyczny pierwszy i ostatni
    // pionowy pas panoramy. To daje widoczny prostokątny szew na niebie.
    // Wyrównanie JEDYNIE wąskiego brzegu U=0/1 (poniżej 0.7% szerokości):
    // oryginalne chmury i kolory zdjęcia pozostają bez zmian na reszcie sfery.
    float seamDist = min(uv.x, 1.0 - uv.x);
    const float seamWidth = 0.007;
    if (seamDist < seamWidth)
    {
        half3 leftEdge = tex2Dgrad(_NieboTex, float2(.0005, uv.y),
                                  float2(du, 0), float2(0, dv)).rgb;
        half3 rightEdge = tex2Dgrad(_NieboTex, float2(.9995, uv.y),
                                   float2(du, 0), float2(0, dv)).rgb;
        half alphaSeam = 1.0 - smoothstep(0.0, seamWidth, seamDist);
        kol = lerp(kol, .5 * (leftEdge + rightEdge), alphaSeam);
    }
    if (_SloObraz.w > 0.5)
    {
        // słońce obrazu w świecie: obrót wokół pionu o „obrot” (lon świata = lon obrazu + obrot)
        float co = cos(obrot), so = sin(obrot);
        float3 s = float3(_SloObraz.x * co - _SloObraz.z * so, _SloObraz.y, _SloObraz.x * so + _SloObraz.z * co);
        float ca = dot(d, s);
        if (ca > _SloObraz.w)
        {
            float R = acos(_SloObraz.w);
            float3 t = normalize(d - s * ca + float3(1e-5, 0, 1e-5));
            float3 d2 = s * cos(R * 1.35) + t * sin(R * 1.35);          // niebo z pierścienia tuż za tarczą
            half3 f = tex2Dlod(_NieboTex, float4(MT_NieboUV(d2, obrot), 0, 2)).rgb;
            half k = smoothstep(_SloObraz.w, lerp(_SloObraz.w, 1.0, 0.35), ca);
            kol = lerp(kol, f, k);
        }
    }
    return kol;
}

// niebo (tylko górna półkula; poniżej horyzontu — kolor przy samym horyzoncie)
inline half3 MT_Niebo(float3 d, float katPiksela)
{
    float3 g = normalize(float3(d.x, max(d.y, 0.002), d.z));
    half3 a = MT_NieboProbka(g, _NieboObrot, katPiksela);
    half w = smoothstep(.10, .32, g.y);
    if (w > 0.001)
    {
        half3 b = MT_NieboProbka(g, _NieboObrot + fmod(_Time.y * _NieboChmury, MT_2PI), katPiksela);
        a = lerp(a, b, w);
    }
    a = MT_Nasyc(a, _NieboNasycenie) * _NieboEkspozycja;
    if (_SloTarcza.y > 0.0)
    {   // słońce miasta: czysta, jasna tarcza z miękką poświatą (bez tekstury), tam skąd pada światło sceny
        float cs = dot(g, normalize(_SloKierunek.xyz));
        half disc = smoothstep(_SloTarcza.x, lerp(_SloTarcza.x, 1.0, 0.3), cs);
        half glow = pow(saturate(cs), 1400.0) * 0.30 + pow(saturate(cs), 90.0) * 0.05;
        a += half3(1.0, 0.97, 0.90) * (disc * _SloTarcza.y + glow);
    }
    return a;
}

// barwa powietrza (mgiełki) w danym azymucie: kolor nieba tuż nad horyzontem (rozmyty) + stały kolor mgiełki
inline half3 MT_Mgielka(float3 d)
{
    float3 h = normalize(float3(d.x, 0.03, d.z));
    float2 uv = MT_NieboUV(h, _NieboObrot);
    half3 s = tex2Dlod(_NieboTex, float4(uv, 0, 5)).rgb;
    s = MT_Nasyc(s, _NieboNasycenie) * _NieboEkspozycja;
    return lerp(_MgielkaKolor.rgb, s, _MgielkaNieboUdzial);
}

inline half MT_MgielkaIle(float t) { return 1.0 - exp(-t * _MgielkaGestosc); }

// przecięcie z pionowym walcem (środek _PasmoSrodek.xz, promień R) — wyjście z walca (kamera w środku)
inline float MT_Walec(float3 c, float3 d, float R)
{
    float a = dot(d.xz, d.xz);
    if (a < 1e-8) return 1e9;
    float b = dot(c.xz, d.xz);
    float cc = dot(c.xz, c.xz) - R * R;
    float disc = b * b - a * cc;
    if (disc < 0.0) return 1e9;
    float t = (-b + sqrt(disc)) / a;
    return t > 0.0 ? t : 1e9;
}

inline float MT_Azymut01(float2 pxz)   // 0..1 od północy (+z) w stronę wschodu (+x)
{
    return frac(atan2(pxz.x, pxz.y) / MT_2PI + 1.0);
}

// ziemia: ląd (pola, łąki, las) albo morze; t = odległość od kamery, p = punkt trafienia, d = kierunek
inline half3 MT_Ziemia(float3 p, float t, float3 d, bool morze, half3 mg, float katPiksela)
{
    half3 c;
    float3 sun = normalize(_SloKierunek.xyz);
    if (morze)
    {
        // głębia + odbicie nieba (fresnel), lekka ciemniejsza linia horyzontu morza
        float3 r = float3(d.x, abs(d.y), d.z);
        half3 nieb = MT_Niebo(r, 0.02);
        half cosv = saturate(-d.y);
        half fres = 0.02 + 0.98 * pow(1.0 - cosv, 5.0);
        c = lerp(_MorzeKolor.rgb * (0.72 + 0.28 * sun.y), nieb * 0.86, fres * 0.9);
        half bl = pow(saturate(dot(r, sun)), 220.0) * 0.6;
        c += half3(1.0, .96, .88) * bl;
        half f = MT_MgielkaIle(t * 0.85);
        return lerp(c, mg * 0.97, f);
    }
    float2 xz = p.xz;
    // gradienty liczone z odległości (rozmiar piksela na ziemi; wzdłuż patrzenia rozciągnięty o 1/|d.y|) — działają też
    // w gałęziach (bez pochodnych ekranu w niejednolitym przepływie)
    float w = t * katPiksela, wd = w / max(abs(d.y), 0.03);
    float2 fw = normalize(d.xz + float2(1e-5, 1e-5)), sd = float2(-fw.y, fw.x);
    float2 ga = sd * w, gb = fw * wd;      // w poprzek i wzdłuż patrzenia (metry)
    half4 m = tex2Dgrad(_ZiemiaMakro, xz / _ZiemiaMakroSkala, ga / _ZiemiaMakroSkala, gb / _ZiemiaMakroSkala);
    float s2 = _ZiemiaMakroSkala * 3.7;
    half4 m2 = tex2Dgrad(_ZiemiaMakro, xz / s2 + .37, ga / s2, gb / s2);
    half3 baza = m.rgb * lerp(0.82, 1.18, dot(m2.rgb, half3(.3, .59, .11)) * 1.6);
    // z bliska trawa (dziury w świecie wewnątrz miasta wyglądają jak trawnik, nie jak plama koloru)
    half blisko = 1.0 - smoothstep(25.0, 140.0, t);
    if (blisko > 0.001)
    {
        half3 tr = tex2Dgrad(_ZiemiaTrawa, xz / _ZiemiaTrawaSkala, ga / _ZiemiaTrawaSkala, gb / _ZiemiaTrawaSkala).rgb * _ZiemiaOdcien.rgb;
        baza = lerp(baza, tr, blisko * (1.0 - m.a * 0.6));
    }
    half swiatlo = 0.55 + 0.62 * saturate(sun.y);
    c = baza * swiatlo;
    return lerp(c, mg, MT_MgielkaIle(t));
}

// grzbiet wzgórz (ściana walca z profilem wysokości), u = azymut 0..1, y = wysokość trafienia
inline half3 MT_Grzbiet(float3 p, float t, float u, float h, float R, half4 kolor, half3 mg, float dhdu, float katPiksela)
{
    float3 sun = normalize(_SloKierunek.xyz);
    float2 rad = normalize(p.xz - _PasmoSrodek.xz);
    // normalna: zbocze zwrócone do miasta, pochylone (zbocze, nie ściana), boki wg spadku profilu
    float3 n = normalize(float3(-rad.x, 0.0, -rad.y) * 0.55 + float3(0, 0.82, 0) + float3(rad.y, 0, -rad.x) * clamp(dhdu / (MT_2PI * R) * 4.0, -0.6, 0.6));
    half ndl = saturate(dot(n, sun));
    // faktura: las (tekstura makro w skali drzew), jaśniejsze polany i skały przy szczycie
    float s = u * MT_2PI * R;
    float gw = t * katPiksela / 260.0;
    half4 m = tex2Dgrad(_ZiemiaMakro, float2(s, p.y * 1.6) / 260.0, float2(gw, 0), float2(0, gw * 1.6));
    half3 alb = kolor.rgb * lerp(0.7, 1.25, m.g * 2.0) * lerp(1.0, 0.82, m.a);
    half3 c = alb * (0.42 + 0.75 * ndl);
    // mgiełka: odległość + przy podnóżu (powietrze doliny gęstsze), szczyt ostrzejszy
    half f = MT_MgielkaIle(t) + 0.22 * (1.0 - smoothstep(0.0, h * 0.75 + 1.0, p.y - _PasmoSrodek.y));
    return lerp(c, mg, saturate(f));
}

// pas gór ze zdjęcia (alfa = góry) z kierunku d dla punktu walca p
inline half4 MT_Pasmo(float3 p, float skala)
{
    float2 q = p.xz - _PasmoSrodek.xz;
    float az = atan2(q.x, q.y) * (180.0 / MT_PI);
    float el = atan2(p.y - _PasmoSrodek.y, _PasmoPromien) * (180.0 / MT_PI);
    float els = el / max(skala, 1e-3);
    float u = frac(0.25 + az / 360.0 + _PasmoU0);
    float v = saturate((els - _PasmoDol) / (_PasmoGora - _PasmoDol));
    half4 s = tex2Dlod(_Pasmo, float4(u, v, 0, 0));
    if (els > _PasmoGora || skala < 0.02) s.a = 0;
    s.rgb = MT_Nasyc(s.rgb, _PasmoNasycenie) * _PasmoJasnosc;
    s.rgb = lerp(s.rgb, _MgielkaKolor.rgb, _PasmoMgla * (1 - smoothstep(0.0, 14.0, el)));
    s.a *= _PasmoJest;
    return s;
}

// TŁO: kolor świata w kierunku d widziany z punktu cam (bez obiektów sceny)
inline half3 MiastoTlo(float3 cam, float3 d, float katPiksela)
{
    half3 mg = MT_Mgielka(d);
    float3 c = cam - _PasmoSrodek.xyz;
    // 1) ziemia / morze
    float tg = 1e9; bool morze = false;
    if (d.y < -1e-5 && cam.y > -0.5)
    {
        float t0 = cam.y / -d.y;
        float2 p0 = cam.xz + d.xz * t0;
        morze = p0.x > MT_Brzeg(p0.y);
        tg = morze ? (cam.y - _Brzeg2.w) / -d.y : t0;
    }
    // 2–3) grzbiety
    float t1 = MT_Walec(c, d, _GrzbietyR.x), t2 = MT_Walec(c, d, _GrzbietyR.y);
    if (t1 < tg)
    {
        float3 p = cam + d * t1;
        float u = MT_Azymut01(p.xz - _PasmoSrodek.xz);
        half4 g = tex2Dlod(_Grzbiety, float4(u, 0.5, 0, 0));
        float h = g.r * _GrzbietyR.z;
        if (p.y - _PasmoSrodek.y < h && h > 0.3)
        {
            half4 g2 = tex2Dlod(_Grzbiety, float4(u + 1.0 / 2048.0, 0.5, 0, 0));
            return MT_Grzbiet(p, t1, u, h, _GrzbietyR.x, _GrzbietKolor1, mg, (g2.r - g.r) * _GrzbietyR.z * 2048.0, katPiksela);
        }
    }
    if (t2 < tg)
    {
        float3 p = cam + d * t2;
        float u = MT_Azymut01(p.xz - _PasmoSrodek.xz);
        half4 g = tex2Dlod(_Grzbiety, float4(u, 0.5, 0, 0));
        float h = g.g * _GrzbietyR.w;
        if (p.y - _PasmoSrodek.y < h && h > 0.3)
        {
            half4 g2 = tex2Dlod(_Grzbiety, float4(u + 1.0 / 2048.0, 0.5, 0, 0));
            return MT_Grzbiet(p, t2, u, h, _GrzbietyR.y, _GrzbietKolor2, mg, (g2.g - g.g) * _GrzbietyR.w * 2048.0, katPiksela);
        }
    }
    // 4) góry ze zdjęcia (przed ziemią za walcem gór i przed niebem)
    float t3 = MT_Walec(c, d, _PasmoPromien);
    half4 pas = half4(0, 0, 0, 0);
    if (t3 < tg && _PasmoJest > 0.5)
    {
        float3 p = cam + d * t3;
        float u = MT_Azymut01(p.xz - _PasmoSrodek.xz);
        half sk = tex2Dlod(_Grzbiety, float4(u, 0.5, 0, 0)).b;
        pas = MT_Pasmo(p, sk);
    }
    half3 tlo;
    if (tg < 1e8) tlo = MT_Ziemia(cam + d * tg, tg, d, morze, mg, katPiksela);
    else if (d.y < 0.0) tlo = mg;   // kamera pod ziemią (piwnica, metro) — tylko kolor powietrza
    else
    {
        tlo = MT_Niebo(d, katPiksela);
        // przy samym horyzoncie niebo przechodzi w mgiełkę (powietrze nad morzem i doliną)
        tlo = lerp(tlo, mg, (1.0 - smoothstep(0.0, 0.05, d.y)) * 0.55);
    }
    return lerp(tlo, pas.rgb, pas.a);
}

// kąt jednego piksela ekranu (radiany) — do gradientów tekstury nieba
inline float MT_KatPiksela()
{
    return 2.0 / (max(abs(unity_CameraProjection._m11), 0.1) * max(_ScreenParams.y, 1.0));
}

#endif
