using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// MIASTO R1 — OSIEDLE BYDGOSKIE przy M. Skłodowskiej-Curie (23.11.2026, od nowa po poprzednim wykonawcy).
/// Właściciel 14:46–15:38: „zobacz, na czym stanęły prace… po naszemu”, „bierz mapę, budynki od nowa”, „nic nie rób na oko”,
/// wzór 32a przyjęty: „zrób wszystko podobne, cały teren”.
///
/// Geometrię liczy unity-tools/claude/osiedle_bdg/osiedle.py (wieżowce 28a–34a, bloki 28–38, garaże, bar mleczny z muralem
/// Zawiszy, pawilony, teren z OSM) i zapisuje do gen/osiedle.bin (BYD2: nagłówek JSON z paletą + siatki budynek × materiał,
/// metry). Ten plik tylko wczytuje, przelicza metry na jednostki gry (× K, punkt zerowy (−145,9; 508,8)) i daje materiały
/// z palety: tekstury z listy właściciela (Poly Haven, ZASADY 5.8) z barwą docelową, naklejki (graffiti, napisy, mural) jako
/// tekstury wypieczone na UV2, okna — shader Braxhorn/BydgoszczOkno (pokój za szybą z UV2), trawa — trawa rejonu.
///
/// WŁĄCZONE 23.11 wieczorem (właściciel 19:50–19:56: teren i infrastruktura, dwa wjazdy z miasta, plan przyjęty): granica
/// północna miasta odsunięta za osiedle (BraxMiastoGranice.Z1), pas lasu i wjazdy są w siatce osiedla (z ≥ 162 j.).
/// Stare supermarkety miasta usunięte — wnętrze sklepu stoi w markecie osiedla (SklepFront, BraxSupermarket.WOsiedlu).
/// Trawa 3D: z maski trawnika (gen/osiedle_trawa_maska.bin → BraxClaudeTrawaMaska), nie z limitu kęp miasta.
///
/// 24.11 (właściciel: „najpierw Stary Rynek”, potem jeden build): STARY RYNEK, Mostowa, Focha, spichrze i Brda — drugi plik
/// BYD2 z unity-tools/claude/rynek_bdg/rynek.py (gen/rynek.bin, punkty, maska trawy), ten sam wczytywacz i paleta. Punkt
/// zerowy Rynku (−328,5; 1384,3): okno x −872…−121,5 (granica miasta X0), z 897…1591 — od szwu z osiedlem (ulica wzdłuż
/// szwu i łączniki do dróg osiedla są w siatce Rynku) do nowej północnej granicy (BraxMiastoGranice.Z1). Figura linoskoczka
/// nad Brdą z paczki szyfrowanej braxhorn-sklep-bydgoszcz-linoskoczek-01 (model właściciela, 15 tys. trójkątów).
/// </summary>
public static class BraxBydgoszcz
{
    public static readonly bool Wlaczone = true;
    // Hala sklepu (BraxSupermarket, 28 × 20 m × K) w markecie przy Skłodowskiej: środek frontu hali 4,8 m za najdłuższą
    // elewacją od parkingu, na osi drzwi w witrynie (osiedle_bdg/ogolny.py: „otwor”), posadzka marketu +0,12 m × K.
    // Wartości liczy test_r1_bydgoszcz_32a.py (test_sklep_w_markecie) z obrysu OSM — przy zmianie danych test pokaże nowe.
    public static readonly Vector3 SklepFront = new Vector3(-391.86f, .12f * 1.294f + .03f, 510.27f);
    public const float SklepYaw = 165.1f;
    public static readonly Vector2 PunktZerowy = new Vector2(-145.9f, 508.8f);
    public static readonly Vector2 PunktZerowyRynku = new Vector2(-328.5f, 1384.3f);   // rynek_bdg/dane/rynek_rzut.json: gra_a
    const string Plik = "unity-tools/claude/osiedle_bdg/gen/osiedle.bin";
    const string PlikRynku = "unity-tools/claude/rynek_bdg/gen/rynek.bin";
    const string WodaPortu = "Assets/PlayableGenerated/CityRebuildWater/HarbourWater.mat";
    const string Naklejki = "Assets/Source/Claude/Bydgoszcz/";
    const string Prywatne = "Assets/Source/Prywatne/Miasto/bydgoszcz_mural/";
    const string Pokoje = "Assets/Source/Claude/Osiedle/detail/pokoje.png";
    static float K => BraxClaudeOsiedle.K;
    static string Out => BraxRegion.Out + "/Bydgoszcz";

    [Serializable] class Mat { public string tex; public float m; public float[] kol; public float[] tint; public float szorst; public float metal; public string nakl; public string alfa; public bool okno; public string gra; public string woda; public float emisja; }

    static string Sciezka(string plik = Plik)
    {
        foreach (var p in new[] { plik, "../" + plik }) if (File.Exists(p)) return p;
        return null;
    }

    /// <summary>Plik obok pliku BYD2 (punkty, akcje, maska trawy): gen/&lt;przedrostek&gt;_&lt;nazwa&gt;.</summary>
    static string Obok(string bin, string przedrostek, string nazwa) => Path.Combine(Path.GetDirectoryName(bin) ?? "", przedrostek + "_" + nazwa);

    static Material rzeka; static bool rzekaProba;

    /// <summary>Brda (24.11): kopia materiału wody portu (HarbourWater — fale, odbicia nieba) z małą falą i stałą głębokością
    /// 2,2 m (bez mapy dna zatoki, która rzeki nie obejmuje), dno mułowe. Bez wody portu — ciemna gładka tafla z palety.</summary>
    static Material Rzeka(string nazwa)
    {
        if (rzekaProba) return rzeka; rzekaProba = true;
        var baza = AssetDatabase.LoadAssetAtPath<Material>(WodaPortu);
        if (!baza) { BraxRegion.Note("Bydgoszcz: brak " + WodaPortu + " — Brda jako gładka tafla z palety"); return null; }
        var m = new Material(baza) { name = "Bydgoszcz: " + nazwa + " (woda rzeki)" };
        if (m.HasProperty("_SwellStrength")) m.SetFloat("_SwellStrength", .12f);
        if (m.HasProperty("_DnoTex")) m.SetTexture("_DnoTex", null);          // domyślna „white” = głębokość _DnoMax wszędzie
        if (m.HasProperty("_DnoMax")) m.SetFloat("_DnoMax", 2.2f * K);
        if (m.HasProperty("_DnoKolor")) m.SetColor("_DnoKolor", new Color(.42f, .38f, .30f, 1f));
        BraxZapisAssetu.Utworz(m, Out + "/brda_woda.mat");
        return rzeka = m;
    }

    static string Tekst(BinaryReader r) { int n = r.ReadInt32(); return Encoding.UTF8.GetString(r.ReadBytes(n)); }

    static Texture2D Obraz(string sciezka, bool srgb = true)
    {
        if (!File.Exists(sciezka)) return null;
        var t = BraxRegion.TeksturaRaz(sciezka);
        var imp = AssetImporter.GetAtPath(sciezka) as TextureImporter;
        if (imp && (imp.sRGBTexture != srgb || imp.maxTextureSize > 2048))
        {
            imp.sRGBTexture = srgb; imp.maxTextureSize = 2048; imp.mipmapEnabled = true; imp.alphaIsTransparency = true;
            imp.textureCompression = TextureImporterCompression.Compressed; imp.SaveAndReimport();
            t = AssetDatabase.LoadAssetAtPath<Texture2D>(sciezka);
        }
        return t;
    }

    /// <summary>Wartości z pliku są w sRGB (0..1, mogą przekraczać 1 — tekstura ciemniejsza od barwy docelowej).</summary>
    static Color C(float[] v) => v == null || v.Length < 3 ? Color.white : new Color(v[0], v[1], v[2], 1f);

    static readonly Dictionary<string, Material> okna = new Dictionary<string, Material>();

    static Material Okno(bool meble)
    {
        string klucz = meble ? "mieszkanie" : "klatka";
        if (okna.TryGetValue(klucz, out var m) && m) return m;
        var sh = Shader.Find("Braxhorn/BydgoszczOkno");
        var atlas = File.Exists(Pokoje) ? AssetDatabase.LoadAssetAtPath<Texture2D>(Pokoje) : null;
        if (!sh || !atlas)
        {
            BraxRegion.Note("Bydgoszcz: brak shadera okna lub atlasu pokoi — szyba przyciemniana");
            m = BraxRegionMeble.Gladki("Bydgoszcz: szyba (bez pokoju)", new Color(.10f, .13f, .15f), .1f, .5f);
        }
        else
        {
            m = new Material(sh) { name = "Bydgoszcz: okno " + klucz };
            m.SetTexture("_Rooms", atlas); m.SetFloat("_Meble", meble ? 1f : 0f);
            m.SetFloat("_CellW", (meble ? 2.6f : 1.8f) * K); m.SetFloat("_CellH", 2.7f * K); m.SetFloat("_Depth", 3.6f * K);
            m.enableInstancing = true;
            BraxZapisAssetu.Utworz(m, Out + "/okno_" + klucz + ".mat");
        }
        okna[klucz] = m; return m;
    }

    static Material Material(string nazwa, Mat d)
    {
        if (d.okno || nazwa == "okno" || nazwa == "okno_klatka") return Okno(nazwa == "okno");
        if (d.gra == "BraxRegionTrawa") return BraxRegionTrawa.Material;
        if (!string.IsNullOrEmpty(d.woda)) { var w = Rzeka(nazwa); if (w) return w; }
        string n = "Bydgoszcz: " + nazwa;
        if (!string.IsNullOrEmpty(d.nakl))
        {
            // naklejka wypieczona na barwie ściany (osiedle_bdg/tekstury_32a.py: wypiecz), na UV2; relief tynku z UV1
            string plik = (nazwa == "mural_zawisza" ? Prywatne : Naklejki) + nazwa + ".png";
            var t = Obraz(plik);
            if (!t)
            {
                BraxRegion.Note("Bydgoszcz: brak naklejki " + plik + " — ściana w barwie tynku");
                return BraxRegionMeble.Gladki(n, C(d.kol == null ? null : new[] { d.kol[0] / 255f, d.kol[1] / 255f, d.kol[2] / 255f }), .95f);
            }
            var m = BraxCityMaterials.Textured(n, t, null, null, 1f);
            m.SetFloat("_MainTex_texCoord", 1f); m.SetTextureScale("_MainTex", Vector2.one);
            m.SetFloat("_Roughness", d.szorst); m.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }
        if (!string.IsNullOrEmpty(d.alfa))
        {
            var t = Obraz(Naklejki + d.alfa);
            if (t)
            {
                var m = BraxCityMaterials.Textured(n, t, null, null, 1f / K);
                m.SetFloat("_Cutoff", .5f); m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cull", 0f);
                m.renderQueue = (int)RenderQueue.AlphaTest; m.SetOverrideTag("RenderType", "TransparentCutout");
                m.SetFloat("_Roughness", d.szorst); m.SetFloat("_Metallic", d.metal); m.SetColor("_Color", C(d.tint));
                EditorUtility.SetDirty(m);
                return m;
            }
        }
        // 24.11: świecące diody (balkon z DJ-em na Rynku) — sama emisja, bez światła czasu rzeczywistego; pulsuje BraxPulsSwiatla
        if (d.emisja > 0f) return BraxRegionMeble.Swiecacy(n, C(d.tint), d.emisja);
        if (!string.IsNullOrEmpty(d.tex))
            return BraxRegionMeble.Tex(n, d.m, C(d.tint), d.szorst, d.tex);
        return BraxRegionMeble.Gladki(n, C(d.tint), d.szorst, d.metal);
    }

    static bool Kolizja(string mat) =>
        !(mat.StartsWith("okno") || mat.StartsWith("balustrada") || mat == "pcv" || mat == "opaska" || mat == "parapet" || mat == "stal" ||
          mat == "stal_balustrada" || mat == "rura_gaz" || mat == "linia_biala" || mat == "lampa" || mat == "tabliczka" || mat == "domofon" ||
          mat == "kratka" || mat == "obrobka" || mat == "szyba_drzwi" || mat == "szyba_piwnica" || mat.StartsWith("dj_"));

    /// <summary>Folder zasobów z rodzicami (AssetDatabase.CreateFolder + dysk). Build 45304516914 (24.11): CreateAsset w nieistniejącym
    /// ClaudeRegion/Bydgoszcz rzucił wyjątek na pierwszej siatce — osiedle i Stary Rynek nie weszły do świata.</summary>
    static void Folder(string sciezka)
    {
        sciezka = sciezka.Replace('\\', '/').TrimEnd('/');
        if (string.IsNullOrEmpty(sciezka) || sciezka == "Assets" || AssetDatabase.IsValidFolder(sciezka)) return;
        string rodzic = (Path.GetDirectoryName(sciezka) ?? "Assets").Replace('\\', '/'), nazwa = Path.GetFileName(sciezka);
        Folder(rodzic);
        if (!AssetDatabase.IsValidFolder(sciezka)) AssetDatabase.CreateFolder(rodzic, nazwa);
        if (!Directory.Exists(sciezka)) Directory.CreateDirectory(sciezka);
    }

    public static void Apply(Transform cityRoot)
    {
        okna.Clear(); rzeka = null; rzekaProba = false;
        Folder(Out); Folder(Out + "/Rynek");
        // każda część osobno: błąd osiedla nie zatrzymuje Rynku (i odwrotnie) — w logu DO POPRAWKI z przyczyną
        Bezpiecznie("osiedle", () => { Wczytaj(cityRoot, Plik, PunktZerowy, "BYDGOSZCZ osiedle Skłodowskiej-Curie", "osiedle", Out); return 1; });
        Bezpiecznie("Stary Rynek", () => { Wczytaj(cityRoot, PlikRynku, PunktZerowyRynku, "BYDGOSZCZ Stary Rynek (Mostowa, Focha, Brda)", "rynek", Out + "/Rynek"); return 1; });
    }

    static int Bezpiecznie(string co, Func<int> f)
    {
        try { return f(); }
        catch (Exception e) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_BYDGOSZCZ " + co + ": " + e); BraxRegion.Note("Bydgoszcz: " + co + " — BŁĄD " + e.Message); return 0; }
    }

    /// <summary>Jeden plik BYD2 (osiedle albo Stary Rynek): siatki × materiał w metrach → jednostki gry od punktu zerowego,
    /// drzewa, modele właściciela i akcje z plików obok (&lt;przedrostek&gt;_punkty.json, _akcje.json), maska trawy 3D.</summary>
    static void Wczytaj(Transform cityRoot, string plikBin, Vector2 zero, string nazwaKorzenia, string przedrostek, string katalogSiatek)
    {
        string sciezka = Sciezka(plikBin);
        if (sciezka == null) { BraxRegion.Note("Bydgoszcz: brak pliku " + plikBin + " — " + przedrostek + " pominięte"); return; }
        Folder(katalogSiatek);
        var root = BraxRegion.Root(cityRoot, nazwaKorzenia);
        var grupy = new Dictionary<string, Transform>();
        Transform Grupa(string g)
        {
            string klucz = g.Split('#')[0];
            if (!grupy.TryGetValue(klucz, out var t)) { t = new GameObject(klucz).transform; t.SetParent(root, false); grupy[klucz] = t; }
            return t;
        }
        Vector3 Swiat(float x, float y, float z) => new Vector3(zero.x + x * K, y * K, zero.y + z * K);
        int siatek = 0, trojkatow = 0, kolizji = 0, bledowZapisu = 0;
        var maty = new Dictionary<string, Material>();
        using (var plik = File.OpenRead(sciezka))
        using (var gz = new GZipStream(plik, CompressionMode.Decompress))
        using (var pamiec = new MemoryStream())
        {
            gz.CopyTo(pamiec); pamiec.Position = 0;
            using (var r = new BinaryReader(pamiec))
            {
                if (Encoding.ASCII.GetString(r.ReadBytes(4)) != "BYD2") { BraxRegion.Note("Bydgoszcz: inny format pliku " + plikBin + " — pominięty"); return; }
                string naglowek = Encoding.UTF8.GetString(r.ReadBytes(r.ReadInt32()));
                var paleta = Paleta(naglowek);
                int ns = r.ReadInt32();
                for (int s = 0; s < ns; s++)
                {
                    string g = Tekst(r), mn = Tekst(r);
                    int nv = r.ReadInt32(), ni = r.ReadInt32();
                    var v = new Vector3[nv]; var n = new Vector3[nv]; var tg = new Vector4[nv]; var uv = new Vector2[nv]; var uv2 = new Vector2[nv];
                    for (int i = 0; i < nv; i++)
                    {
                        v[i] = Swiat(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                        n[i] = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                        tg[i] = new Vector4(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                        uv[i] = new Vector2(r.ReadSingle(), r.ReadSingle()) * K;      // metry -> jednostki gry (kafel tekstury = metry × K)
                        uv2[i] = new Vector2(r.ReadSingle(), r.ReadSingle());         // pokój za szybą / naklejka 0..1
                    }
                    var idx = new int[ni]; for (int i = 0; i < ni; i++) idx[i] = (int)r.ReadUInt32();
                    if (!maty.TryGetValue(mn, out var mat))
                    {
                        paleta.TryGetValue(mn, out var d);
                        mat = Material(mn, d ?? new Mat { kol = new[] { 200f, 200f, 200f }, szorst = .9f });
                        maty[mn] = mat;
                    }
                    if (nv == 0 || ni == 0 || !mat) continue;
                    var mesh = new Mesh { name = "Bydgoszcz " + g + " " + mn, indexFormat = nv > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                    mesh.vertices = v; mesh.normals = n; mesh.tangents = tg; mesh.uv = uv; mesh.uv2 = uv2;
                    mesh.SetTriangles(idx, 0); mesh.RecalculateBounds();
                    try { BraxZapisAssetu.Utworz(mesh, katalogSiatek + "/" + g.Replace('#', '_') + "_" + mn + ".asset"); }
                    catch (Exception e) { if (bledowZapisu++ < 5) Debug.LogWarning("DO POPRAWKI: BRAXHORN_BYDGOSZCZ zapis siatki " + g + "/" + mn + ": " + e.Message); }
                    var go = new GameObject(g + " [" + mn + "]"); go.transform.SetParent(Grupa(g), false);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
                    mr.shadowCastingMode = mn.StartsWith("okno") || g.StartsWith("teren") ? ShadowCastingMode.Off : ShadowCastingMode.On;
                    mr.receiveShadows = true;
                    if (mn == "trawa") BraxRegionTrawa.Teren(mr, false, false);   // kępy z maski (TrawaMaska), nie z limitu miasta
                    if (Kolizja(mn)) { go.AddComponent<MeshCollider>().sharedMesh = mesh; kolizji++; }
                    GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
                    siatek++; trojkatow += ni / 3;
                }
            }
        }
        string punkty = Obok(sciezka, przedrostek, "punkty.json");
        int barier = Bezpiecznie(przedrostek + " bariery", () => Bariery(root, Swiat, punkty));
        int drzew = Bezpiecznie(przedrostek + " drzewa", () => Drzewa(root, Swiat, punkty));
        int modeli = Bezpiecznie(przedrostek + " modele właściciela", () => ModeleWlasciciela(root, Swiat, punkty));
        int muzyk = Bezpiecznie(przedrostek + " muzyka", () => Muzyka(root, Swiat, punkty, maty, katalogSiatek));
        int akcji = Bezpiecznie(przedrostek + " akcje", () => Akcje(root, Swiat, Obok(sciezka, przedrostek, "akcje.json")));
        Bezpiecznie(przedrostek + " maska trawy", () => { TrawaMaska(root, Obok(sciezka, przedrostek, "trawa_maska.bin"), katalogSiatek); return 1; });
        int lodow = Bezpiecznie(przedrostek + " LOD", () => Zasiegi(root));
        Debug.Log("BRAXHORN_BYDGOSZCZ_" + przedrostek.ToUpperInvariant() + " siatek=" + siatek + " trojkatow=" + trojkatow + " kolizji=" + kolizji + " materialow=" + maty.Count +
                  " drzew=" + drzew + " modeli_wlasciciela=" + modeli + " muzyki=" + muzyk + " akcji=" + akcji + " barier=" + barier + " lod=" + lodow + " bledow_zapisu=" + bledowZapisu + " punkt_zerowy=(" + zero.x + "; " + zero.y + ")");
    }

    /// <summary>Niewidzialne ściany (punkty.json → "bariery": [x0, z0, x1, z1, y0, y1], metry): Brda ucięta granicą miasta —
    /// tam nie ma muru nabrzeża, a płot granicy stoi na poziomie bulwaru; ściana od dna do 3 m nad bulwarem.</summary>
    static int Bariery(Transform root, Func<float, float, float, Vector3> swiat, string p)
    {
        if (!File.Exists(p)) return 0;
        string json = File.ReadAllText(p);
        int a = json.IndexOf("\"bariery\"", StringComparison.Ordinal);
        if (a < 0) return 0;
        int e = json.IndexOf("]]", a, StringComparison.Ordinal); if (e < 0) return 0;
        var ci = System.Globalization.CultureInfo.InvariantCulture; var fs = System.Globalization.NumberStyles.Float;
        var kol = new GameObject("Bydgoszcz: bariery (brzeg rzeki na granicy)").transform; kol.SetParent(root, false);
        int n = 0;
        foreach (var wpis in json.Substring(a, e - a).Split('['))
        {
            var pola = wpis.Split(',');
            if (pola.Length < 6) continue;
            var v = new float[6]; bool ok = true;
            for (int k = 0; k < 6; k++) ok &= float.TryParse(pola[k].Trim().Trim(']', ' '), fs, ci, out v[k]);
            if (!ok) continue;
            Vector3 p0 = swiat(v[0], v[4], v[1]), p1 = swiat(v[2], v[4], v[3]); float h = (v[5] - v[4]) * K;
            var d = p1 - p0; d.y = 0f; float L = d.magnitude; if (L < .05f) continue;
            var bc = new GameObject("bariera").AddComponent<BoxCollider>(); bc.transform.SetParent(kol, false);
            bc.transform.position = (p0 + p1) * .5f + Vector3.up * h * .5f; bc.transform.rotation = Quaternion.LookRotation(d / L, Vector3.up);
            bc.size = new Vector3(.4f, h, L + .4f); n++;
        }
        return n;
    }

    /// <summary>Budżet kadru na telefonie (ZASADY 10.2, 800 tys./kadr): każda grupa osiedla (budynek, wyposażenie, budki) dostaje
    /// LODGroup z jednym poziomem i odcięciem po wielkości na ekranie — drobne rzeczy znikają z daleka, budynki dopiero przy
    /// horyzoncie; teren zawsze. Drzewa mają własne LOD.</summary>
    static int Zasiegi(Transform root)
    {
        int n = 0;
        foreach (Transform g in root)
        {
            if (!g || g.name.StartsWith("teren") || g.GetComponent<LODGroup>() || g.GetComponent<BraxClaudeMuzyka>()) continue;   // lasery: 45 m, ruchome
            var rs = g.GetComponentsInChildren<MeshRenderer>(true); if (rs.Length == 0) continue;
            bool drobne = g.name.StartsWith("wyposazenie") || g.name.StartsWith("budki") || g.name.StartsWith("zielen");
            var lod = g.gameObject.AddComponent<LODGroup>();
            lod.SetLODs(new[] { new LOD(drobne ? .035f : .008f, rs) }); lod.fadeMode = LODFadeMode.None; lod.RecalculateBounds(); n++;
        }
        return n;
    }

    /// <summary>Maska trawnika (osiedle.py: zapisz_maske_trawy) → zasób BraxClaudeTrawaMaska; kępy dokłada krok trawnika miasta.</summary>
    static void TrawaMaska(Transform root, string p, string katalog)
    {
        if (!File.Exists(p)) { BraxRegion.Note("Bydgoszcz: brak maski trawnika " + p + " — trawniki bez kęp 3D"); return; }
        using (var gz = new GZipStream(File.OpenRead(p), CompressionMode.Decompress))
        using (var ms = new MemoryStream()) { gz.CopyTo(ms); var b = ms.ToArray();
            if (b.Length < 28 || Encoding.ASCII.GetString(b, 0, 4) != "BTM1") { BraxRegion.Note("Bydgoszcz: zła maska trawnika"); return; }
            var m = ScriptableObject.CreateInstance<BraxClaudeTrawaMaska>();
            m.poczatek = new Vector2(BitConverter.ToSingle(b, 4), BitConverter.ToSingle(b, 8)); m.krok = BitConverter.ToSingle(b, 12);
            m.y = BitConverter.ToSingle(b, 16); m.szer = BitConverter.ToInt32(b, 20); m.wys = BitConverter.ToInt32(b, 24);
            m.bity = new byte[b.Length - 28]; Array.Copy(b, 28, m.bity, 0, m.bity.Length);
            BraxZapisAssetu.Utworz(m, katalog + "/trawa_maska.asset");
            BraxRegionTrawa.MaskaKep(root, m);
            Debug.Log("BRAXHORN_BYDGOSZCZ_TRAWA maska " + m.szer + " × " + m.wys + " krok " + m.krok.ToString("F2") + " y " + m.y.ToString("F3")); }
    }

    /// <summary>Paleta z nagłówka BYD2 (JsonUtility nie czyta słowników — prosty podział na wpisy materiałów).</summary>
    static Dictionary<string, Mat> Paleta(string json)
    {
        var wynik = new Dictionary<string, Mat>();
        int a = json.IndexOf("\"materialy\"", StringComparison.Ordinal);
        if (a < 0) return wynik;
        int i = json.IndexOf('{', a) + 1, glebokosc = 0, start = -1; string klucz = null;
        for (; i < json.Length; i++)
        {
            char c = json[i];
            if (glebokosc == 0 && c == '"' && klucz == null)
            {
                int e = json.IndexOf('"', i + 1); klucz = json.Substring(i + 1, e - i - 1); i = e; continue;
            }
            if (c == '{') { if (glebokosc == 0) start = i; glebokosc++; }
            else if (c == '}')
            {
                if (glebokosc == 0) break;
                glebokosc--;
                if (glebokosc == 0 && klucz != null)
                {
                    try { wynik[klucz] = JsonUtility.FromJson<Mat>(json.Substring(start, i - start + 1)); } catch (Exception) { }
                    klucz = null;
                }
            }
        }
        return wynik;
    }

    /// <summary>Modele właściciela z paczek szyfrowanych (osiedle_punkty.json → "modele": [paczka, plik, x, z, kier_x, kier_z],
    /// metry osiedla). 23.11, 22:44: bus z bosną (jego model Tripo, bez logo firm) na parkingu przed marketem przy zapiekankach.
    /// Model w metrach (BraxMiastoAssety.Postaw skaluje × K); glTFast odbija x, więc kabina (−x w glTF) jest lokalnie +x:
    /// yaw = atan2(−kier_z, kier_x) ustawia kabinę wzdłuż kier, a bok z okienkiem (+z) w stronę parkingu. Bez paczki — pomija.</summary>
    static int ModeleWlasciciela(Transform root, Func<float, float, float, Vector3> swiat, string p)
    {
        if (!File.Exists(p)) return 0;
        string json = File.ReadAllText(p);
        int a = json.IndexOf("\"modele\"", StringComparison.Ordinal);
        if (a < 0) return 0;
        int e = json.IndexOf("]]", a, StringComparison.Ordinal);              // tylko lista „modele” (kolejne klucze — nie)
        var ci = System.Globalization.CultureInfo.InvariantCulture; var fs = System.Globalization.NumberStyles.Float;
        int licz = 0;
        foreach (var wpis in json.Substring(a, (e > a ? e + 2 : json.Length) - a).Split(new[] { "[" }, StringSplitOptions.RemoveEmptyEntries))
        {
            var pola = wpis.Split(',');
            if (pola.Length < 6) continue;
            for (int k = 0; k < pola.Length; k++) pola[k] = pola[k].Trim().Trim('"', ' ', ']', '}', '\n', '\r');
            string paczka = pola[0], plik = pola[1];
            if (!float.TryParse(pola[2], fs, ci, out float x) || !float.TryParse(pola[3], fs, ci, out float z) ||
                !float.TryParse(pola[4], fs, ci, out float kx) || !float.TryParse(pola[5], fs, ci, out float kz)) continue;
            // 7. pole (opcjonalne): wysokość w metrach — figura linoskoczka na linie nad Brdą (rynek_punkty.json)
            float y = pola.Length > 6 && float.TryParse(pola[6], fs, ci, out float yy) ? yy : 0f;
            var m = BraxMiastoAssety.Jest(paczka) ? BraxMiastoAssety.Model(paczka, plik) : null;
            if (!m) { BraxRegion.Note("Bydgoszcz: brak paczki " + paczka + "/" + plik + " — miejsce bez modelu"); continue; }
            float yaw = Mathf.Atan2(-kz, kx) * Mathf.Rad2Deg;
            var o = BraxMiastoAssety.Postaw(root, m, "Bydgoszcz: model właściciela · " + plik, swiat(x, y, z), yaw, 1f, true, true);
            if (!o) continue;
            // kolizja: prostopadłościan z granic siatek w układzie modelu (nie z AABB świata — model jest obrócony)
            bool jest = false; var lb = new Bounds();
            foreach (var mf in o.GetComponentsInChildren<MeshFilter>())
            {
                if (!mf.sharedMesh) continue; var mb = mf.sharedMesh.bounds;
                for (int k = 0; k < 8; k++)
                {
                    var c = mb.center + Vector3.Scale(mb.extents, new Vector3((k & 1) == 0 ? -1 : 1, (k & 2) == 0 ? -1 : 1, (k & 4) == 0 ? -1 : 1));
                    var lp = o.transform.InverseTransformPoint(mf.transform.TransformPoint(c));
                    if (!jest) { lb = new Bounds(lp, Vector3.zero); jest = true; } else lb.Encapsulate(lp);
                }
            }
            if (jest) { var kol = o.AddComponent<BoxCollider>(); kol.center = lb.center; kol.size = lb.size; }
            licz++;
        }
        return licz;
    }

    /// <summary>Muzyka z głośników w świecie (punkty.json → "muzyka": [plik, x0, z0, x1, z1, wys, głośność, g1x, g1y, g1z, g2x, g2y,
    /// g2z, materiał świateł, bpm, faza], metry). 24.11, 14:09 właściciel: balkon z DJ-em na Starym Rynku — „sprzęt DJ, głośniki
    /// i muzyka leci”. BraxClaudeMuzyka (jak dyskoteka osiedla): „sala” = plac przed kamienicą (pełne pasmo, głośniej przy
    /// kolumnach), dalej zza budynków stłumiona; suwak muzyki z ustawień działa (BraxUstawienia). Diody pulsują w rytm.</summary>
    static int Muzyka(Transform root, Func<float, float, float, Vector3> swiat, string p, Dictionary<string, Material> maty, string katalog)
    {
        if (!File.Exists(p)) return 0;
        string json = File.ReadAllText(p);
        int a = json.IndexOf("\"muzyka\"", StringComparison.Ordinal);
        if (a < 0) return 0;
        int e = json.IndexOf("]]", a, StringComparison.Ordinal); if (e < 0) return 0;
        var ci = System.Globalization.CultureInfo.InvariantCulture; var fs = System.Globalization.NumberStyles.Float;
        int licz = 0;
        foreach (var wpis in json.Substring(a, e + 2 - a).Split(new[] { "[" }, StringSplitOptions.RemoveEmptyEntries))
        {
            var pola = wpis.Split(',');
            if (pola.Length < 13) continue;
            for (int k = 0; k < pola.Length; k++) pola[k] = pola[k].Trim().Trim('"', ' ', ']', '}', '\n', '\r');
            var v = new float[13]; bool ok = true;
            for (int k = 1; k < 13; k++) ok &= float.TryParse(pola[k], fs, ci, out v[k]);
            if (!ok) continue;
            string sciezka = "Assets/Claude/Audio/" + pola[0];
            if (!File.Exists(sciezka)) { BraxRegion.Note("Bydgoszcz: brak utworu " + sciezka + " — balkon bez muzyki"); continue; }
            AssetDatabase.ImportAsset(sciezka, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(sciezka) is AudioImporter imp)
            {
                var st = imp.defaultSampleSettings;                       // strumień z pliku (telefon: bez całego utworu w RAM)
                if (st.loadType != AudioClipLoadType.Streaming || st.compressionFormat != AudioCompressionFormat.Vorbis)
                {
                    st.loadType = AudioClipLoadType.Streaming; st.compressionFormat = AudioCompressionFormat.Vorbis; st.quality = .6f;
                    imp.defaultSampleSettings = st; imp.loadInBackground = true; imp.SaveAndReimport();
                }
            }
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(sciezka);
            if (!clip) { BraxRegion.Note("Bydgoszcz: utwór " + sciezka + " się nie wczytał"); continue; }
            Vector3 c0 = swiat(v[1], 0f, v[2]), c1 = swiat(v[3], v[5], v[4]);
            var b = new Bounds(); b.SetMinMax(Vector3.Min(c0, c1), Vector3.Max(c0, c1));
            var go = new GameObject("Bydgoszcz: muzyka · balkon DJ"); go.transform.SetParent(root, false); go.transform.position = b.center;
            var mz = go.AddComponent<BraxClaudeMuzyka>();
            mz.utwor = clip; mz.wnetrze = b; mz.glosnosc = Mathf.Clamp(v[6], .05f, 1f); mz.wygaszenie = 3f; mz.zasiegZewn = 40f * K;
            mz.glosniki = new[] { swiat(v[7], v[8], v[9]), swiat(v[10], v[11], v[12]) };
            if (pola.Length >= 16 && maty.TryGetValue(pola[13], out var led) && led)
            {
                var puls = go.AddComponent<BraxPulsSwiatla>(); puls.material = led;
                if (float.TryParse(pola[14], fs, ci, out float bpm)) puls.bpm = bpm;
                if (float.TryParse(pola[15], fs, ci, out float faza)) puls.faza = faza;
            }
            // 24.11, 14:40 właściciel: „daj lasery, że w nocy będą odpalać, ruszające się i migające” — dwie głowice na narożnikach
            // balustrady (pola 16–21), kierunek frontu (22–23); wachlarze promieni (shader klubu NOVA: addytywny, bez głębi)
            var l = new float[8]; bool okL = pola.Length >= 24;
            for (int k = 0; okL && k < 8; k++) okL &= float.TryParse(pola[16 + k], fs, ci, out l[k]);
            if (okL)
            {
                var promien = Wachlarz("laser_balkonu_wachlarz", 7, 44f, 45f * K, .012f * K, .14f * K, katalog);
                var piv = new List<Transform>();
                for (int i = 0; i < 2 && promien; i++)
                {
                    var lm = LaserMaterial("laser_balkonu_" + (i + 1), i == 0 ? new Color(.15f, 1f, .25f) : new Color(1f, .15f, .7f), katalog);
                    if (!lm) break;
                    var t = new GameObject("laser balkonu " + (i + 1)).transform; t.SetParent(go.transform, true);
                    t.position = swiat(l[i * 3], l[i * 3 + 1], l[i * 3 + 2]);
                    t.rotation = Quaternion.LookRotation(new Vector3(l[6], 0f, l[7]), Vector3.up);
                    var w = new GameObject("wachlarz promieni"); w.transform.SetParent(t, false);
                    w.AddComponent<MeshFilter>().sharedMesh = promien;
                    var wr = w.AddComponent<MeshRenderer>(); wr.sharedMaterial = lm; wr.shadowCastingMode = ShadowCastingMode.Off; wr.receiveShadows = false;
                    piv.Add(t);
                }
                if (piv.Count > 0)
                {
                    var las = go.AddComponent<BraxLaseryBalkonu>(); las.lasery = piv.ToArray(); las.bpm = float.TryParse(pola[14], fs, ci, out float bl) ? bl : 138f;
                    if (float.TryParse(pola[15], fs, ci, out float fl)) las.faza = fl;
                }
            }
            licz++;
            Debug.Log("BRAXHORN_BYDGOSZCZ_MUZYKA " + clip.name + " " + clip.length.ToString("F0") + " s, plac " + b.size + ", głośniki " + mz.glosniki.Length);
        }
        return licz;
    }

    /// <summary>Wachlarz promieni lasera (jak w klubie NOVA): każdy promień = dwa skrzyżowane pasy od głowicy (uv.y 0 → 1 wzdłuż),
    /// rozpiętość w stopniach wokół osi pionowej, jednostki gry.</summary>
    static Mesh Wachlarz(string nazwa, int ile, float rozp, float dl, float w0, float w1, string katalog)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tr = new List<int>();
        for (int i = 0; i < ile; i++)
        {
            float a = ile == 1 ? 0f : Mathf.Lerp(-rozp * .5f, rozp * .5f, i / (float)(ile - 1));
            var d = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
            foreach (var bok in new[] { Vector3.Cross(Vector3.up, d).normalized, Vector3.up })
            {
                int k = v.Count;
                v.Add(-bok * w0); v.Add(bok * w0); v.Add(d * dl + bok * w1); v.Add(d * dl - bok * w1);
                uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(1f, 0f)); uv.Add(new Vector2(1f, 1f)); uv.Add(new Vector2(0f, 1f));
                tr.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
            }
        }
        var m = new Mesh { name = "Bydgoszcz " + nazwa }; m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tr, 0); m.RecalculateBounds();
        try { BraxZapisAssetu.Utworz(m, katalog + "/" + nazwa + ".asset"); } catch (Exception e) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_BYDGOSZCZ laser: " + e.Message); }
        return m;
    }

    static Material LaserMaterial(string nazwa, Color c, string katalog)
    {
        var sh = Shader.Find("Braxhorn/WnetrzaPromien");
        if (!sh) { BraxRegion.Note("Bydgoszcz: brak shadera Braxhorn/WnetrzaPromien — balkon bez laserów"); return null; }
        var m = new Material(sh) { name = "Bydgoszcz: " + nazwa };
        m.SetColor("_Tint", c); m.SetFloat("_Moc", 1.6f);
        try { BraxZapisAssetu.Utworz(m, katalog + "/" + nazwa + ".mat"); } catch (Exception e) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_BYDGOSZCZ laser: " + e.Message); }
        return m;
    }

    [Serializable] class AkcjaJ { public string id, etykieta, odpowiedz, mowca; public float x, y, z, promien = 1.7f, kier_x, kier_z; }
    [Serializable] class AkcjeJ { public AkcjaJ[] akcje; }

    /// <summary>Punkty akcji osiedla (osiedle_akcje.json, metry): 24.11 00:16 — bus z bosną: „KUP BOSNĘ” przy okienku i miejsce
    /// sprzedawcy za ladą (znacznik z kierunkiem twarzą do okienka — dla obsady, gdy ludzie będą włączeni, i dla pracy gracza).</summary>
    static int Akcje(Transform root, Func<float, float, float, Vector3> swiat, string p)
    {
        if (!File.Exists(p)) return 0;
        var dane = JsonUtility.FromJson<AkcjeJ>(File.ReadAllText(p));
        if (dane == null || dane.akcje == null) return 0;
        int n = 0; var wg = new Dictionary<string, BraxPunktAkcji>();
        foreach (var a in dane.akcje)
        {
            var pos = swiat(a.x, a.y, a.z);
            var pk = BraxMiastoA.Akcja(root, a.id, a.etykieta, a.odpowiedz, pos, a.promien * K);
            if (!pk) continue;
            if (a.kier_x != 0f || a.kier_z != 0f) pk.transform.rotation = Quaternion.LookRotation(new Vector3(a.kier_x, 0f, a.kier_z), Vector3.up);
            wg[a.id] = pk; n++;
        }
        foreach (var a in dane.akcje)                                   // dymek kupna nad sprzedawcą (punkt obsady)
            if (!string.IsNullOrEmpty(a.mowca) && wg.TryGetValue(a.id, out var pk) && wg.TryGetValue(a.mowca, out var m)) pk.mowca = m.transform;
        return n;
    }

    /// <summary>Drzewa z osiedle_punkty.json — modele z biblioteki właściciela (drzewo_verdant, las_sosna_a).</summary>
    static int Drzewa(Transform root, Func<float, float, float, Vector3> swiat, string p)
    {
        if (!File.Exists(p)) return 0;
        string json = File.ReadAllText(p);
        int a = json.IndexOf("\"drzewa\"", StringComparison.Ordinal), b = json.IndexOf("\"latarnie\"", StringComparison.Ordinal);
        if (a < 0) return 0;
        string czesc = json.Substring(a, (b > a ? b : json.Length) - a);
        int licz = 0;
        foreach (var wpis in czesc.Split('['))
        {
            var pola = wpis.Split(',');
            if (pola.Length < 3) continue;
            if (!float.TryParse(pola[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x)) continue;
            if (!float.TryParse(pola[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float z)) continue;
            string typ = pola[2].Trim().Trim(']', '"', ' ', '}');
            // 23.11 wieczorem (właściciel: „zwróć uwagę na drzewa”): młode = las_drzewko (tree_small_02 na telefon), liściaste =
            // pień drzewo_verdant + korona z kart-gałązek drzewo_klaster (bez płaskich trójkątnych liści), iglaste = las_sosna_a
            string id = typ == "iglaste" ? "las_sosna_a" : typ == "mlode" ? "las_drzewko" : "drzewo_verdant";
            float h = typ == "duze" ? 13f : typ == "srednie" ? 9f : typ == "mlode" ? 5.5f : 14f;
            var src = BraxRegion.Model(id);
            if (!src) continue;
            var pos = swiat(x, .08f, z);
            var o = BraxRegion.Put(root, src, "Bydgoszcz: " + id, pos, (licz * 137.5f) % 360f, 1f, false);
            if (!o) continue;
            if (id == "drzewo_verdant") Korona(o, licz & 1); else BraxRegion.TreeLods(o);
            float wys = 0f; foreach (var rr in o.GetComponentsInChildren<Renderer>()) wys = Mathf.Max(wys, rr.bounds.max.y - pos.y);
            if (wys > .01f) { float f = h * K / wys; o.transform.localScale *= f; o.transform.position = pos + (o.transform.position - pos) * f; }
            var kol = new GameObject("Bydgoszcz: pień kolizja").AddComponent<CapsuleCollider>(); kol.transform.SetParent(root, false);
            kol.transform.position = pos + Vector3.up * 1.5f; kol.radius = .3f; kol.height = 3f;
            BraxRegionTrawa.Drzewo(pos);
            licz++;
        }
        return licz;
    }

    // Korona z kart-gałązek (unity-tools/claude/drzewo_klaster.py; te same zasoby i shader co drzewa osiedla R2, kod własny —
    // nie ruszamy cudzego rejonu). Siatki liscie_0..3 mają ten sam układ współrzędnych co liście drzewo_verdant.
    const string KlasterDir = "Assets/Source/Claude/Osiedle/models/drzewo_klaster/";
    static Mesh[] klLiscie; static Material[] klMat; static bool klProba;

    static bool KlasterWczytaj()
    {
        if (klMat != null) return true;
        if (klProba) return false; klProba = true;
        string glb = KlasterDir + "drzewo_klaster.glb", atlas = KlasterDir + "galazki.png";
        if (!File.Exists(glb) || !File.Exists(atlas)) { BraxRegion.Note("Bydgoszcz: brak drzewo_klaster — korony z liści drzewo_verdant"); return false; }
        AssetDatabase.ImportAsset(glb, ImportAssetOptions.ForceSynchronousImport);
        var siatki = new List<Mesh>(); foreach (var a in AssetDatabase.LoadAllAssetsAtPath(glb)) if (a is Mesh m) siatki.Add(m);
        Mesh M(string n) { foreach (var m in siatki) if (m.name == n) return m; return null; }
        klLiscie = new[] { M("liscie_0"), M("liscie_1"), M("liscie_2"), M("liscie_3") };
        foreach (var m in klLiscie) if (!m) { klLiscie = null; BraxRegion.Note("Bydgoszcz: drzewo_klaster bez liscie_0..3"); return false; }
        var imp = AssetImporter.GetAtPath(atlas) as TextureImporter;
        if (imp)
        {
            imp.textureType = TextureImporterType.Default; imp.sRGBTexture = true; imp.alphaSource = TextureImporterAlphaSource.FromInput;
            imp.alphaIsTransparency = true; imp.mipmapEnabled = true; imp.mipMapsPreserveCoverage = true; imp.alphaTestReferenceValue = .45f;
            imp.wrapMode = TextureWrapMode.Clamp; imp.maxTextureSize = 1024; imp.textureCompression = TextureImporterCompression.Compressed; imp.SaveAndReimport();
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(atlas); var sh = Shader.Find("Braxhorn/OsiedleLiscie");
        if (!tex || !sh) { BraxRegion.Note("Bydgoszcz: brak atlasu gałązek albo shadera OsiedleLiscie"); return false; }
        var b = klLiscie[0].bounds; klMat = new Material[2];
        var ziel = new[] { (new Color(.310f, .522f, .147f), new Color(.408f, .553f, .164f)), (new Color(.36f, .50f, .15f), new Color(.30f, .47f, .13f)) };
        for (int v = 0; v < 2; v++)
        {
            var m = new Material(sh) { name = "Bydgoszcz: liście drzew (gałązki) " + v, enableInstancing = true, mainTexture = tex };
            m.SetColor("_Leaf", ziel[v].Item1); m.SetColor("_Leaf2", ziel[v].Item2); m.SetFloat("_InnerDark", .35f); m.SetFloat("_Recolor", .85f);
            m.SetFloat("_MeanGamma", .10993f); m.SetFloat("_MeanLinear", .014248f); m.SetFloat("_Cutoff", .45f);
            m.SetVector("_Center", b.center + new Vector3(0, b.extents.y * .15f, 0)); m.SetVector("_Radius", b.extents);
            BraxZapisAssetu.Utworz(m, Out + "/liscie_galazki_" + v + ".mat"); klMat[v] = m;
        }
        return true;
    }

    static void Korona(GameObject o, int odmiana)
    {
        if (!KlasterWczytaj()) { BraxRegion.TreeLods(o); return; }
        Renderer pien = null; var lv = new Renderer[4];
        foreach (var r in o.GetComponentsInChildren<Renderer>(true))
        {
            if (r.name == "pien") pien = r;
            for (int k = 0; k < 4; k++) if (r.name == "liscie_" + k || (r.transform.parent && r.transform.parent.name == "liscie_" + k)) { if (!lv[k]) lv[k] = r; }
        }
        if (!pien || !lv[0]) { BraxRegion.TreeLods(o); return; }
        var lods = new List<LOD>(); float[] prog = { .40f, .18f, .075f, .012f };
        for (int k = 0; k < 4; k++)
        {
            var r = lv[k];
            if (!r) { if (k < 3) continue; var g = new GameObject("liscie_3"); g.transform.SetParent(lv[0].transform.parent, false);
                g.transform.localPosition = lv[0].transform.localPosition; g.transform.localRotation = lv[0].transform.localRotation; g.transform.localScale = lv[0].transform.localScale;
                g.AddComponent<MeshFilter>(); r = g.AddComponent<MeshRenderer>(); }
            var mf = r.GetComponent<MeshFilter>(); if (!mf) continue;
            mf.sharedMesh = klLiscie[k]; r.sharedMaterial = klMat[odmiana & 1];
            r.shadowCastingMode = k <= 1 ? ShadowCastingMode.On : ShadowCastingMode.Off; r.receiveShadows = true;
            lods.Add(new LOD(prog[k], new[] { pien, r }));
        }
        var lod = o.GetComponent<LODGroup>(); if (!lod) lod = o.AddComponent<LODGroup>();
        lod.SetLODs(lods.ToArray()); lod.fadeMode = LODFadeMode.CrossFade; lod.animateCrossFading = true; lod.RecalculateBounds();
    }
}
