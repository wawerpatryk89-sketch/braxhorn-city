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
    const string Plik = "unity-tools/claude/osiedle_bdg/gen/osiedle.bin";
    const string Naklejki = "Assets/Source/Claude/Bydgoszcz/";
    const string Prywatne = "Assets/Source/Prywatne/Miasto/bydgoszcz_mural/";
    const string Pokoje = "Assets/Source/Claude/Osiedle/detail/pokoje.png";
    static float K => BraxClaudeOsiedle.K;
    static string Out => BraxRegion.Out + "/Bydgoszcz";

    [Serializable] class Mat { public string tex; public float m; public float[] kol; public float[] tint; public float szorst; public float metal; public string nakl; public string alfa; public bool okno; public string gra; }

    static string Sciezka()
    {
        foreach (var p in new[] { Plik, "../" + Plik }) if (File.Exists(p)) return p;
        return null;
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
        if (!string.IsNullOrEmpty(d.tex))
            return BraxRegionMeble.Tex(n, d.m, C(d.tint), d.szorst, d.tex);
        return BraxRegionMeble.Gladki(n, C(d.tint), d.szorst, d.metal);
    }

    static bool Kolizja(string mat) =>
        !(mat.StartsWith("okno") || mat.StartsWith("balustrada") || mat == "pcv" || mat == "opaska" || mat == "parapet" || mat == "stal" ||
          mat == "stal_balustrada" || mat == "rura_gaz" || mat == "linia_biala" || mat == "lampa" || mat == "tabliczka" || mat == "domofon" ||
          mat == "kratka" || mat == "obrobka" || mat == "szyba_drzwi" || mat == "szyba_piwnica");

    public static void Apply(Transform cityRoot)
    {
        okna.Clear();
        string sciezka = Sciezka();
        if (sciezka == null) { BraxRegion.Note("Bydgoszcz: brak pliku " + Plik + " — osiedle pominięte"); return; }
        var root = BraxRegion.Root(cityRoot, "BYDGOSZCZ osiedle Skłodowskiej-Curie");
        var grupy = new Dictionary<string, Transform>();
        Transform Grupa(string g)
        {
            string klucz = g.Split('#')[0];
            if (!grupy.TryGetValue(klucz, out var t)) { t = new GameObject(klucz).transform; t.SetParent(root, false); grupy[klucz] = t; }
            return t;
        }
        Vector3 Swiat(float x, float y, float z) => new Vector3(PunktZerowy.x + x * K, y * K, PunktZerowy.y + z * K);
        int siatek = 0, trojkatow = 0, kolizji = 0;
        var maty = new Dictionary<string, Material>();
        using (var plik = File.OpenRead(sciezka))
        using (var gz = new GZipStream(plik, CompressionMode.Decompress))
        using (var pamiec = new MemoryStream())
        {
            gz.CopyTo(pamiec); pamiec.Position = 0;
            using (var r = new BinaryReader(pamiec))
            {
                if (Encoding.ASCII.GetString(r.ReadBytes(4)) != "BYD2") { BraxRegion.Note("Bydgoszcz: inny format pliku — osiedle pominięte"); return; }
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
                    BraxZapisAssetu.Utworz(mesh, Out + "/" + g.Replace('#', '_') + "_" + mn + ".asset");
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
        int drzew = Drzewa(root, Swiat);
        TrawaMaska(root);
        int lodow = Zasiegi(root);
        Debug.Log("BRAXHORN_BYDGOSZCZ_OSIEDLE siatek=" + siatek + " trojkatow=" + trojkatow + " kolizji=" + kolizji + " materialow=" + maty.Count +
                  " drzew=" + drzew + " lod=" + lodow + " punkt_zerowy=(" + PunktZerowy.x + "; " + PunktZerowy.y + ")");
    }

    /// <summary>Budżet kadru na telefonie (ZASADY 10.2, 800 tys./kadr): każda grupa osiedla (budynek, wyposażenie, budki) dostaje
    /// LODGroup z jednym poziomem i odcięciem po wielkości na ekranie — drobne rzeczy znikają z daleka, budynki dopiero przy
    /// horyzoncie; teren zawsze. Drzewa mają własne LOD.</summary>
    static int Zasiegi(Transform root)
    {
        int n = 0;
        foreach (Transform g in root)
        {
            if (!g || g.name.StartsWith("teren") || g.GetComponent<LODGroup>()) continue;
            var rs = g.GetComponentsInChildren<MeshRenderer>(true); if (rs.Length == 0) continue;
            bool drobne = g.name.StartsWith("wyposazenie") || g.name.StartsWith("budki") || g.name.StartsWith("zielen");
            var lod = g.gameObject.AddComponent<LODGroup>();
            lod.SetLODs(new[] { new LOD(drobne ? .035f : .008f, rs) }); lod.fadeMode = LODFadeMode.None; lod.RecalculateBounds(); n++;
        }
        return n;
    }

    /// <summary>Maska trawnika (osiedle.py: zapisz_maske_trawy) → zasób BraxClaudeTrawaMaska; kępy dokłada krok trawnika miasta.</summary>
    static void TrawaMaska(Transform root)
    {
        string p = Path.Combine(Path.GetDirectoryName(Sciezka() ?? Plik) ?? "", "osiedle_trawa_maska.bin");
        if (!File.Exists(p)) { BraxRegion.Note("Bydgoszcz: brak maski trawnika — trawniki bez kęp 3D"); return; }
        using (var gz = new GZipStream(File.OpenRead(p), CompressionMode.Decompress))
        using (var ms = new MemoryStream()) { gz.CopyTo(ms); var b = ms.ToArray();
            if (b.Length < 28 || Encoding.ASCII.GetString(b, 0, 4) != "BTM1") { BraxRegion.Note("Bydgoszcz: zła maska trawnika"); return; }
            var m = ScriptableObject.CreateInstance<BraxClaudeTrawaMaska>();
            m.poczatek = new Vector2(BitConverter.ToSingle(b, 4), BitConverter.ToSingle(b, 8)); m.krok = BitConverter.ToSingle(b, 12);
            m.y = BitConverter.ToSingle(b, 16); m.szer = BitConverter.ToInt32(b, 20); m.wys = BitConverter.ToInt32(b, 24);
            m.bity = new byte[b.Length - 28]; Array.Copy(b, 28, m.bity, 0, m.bity.Length);
            BraxZapisAssetu.Utworz(m, Out + "/trawa_maska.asset");
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

    /// <summary>Drzewa z osiedle_punkty.json — modele z biblioteki właściciela (drzewo_verdant, las_sosna_a).</summary>
    static int Drzewa(Transform root, Func<float, float, float, Vector3> swiat)
    {
        string p = Path.Combine(Path.GetDirectoryName(Sciezka() ?? Plik) ?? "", "osiedle_punkty.json");
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
