using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// TELEWIZORY R1 Z FILMAMI WŁAŚCICIELA (11.11.2026, właściciel 20:56: „jak jest telewizor, to w telewizorach lecą filmiki moje”;
// 21:40: „Twój kolega zrobił telewizory, które grają, filmy odtwarzają, tak samo możesz z nich skorzystać, żeby w każdym
// pomieszczeniu były”). Telewizor płaski z kodu (16:9): ekran (jedna płaszczyzna w otworze ramki), cienka ramka z czarnego
// aluminium, korpus z tyłu (bez przedniej ściany — zero migotania), na ścianie: wypukła tylna część na uchwycie; na szafce: noga
// i stopa. Ekran wszystkich telewizorów to JEDEN wspólny materiał świecący: obraz stały „BRAXHORN TV” (z kodu), a w grze
// BraxR1ReklamyTV podmienia go na film (reklamy właściciela z paczki kolegi, odszyfrowane w CI — r1_reklamy_tv.py), gdy gracz
// jest blisko któregoś telewizora. Siatki raz na rozmiar (pamięć podręczna), obiekty z obrotem — dowolny kierunek ściany.
internal static class BraxTelewizory
{
    public static readonly string[] Reklamy = { "reklama_1_puma.mp4", "reklama_2_alpy.mp4", "reklama_3_roboty.mp4", "reklama_4_gangsterzy.mp4", "reklama_5_fabryka.mp4" };
    const string Katalog = BraxRegion.Out + "/Telewizory";
    static Material ekran, ramka, korpus;
    static readonly Dictionary<string, (Mesh[] m, Material[] mat)> wzory = new Dictionary<string, (Mesh[], Material[])>();
    static BraxR1ReklamyTV odtwarzacz;
    public static int Postawione;

    static void Materialy()
    {
        if (ekran) return;
        ramka = BraxUliceKit.Tex("drzwi lokalu: czarne aluminium anodowane", 1.2f, new Color(.075f, .075f, .085f), new Color(.06f, .06f, .07f), .45f, "blue_metal_plate", "metal_plate");
        korpus = BraxCityMaterials.Surface("telewizor: korpus (czarny mat)", new Color(.035f, .035f, .04f), .7f, .1f);
        ekran = BraxMiastoA.Swieci("R1: ekran TV (filmy właściciela)", Color.white, new Color(.85f, .85f, .85f));
        // obraz stały: granat z gradientem, BRAXHORN i pasek „TV” (Bebas) — 512 × 288
        const int W = 512, H = 288; var px = new Color[W * H];
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
        {
            float u = x / (float)W - .5f, v = y / (float)H - .5f, r = Mathf.Sqrt(u * u + v * v * 3f);
            px[y * W + x] = Color.Lerp(new Color(.08f, .16f, .34f), new Color(.015f, .03f, .07f), Mathf.Clamp01(r * 1.6f));
        }
        for (int y = 70; y < 74; y++) for (int x = 156; x < 356; x++) px[y * W + x] = new Color(.85f, .1f, .12f);
        BraxChlodnie.TekstAtlasu(px, W, 0, 0, W, H, "BRAXHORN", 120f, 82f, Color.white);
        BraxChlodnie.TekstAtlasu(px, W, 0, 0, W, H, "TV", 36f, 26f, new Color(.85f, .88f, .92f));
        var tx = new Texture2D(W, H, TextureFormat.RGB24, true); tx.SetPixels(px); tx.Apply();
        Directory.CreateDirectory(Katalog); string path = Katalog + "/ekran_braxhorn_tv.png";
        File.WriteAllBytes(path, tx.EncodeToPNG()); Object.DestroyImmediate(tx);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        if (AssetImporter.GetAtPath(path) is TextureImporter imp) { imp.sRGBTexture = true; imp.wrapMode = TextureWrapMode.Clamp; imp.maxTextureSize = 512; imp.mipmapEnabled = true; imp.SaveAndReimport(); }
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (t) { ekran.SetTexture("_MainTex", t); if (ekran.HasProperty("_EmissionMap")) ekran.SetTexture("_EmissionMap", t); EditorUtility.SetDirty(ekran); }
    }

    // siatki jednego rozmiaru (lokalnie: środek ekranu w 0, ekran patrzy w +z; x w lewo patrzącego)
    static (Mesh[] m, Material[] mat) Wzor(float w, bool naScianie)
    {
        string klucz = Mathf.RoundToInt(w * 100f) + (naScianie ? "_sciana" : "_szafka");
        if (wzory.TryGetValue(klucz, out var z) && z.m.All(q => q)) return z;
        Materialy();
        float h = w * 9f / 16f, bz = .012f + w * .004f, bd = bz * 1.7f;   // ramka (dół szerszy)
        var sE = new UlSiatka(); var sR = new UlSiatka(); var sK = new UlSiatka();
        int k0 = sE.v.Count;
        sE.Quad(new Vector3(w * .5f, -h * .5f, 0f), new Vector3(-w * .5f, -h * .5f, 0f), new Vector3(-w * .5f, h * .5f, 0f), new Vector3(w * .5f, h * .5f, 0f), Vector3.forward);
        sE.uv[k0] = new Vector2(0f, 0f); sE.uv[k0 + 1] = new Vector2(1f, 0f); sE.uv[k0 + 2] = new Vector2(1f, 1f); sE.uv[k0 + 3] = new Vector2(0f, 1f);   // lewo patrzącego = +x
        // ramka: 4 listwy w płaszczyźnie ekranu (lico z = 0), głębokość 1.2 cm
        sR.Box(new Vector3(-w * .5f - bz, h * .5f, -.012f), new Vector3(w * .5f + bz, h * .5f + bz, 0f));
        sR.Box(new Vector3(-w * .5f - bz, -h * .5f - bd, -.012f), new Vector3(w * .5f + bz, -h * .5f, 0f));
        sR.Box(new Vector3(-w * .5f - bz, -h * .5f, -.012f), new Vector3(-w * .5f, h * .5f, 0f));
        sR.Box(new Vector3(w * .5f, -h * .5f, -.012f), new Vector3(w * .5f + bz, h * .5f, 0f));
        // korpus za ramką (bez przedniej ściany) i wypukła tylna część z elektroniką
        sK.Box(new Vector3(-w * .5f - bz + .003f, -h * .5f - bd + .003f, -.04f), new Vector3(w * .5f + bz - .003f, h * .5f + bz - .003f, -.012f), 32);
        sK.Poducha(new Vector3(-w * .3f, -h * .32f, -.075f), new Vector3(w * .3f, h * .3f, -.04f), .012f, 0f, 3);
        if (!naScianie)
        {   // noga i stopa (stoi na blacie: stopa od y = −h/2 − bd − .11)
            float yb = -h * .5f - bd - .11f;
            sK.Box(new Vector3(-.035f, yb + .016f, -.065f), new Vector3(.035f, -h * .5f - bd + .02f, -.04f));
            sK.Poducha(new Vector3(-w * .2f, yb, -.17f), new Vector3(w * .2f, yb + .016f, .07f), .008f, 0f, 3);
        }
        Directory.CreateDirectory(Katalog);
        var ls = new List<Mesh>(); var lm = new List<Material>();
        foreach (var (s, m) in new[] { (sE, ekran), (sR, ramka), (sK, korpus) })
        {
            var mesh = s.Zbuduj("telewizor " + klucz + " / " + m.name);
            BraxZapisAssetu.Utworz(mesh, Katalog + "/tv_" + klucz + "_" + ls.Count + ".asset");
            ls.Add(mesh); lm.Add(m);
        }
        return wzory[klucz] = (ls.ToArray(), lm.ToArray());
    }

    /// <summary>Wysokość od blatu do środka ekranu dla telewizora na nóżce (szer = szerokość ekranu, m świata).</summary>
    public static float NaSzafceDoSrodka(float szer) { float h = szer * 9f / 16f, bd = (.012f + szer * .004f) * 1.7f; return h * .5f + bd + .11f; }

    /// <summary>Telewizor na ścianie: punkt = środek na licu ściany, przod = kierunek do pomieszczenia, szer = szerokość ekranu (m).</summary>
    public static Transform NaScianie(Transform rodzic, string nazwa, Vector3 punkt, Vector3 przod, float szer)
        => Postaw(rodzic, nazwa, punkt + przod.normalized * .082f, przod, szer, true);

    /// <summary>Telewizor na nóżce: punkt = środek stopy na blacie, przod = kierunek ekranu.</summary>
    public static Transform NaSzafce(Transform rodzic, string nazwa, Vector3 punkt, Vector3 przod, float szer)
        => Postaw(rodzic, nazwa, punkt + Vector3.up * NaSzafceDoSrodka(szer), przod, szer, false);

    static Transform Postaw(Transform rodzic, string nazwa, Vector3 srodek, Vector3 przod, float szer, bool naScianie)
    {
        przod.y = 0f; przod.Normalize();
        var w = Wzor(szer, naScianie);
        var g = new GameObject("Telewizor: " + nazwa).transform; g.SetParent(rodzic, false);
        g.position = srodek; g.rotation = Quaternion.LookRotation(przod, Vector3.up);
        for (int i = 0; i < w.m.Length; i++)
        {
            var c = new GameObject(w.mat[i].name); c.transform.SetParent(g, false);
            c.AddComponent<MeshFilter>().sharedMesh = w.m[i];
            var r = c.AddComponent<MeshRenderer>(); r.sharedMaterial = w.mat[i];
            r.shadowCastingMode = w.mat[i] == ekran ? ShadowCastingMode.Off : ShadowCastingMode.On; r.receiveShadows = w.mat[i] != ekran;
        }
        Zarejestruj(rodzic, srodek);
        Postawione++;
        return g;
    }

    // jeden odtwarzacz na scenę (wspólny materiał ekranów): pozycje wszystkich telewizorów R1
    static void Zarejestruj(Transform rodzic, Vector3 p)
    {
        if (!odtwarzacz)
        {
            odtwarzacz = Object.FindObjectsByType<BraxR1ReklamyTV>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
            if (!odtwarzacz)
            {
                var g = new GameObject("R1: telewizory — filmy właściciela (BraxR1ReklamyTV)"); g.transform.SetParent(rodzic.root, false);
                odtwarzacz = g.AddComponent<BraxR1ReklamyTV>();
            }
            odtwarzacz.ekrany = new[] { ekran }; odtwarzacz.pliki = Reklamy; odtwarzacz.katalog = "R1/Reklamy";
        }
        if (odtwarzacz.telewizory.Any(q => (q - p).sqrMagnitude < .01f)) return;
        odtwarzacz.telewizory = odtwarzacz.telewizory.Concat(new[] { p }).ToArray();
    }
}
