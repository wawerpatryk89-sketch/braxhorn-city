using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// BRAXHORN CITY — HORYZONT I KRANIEC MAPY MIASTA (wykonawca „horyzont”, 2.11; zgłoszenia właściciela z gry 13.1:
// 14 taras „miał być z widokiem — stałe widoki, horyzonty porobione, piękne, 3D, panoramy”, 24 „za wieżowcami koniec planszy
// i tam panorama 3D — jak ten wiadukt się ciągnie, góry się pokazują — nic nie ma”, 26 niebo jak pudełko i wzgórze-kopuła,
// 27/30 koniec mapy = nieskończone lustro, wiadukt urywa się w powietrzu, 34 gładkie stożki zamiast panoramy).
//
// Źródło „lustra”: niebo Kloofendal Pure Sky (Skybox/Panoramic) ma w dolnej połowie odbite niebo; za granicą miasta nie było
// żadnej siatki, więc widać było dolną połowę nieba, a gracz chodził po niewidzialnej podłodze oryginału (y = 0).
// Co robi ten krok (kolejno; zasada właściciela 11:00 — najpierw ZDJĄĆ stare, potem położyć nowe):
//  0) USUWA: stare niebo (materiał Skybox/Panoramic z lustrem w dolnej połowie), kopułę wzgórza tyłów z 13.1 (jeśli jeszcze
//     stoi), wszystkie siatki ORYGINAŁU miasta za granicą gry na lądzie (stara łąka i las-płyta na północy, resztki na
//     zachodzie) — lista w logu BRAXHORN_HORYZONT_MIASTO usunięte;
//  1) niebo i panorama 360° (shader Braxhorn/MiastoNiebo, MiastoTlo.cginc): chmury, w dół ziemia albo morze (nigdy lustro),
//     dwa plany zalesionych grzbietów, góry ze zdjęcia (Poly Haven Alps Field, CC0 — ten sam pas co na osiedlu), od strony
//     morza otwarty horyzont wody, góry schodzą do morza dwoma cyplami;
//  2) teren 3D za granicą (siatki bez kolizji, rzadsze z odległością, łączone w kawałki): łąki i pola, zalesione wzgórza
//     na zachodzie i północy, skalisty brzeg morza, pas zieleni pod wzgórzem „tyłów”; z odległością przechodzi w tło;
//  3) las: karty drzew (2 trójkąty na drzewo) na wzgórzach + prawdziwe drzewa przy granicy;
//  4) granica grywalna z kolizją: kamienny murek z balustradą (południe, zachód), drewniany płot lasu (północ, wycięcia);
//  5) wiadukt „ciągnie się dalej” na północ za granicą (przęsła tym samym przekrojem co część „tyłów”) i znika w tunelu
//     w zalesionym wzgórzu; na pomoście na granicy zamknięcie drogi (bariery, znak).
// Prostokąt grywalny, linia brzegu i oś wiaduktu: BraxMiastoGranice (jedno miejsce).
public static class BraxRegionHoryzont
{
    const string OutDir = BraxRegion.Out + "/Horyzont";
    const string OsTex = "Assets/Source/Claude/Osiedle/textures/";
    const string OsMdl = "Assets/Source/Claude/Osiedle/models/";
    const string PasmoPath = "Assets/Source/Claude/Osiedle/sky/panorama_pasmo.png";
    const string PasmoJson = "Assets/Source/Claude/Osiedle/sky/panorama_pasmo.json";
    const float Zasieg = 520f;           // jak daleko od granicy sięga teren 3D (dalej tło nieba)
    const float R1 = 850f, R2 = 1450f, R3 = 2600f, H1 = 160f, H2 = 320f;

    static Transform root;
    static Material nieboMat, terenMat, lasMat, obiektMat;
    static Texture2D nieboTex, pasmoTex, grzbietyTex, makroTex, trawaTex;
    static readonly List<string> usuniete = new List<string>();
    static readonly List<string> kroki = new List<string>();
    static readonly List<Vector3> drzewa = new List<Vector3>();
    static int siatki, polaTerenu, karty, drzewa3D, odcinkiGranicy, przesla;
    static float dlugoscGranicy;

    [Serializable] sealed class PasmoInfo { public string zrodlo = ""; public float przesuniecie_u = .02f, elewacja_gora = 35f, elewacja_dol = -10f; }

    public static void Apply(BraxCityWorld world, Transform cityRoot)
    {
        usuniete.Clear(); kroki.Clear(); drzewa.Clear();
        sloZnane = false;
        siatki = polaTerenu = karty = drzewa3D = odcinkiGranicy = przesla = 0; dlugoscGranicy = 0;
        nieboMat = terenMat = lasMat = obiektMat = null; nieboTex = pasmoTex = grzbietyTex = makroTex = trawaTex = null;
        Directory.CreateDirectory(OutDir);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        root = BraxRegion.Root(cityRoot, "CLAUDE horyzont: teren za granicą miasta, granica, wiadukt dalej");
        Krok("usunięcie starego (niebo-lustro, kopuła, oryginał za granicą)", () => UsunStare(cityRoot));
        Krok("tekstury tła (profile grzbietów, ziemia z daleka)", Tekstury);
        Krok("niebo i panorama 360°", Niebo);
        Krok("teren 3D za granicą", Teren);
        Krok("las za granicą (karty + drzewa przy granicy)", Las);
        Krok("granica grywalna z kolizją", Granica);
        BraxRegion.Note("horyzont: " + string.Join("; ", kroki));
        Debug.Log("BRAXHORN_HORYZONT_MIASTO prostokąt x " + BraxMiastoGranice.X0 + "…" + BraxMiastoGranice.X1 + " z " + BraxMiastoGranice.Z0 + "…" + BraxMiastoGranice.Z1 +
                  " wycięcia=" + BraxMiastoGranice.Wyciecia.Length + " teren: pól " + polaTerenu + " siatek " + siatki + " karty lasu " + karty + " drzewa 3D " + drzewa3D +
                  " granica " + odcinkiGranicy + " odcinków " + dlugoscGranicy.ToString("0") + " m, przęsła wiaduktu za granicą " + przesla +
                  " niebo=" + (nieboMat ? nieboMat.shader.name : "BRAK") + " góry=" + (pasmoTex ? pasmoTex.width + "x" + pasmoTex.height : "BRAK") + " lustro=0 stożki=0 kopuły=0");
        Debug.Log("BRAXHORN_HORYZONT_MIASTO usunięte (" + usuniete.Count + "): " + string.Join(" | ", usuniete.Take(60)));
    }

    static void Krok(string nazwa, Action a)
    {
        try { a(); kroki.Add(nazwa + ": OK"); }
        catch (Exception e) { kroki.Add(nazwa + ": BŁĄD " + e.Message); Debug.LogError("BRAXHORN_HORYZONT_MIASTO " + nazwa + ": " + e); }
    }

    static float S01(float a, float b, float v) { float t = Mathf.Clamp01((v - a) / (b - a)); return t * t * (3f - 2f * t); }

    // =====================================================================================================================
    // 0) usunięcie starego
    static void UsunStare(Transform cityRoot)
    {
        // a) kopuła wzgórza tyłów z 13.1 (gładka zielona kopuła z szumem, zgłoszenia 26/27) — tylko stara siatka o tej nazwie
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!t || t.name != "Wzgórze za wiaduktem (trawa)") continue;
            var mf = t.GetComponent<MeshFilter>();
            if (mf && mf.sharedMesh && mf.sharedMesh.name != "Wzgórze za wiaduktem") continue;   // nowe wzgórze „tyłów” zostaje
            var parent = t.parent;
            usuniete.Add("kopuła: " + t.name);
            UnityEngine.Object.DestroyImmediate(t.gameObject);
            if (parent)
                foreach (var c in parent.Cast<Transform>().Where(c => c.name.StartsWith("Wzgórze: ")).ToList())
                { usuniete.Add("kopuła: " + c.name); UnityEngine.Object.DestroyImmediate(c.gameObject); }
        }
        // b) siatki oryginału miasta na lądzie za granicą gry (łąka-płyta, las-płyta, resztki) — wycięte w całości
        var original = BraxClaudeGround.SourceCity;
        if (original)
        {
            var obj = new List<(string, Bounds)>
            {
                ("zachod", Bx(-3000f, BraxMiastoGranice.X0 - .3f, -120f, 400f, -3000f, 3000f)),
                ("poludnie", Bx(BraxMiastoGranice.X0 - .3f, BraxMiastoGranice.BrzegPoludnie - .3f, -120f, 400f, -3000f, BraxMiastoGranice.Z0 - .3f)),
                ("polnoc", Bx(BraxMiastoGranice.X0 - .3f, BraxMiastoGranice.BrzegPolnoc + 30f, -120f, 400f, BraxMiastoGranice.Z1 + .3f, 3000f)),
            };
            int k = 0;
            foreach (var w in BraxMiastoGranice.Wyciecia) obj.Add(("wyciecie" + (k++), Bx(w.xMin + .3f, w.xMax - .3f, -120f, 400f, w.yMin + .3f, w.yMax - .3f)));
            foreach (var (nazwa, b) in obj)
            {
                var przed = original.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && r.bounds.Intersects(b)).Select(r => r.name).ToList();
                int n = 0;
                try { n = BraxCityMeshCut.Subtract(original, b, OutDir + "/Wyciete", "za_granica_" + nazwa); }
                catch (Exception e) { usuniete.Add("oryginał " + nazwa + ": błąd " + e.Message); continue; }
                if (n > 0) usuniete.Add("oryginał za granicą (" + nazwa + "): " + n + " siatek [" + string.Join(",", przed.Take(12)) + (przed.Count > 12 ? ",…" : "") + "]");
            }
        }
        // c) stare niebo (Skybox/Panoramic z odbitym niebem w dolnej połowie) — zastąpione w kroku „niebo”, materiał usuwany tam
    }

    static Bounds Bx(float x0, float x1, float y0, float y1, float z0, float z1)
    { var b = new Bounds(); b.SetMinMax(new Vector3(x0, y0, z0), new Vector3(x1, y1, z1)); return b; }

    // =====================================================================================================================
    // 1) tekstury tła: profile grzbietów (2048×1, liniowa) i ziemia z daleka (1024², kafel 1600 m)
    static Vector2 Srodek => new Vector2(BraxMiastoGranice.SrodekX, BraxMiastoGranice.SrodekZ);

    static float Okresowy(float az, int k0, int k1, int ziarno, float spadek)
    {
        // szum okresowy po azymucie (suma harmonicznych z losowymi fazami) — bez szwu na 0/360°
        var r = new System.Random(ziarno); float s = 0, norm = 0;
        for (int k = k0; k <= k1; k++)
        {
            float a = Mathf.Pow(k, -spadek) * (.6f + (float)r.NextDouble() * .8f), f = (float)r.NextDouble() * Mathf.PI * 2f;
            s += a * Mathf.Sin(k * az + f); norm += a;
        }
        return s / norm;   // ok. −1…1 (zwykle −.6….6)
    }

    static void Tekstury()
    {
        // --- profile grzbietów ---
        const int N = 2048;
        var px = new Color32[N];
        var c = Srodek;
        for (int i = 0; i < N; i++)
        {
            float az = (i + .5f) / N * Mathf.PI * 2f;
            var dir = new Vector2(Mathf.Sin(az), Mathf.Cos(az));
            float Lad(float R, float szer) { var p = c + dir * R; return S01(0f, szer, BraxMiastoGranice.Brzeg(p.y) - p.x); }
            // grzbiet 1: zalesione wzgórza 30…85 m, korony drzew na linii grzbietu (drobne ząbki)
            float h1 = 52f + 30f * Okresowy(az, 2, 26, 11, 1.1f) + 2.2f * Okresowy(az, 120, 260, 12, 0f) + 1.4f * Okresowy(az, 300, 700, 13, 0f);
            // przełęcz na północy za wiaduktem (wiadukt wchodzi w tunel bliżej) i obniżenie na zachodzie (droga wylotowa)
            float azN = Mathf.DeltaAngle(az * Mathf.Rad2Deg, 0f), azW = Mathf.DeltaAngle(az * Mathf.Rad2Deg, 270f);
            h1 *= 1f - .35f * Mathf.Exp(-azW * azW / (2f * 9f * 9f));
            h1 = Mathf.Max(0f, h1) * Lad(R1, 380f);
            // grzbiet 2: wyższe pasmo 100…250 m
            float h2 = 170f + 75f * Okresowy(az, 2, 20, 21, 1.2f) + 3.5f * Okresowy(az, 100, 240, 22, 0f) + 2f * Okresowy(az, 300, 700, 23, 0f);
            h2 *= 1f - .25f * Mathf.Exp(-azN * azN / (2f * 14f * 14f));
            h2 = Mathf.Max(0f, h2) * Lad(R2, 650f);
            // skala gór ze zdjęcia: 0 nad morzem, łagodnie rośnie w głąb lądu (cyple)
            float b = Lad(R3, 1300f);
            px[i] = new Color32((byte)Mathf.Clamp(Mathf.RoundToInt(h1 / H1 * 255f), 0, 255), (byte)Mathf.Clamp(Mathf.RoundToInt(h2 / H2 * 255f), 0, 255),
                                (byte)Mathf.Clamp(Mathf.RoundToInt(b * 255f), 0, 255), 255);
        }
        var t = new Texture2D(N, 1, TextureFormat.RGBA32, false, true);
        t.SetPixels32(px); t.Apply();
        grzbietyTex = ZapiszPng(t, OutDir + "/grzbiety.png", liniowa: true, mip: false, kompresja: false, powtarzaj: true);

        // --- ziemia z daleka: działki pól, łąki, kępy lasu, miedze z zadrzewieniem (kafelkowalna) ---
        const int M = 1024; const int K = 16;                       // 16×16 działek na kafel (100 m)
        var rnd = new System.Random(4242);
        var pts = new Vector2[K, K]; var typ = new int[K, K]; var ton = new float[K, K]; var kier = new float[K, K];
        for (int a = 0; a < K; a++) for (int b2 = 0; b2 < K; b2++)
        {
            pts[a, b2] = new Vector2(a + .15f + (float)rnd.NextDouble() * .7f, b2 + .15f + (float)rnd.NextDouble() * .7f);
            double r = rnd.NextDouble();
            typ[a, b2] = r < .40 ? 0 : r < .60 ? 1 : r < .72 ? 2 : r < .78 ? 3 : r < .84 ? 4 : 5;
            ton[a, b2] = (float)rnd.NextDouble(); kier[a, b2] = (float)rnd.NextDouble() * Mathf.PI;
        }
        Color S(int r, int g, int b3) => new Color(r / 255f, g / 255f, b3 / 255f);
        var kolory = new[] { new[] { S(90, 116, 54), S(84, 110, 50), S(100, 124, 60) },      // łąka
                             new[] { S(108, 136, 58), S(116, 142, 64), S(104, 130, 56) },    // zboże zielone
                             new[] { S(138, 144, 70), S(146, 148, 76), S(130, 138, 66) },    // zboże dojrzewające
                             new[] { S(166, 152, 94), S(172, 158, 100), S(158, 146, 90) },   // rżysko / dojrzałe
                             new[] { S(114, 96, 72), S(106, 90, 68), S(120, 102, 76) },      // zaorane
                             new[] { S(46, 66, 36), S(42, 60, 34), S(52, 72, 40) } };        // las
        var pix = new Color32[M * M];
        float Szum(float x, float y, float okres, int ziarno)
        {
            // kafelkowalny szum (Perlin na torusie przez mieszanie 4 próbek)
            float u = x / M, v = y / M, o = ziarno * 17.3f;
            float A = Mathf.PerlinNoise(u * okres + o, v * okres + o), B = Mathf.PerlinNoise((u - 1) * okres + o, v * okres + o);
            float C = Mathf.PerlinNoise(u * okres + o, (v - 1) * okres + o), D = Mathf.PerlinNoise((u - 1) * okres + o, (v - 1) * okres + o);
            return Mathf.Lerp(Mathf.Lerp(A, B, u), Mathf.Lerp(C, D, u), v);
        }
        var prng = new System.Random(77);
        for (int y = 0; y < M; y++)
            for (int x = 0; x < M; x++)
            {
                float gx = (float)x / M * K, gy = (float)y / M * K;
                float d1 = 1e9f, d2 = 1e9f; int ia = 0, ib = 0;
                int cx = Mathf.FloorToInt(gx), cy = Mathf.FloorToInt(gy);
                for (int ox = -1; ox <= 1; ox++) for (int oy = -1; oy <= 1; oy++)
                {
                    int qa = ((cx + ox) % K + K) % K, qb = ((cy + oy) % K + K) % K;
                    var p = pts[qa, qb] + new Vector2((cx + ox) - qa, (cy + oy) - qb);
                    float dd = (p - new Vector2(gx, gy)).sqrMagnitude;
                    if (dd < d1) { d2 = d1; d1 = dd; ia = qa; ib = qb; } else if (dd < d2) d2 = dd;
                }
                int ty = typ[ia, ib];
                var paleta = kolory[ty];
                Color col = Color.Lerp(paleta[0], ton[ia, ib] < .5f ? paleta[1] : paleta[2], Mathf.Abs(ton[ia, ib] - .5f) * 2f);
                float las = 0f;
                // kępy lasu (niezależne od działek) — duże płaty
                float f = Szum(x, y, 5f, 1) * .65f + Szum(x, y, 13f, 2) * .35f;
                if (f > .58f) { las = S01(.58f, .62f, f); col = Color.Lerp(col, Color.Lerp(kolory[5][0], kolory[5][2], Szum(x, y, 60f, 3)), las); }
                if (ty == 5) las = 1f;
                // rzędy upraw (pasy co ~6 m w kierunku działki)
                if (ty >= 1 && ty <= 4 && las < .5f)
                {
                    float kk = kier[ia, ib]; float s = Mathf.Sin((x * Mathf.Cos(kk) + y * Mathf.Sin(kk)) * 1.05f);
                    col *= 1f + .035f * s;
                }
                // miedze z zadrzewieniem (granica działek)
                float brzeg = Mathf.Sqrt(d2) - Mathf.Sqrt(d1);
                if (brzeg < .035f && las < .5f)
                {
                    float m2 = 1f - brzeg / .035f;
                    bool drzewaNaMiedzy = Szum(x, y, 31f, 4) > .5f;
                    col = Color.Lerp(col, drzewaNaMiedzy ? S(54, 76, 40) : S(98, 112, 60), m2 * (drzewaNaMiedzy ? .85f : .5f));
                    if (drzewaNaMiedzy) las = Mathf.Max(las, m2 * .6f);
                }
                col *= .94f + (float)prng.NextDouble() * .12f;
                pix[y * M + x] = new Color32((byte)Mathf.Clamp(col.r * 255f, 0, 255), (byte)Mathf.Clamp(col.g * 255f, 0, 255), (byte)Mathf.Clamp(col.b * 255f, 0, 255), (byte)Mathf.Clamp(las * 255f, 0, 255));
            }
        var mt = new Texture2D(M, M, TextureFormat.RGBA32, false, false);
        mt.SetPixels32(pix); mt.Apply();
        makroTex = ZapiszPng(mt, OutDir + "/ziemia_makro.png", liniowa: false, mip: true, kompresja: true, powtarzaj: true);
    }

    static Texture2D ZapiszPng(Texture2D t, string path, bool liniowa, bool mip, bool kompresja, bool powtarzaj)
    {
        File.WriteAllBytes(path, t.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Default; imp.textureShape = TextureImporterShape.Texture2D;
        imp.sRGBTexture = !liniowa; imp.mipmapEnabled = mip; imp.isReadable = false; imp.npotScale = TextureImporterNPOTScale.None;
        imp.alphaSource = TextureImporterAlphaSource.FromInput; imp.alphaIsTransparency = false;
        imp.wrapMode = powtarzaj ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        imp.filterMode = mip ? FilterMode.Trilinear : FilterMode.Bilinear;
        imp.maxTextureSize = 2048;
        imp.textureCompression = kompresja ? TextureImporterCompression.Compressed : TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // Niebo HD: pełna rozdzielczość pliku (do 8192), wysoka jakość kompresji na telefon i WebGL (bez ETC2-kwadratów).
    static Texture2D NieboHD(string p)
    {
        if (!File.Exists(p)) return null;
        AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
        var imp = AssetImporter.GetAtPath(p) as TextureImporter;
        if (!imp) return null;
        imp.textureType = TextureImporterType.Default; imp.textureShape = TextureImporterShape.Texture2D; imp.sRGBTexture = true;
        imp.mipmapEnabled = true; imp.wrapModeU = TextureWrapMode.Repeat; imp.wrapModeV = TextureWrapMode.Clamp;
        imp.filterMode = FilterMode.Trilinear; imp.anisoLevel = 2; imp.npotScale = TextureImporterNPOTScale.None;
        imp.alphaSource = TextureImporterAlphaSource.None; imp.maxTextureSize = 8192;
        imp.textureCompression = TextureImporterCompression.CompressedHQ; imp.compressionQuality = 100;
        var and = imp.GetPlatformTextureSettings("Android"); and.name = "Android"; and.overridden = true; and.maxTextureSize = 8192;
        and.format = TextureImporterFormat.ASTC_5x5; and.compressionQuality = 100; imp.SetPlatformTextureSettings(and);
        var web = imp.GetPlatformTextureSettings("WebGL"); web.name = "WebGL"; web.overridden = true; web.maxTextureSize = 8192;
        web.format = TextureImporterFormat.DXT1; web.compressionQuality = 100; imp.SetPlatformTextureSettings(web);
        imp.SaveAndReimport();
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        if (t) Debug.Log("BRAXHORN_NIEBO HD: " + p + " " + t.width + "x" + t.height + " (Android ASTC 5x5, WebGL DXT1, mipmapy)");
        return t;
    }

    static Texture2D Albedo(int max, params string[] sciezki)
    {
        foreach (var p in sciezki)
        {
            if (!File.Exists(p)) continue;
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
            var imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp)
            {
                bool zmiana = !imp.mipmapEnabled || !imp.sRGBTexture || imp.wrapMode != TextureWrapMode.Repeat || imp.maxTextureSize > max || imp.textureType != TextureImporterType.Default;
                if (zmiana)
                {
                    imp.textureType = TextureImporterType.Default; imp.sRGBTexture = true; imp.mipmapEnabled = true;
                    imp.wrapMode = TextureWrapMode.Repeat; imp.filterMode = FilterMode.Trilinear; imp.anisoLevel = 4;
                    imp.maxTextureSize = Mathf.Min(imp.maxTextureSize, max); imp.textureCompression = TextureImporterCompression.Compressed;
                    imp.SaveAndReimport();
                }
            }
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            if (t) return t;
        }
        return null;
    }

    // M8 (właściciel 2.11 22:30: „w dzień nisko przy horyzoncie duża żółta tarcza z teksturą jak księżyc”): słońce ZDJĘCIA nieba
    // (Kloofendal: nisko, przepalone, JPG robi z niego pierścienie = „kratery”) — wyszukane w pliku (najjaśniejsza plama w górnej
    // połowie), w shaderze wymazane z obu warstw nieba (stałej i płynących chmur, która obracała drugą, bladą tarczę po niebie).
    // Słońce miasta: czysta tarcza 0.6° z poświatą w kierunku światła sceny (wysoko, zgodnie z cieniami). Raz na build.
    static Vector4 sloObraz; static bool sloZnane;
    static void SlonceObrazu(Material m)
    {
        var sun = BraxCityMaterials.SunDirection.normalized;
        m.SetVector("_SloTarcza", new Vector4(Mathf.Cos(.6f * Mathf.Deg2Rad), 1.6f, 0, 0));
        if (!sloZnane)
        {
            sloZnane = true; sloObraz = Vector4.zero;
            string p = nieboTex ? AssetDatabase.GetAssetPath(nieboTex) : null;
            if (string.IsNullOrEmpty(p) || !File.Exists(p)) { Debug.LogWarning("BRAXHORN_NIEBO brak pliku obrazu nieba — słońce obrazu bez maski"); }
            else
            {
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (t.LoadImage(File.ReadAllBytes(p)))
                {
                    int Wt = t.width, Ht = t.height; var px = t.GetPixels32(); float best = -1; int bi = 0, bj = 0;
                    for (int j = Ht / 2; j < Ht; j += 2) for (int i = 0; i < Wt; i += 2) { var c = px[j * Wt + i]; float L = c.r * .3f + c.g * .59f + c.b * .11f; if (L > best) { best = L; bi = i; bj = j; } }
                    // rdzeń tarczy: piksele ≥ 97 % maksimum w promieniu 10° od najjaśniejszego
                    float lonB = (.5f - (bi + .5f) / Wt) * Mathf.PI * 2f, latB = ((bj + .5f) / Ht - .5f) * Mathf.PI;
                    var dB = new Vector3(Mathf.Cos(latB) * Mathf.Cos(lonB), Mathf.Sin(latB), Mathf.Cos(latB) * Mathf.Sin(lonB));
                    Vector3 suma = Vector3.zero; float pole = 0; float cos10 = Mathf.Cos(10f * Mathf.Deg2Rad);
                    for (int j = Ht / 2; j < Ht; j++)
                    {
                        float lat = ((j + .5f) / Ht - .5f) * Mathf.PI; if (Mathf.Abs(lat - latB) > .2f) continue;
                        for (int i = 0; i < Wt; i++)
                        {
                            var c = px[j * Wt + i]; float L = c.r * .3f + c.g * .59f + c.b * .11f; if (L < best * .97f) continue;
                            float lon = (.5f - (i + .5f) / Wt) * Mathf.PI * 2f;
                            var d = new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Sin(lon));
                            if (Vector3.Dot(d, dB) < cos10) continue;
                            float a = Mathf.Cos(lat) * (2f * Mathf.PI / Wt) * (Mathf.PI / Ht); suma += d * a; pole += a;
                        }
                    }
                    var s = pole > 0 ? (suma / pole).normalized : dB;
                    float rc = Mathf.Sqrt(Mathf.Max(pole, 1e-8f) / Mathf.PI) * Mathf.Rad2Deg;   // promień rdzenia (°)
                    float R = Mathf.Clamp(rc * 2.6f, 1.6f, 7f);
                    sloObraz = new Vector4(s.x, s.y, s.z, Mathf.Cos(R * Mathf.Deg2Rad));
                    Debug.Log("BRAXHORN_NIEBO słońce obrazu: jasność " + best.ToString("F0") + "/255, elewacja " + (Mathf.Asin(s.y) * Mathf.Rad2Deg).ToString("F1") + "°, rdzeń " + rc.ToString("F2") + "°, maska " + R.ToString("F1")
                              + "° (wymazane z obu warstw nieba); słońce miasta: tarcza 0.6° bez tekstury, elewacja " + (Mathf.Asin(sun.y) * Mathf.Rad2Deg).ToString("F1") + "° (zgodnie ze światłem sceny); obraz " + Wt + "×" + Ht + " " + p);
                }
                UnityEngine.Object.DestroyImmediate(t);
            }
        }
        m.SetVector("_SloObraz", sloObraz);
    }

    // =====================================================================================================================
    // 2) niebo
    static void UstawTlo(Material m)
    {
        if (!m) return;
        m.SetTexture("_NieboTex", nieboTex);
        // słońce obrazu nieba (u ≈ 0.603, Kloofendal) ustawione na kierunek słońca miasta (CityStaticPBR._SunDirection)
        var sun = BraxCityMaterials.SunDirection.normalized;
        float lonSlonca = Mathf.Atan2(sun.z, sun.x), lonObrazu = (.5f - .603f) * Mathf.PI * 2f;
        m.SetFloat("_NieboObrot", lonSlonca - lonObrazu);
        m.SetFloat("_NieboEkspozycja", 1f); m.SetFloat("_NieboNasycenie", 1.3f); m.SetFloat("_NieboChmury", .0035f);
        m.SetVector("_SloKierunek", new Vector4(sun.x, sun.y, sun.z, 0));
        try { SlonceObrazu(m); } catch (Exception e) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_NIEBO słońce obrazu: " + e.Message); }
        var info = new PasmoInfo();
        try { if (File.Exists(PasmoJson)) JsonUtility.FromJsonOverwrite(File.ReadAllText(PasmoJson), info); } catch { }
        m.SetTexture("_Pasmo", pasmoTex); m.SetFloat("_PasmoJest", pasmoTex ? 1f : 0f);
        m.SetVector("_PasmoSrodek", new Vector4(Srodek.x, 0f, Srodek.y, 0f));
        m.SetFloat("_PasmoPromien", R3);
        // pas osiedla ma na swojej północy (u ≈ 0.27) sylwetkę NASZEGO miasta — tu obrócony tak, że ta część leży nad
        // morzem (wschód, skala 0 = niewidoczna); na południu (za placem wieżowców) ciemny masyw, na zachodzie ośnieżony
        // szczyt i zalesiona góra, na północy zalesione zbocza
        m.SetFloat("_PasmoU0", .27f - .5f + (info.przesuniecie_u - .02f));
        m.SetFloat("_PasmoGora", info.elewacja_gora); m.SetFloat("_PasmoDol", info.elewacja_dol);
        m.SetFloat("_PasmoJasnosc", .96f); m.SetFloat("_PasmoNasycenie", 1.12f); m.SetFloat("_PasmoMgla", .2f);
        m.SetTexture("_Grzbiety", grzbietyTex);
        m.SetVector("_GrzbietyR", new Vector4(R1, R2, H1, H2));
        m.SetColor("_GrzbietKolor1", new Color(.21f, .29f, .16f)); m.SetColor("_GrzbietKolor2", new Color(.22f, .29f, .19f));
        m.SetTexture("_ZiemiaMakro", makroTex); m.SetFloat("_ZiemiaMakroSkala", 1600f);
        m.SetTexture("_ZiemiaTrawa", trawaTex); m.SetFloat("_ZiemiaTrawaSkala", 3.2f);
        m.SetColor("_ZiemiaOdcien", new Color(.92f, 1f, .86f));
        m.SetVector("_Brzeg1", new Vector4(BraxMiastoGranice.BrzegPoludnie, BraxMiastoGranice.BrzegPolnoc, BraxMiastoGranice.BrzegCypelZ0, BraxMiastoGranice.BrzegCypelZ1));
        m.SetVector("_Brzeg2", new Vector4(BraxMiastoGranice.BrzegLukPolnocZ, BraxMiastoGranice.BrzegLukPoludnieZ, BraxMiastoGranice.BrzegLukNachylenie, BraxMiastoGranice.PoziomMorza));
        m.SetColor("_MorzeKolor", new Color(.06f, .19f, .24f));
        m.SetColor("_MgielkaKolor", new Color(.70f, .76f, .83f));
        m.SetFloat("_MgielkaGestosc", .00042f); m.SetFloat("_MgielkaNieboUdzial", .6f);
    }

    static void Niebo()
    {
        var stare = RenderSettings.skybox;
        // 3.11 (właściciel: „panorama w HD, 360°”, „niebo kwadratowe”): dotąd brany był obraz STAREGO skyboxa (2048×1024,
        // ETC2 — rozciągnięty 7× na ekranie telefonu = kwadraty). Teraz najpierw HD z Poly Haven (8192×4096, ASTC 5×5 /
        // DXT1 WebGL, mipmapy przez tex2Dgrad), potem 4K, stary skybox tylko gdy brak plików.
        nieboTex = NieboHD("Assets/Source/Claude/PolyHaven/sky/kloofendal_8k.jpg") ?? NieboHD("Assets/Source/Claude/PolyHaven/sky/kloofendal_4k.jpg");
        if (!nieboTex && stare && stare.HasProperty("_MainTex")) { nieboTex = stare.GetTexture("_MainTex") as Texture2D; Debug.LogWarning("DO POPRAWKI: BRAXHORN_NIEBO brak HD z Poly Haven — stary obraz nieba " + (nieboTex ? nieboTex.width + "x" + nieboTex.height : "brak")); }
        if (!nieboTex) nieboTex = Albedo(4096, "Assets/CityRebuild/Waterfront/daylight-sky.jpg", "Assets/Source/Claude/Osiedle/sky/niebo_4k.jpg");
        if (nieboTex)
        {
            var imp = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(nieboTex)) as TextureImporter;
            if (imp && (imp.wrapModeU != TextureWrapMode.Repeat || !imp.mipmapEnabled)) { imp.wrapModeU = TextureWrapMode.Repeat; imp.wrapModeV = TextureWrapMode.Clamp; imp.mipmapEnabled = true; imp.SaveAndReimport(); }
        }
        trawaTex = Albedo(1024, OsTex + "grass004/albedo.jpg", BraxRegion.PH + "textures/grass_path_2/diffuse.jpg");
        if (File.Exists(PasmoPath))
        {
            AssetDatabase.ImportAsset(PasmoPath, ImportAssetOptions.ForceSynchronousImport);
            var imp = AssetImporter.GetAtPath(PasmoPath) as TextureImporter;
            if (imp)
            {
                // jak na osiedlu (BraxClaudeOsiedleKraniec): bez mipmap (zawsze powiększone), 4096 szerokości dla nieba
                imp.textureType = TextureImporterType.Default; imp.textureShape = TextureImporterShape.Texture2D; imp.sRGBTexture = true;
                imp.mipmapEnabled = false; imp.alphaSource = TextureImporterAlphaSource.FromInput; imp.alphaIsTransparency = false;
                imp.wrapModeU = TextureWrapMode.Repeat; imp.wrapModeV = TextureWrapMode.Clamp; imp.filterMode = FilterMode.Bilinear;
                imp.maxTextureSize = 4096; imp.textureCompression = TextureImporterCompression.Compressed; imp.SaveAndReimport();
            }
            pasmoTex = AssetDatabase.LoadAssetAtPath<Texture2D>(PasmoPath);
        }
        var sh = Shader.Find("Braxhorn/MiastoNiebo");
        if (!sh) throw new InvalidOperationException("brak shadera Braxhorn/MiastoNiebo");
        nieboMat = new Material(sh) { name = "BRAXHORN CITY niebo i panorama 360° (horyzont)" };
        UstawTlo(nieboMat);
        BraxZapisAssetu.Utworz(nieboMat, OutDir + "/niebo_miasta.mat");
        RenderSettings.skybox = nieboMat;
        foreach (var cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (cam && cam.targetTexture == null && cam.clearFlags == CameraClearFlags.SolidColor && cam.CompareTag("MainCamera")) cam.clearFlags = CameraClearFlags.Skybox;
        // stare niebo (Skybox/Panoramic, dolna połowa = odbite niebo) — usunięte, żeby nic go już nie użyło
        if (stare && stare != nieboMat)
        {
            string p = AssetDatabase.GetAssetPath(stare);
            usuniete.Add("stare niebo: " + stare.name + " (" + (stare.shader ? stare.shader.name : "?") + ")" + (string.IsNullOrEmpty(p) ? "" : " " + p));
            if (!string.IsNullOrEmpty(p) && p.StartsWith("Assets/PlayableGenerated/")) AssetDatabase.DeleteAsset(p);
        }
        DynamicGI.UpdateEnvironment();
        Debug.Log("BRAXHORN_HORYZONT_MIASTO niebo: " + (nieboTex ? nieboTex.name + " " + nieboTex.width + "x" + nieboTex.height : "BRAK obrazu nieba") +
                  ", góry " + (pasmoTex ? "alps_field " + pasmoTex.width + "x" + pasmoTex.height : "BRAK (" + PasmoPath + ") — tylko grzbiety") +
                  ", grzbiety R " + R1 + "/" + R2 + " m, góry R " + R3 + " m, ziemia/morze w dolnej półkuli (bez lustra)");
    }

    // =====================================================================================================================
    // 3) teren 3D
    static float[,] odl; static float odlX0, odlZ0; const float OdlKrok = 4f; static int odlNx, odlNz;

    // pole odległości od obszaru grywalnego (siatka 4 m, przybliżenie dwuprzebiegowe 3-4)
    static void PoleOdleglosci(float x0, float x1, float z0, float z1)
    {
        odlX0 = x0; odlZ0 = z0; odlNx = Mathf.CeilToInt((x1 - x0) / OdlKrok) + 1; odlNz = Mathf.CeilToInt((z1 - z0) / OdlKrok) + 1;
        odl = new float[odlNx, odlNz];
        for (int i = 0; i < odlNx; i++) for (int j = 0; j < odlNz; j++)
            odl[i, j] = BraxMiastoGranice.Grywalny(x0 + i * OdlKrok, z0 + j * OdlKrok) ? 0f : 1e6f;
        float a = OdlKrok, b = OdlKrok * 1.41421f;
        for (int j = 0; j < odlNz; j++) for (int i = 0; i < odlNx; i++)
        {
            float v = odl[i, j];
            if (i > 0) v = Mathf.Min(v, odl[i - 1, j] + a);
            if (j > 0) v = Mathf.Min(v, odl[i, j - 1] + a);
            if (i > 0 && j > 0) v = Mathf.Min(v, odl[i - 1, j - 1] + b);
            if (i < odlNx - 1 && j > 0) v = Mathf.Min(v, odl[i + 1, j - 1] + b);
            odl[i, j] = v;
        }
        for (int j = odlNz - 1; j >= 0; j--) for (int i = odlNx - 1; i >= 0; i--)
        {
            float v = odl[i, j];
            if (i < odlNx - 1) v = Mathf.Min(v, odl[i + 1, j] + a);
            if (j < odlNz - 1) v = Mathf.Min(v, odl[i, j + 1] + a);
            if (i < odlNx - 1 && j < odlNz - 1) v = Mathf.Min(v, odl[i + 1, j + 1] + b);
            if (i > 0 && j < odlNz - 1) v = Mathf.Min(v, odl[i - 1, j + 1] + b);
            odl[i, j] = v;
        }
    }

    static float Odl(float x, float z)
    {
        if (odl == null) return BraxMiastoGranice.Odleglosc(x, z);
        float fi = (x - odlX0) / OdlKrok, fj = (z - odlZ0) / OdlKrok;
        int i = Mathf.Clamp(Mathf.FloorToInt(fi), 0, odlNx - 2), j = Mathf.Clamp(Mathf.FloorToInt(fj), 0, odlNz - 2);
        float u = Mathf.Clamp01(fi - i), v = Mathf.Clamp01(fj - j);
        float d = Mathf.Lerp(Mathf.Lerp(odl[i, j], odl[i + 1, j], u), Mathf.Lerp(odl[i, j + 1], odl[i + 1, j + 1], u), v);
        return d > 1e5f ? BraxMiastoGranice.Odleglosc(x, z) : d;
    }

    static float DoWzgorzaTylow(float x, float z)
    {
        var w = BraxMiastoGranice.WzgorzeTylow;
        float dx = Mathf.Max(0f, Mathf.Max(w.xMin - x, x - w.xMax)), dz = Mathf.Max(0f, Mathf.Max(w.yMin - z, z - w.yMax));
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    // oś wiaduktu za granicą na północ: prosto do z ≈ 300, potem łagodny łuk na zachód do portalu tunelu w zboczu
    public static readonly float WiaduktPortalZ = 452f;
    static Vector2 WiaduktOs(float z)
    {
        float x = BraxMiastoGranice.WiaduktX;
        if (z > 300f) { float t = (z - 300f); x -= t * t * .0021f; }
        return new Vector2(x, z);
    }

    // wysokość terenu za granicą
    static float Wys(float x, float z)
    {
        float d = Odl(x, z);
        float lad = BraxMiastoGranice.Brzeg(z) - x;                    // odległość od brzegu w głąb lądu
        float zach = S01(0f, 160f, BraxMiastoGranice.X0 - x), pol = S01(0f, 160f, z - BraxMiastoGranice.Z1), polud = S01(0f, 160f, BraxMiastoGranice.Z0 - z);
        // wzgórza rosnące od granicy: łąka przy granicy, dalej zbocza (zachód i północ wyżej, południowy wschód nisko — do morza)
        float rzezba = (Mathf.PerlinNoise(x / 140f + 3.1f, z / 140f + 7.7f) - .35f) * 2f;
        float drob = (Mathf.PerlinNoise(x / 41f + 11f, z / 41f + 5f) - .5f);
        float wzg = 6f * S01(15f, 90f, d) + 34f * S01(90f, 360f, d) * (.55f + .45f * rzezba) + 22f * S01(260f, 500f, d) * (zach + pol);
        float h = wzg + drob * 5f * S01(10f, 120f, d);
        // przy granicy płasko na poziomie miasta (murek / płot stoi na równym)
        h *= S01(1.5f, 26f, d);
        h -= .04f;
        // południe przy wzgórzu „tyłów”: nisko, wzgórze tamtego wykonawcy jest wyżej (pod nim mój teren 1.5 m niżej, ukryty)
        float dw = DoWzgorzaTylow(x, z);
        if (dw < 40f) h = Mathf.Min(h, Mathf.Lerp(-1.5f, h, S01(0f, 40f, dw)));
        // południowy wschód: dolina schodzi do morza (ujście potoku), mniej wzgórz
        h *= 1f - .55f * polud * S01(-80f, 40f, x);
        // (2.11, B: wiadukt usunięty — bez korytarza i przekopu na północy)
        // brzeg morza: skalisty, schodzi pod wodę (woda miasta y −1.7)
        float brzeg = -3.4f + 3.4f * S01(-14f, 10f, lad);   // na linii brzegu ok. −1.3 m (woda −1.7 zaczyna się tuż dalej)
        h = brzeg + h * S01(8f, 110f, lad);
        return h;
    }

    static Vector3 Normalna(float x, float z)
    {
        const float e = 1.5f;
        return new Vector3(Wys(x - e, z) - Wys(x + e, z), 2f * e, Wys(x, z - e) - Wys(x, z + e)).normalized;
    }

    // gęstość lasu 0…1 (płaty lasu na wzgórzach, łąka przy granicy i w dolinach)
    static float GestoscLasu(float x, float z, float d, float h)
    {
        float f = Mathf.PerlinNoise(x / 95f + 21f, z / 95f + 4f) * .7f + Mathf.PerlinNoise(x / 31f + 2f, z / 31f + 9f) * .3f;
        float wys = S01(4f, 22f, h);
        return S01(.42f, .6f, f * (.75f + .5f * wys)) * S01(22f, 60f, d);
    }

    static Color KolorWierzcholka(float x, float z, Vector3 n, float d, float h)
    {
        float lad = BraxMiastoGranice.Brzeg(z) - x;
        float r = S01(.22f, .45f, 1f - n.y);
        r = Mathf.Max(r, (1f - S01(2f, 14f, lad)) * .85f);                                   // skalisty brzeg
        if (h > 18f) r = Mathf.Max(r, S01(.68f, .78f, Mathf.PerlinNoise(x / 17f + 9.3f, z / 17f + 2.2f)) * .8f);   // wychodnie
        float g = GestoscLasu(x, z, d, h) * .75f;
        float b = 0f;
        if (d > 30f && h < 12f && n.y > .95f)
        {
            int pas = Mathf.FloorToInt((x * .8f + z * .35f) / 52f);
            float los = Mathf.Abs(Mathf.Sin(pas * 12.9898f) * 43758.5453f) % 1f;
            b = (los > .5f ? .5f + los * .5f : 0f) * S01(30f, 60f, d) * (1f - g);
        }
        return new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), 1f);
    }

    static List<float> Linie(float in0, float in1, float krokW, float minV, float maxV, (float d, float krok)[] wyjscie)
    {
        var l = new List<float>();
        var neg = new List<float>(); float dd = 0f;
        foreach (var (d, krok) in wyjscie) while (dd < d && in0 - dd > minV) { dd += krok; neg.Add(in0 - dd); }
        neg.Reverse(); l.AddRange(neg);
        for (float v = in0; v < in1 - .01f; v = Mathf.Min(in1, v + krokW)) l.Add(v);
        l.Add(in1);
        dd = 0f;
        foreach (var (d, krok) in wyjscie) while (dd < d && in1 + dd < maxV) { dd += krok; l.Add(in1 + dd); }
        return l;
    }

    sealed class Kawalek { public readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>(); public readonly List<Color> c = new List<Color>(); public readonly List<int> i = new List<int>(); public readonly Dictionary<long, int> idx = new Dictionary<long, int>(); }

    static Material TerenMat()
    {
        if (terenMat) return terenMat;
        var sh = Shader.Find("Braxhorn/MiastoDal");
        if (!sh) throw new InvalidOperationException("brak shadera Braxhorn/MiastoDal");
        var m = new Material(sh) { name = "Miasto: teren za granicą (łąki, pola, wzgórza, brzeg)" };
        UstawTlo(m);
        m.SetFloat("_Tryb", 0f);
        if (trawaTex) m.SetTexture("_MainTex", trawaTex);
        m.SetColor("_Color", new Color(.9f, 1f, .86f)); m.SetFloat("_Tile", 3.2f);
        var skala = Albedo(2048, OsTex + "rock_boulder_dry/albedo.jpg", BraxRegion.PH + "textures/rock_face_03/diffuse.jpg", BraxRegion.PH + "textures/rock_wall_08/diffuse.jpg");
        if (skala) m.SetTexture("_RockTex", skala);
        m.SetFloat("_RockTile", 4.5f);
        var ziemia = Albedo(2048, OsTex + "brown_mud_leaves_01/albedo.jpg", BraxRegion.PH + "textures/brown_mud_leaves_01/diffuse.jpg");
        if (ziemia) m.SetTexture("_SoilTex", ziemia);
        m.SetFloat("_MglaOd", .6f); m.SetFloat("_MglaDo", 470f);
        BraxZapisAssetu.Utworz(m, OutDir + "/teren_za_granica.mat");
        return terenMat = m;
    }

    static void Teren()
    {
        var G = BraxMiastoGranice.Prostokat;
        float xMin = G.xMin - Zasieg, zMin = G.yMin - Zasieg, zMax = G.yMax + Zasieg;
        float xMax = Mathf.Max(BraxMiastoGranice.Brzeg(zMin), BraxMiastoGranice.Brzeg(zMax)) + 30f;
        PoleOdleglosci(xMin - 8f, xMax + 8f, zMin - 8f, zMax + 8f);
        var m = TerenMat();
        var wyjscie = new[] { (40f, 4f), (110f, 6f), (220f, 11f), (360f, 18f), (Zasieg + 10f, 28f) };
        var xs = Linie(G.xMin, G.xMax, 8f, xMin, xMax, wyjscie);
        var zs = Linie(G.yMin, G.yMax, 8f, zMin, zMax, wyjscie);
        // wycięcia: gęstsze linie wzdłuż ich krawędzi
        foreach (var w in BraxMiastoGranice.Wyciecia)
        {
            for (float v = w.xMin; v <= w.xMax; v += 4f) xs.Add(v);
            for (float v = w.yMin; v <= w.yMax; v += 4f) zs.Add(v);
        }
        xs = xs.Where(v => v <= xMax).Distinct().OrderBy(v => v).ToList(); zs = zs.Distinct().OrderBy(v => v).ToList();
        const float Kaw = 220f;
        var kaw = new Dictionary<(int, int), Kawalek>();
        int V(Kawalek s, int ix, int iz)
        {
            long key = ((long)ix << 32) | (uint)iz;
            if (s.idx.TryGetValue(key, out var k)) return k;
            float x = xs[ix], z = zs[iz];
            float h = Wys(x, z); var nn = Normalna(x, z);
            k = s.v.Count; s.v.Add(new Vector3(x, h, z)); s.n.Add(nn); s.c.Add(KolorWierzcholka(x, z, nn, Odl(x, z), h)); s.idx[key] = k; return k;
        }
        for (int ix = 0; ix < xs.Count - 1; ix++)
            for (int iz = 0; iz < zs.Count - 1; iz++)
            {
                float cx = (xs[ix] + xs[ix + 1]) * .5f, cz = (zs[iz] + zs[iz + 1]) * .5f;
                if (BraxMiastoGranice.Grywalny(cx, cz)) continue;                                   // miasto
                if (BraxMiastoGranice.Brzeg(cz) - cx < -16f) continue;                             // morze
                if (Odl(cx, cz) > Zasieg + 20f) continue;
                var key = (Mathf.FloorToInt(cx / Kaw), Mathf.FloorToInt(cz / Kaw));
                if (!kaw.TryGetValue(key, out var s)) kaw[key] = s = new Kawalek();
                int a = V(s, ix, iz), b = V(s, ix + 1, iz), c = V(s, ix + 1, iz + 1), dd = V(s, ix, iz + 1);
                s.i.AddRange(new[] { a, dd, c, a, c, b }); polaTerenu++;
            }
        var tr = new GameObject("Teren za granicą").transform; tr.SetParent(root, false);
        foreach (var kv in kaw.OrderBy(k => k.Key.Item1).ThenBy(k => k.Key.Item2))
            Siatka(tr, "teren " + kv.Key.Item1 + "_" + kv.Key.Item2, kv.Value.v, kv.Value.n, kv.Value.c, kv.Value.i, null, null, m, null);
        Debug.Log("BRAXHORN_HORYZONT_MIASTO teren: pól " + polaTerenu + ", kawałków " + kaw.Count + ", linie " + xs.Count + "×" + zs.Count +
                  ", zasięg " + Zasieg + " m od granicy, bez kolizji (za granicą), brzeg skalisty do wody, wzgórze tyłów nietknięte");
    }

    static GameObject Siatka(Transform parent, string nazwa, List<Vector3> v, List<Vector3> n, List<Color> c, List<int> i, List<Vector2> uv, List<Vector2> uv2, Material m, Bounds? granice)
    {
        if (v.Count == 0 || !m) return null;
        var mesh = new Mesh { name = "Horyzont " + nazwa, indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        mesh.SetVertices(v); if (n != null) mesh.SetNormals(n); if (c != null) mesh.SetColors(c);
        if (uv != null) mesh.SetUVs(0, uv); if (uv2 != null) mesh.SetUVs(1, uv2);
        mesh.SetTriangles(i, 0);
        if (n == null) mesh.RecalculateNormals();
        mesh.RecalculateBounds(); if (granice.HasValue) mesh.bounds = granice.Value;
        BraxZapisAssetu.Utworz(mesh, OutDir + "/siatka_" + (siatki++).ToString("D3") + ".asset");
        var g = new GameObject("Horyzont: " + nazwa); g.transform.SetParent(parent, false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
        r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        return g;
    }

    // =====================================================================================================================
    // 4) las
    static void Las()
    {
        if (odl == null) { var G = BraxMiastoGranice.Prostokat; PoleOdleglosci(G.xMin - Zasieg - 8f, 400f, G.yMin - Zasieg - 8f, G.yMax + Zasieg + 8f); }
        var lasPng = OsMdl + "drzewo_klaster/las.png";
        var imp = AssetImporter.GetAtPath(lasPng) as TextureImporter;
        var sh = Shader.Find("Braxhorn/MiastoDalLas");
        var rnd = new System.Random(9090);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        var lasy = new GameObject("Las za granicą").transform; lasy.SetParent(root, false);
        if (imp && sh)
        {
            imp.textureType = TextureImporterType.Default; imp.sRGBTexture = true; imp.alphaSource = TextureImporterAlphaSource.FromInput; imp.alphaIsTransparency = true;
            imp.mipmapEnabled = true; imp.mipMapsPreserveCoverage = true; imp.alphaTestReferenceValue = .45f; imp.wrapMode = TextureWrapMode.Clamp;
            imp.maxTextureSize = 2048; imp.textureCompression = TextureImporterCompression.Compressed; imp.SaveAndReimport();
            var m = new Material(sh) { name = "Miasto: las za granicą (karty drzew)" };
            m.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(lasPng)); m.SetFloat("_Cutoff", .45f); m.SetFloat("_Jasnosc", .92f);
            UstawTlo(m); m.SetFloat("_MglaOd", .6f); m.SetFloat("_MglaDo", 470f);
            BraxZapisAssetu.Utworz(m, OutDir + "/las_karty.mat"); lasMat = m;
            var kaw = new Dictionary<(int, int), (List<Vector3> v, List<Vector2> uv, List<Vector2> uv2, List<int> i)>();
            var G = BraxMiastoGranice.Prostokat;
            for (int k = 0; k < 60000 && karty < 2600; k++)
            {
                float x = R(G.xMin - Zasieg, 360f), z = R(G.yMin - Zasieg, G.yMax + Zasieg);
                if (BraxMiastoGranice.Grywalny(x, z)) continue;
                float d = Odl(x, z); if (d < 26f || d > Zasieg - 30f) continue;
                if (BraxMiastoGranice.Brzeg(z) - x < 20f) continue;
                if (DoWzgorzaTylow(x, z) < 6f) continue;
                if (z > BraxMiastoGranice.Z1 - 10f && Mathf.Abs(x - WiaduktOs(Mathf.Min(z, WiaduktPortalZ)).x) < 16f && z < WiaduktPortalZ + 10f) continue;   // pod wiaduktem
                float h = Wys(x, z);
                if (rnd.NextDouble() > GestoscLasu(x, z, d, h)) continue;
                if (Normalna(x, z).y < .8f) continue;
                float wys = R(13f, 20f);
                var key = (Mathf.FloorToInt(x / 260f), Mathf.FloorToInt(z / 260f));
                if (!kaw.TryGetValue(key, out var s)) kaw[key] = s = (new List<Vector3>(), new List<Vector2>(), new List<Vector2>(), new List<int>());
                var baza = new Vector3(x, h - .35f, z);
                int war = rnd.Next(4); float u0 = war * .25f, u1 = u0 + .25f, w = wys * R(.8f, 1f);
                int q = s.v.Count;
                s.v.AddRange(new[] { baza, baza, baza, baza });
                s.uv.AddRange(new[] { new Vector2(u0, 0), new Vector2(u1, 0), new Vector2(u1, 1), new Vector2(u0, 1) });
                s.uv2.AddRange(new[] { new Vector2(-w * .5f, 0), new Vector2(w * .5f, 0), new Vector2(w * .5f, wys), new Vector2(-w * .5f, wys) });
                s.i.AddRange(new[] { q, q + 2, q + 1, q, q + 3, q + 2 });
                karty++;
            }
            foreach (var kv in kaw)
            {
                var b = new Bounds(); bool first = true;
                foreach (var p in kv.Value.v) { if (first) { b = new Bounds(p, Vector3.zero); first = false; } else b.Encapsulate(p); }
                b.Expand(new Vector3(24f, 44f, 24f)); b.center += Vector3.up * 10f;
                Siatka(lasy, "las " + kv.Key.Item1 + "_" + kv.Key.Item2, kv.Value.v, Enumerable.Repeat(Vector3.up, kv.Value.v.Count).ToList(), null, kv.Value.i, kv.Value.uv, kv.Value.uv2, m, b);
            }
        }
        else Debug.Log("BRAXHORN_HORYZONT_MIASTO las: brak las.png (" + lasPng + ") albo shadera — tylko drzewa 3D");
        // prawdziwe drzewa przy granicy (6…24 m za płotem: z bliska karta byłaby płaska)
        var model = BraxRegion.Model("drzewo_verdant");
        if (model)
        {
            var p3 = new GameObject("Drzewa przy granicy").transform; p3.SetParent(lasy, false);
            var G = BraxMiastoGranice.Prostokat;
            for (int k = 0; k < 9000 && drzewa3D < 70; k++)
            {
                float x = R(G.xMin - 30f, G.xMax + 30f), z = R(G.yMin - 30f, G.yMax + 30f);
                if (BraxMiastoGranice.Grywalny(x, z)) continue;
                float d = Odl(x, z); if (d < 7f || d > 26f) continue;
                if (BraxMiastoGranice.Brzeg(z) - x < 14f || DoWzgorzaTylow(x, z) < 10f) continue;
                if (Mathf.Abs(x - BraxMiastoGranice.WiaduktX) < 14f) continue;
                if (drzewa.Any(t => (t.x - x) * (t.x - x) + (t.z - z) * (t.z - z) < 7.5f * 7.5f)) continue;
                if (Mathf.PerlinNoise(x / 37f + 5f, z / 37f + 1f) < .38f) continue;   // przerwy (widok na las dalej)
                var o = BraxRegion.Put(p3, model, "Drzewo za granicą", new Vector3(x, Wys(x, z) - .1f, z), R(0, 360), R(.95f, 1.25f), false, 0f, .05f);
                if (!o) continue;
                foreach (var rr in o.GetComponentsInChildren<Renderer>(true)) rr.shadowCastingMode = ShadowCastingMode.Off;
                drzewa.Add(new Vector3(x, 0, z)); drzewa3D++;
            }
        }
        Debug.Log("BRAXHORN_HORYZONT_MIASTO las: kart " + karty + " (2 trójkąty), drzew 3D przy granicy " + drzewa3D);
    }

    // =====================================================================================================================
    // 5) granica grywalna z kolizją
    sealed class Bryly { public readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>(); public readonly List<Vector2> uv = new List<Vector2>(); public readonly List<int> i = new List<int>(); }

    static void Prostopadloscian(Bryly b, Vector3 srodek, Vector3 rozm, Quaternion obrot)
    {
        var e = rozm * .5f;
        Vector3[] kier = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        foreach (var f in kier)
        {
            Vector3 u = Mathf.Abs(f.y) > .5f ? Vector3.right : Vector3.up, w = Vector3.Cross(f, u);
            Vector3 c = Vector3.Scale(f, e);
            Vector3 su = Vector3.Scale(u, e), sw = Vector3.Scale(w, e);
            int k = b.v.Count;
            var pts = new[] { c - su - sw, c + su - sw, c + su + sw, c - su + sw };
            foreach (var p in pts) { b.v.Add(srodek + obrot * p); b.n.Add(obrot * f); }
            // UV w metrach (tekstura w skali świata)
            float lu = (Vector3.Scale(u, rozm)).magnitude, lw = (Vector3.Scale(w, rozm)).magnitude;
            b.uv.AddRange(new[] { new Vector2(0, 0), new Vector2(lu, 0), new Vector2(lu, lw), new Vector2(0, lw) });
            b.i.AddRange(new[] { k, k + 2, k + 1, k, k + 3, k + 2 });
        }
    }

    static GameObject Zapisz(Transform parent, string nazwa, Bryly b, Material m)
    {
        if (b.v.Count == 0 || !m) return null;
        var mesh = new Mesh { name = "Horyzont " + nazwa, indexFormat = b.v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        mesh.SetVertices(b.v); mesh.SetNormals(b.n); mesh.SetUVs(0, b.uv); mesh.SetTriangles(b.i, 0); mesh.RecalculateBounds(); mesh.RecalculateTangents();
        BraxZapisAssetu.Utworz(mesh, OutDir + "/siatka_" + (siatki++).ToString("D3") + ".asset");
        var g = new GameObject(nazwa); g.transform.SetParent(parent, false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
        r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true;
        GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic);
        return g;
    }

    static Material MetrowyPbr(string nazwa, float metry, Color? odcien, params string[] slugi)
    {
        // materiał miasta z teksturą Poly Haven; UV siatek granicy są w metrach, więc skala = 1/metry
        try { var m = BraxRegion.Pbr(nazwa, metry, odcien, slugi); return m; }
        catch (Exception) { return null; }
    }

    static void Granica()
    {
        float K = BraxClaudeOsiedle.K;
        var G = BraxMiastoGranice.Prostokat;
        // 1) krawędzie obszaru grywalnego (siatka 1 m): między polem grywalnym a niegrywalnym LĄDEM
        const float c = 1f;
        float gx0 = Mathf.Floor(G.xMin) - 3f, gz0 = Mathf.Floor(G.yMin) - 3f;
        int nx = Mathf.CeilToInt(G.width) + 6, nz = Mathf.CeilToInt(G.height) + 6;
        var gr = new bool[nx, nz];
        for (int i = 0; i < nx; i++) for (int j = 0; j < nz; j++) gr[i, j] = BraxMiastoGranice.Grywalny(gx0 + (i + .5f) * c, gz0 + (j + .5f) * c);
        bool LadZaGranica(float x, float z) => BraxMiastoGranice.Brzeg(z) - x > 1.5f;
        // odcinki: (start, koniec, normalna na zewnątrz)
        var odc = new List<(Vector2 a, Vector2 b, Vector2 n)>();
        // pionowe krawędzie (między i-1 a i)
        for (int i = 1; i < nx; i++)
        {
            int j = 0;
            while (j < nz)
            {
                int typ = gr[i - 1, j] == gr[i, j] ? 0 : gr[i - 1, j] ? 1 : -1;
                float xe = gx0 + i * c;
                bool lad = typ != 0 && LadZaGranica(xe + typ * 2f, gz0 + (j + .5f) * c);
                if (typ == 0 || !lad) { j++; continue; }
                int j0 = j;
                while (j < nz && (gr[i - 1, j] == gr[i, j] ? 0 : gr[i - 1, j] ? 1 : -1) == typ && LadZaGranica(xe + typ * 2f, gz0 + (j + .5f) * c)) j++;
                odc.Add((new Vector2(xe, gz0 + j0 * c), new Vector2(xe, gz0 + j * c), new Vector2(typ, 0)));
            }
        }
        for (int j = 1; j < nz; j++)
        {
            int i = 0;
            while (i < nx)
            {
                int typ = gr[i, j - 1] == gr[i, j] ? 0 : gr[i, j - 1] ? 1 : -1;
                float ze = gz0 + j * c;
                bool lad = typ != 0 && LadZaGranica(gx0 + (i + .5f) * c, ze + typ * 2f);
                if (typ == 0 || !lad) { i++; continue; }
                int i0 = i;
                while (i < nx && (gr[i, j - 1] == gr[i, j] ? 0 : gr[i, j - 1] ? 1 : -1) == typ && LadZaGranica(gx0 + (i + .5f) * c, ze + typ * 2f)) i++;
                odc.Add((new Vector2(gx0 + i0 * c, ze), new Vector2(gx0 + i * c, ze), new Vector2(0, typ)));
            }
        }
        // portal tunelu wyjazdu na osiedle (BraxRegionMiastoB: mur portalu x −148…−104 na z = −84…−85.4) — tam granicę robi mur portalu
        var portal = BraxMiastoGranice.PortalTunelu;
        var gp = new GameObject("Granica miasta (murek z balustradą, płot lasu)").transform; gp.SetParent(root, false);
        var kamien = MetrowyPbr("Granica: kamień murku", 1f, null, "rough_block_wall", "rock_wall_08", "concrete_wall_008") ?? BraxCityMaterials.Surface("Granica: kamień", new Color(.58f, .56f, .52f), .9f);
        var stal = BraxRegion.Fitting("Granica: stal balustrady (grafit)", new Color(.16f, .17f, .18f), .45f, .75f);
        var drewno = MetrowyPbr("Granica: drewno płotu", 1f, new Color(.78f, .7f, .6f), "weathered_planks", "wood_planks_grey", "wooden_planks") ?? BraxCityMaterials.Surface("Granica: drewno", new Color(.45f, .36f, .27f), .85f);
        var murek = new Bryly(); var bal = new Bryly(); var plot = new Bryly();
        var kol = new GameObject("Granica: kolizja").transform; kol.SetParent(gp, false);
        foreach (var (a0, b0, n) in odc)
        {
            // odcinek pocięty przez portal
            var czesci = new List<(Vector2, Vector2)>();
            if (Mathf.Abs(n.y) > .5f && a0.y > portal.yMin && a0.y < portal.yMax)
            {
                float xa = Mathf.Min(a0.x, b0.x), xb = Mathf.Max(a0.x, b0.x);
                if (xa < portal.xMin) czesci.Add((new Vector2(xa, a0.y), new Vector2(Mathf.Min(xb, portal.xMin), a0.y)));
                if (xb > portal.xMax) czesci.Add((new Vector2(Mathf.Max(xa, portal.xMax), a0.y), new Vector2(xb, a0.y)));
            }
            else czesci.Add((a0, b0));
            foreach (var (a, b) in czesci)
            {
                float L = Vector2.Distance(a, b); if (L < .5f) continue;
                var dir = (b - a) / L; var mid = (a + b) * .5f;
                // linia granicy 0.3 m na zewnątrz krawędzi (płot nie wchodzi w chodnik)
                var o = mid + n * .3f;
                bool lesny = n.y > .5f || BraxMiastoGranice.Wyciecia.Any(w => w.Contains(mid + n * 3f));   // północ i wycięcia: płot lasu
                var rot = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.y), Vector3.up);
                float y0 = Mathf.Max(0f, Wys(o.x + n.x * 1.5f, o.y + n.y * 1.5f));
                if (!lesny)
                {
                    // kamienny murek 0.55 m (×k) z czapą + balustrada stalowa do 1.1 m × k (słupki co 1.6 m, pochwyt, 2 przeciągi)
                    float hm = .55f * K, hb = 1.1f * K;
                    Prostopadloscian(murek, new Vector3(o.x, y0 + hm * .5f - .05f, o.y), new Vector3(.42f * K, hm + .1f, L + .42f * K), rot);
                    Prostopadloscian(murek, new Vector3(o.x, y0 + hm + .03f, o.y), new Vector3(.5f * K, .06f, L + .5f * K), rot);
                    int sl = Mathf.Max(1, Mathf.RoundToInt(L / (1.6f * K)));
                    for (int s = 0; s <= sl; s++)
                    {
                        var p = a + dir * (L * s / sl) + n * .3f;
                        Prostopadloscian(bal, new Vector3(p.x, y0 + hm + (hb - hm) * .5f, p.y), new Vector3(.05f * K, hb - hm, .05f * K), rot);
                    }
                    Prostopadloscian(bal, new Vector3(o.x, y0 + hb, o.y), new Vector3(.06f * K, .05f * K, L), rot);
                    foreach (float f in new[] { .38f, .72f }) Prostopadloscian(bal, new Vector3(o.x, y0 + hm + (hb - hm) * f, o.y), new Vector3(.022f * K, .022f * K, L), rot);
                }
                else
                {
                    // drewniany płot lasu: słupy co 2.4 m × k, 3 żerdzie, wys. 1.25 m × k
                    float hp = 1.25f * K;
                    int sl = Mathf.Max(1, Mathf.RoundToInt(L / (2.4f * K)));
                    for (int s = 0; s <= sl; s++)
                    {
                        var p = a + dir * (L * s / sl) + n * .3f;
                        float ys = Mathf.Max(0f, Wys(p.x + n.x * 1.5f, p.y + n.y * 1.5f));
                        Prostopadloscian(plot, new Vector3(p.x, ys + hp * .5f - .1f, p.y), new Vector3(.13f * K, hp + .2f, .13f * K), rot * Quaternion.Euler(0, (s * 37 % 7) - 3f, 0));
                    }
                    foreach (float f in new[] { .3f, .62f, .94f }) Prostopadloscian(plot, new Vector3(o.x, y0 + hp * f, o.y), new Vector3(.07f * K, .1f * K, L), rot);
                }
                // kolizja: wzdłuż całego odcinka, 3 m wysokości (nie da się przeskoczyć), dokładnie w linii płotu / balustrady
                var bc = new GameObject("Granica: kolizja odcinka").AddComponent<BoxCollider>();
                bc.transform.SetParent(kol, false); bc.transform.position = new Vector3(o.x, y0 + 1.5f, o.y); bc.transform.rotation = rot;
                bc.size = new Vector3(.5f, 3f, L + .5f);
                odcinkiGranicy++; dlugoscGranicy += L;
            }
        }
        Zapisz(gp, "Granica: murek kamienny", murek, kamien);
        Zapisz(gp, "Granica: balustrada stalowa", bal, stal);
        Zapisz(gp, "Granica: płot lasu (drewno)", plot, drewno);
        Debug.Log("BRAXHORN_HORYZONT_MIASTO granica: odcinków " + odcinkiGranicy + ", " + dlugoscGranicy.ToString("0") + " m (murek z balustradą: zachód/południe, płot lasu: północ/wycięcia), " +
                  "kolizja 3 m w linii ogrodzenia, przerwa na portal tunelu (mur portalu, BraxMiastoGranice.PortalTunelu), od strony morza granicą jest woda");
    }

    // =====================================================================================================================
    // 6) wiadukt dalej na północ
    static void WiaduktDalej()
    {
        // (commit 2)
    }
}
