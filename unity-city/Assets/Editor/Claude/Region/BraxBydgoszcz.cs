using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// MIASTO R1 — OSIEDLE BYDGOSKIE przy M. Skłodowskiej-Curie 32a (23.11.2026, etap 1).
/// Właściciel 23.11, 02:07–02:49: „w miasto R1 odwzorować ulice w Bydgoszczy… blokowisko”, „musisz pobrać mapę”, „cała prawa strona, tam gdzie
/// jest sklep, ten duży”, „nie chcę lipy”, „to ma wyglądać jak najbardziej realistycznie”, „zdjęcia do wglądu, zanim cokolwiek zaakceptuję”.
///
/// Geometrię (grunt z mapy, bloki, wieżowce, garaże, pawilony, szkoła) liczy unity-tools/claude/bydgoszcz/osiedle_gen.py i zapisuje do
/// osiedle.bin; ten plik ją tylko wczytuje, skaluje (metr × K) i daje materiały. Podgląd geometrii jest rysowany z tego samego pliku.
///
/// WYŁĄCZONE (Wlaczone = false): na północ od granicy gry (z > BraxMiastoGranice.Z1 = 180) BraxRegionHoryzont stawia dziś wzgórza i las,
/// więc osiedle stanęłoby w zboczu. Włączenie wymaga przesunięcia granicy północnej i horyzontu — osobna zmiana, do sprawdzenia w buildzie.
/// </summary>
public static class BraxBydgoszcz
{
    /// <summary>Przełącznik kroku w BraxRegion.Apply. Patrz opis klasy — nie włączać bez przesunięcia granicy północnej.</summary>
    public static readonly bool Wlaczone = false;
    /// <summary>Miejsce bloku 32a w mieście (jednostki gry). JEDNA stała — właściciel potwierdził stronę 23.11, 02:29; dokładne miejsce wg planu „propozycja 2”.</summary>
    public static readonly Vector2 Blok32a = new Vector2(-145.9f, 508.8f);
    public const float PoziomY = 0f;
    const string Plik = "unity-tools/claude/bydgoszcz/osiedle.bin";
    static float K => BraxClaudeOsiedle.K;

    static string Sciezka()
    {
        foreach (var p in new[] { Plik, "../" + Plik }) if (File.Exists(p)) return p;
        return null;
    }

    static string Tekst(BinaryReader r) { int n = r.ReadInt32(); return Encoding.UTF8.GetString(r.ReadBytes(n)); }

    /// <summary>Materiał wg rodzaju z pliku. Tekstury: najpierw lista właściciela (MATERIALY-WLASCICIELA.md, ZASADY 5.8), potem zestawy już pobierane w R1.</summary>
    static Material Mat(string nazwa, string rodzaj, Color c)
    {
        string n = "Bydgoszcz: " + nazwa;
        switch (rodzaj)
        {
            // tynk: jasny skan white_stucco_02 (średnia sRGB 0,68); barwa z pliku to tint liczony do jego średniej (osiedle_gen.tint)
            case "tynk": return BraxRegionMeble.Tex(n, 2.0f, c, .92f, "white_stucco_02", "white_stucco", "plastered_wall");
            case "beton": return BraxRegionMeble.Tex(n, 2.0f, c, .9f, "brushed_concrete_04", "concrete_wall_008", "concrete_floor_02");
            case "cegla": return BraxRegionMeble.Tex(n, 1.0f, Color.white, .9f, "brick_wall_02", "red_brick_03", "brick_wall_001");
            case "blacha": return BraxRegionMeble.Tex(n, 2.0f, c, .5f, "box_profile_metal_sheet", "corrugated_iron", "metal_plate");
            case "drewno": return BraxRegionMeble.Tex(n, 1.5f, c, .7f, "weathered_brown_planks", "wooden_planks", "wood_planks_grey");
            case "asfalt": return BraxRegionMeble.Tex(n, 4.0f, new Color(.80f, .80f, .80f), .9f, "clean_asphalt");
            case "asfalt_stary": return BraxRegionMeble.Tex(n, 4.0f, Color.white, .95f, "clean_asphalt");
            case "chodnik": return BraxRegionMeble.Tex(n, 2.0f, Color.white, .9f, "concrete_pavers", "pavement_02", "concrete_pavement_02");
            case "ziemia": return BraxRegionMeble.Tex(n, 2.0f, Color.white, 1f, "brown_mud_dry", "brown_mud_leaves_01");
            case "szklo": return BraxRegionMeble.Gladki(n, c, .08f, .6f);
            case "trawa": return BraxRegionTrawa.Material;
            case "obraz":   // 23.11, 08:01 — właściciel o muralu na barze mlecznym: „mój kolega tam robił… możesz go normalnie wstawić”
            {
                string obraz = "Assets/Source/Claude/Bydgoszcz/" + nazwa + ".jpg";
                Texture2D tex = null;
                if (File.Exists(obraz)) { AssetDatabase.ImportAsset(obraz, ImportAssetOptions.ForceSynchronousImport); tex = AssetDatabase.LoadAssetAtPath<Texture2D>(obraz); }
                if (!tex) { BraxRegion.Note("Bydgoszcz: brak obrazu " + obraz + " — ściana w kolorze"); return BraxRegionMeble.Gladki(n, c, .9f, 0f); }
                var m = BraxCityMaterials.Textured(n, tex, null, null, 1f); m.SetFloat("_Roughness", .9f); m.SetFloat("_Metallic", 0f);
                return m;
            }
            default: return BraxRegionMeble.Gladki(n, c, .55f, .1f);
        }
    }

    public static void Apply(Transform cityRoot)
    {
        string sciezka = Sciezka();
        if (sciezka == null) { BraxRegion.Note("Bydgoszcz: brak pliku " + Plik + " — osiedle pominięte"); return; }
        var root = BraxRegion.Root(cityRoot, "BYDGOSZCZ osiedle Skłodowskiej-Curie 32a");
        var grupy = new Dictionary<string, Transform>();
        Transform Grupa(string g)
        {
            string klucz = g.StartsWith("grunt_") ? "Grunt" : g;
            if (!grupy.TryGetValue(klucz, out var t)) { t = new GameObject(klucz).transform; t.SetParent(root, false); grupy[klucz] = t; }
            return t;
        }
        Vector3 Swiat(float x, float y, float z) => new Vector3(Blok32a.x + x * K, PoziomY + y * K, Blok32a.y + z * K);
        int siatek = 0, trojkatow = 0, kolizji = 0, napisow = 0;
        using (var plik = File.OpenRead(sciezka))
        using (var gz = new GZipStream(plik, CompressionMode.Decompress))
        using (var pamiec = new MemoryStream())
        {
            gz.CopyTo(pamiec); pamiec.Position = 0;
            using (var r = new BinaryReader(pamiec))
            {
                if (Encoding.ASCII.GetString(r.ReadBytes(4)) != "BYD1") { BraxRegion.Note("Bydgoszcz: " + Plik + " ma inny format — osiedle pominięte"); return; }
                int nm = r.ReadInt32(); var maty = new Material[nm]; var rodzaje = new string[nm];
                for (int i = 0; i < nm; i++)
                {
                    string nazwa = Tekst(r); rodzaje[i] = Tekst(r);
                    var c = new Color(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                    maty[i] = Mat(nazwa, rodzaje[i], c);
                }
                int ns = r.ReadInt32();
                for (int s = 0; s < ns; s++)
                {
                    string g = Tekst(r); int mi = r.ReadInt32(); byte flagi = r.ReadByte(); int nv = r.ReadInt32(), ni = r.ReadInt32();
                    var v = new List<Vector3>(nv); var n = new List<Vector3>(nv); var uv = new List<Vector2>(nv);
                    float skalaUv = (flagi & 4) != 0 ? 1f : K;   // obraz (mural) ma UV 0…1, reszta — metry
                    for (int i = 0; i < nv; i++)
                    {
                        float x = r.ReadSingle(), y = r.ReadSingle(), z = r.ReadSingle();
                        v.Add(Swiat(x, y, z)); n.Add(new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()));
                        uv.Add(new Vector2(r.ReadSingle() * skalaUv, r.ReadSingle() * skalaUv));
                    }
                    var t = new int[ni]; for (int i = 0; i < ni; i++) t[i] = r.ReadInt32();
                    if (nv == 0 || ni == 0 || !maty[mi]) continue;
                    var mesh = new Mesh { name = "Bydgoszcz " + g, indexFormat = nv > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                    mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0); mesh.RecalculateBounds(); mesh.RecalculateTangents();
                    var go = new GameObject(g + " [" + maty[mi].name + "]"); go.transform.SetParent(Grupa(g), false);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = maty[mi]; mr.shadowCastingMode = ShadowCastingMode.On; mr.receiveShadows = true;
                    if ((flagi & 1) != 0) go.AddComponent<MeshCollider>().sharedMesh = mesh;
                    if ((flagi & 2) != 0) BraxRegionTrawa.Teren(mr, true, false);
                    GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
                    siatek++; trojkatow += ni / 3;
                }
                int nb = r.ReadInt32();
                for (int i = 0; i < nb; i++)
                {
                    float cx = r.ReadSingle(), cy = r.ReadSingle(), cz = r.ReadSingle(), sx = r.ReadSingle(), sy = r.ReadSingle(), sz = r.ReadSingle(), yaw = r.ReadSingle();
                    BraxRegionMeble.Kolizja(root, "Bydgoszcz: kolizja budynku " + i, Swiat(cx, cy, cz), new Vector3(sx, sy, sz) * K, Quaternion.Euler(0f, yaw, 0f));
                    kolizji++;
                }
                int nn = r.ReadInt32();
                for (int i = 0; i < nn; i++)
                {
                    string tekst = Tekst(r);
                    var p = Swiat(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()); var przod = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                    float wys = r.ReadSingle() * K; var kolor = new Color(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                    BraxRegionMeble.Napis(root, tekst, p, przod, wys, kolor, .06f * K);
                    napisow++;
                }
            }
        }
        Debug.Log("BRAXHORN_BYDGOSZCZ_OSIEDLE siatek=" + siatek + " trojkatow=" + trojkatow + " kolizji=" + kolizji + " napisow=" + napisow +
                  " blok32a=(" + Blok32a.x + "; " + Blok32a.y + ")");
    }
}
