using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// BRAXHORN CITY — PODWÓRKO: BOISKA OD NOWA (właściciel 2.11 po teście 13.1: „jakie boisko, jakie okrąg — ławki gdzieś stoją
// rozjebane”, „dosłownie wszystko do poprawy” — altana śmietnikowa; 11:00 „zawsze ściągasz, kładziesz nową, a nie na to”).
// 1) ZDJĘTE: z oryginału wycięte (siatki i kolizje) wszystko nad nawierzchnią w obrysie boisk i altany: stary kort z liniami,
//    płoty, bramki, siatka, ławki, latarnia przy ziemi, altana z kontenerami „w powietrzu” — dawny krok podmieniał tylko
//    materiał kortu i dokładał linie z kwadracików (koło środkowe z 48 kostek).
// 2) BOISKO PIŁKARSKIE (środek x 10, z 80 — punkt starcia nr 1 z danych misji): sztuczna trawa z pasami, linie ciągłe 10 cm
//    (koło środkowe, pola karne, punkty, łuki rożne) bez nakładania się (styk krawędzią), bramki 3 × 2 m × k z siatką,
//    piłkochwyty za bramkami, niska balustrada wzdłuż boków z wejściem, rząd ławek dla kibiców (równo, przodem do boiska),
//    tablica regulaminu.
// 3) BOISKO DO KOSZYKÓWKI (środek x 39, z 80): nawierzchnia akrylowa, pola trzech sekund, linie FIBA × k (rzut za 3 pkt,
//    półkola rzutów wolnych, koło środkowe), kosze: słup z osłoną, wysięgnik, tablica 1.8 × 1.05 m × k, obręcz Ø 0.45 × k
//    na 3.05 m × k, siatka; ogrodzenie z furtką, ławki.
// 4) WIATA ŚMIETNIKOWA (x 23.6..31.8, z 62.4..65): mur z bloczków, dach z blachy trapezowej, kraty, pojemniki 1100 l i 240 l
//    w kolorach segregacji (zmieszane, papier, metale i tworzywa, szkło, bio) w skali × k, stojące na posadzce.
// 5) GRA: BraxSportBoisko na obu boiskach (piłka nożna, kosz) ze skalą świata k (piłki, obręcze, kolizje × k).
public static class BraxClaudeCourts
{
    [Serializable] sealed class Report { public bool football, basketball, shelter; public Vector3 court1, court2; public Vector2 size1, size2; public int cutMeshes, cutColliders, benches, containers; public List<string> notes = new List<string>(); }
    public static readonly Vector3 SrodekPilki = new Vector3(10f, 0f, 80f), SrodekKosza = new Vector3(39f, 0f, 80f);
    public static readonly Rect ObrysWiaty = Rect.MinMaxRect(23.6f, 62.4f, 31.8f, 65.0f);
    static float K => BraxUliceKit.K;

    public static void Apply(Transform cityRoot)
    {
        if (!BraxClaudeTowerTerrace.Enabled) return;
        var rep = new Report();
        try { Run(cityRoot, rep); } catch (Exception e) { rep.notes.Add("BŁĄD: " + e.Message); Debug.LogError("BRAXHORN_CLAUDE_COURTS " + e); }
        Directory.CreateDirectory("Assets/PlayableGenerated/ClaudeGround");
        File.WriteAllText("Assets/PlayableGenerated/ClaudeGround/courts.json", JsonUtility.ToJson(rep, true));
        Debug.Log("BRAXHORN_CLAUDE_COURTS " + JsonUtility.ToJson(rep));
    }

    static void Run(Transform cityRoot, Report rep)
    {
        BraxUliceKit.Start();
        var old = cityRoot.Find("CLAUDE boiska na podwórku"); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var root = new GameObject("CLAUDE boiska na podwórku").transform; root.SetParent(cityRoot, false); root.position = Vector3.zero;
        // --- zdjęcie starego (oryginał: siatki i kolizje nad nawierzchnią) ---
        var src = BraxClaudeGround.SourceCity; var phys = BraxClaudeGround.SourcePhysics;
        foreach (var (nazwa, v) in new[] {
            ("stare boisko piłkarskie z płotami, bramkami i ławkami", MinMax(-5.5f, 25.2f, .165f, 8f, 69.5f, 91.4f)),
            ("stare boisko do kosza i otoczenie", MinMax(25.4f, 52.6f, .165f, 8f, 70.2f, 89.8f)),
            ("stara altana śmietnikowa z kontenerami", MinMax(23.3f, 32.1f, .165f, 5f, 62.2f, 67.2f)) })
        {
            if (src) rep.cutMeshes += BraxCityMeshCut.Subtract(src, v, "Assets/PlayableGenerated/ClaudeGround/Cuts", "podworko");
            if (phys) rep.cutColliders += BraxCityMeshCut.SubtractBoxColliders(phys, v);
            rep.notes.Add("zdjęte: " + nazwa + " " + v.min.ToString("F1") + ".." + v.max.ToString("F1"));
        }
        Physics.SyncTransforms();
        float g = Grunt(SrodekPilki), g2 = Grunt(SrodekKosza), gw = Grunt(new Vector3(ObrysWiaty.center.x, 0, ObrysWiaty.center.y));
        BraxUliceKit.UstawGrupe("Boisko piłkarskie");
        Pilkarskie(root, g, rep);
        BraxUliceKit.UstawGrupe("Boisko do koszykówki");
        Koszykowka(root, g2, rep);
        BraxUliceKit.UstawGrupe("Wiata śmietnikowa", 70f);
        Wiata(root, gw, rep);
        int n = BraxUliceKit.Zapisz(root, "boiska");
        rep.notes.Add("siatek " + n + ", k = " + K.ToString("F3"));
    }

    static Bounds MinMax(float x0, float x1, float y0, float y1, float z0, float z1) { var b = new Bounds(); b.SetMinMax(new Vector3(x0, y0, z0), new Vector3(x1, y1, z1)); return b; }
    static float Grunt(Vector3 p)
    {
        float best = .15f; bool found = false;
        foreach (var h in Physics.RaycastAll(new Vector3(p.x, 6f, p.z), Vector3.down, 8f, ~0, QueryTriggerInteraction.Ignore))
            if (h.normal.y > .8f && h.point.y < 1f && (!found || h.point.y > best)) { best = h.point.y; found = true; }
        return found ? best : .15f;
    }

    // ---------------- materiały ----------------
    const string OS = "Assets/Source/Claude/Osiedle/textures/";
    static Material Biala => BraxUliceKit.Mat("boisko: farba linii (biała)", new Color(.93f, .93f, .91f), .75f);
    static Material Stal => BraxUliceKit.Mat("boisko: stal ocynkowana", new Color(.62f, .63f, .64f), .4f, .85f);
    static Material StalBiala => BraxUliceKit.Mat("boisko: stal malowana biała (bramki)", new Color(.93f, .93f, .91f), .4f, .2f);
    static Material Ziel => BraxUliceKit.Mat("boisko: stal malowana zielona (ogrodzenie)", new Color(.10f, .24f, .15f), .5f, .4f);
    static Material Obrzeze => BraxUliceKit.Tex("boisko: obrzeże betonowe", 1.2f, new Color(.86f, .85f, .82f), new Color(.62f, .61f, .58f), .9f, "concrete_floor_02", "concrete_wall_008");
    static Material Siatka(string klucz, float oczko, Color kolor)
    {
        const string dir = "Assets/PlayableGenerated/ClaudeCourts";
        Directory.CreateDirectory(dir);
        string png = dir + "/siatka_" + klucz + ".png";
        if (!File.Exists(png))
        {
            const int n = 128; var t = new Texture2D(n, n, TextureFormat.RGBA32, false); var px = new Color32[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                bool line = klucz == "romb" ? ((x + y) % 32 < 3 || (x - y + 128) % 32 < 3) : ((x % 16) < 2 || (y % 16) < 2);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(line ? 255 : 0));
            }
            t.SetPixels32(px); t.Apply(); File.WriteAllBytes(png, t.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(png, ImportAssetOptions.ForceSynchronousImport);
            var imp = (TextureImporter)AssetImporter.GetAtPath(png);
            imp.textureType = TextureImporterType.Default; imp.alphaIsTransparency = true; imp.alphaSource = TextureImporterAlphaSource.FromInput;
            imp.mipmapEnabled = true; imp.mipMapsPreserveCoverage = true; imp.alphaTestReferenceValue = .5f;
            imp.wrapMode = TextureWrapMode.Repeat; imp.filterMode = FilterMode.Trilinear; imp.anisoLevel = 4; imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(png); var sh = Shader.Find(BraxCityMaterials.ShaderName);
        string path = dir + "/siatka_" + klucz + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m) return m;
        if (!tex || !sh) return BraxUliceKit.Mat("siatka " + klucz + " (bez tekstury)", new Color(kolor.r, kolor.g, kolor.b, .35f), .9f);
        m = new Material(sh) { name = "Boisko: siatka " + klucz };
        m.SetTexture("_MainTex", tex); m.SetTextureScale("_MainTex", Vector2.one / oczko); m.SetColor("_Color", kolor);
        m.SetFloat("_Roughness", .8f); m.SetFloat("_Metallic", klucz == "romb" ? .5f : 0f); m.SetFloat("_AlphaClip", 1); m.SetFloat("_Cutoff", .5f); m.SetFloat("_Cull", 0);
        m.renderQueue = 2450; m.SetOverrideTag("RenderType", "TransparentCutout"); m.doubleSidedGI = true;
        BraxZapisAssetu.Utworz(m, path); return m;
    }

    // ---------------- LINIE W TEKSTURZE NAWIERZCHNI (A 2.11; właściciel 15:40 „NIGDY nic nie nakładamy”) ----------------
    // Obchód boisk: linie leżały 8 mm NAD trawą i akrylem (nakładka — migotanie z daleka). Teraz te same trójkąty linii (pasy, łuki,
    // punkty) są rasteryzowane do maski 2048 × 1024 (≈ 1.5 cm na piksel, 4 próbki na piksel), a nawierzchnię rysuje shader
    // Braxhorn/BoiskoLinie (kopia CityStaticPBR + maska): farba linii jest W tej samej płaszczyźnie co nawierzchnia.
    static void Dolacz(UlSiatka cel, UlSiatka z)
    {
        int o = cel.v.Count;
        cel.v.AddRange(z.v); cel.n.AddRange(z.n); cel.t.AddRange(z.t); cel.uv.AddRange(z.uv); cel.c.AddRange(z.c);
        foreach (int i in z.i) cel.i.Add(i + o);
    }
    static Material NawierzchniaZLiniami(Material baza, UlSiatka linie, float x0, float x1, float z0, float z1, string klucz, Report rep)
    {
        const string dir = "Assets/PlayableGenerated/ClaudeCourts"; Directory.CreateDirectory(dir);
        var sh = Shader.Find("Braxhorn/BoiskoLinie");
        if (!sh) { rep.notes.Add("BRAK shadera Braxhorn/BoiskoLinie — nawierzchnia " + klucz + " bez linii (nie nakładamy płytek)"); return baza; }
        const int MW = 2048, MH = 1024; var cov = new float[MW * MH];
        float sx = MW / (x1 - x0), sz = MH / (z1 - z0);
        Vector2 P(Vector3 w) => new Vector2((w.x - x0) * sx, (w.z - z0) * sz);
        float E(Vector2 a, Vector2 b, Vector2 q) => (b.x - a.x) * (q.y - a.y) - (b.y - a.y) * (q.x - a.x);
        for (int t = 0; t + 2 < linie.i.Count; t += 3)
        {
            Vector2 a = P(linie.v[linie.i[t]]), b = P(linie.v[linie.i[t + 1]]), c = P(linie.v[linie.i[t + 2]]);
            float ar = E(a, b, c); if (Mathf.Abs(ar) < 1e-6f) continue; float sg = Mathf.Sign(ar);
            int mx0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x)))), mx1 = Mathf.Min(MW - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
            int my0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y)))), my1 = Mathf.Min(MH - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));
            for (int y = my0; y <= my1; y++)
                for (int x = mx0; x <= mx1; x++)
                {
                    int hit = 0;
                    for (int q = 0; q < 4; q++)
                    {
                        var pq = new Vector2(x + .25f + .5f * (q & 1), y + .25f + .5f * (q >> 1));
                        if (E(b, c, pq) * sg >= 0f && E(c, a, pq) * sg >= 0f && E(a, b, pq) * sg >= 0f) hit++;
                    }
                    if (hit > 0) cov[y * MW + x] += hit * .25f;
                }
        }
        var px = new Color[MW * MH]; for (int i = 0; i < px.Length; i++) { float v = Mathf.Clamp01(cov[i]); px[i] = new Color(v, v, v, 1f); }
        string png = dir + "/linie_" + klucz + ".png";
        var tx = new Texture2D(MW, MH, TextureFormat.RGB24, false); tx.SetPixels(px); tx.Apply(); File.WriteAllBytes(png, tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx);
        AssetDatabase.ImportAsset(png, ImportAssetOptions.ForceSynchronousImport);
        if (AssetImporter.GetAtPath(png) is TextureImporter imp)
        {
            imp.sRGBTexture = false; imp.wrapMode = TextureWrapMode.Clamp; imp.mipmapEnabled = true; imp.filterMode = FilterMode.Trilinear; imp.anisoLevel = 8;
            imp.maxTextureSize = 2048; imp.textureCompression = TextureImporterCompression.Compressed; imp.SaveAndReimport();
        }
        var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(png);
        var m = new Material(sh) { name = "Boisko: nawierzchnia z liniami (" + klucz + ")" };
        m.CopyPropertiesFromMaterial(baza);
        m.SetTexture("_Linie", mask); m.SetVector("_LinieObszar", new Vector4(x0, z0, 1f / (x1 - x0), 1f / (z1 - z0)));
        m.SetColor("_LinieKolor", new Color(.93f, .93f, .91f)); m.SetFloat("_LinieSzorstkosc", .75f);
        string mp = dir + "/nawierzchnia_" + klucz + ".mat";   // C1: bez usuwania (BraxZapisAssetu)
        BraxZapisAssetu.Utworz(m, mp);
        rep.notes.Add("linie " + klucz + " w teksturze nawierzchni: " + (linie.i.Count / 3) + " trójkątów → maska " + MW + "×" + MH);
        Debug.Log("BRAXHORN_MIASTO_A_BOISKA linie " + klucz + " w teksturze (maska " + MW + "×" + MH + ", trójkątów " + (linie.i.Count / 3) + ", nakładek 0)");
        return m;
    }

    // ---------------- linie (geometria do maski; w scenie nie ma osobnych płytek linii) ----------------
    static void Pas(UlSiatka s, float x0, float z0, float x1, float z1, float y)
    {
        if (x1 - x0 < .002f || z1 - z0 < .002f) return;
        s.Quad(new Vector3(x0, y, z0), new Vector3(x1, y, z0), new Vector3(x1, y, z1), new Vector3(x0, y, z1), Vector3.up);
    }

    // ---------------- boisko piłkarskie ----------------
    static void Pilkarskie(Transform root, float g, Report rep)
    {
        float k = K, cx = SrodekPilki.x, cz = SrodekPilki.z, hx = 13f, hz = 7.5f, w = .1f, yS = g + .025f, yL = yS + .008f;
        float x0 = cx - hx, x1 = cx + hx, z0 = cz - hz, z1 = cz + hz;
        float bramkaW = 3f * k, bramkaH = 2f * k, bramkaD = 1.0f * k;
        float sx0 = x0 - bramkaD - .5f, sx1 = x1 + bramkaD + .5f, sz0 = z0 - 1.5f, sz1 = z1 + 1.5f;   // płyta z wybiegiem
        // nawierzchnia: sztuczna trawa w pasach (kolor wierzchołków), boki płyty 2.5 cm, obrzeże betonowe
        var trawa = BraxUliceKit.TexPlik("boisko: sztuczna trawa", OS, "grass004", 1.1f, new Color(.55f, .92f, .48f), new Color(.20f, .48f, .18f), .9f);
        var st = new UlSiatka(); int pas = 0;     // nawierzchnia: materiał z maską linii dopiero po narysowaniu linii (niżej)
        for (float x = sx0; x < sx1 - .001f; x += 2.6f, pas++)
        {
            float xe = Mathf.Min(x + 2.6f, sx1); st.kolor = pas % 2 == 0 ? BraxUliceKit.C(1f, 1f, 1f) : BraxUliceKit.C(.90f, .94f, .88f);
            st.Quad(new Vector3(x, yS, sz0), new Vector3(xe, yS, sz0), new Vector3(xe, yS, sz1), new Vector3(x, yS, sz1), Vector3.up);
        }
        st.kolor = BraxUliceKit.Bialy;
        var ob = BraxUliceKit.S(Obrzeze);
        ob.Box(new Vector3(sx0 - .12f, g - .02f, sz0 - .12f), new Vector3(sx1 + .12f, yS + .02f, sz0), 4);
        ob.Box(new Vector3(sx0 - .12f, g - .02f, sz1), new Vector3(sx1 + .12f, yS + .02f, sz1 + .12f), 4);
        ob.Box(new Vector3(sx0 - .12f, g - .02f, sz0), new Vector3(sx0, yS + .02f, sz1), 4);
        ob.Box(new Vector3(sx1, g - .02f, sz0), new Vector3(sx1 + .12f, yS + .02f, sz1), 4);
        // kolizja nawierzchni (piłka i gracz): płyta pod trawą
        var kol = new GameObject("Boisko piłkarskie: nawierzchnia (kolizja)"); kol.transform.SetParent(root, false);
        var bc = kol.AddComponent<BoxCollider>(); bc.center = new Vector3((sx0 + sx1) * .5f, yS - .05f, (sz0 + sz1) * .5f); bc.size = new Vector3(sx1 - sx0, .1f, sz1 - sz0);
        // linie: do maski w teksturze nawierzchni (NIE jako płytki nad trawą)
        var l = new UlSiatka(); float h = w * .5f;
        Pas(l, x0 - h, z0 - h, x1 + h, z0 + h, yL); Pas(l, x0 - h, z1 - h, x1 + h, z1 + h, yL);           // boczne (pełne)
        Pas(l, x0 - h, z0 + h, x0 + h, z1 - h, yL); Pas(l, x1 - h, z0 + h, x1 + h, z1 - h, yL);           // bramkowe (między bocznymi)
        float R = 3f * k, rs = .12f;                                                                      // koło środkowe r 3 m × k
        Pas(l, cx - h, z0 + h, cx + h, cz - R - h, yL); Pas(l, cx - h, cz + R + h, cx + h, z1 - h, yL);   // środkowa poza kołem
        Pas(l, cx - h, cz - R + h, cx + h, cz - rs, yL); Pas(l, cx - h, cz + rs, cx + h, cz + R - h, yL);   // środkowa w kole (bez punktu)
        l.Pierscien(new Vector3(cx, yL, cz), R - h, R + h, 0, 360, 96);
        l.Kolo(new Vector3(cx, yL, cz), rs, 20);
        foreach (int sg in new[] { -1, 1 })
        {
            float xg = sg < 0 ? x0 : x1, gl = 5f * k * .8f, pw = 4.6f;                                    // pole karne: głębokość 5.2, szerokość 9.2
            float xf = xg - sg * gl;
            Pas(l, Mathf.Min(xf, xf) - h, cz - pw - h, xf + h, cz + pw + h, yL);                          // linia przednia pola karnego
            float a0 = sg < 0 ? xg + h : xf + h, a1 = sg < 0 ? xf - h : xg - h;
            Pas(l, a0, cz - pw - h, a1, cz - pw + h, yL); Pas(l, a0, cz + pw - h, a1, cz + pw + h, yL);   // boki pola karnego
            l.Kolo(new Vector3(xg - sg * 4f * k * .9f, yL, cz), rs, 20);                                   // punkt karny
            foreach (int sz in new[] { -1, 1 })                                                            // łuki rożne r 0.5 × k
            {
                float zc = sz < 0 ? z0 : z1, a = sg < 0 ? (sz < 0 ? 0f : 270f) : (sz < 0 ? 90f : 180f);
                l.Pierscien(new Vector3(xg, yL, zc), .5f * k - h, .5f * k + h, a + 12f, a + 78f, 10);
            }
            Bramka(xg, g, cz, -sg, bramkaW, bramkaH, bramkaD, root);
            // piłkochwyt za bramką (4.5 m), słupy co 2.5 m, siatka w kratę
            float xp = xg + sg * (bramkaD + .4f);
            Ogrodzenie(root, new Vector3(xp, g, z0 - 1.2f), new Vector3(xp, g, z1 + 1.2f), 4.5f, Siatka("krata", .125f, new Color(.2f, .22f, .2f)), "piłkochwyt");
        }
        Dolacz(BraxUliceKit.S(NawierzchniaZLiniami(trawa, l, sx0, sx1, sz0, sz1, "pilka", rep)), st);
        // balustrada wzdłuż boków (1.2 m × k), wejścia na wprost pawilonu (południe) i ławek (północ)
        float bz0 = sz0 - .25f, bz1 = sz1 + .25f;
        foreach (float bz in new[] { bz0, bz1 })
        {
            Balustrada(root, new Vector3(sx0, g, bz), new Vector3(cx - 1.4f, g, bz));
            Balustrada(root, new Vector3(cx + 1.4f, g, bz), new Vector3(sx1, g, bz));
        }
        // kibice: rząd 4 ławek przodem do boiska (północ, z ≈ 90) — tu czeka przeciwnik misji (punkt fight1_enemy_wait 10, 89.4)
        float lz = bz1 + .9f; int n = 0;
        foreach (float dx in new[] { -3.9f, -1.3f, 1.3f, 3.9f }) { Lawka(root, new Vector3(cx + dx, g, lz), 180f, "Ławka dla kibiców (boisko piłkarskie)"); n++; }
        Tablica(root, new Vector3(cx - 4.2f, g, bz0 - 1.1f), 0f, "REGULAMIN BOISKA");
        rep.benches += n;
        rep.football = true; rep.court1 = new Vector3(cx, yS, cz); rep.size1 = new Vector2(hx * 2, hz * 2);
        // gra: piłka nożna × k
        var b = new GameObject("Gra: piłka nożna (podwórko)").AddComponent<BraxSportBoisko>(); b.transform.SetParent(root, false);
        b.gra = BraxSportBoisko.Gra.Nozna; b.srodek = new Vector3(cx, yS, cz); b.osX = Vector3.right; b.pol = new Vector2(hx, hz); b.strefa = new Vector2(hx + 2.5f, hz + 2f);
        b.cele = new[] { new Vector3(x0, yS, cz), new Vector3(x1, yS, cz) }; b.bramkaSzer = bramkaW; b.bramkaWys = bramkaH; b.bramkaGl = bramkaD;
    }

    // Bramka: linia bramkowa x = gx, wnętrze w stronę ox (−1 = na zachód); słupki i poprzeczka z rur, rama tylna, siatka.
    static void Bramka(float gx, float g, float gz, float ox, float w, float hgt, float d, Transform root)
    {
        float k = K, r = .06f * k, xb = gx - ox * d, hw = w * .5f;   // ox: kierunek do środka boiska; bramka wychodzi na zewnątrz
        var s = BraxUliceKit.S(StalBiala);
        foreach (int sg in new[] { -1, 1 })
        {
            float zz = gz + sg * (hw + r);
            s.Rura(new Vector3(gx, g, zz), new Vector3(gx, g + hgt + r, zz), r, 12);
            s.Rura(new Vector3(gx, g + hgt, zz), new Vector3(xb, g + hgt * .85f, zz), r * .45f, 8);     // wysięgnik siatki
            s.Rura(new Vector3(xb, g + .03f, zz), new Vector3(xb, g + hgt * .85f, zz), r * .45f, 8);
            s.Rura(new Vector3(gx, g + .03f, zz), new Vector3(xb, g + .03f, zz), r * .45f, 8);          // rama dolna
            var c = new GameObject("Bramka: słupek (kolizja)"); c.transform.SetParent(root, false); c.transform.position = new Vector3(gx, g + (hgt + r) * .5f, zz);
            var cc = c.AddComponent<CapsuleCollider>(); cc.radius = r; cc.height = hgt + r;
        }
        s.Rura(new Vector3(gx, g + hgt + r, gz - hw - 2 * r), new Vector3(gx, g + hgt + r, gz + hw + 2 * r), r, 12);   // poprzeczka
        s.Rura(new Vector3(xb, g + .03f, gz - hw - r), new Vector3(xb, g + .03f, gz + hw + r), r * .45f, 8);
        s.Rura(new Vector3(xb, g + hgt * .85f, gz - hw - r), new Vector3(xb, g + hgt * .85f, gz + hw + r), r * .45f, 8);
        var n = BraxUliceKit.S(Siatka("bramki", .125f, new Color(.94f, .94f, .92f)));
        float x0 = Mathf.Min(gx, xb), x1 = Mathf.Max(gx, xb);
        n.Quad(new Vector3(xb, g, gz - hw), new Vector3(xb, g, gz + hw), new Vector3(xb, g + hgt * .85f, gz + hw), new Vector3(xb, g + hgt * .85f, gz - hw), new Vector3(-ox, 0, 0));   // tył
        foreach (int sg in new[] { -1, 1 }) { float zz = gz + sg * hw; n.Quad(new Vector3(gx, g, zz), new Vector3(xb, g, zz), new Vector3(xb, g + hgt * .85f, zz), new Vector3(gx, g + hgt, zz), new Vector3(0, 0, sg)); }
        n.Quad(new Vector3(gx, g + hgt, gz - hw), new Vector3(gx, g + hgt, gz + hw), new Vector3(xb, g + hgt * .85f, gz + hw), new Vector3(xb, g + hgt * .85f, gz - hw), Vector3.up);   // dach siatki
    }

    // Ogrodzenie z siatką (od a do b): słupy co 2.5 m, rury górna, środkowa i dolna, siatka; kolizja (cienka płyta).
    static void Ogrodzenie(Transform root, Vector3 a, Vector3 b, float wys, Material siatka, string nazwa, float bramkaOd = -1f, float bramkaDo = -1f)
    {
        var s = BraxUliceKit.S(Ziel); var dir = (b - a); float L = dir.magnitude; dir /= L; var n = Vector3.Cross(Vector3.up, dir);
        int seg = Mathf.Max(1, Mathf.CeilToInt(L / 2.5f));
        for (int i = 0; i <= seg; i++) { var p = a + dir * (L * i / seg); s.Rura(p, p + Vector3.up * wys, .045f, 8); }
        void Odcinek(float t0, float t1)
        {
            if (t1 - t0 < .05f) return;
            Vector3 p0 = a + dir * t0, p1 = a + dir * t1;
            foreach (float y in new[] { .05f, wys * .5f, wys - .03f }) s.Rura(p0 + Vector3.up * y, p1 + Vector3.up * y, .025f, 6);
            BraxUliceKit.S(siatka).Quad(p0 + Vector3.up * .05f, p1 + Vector3.up * .05f, p1 + Vector3.up * (wys - .03f), p0 + Vector3.up * (wys - .03f), n);
            var c = new GameObject("kolizja: " + nazwa); c.transform.SetParent(root, false); c.transform.position = (p0 + p1) * .5f + Vector3.up * wys * .5f;
            c.transform.rotation = Quaternion.LookRotation(n); var bc = c.AddComponent<BoxCollider>(); bc.size = new Vector3(t1 - t0, wys, .06f);
        }
        if (bramkaOd > 0f) { Odcinek(0f, bramkaOd); Odcinek(bramkaDo, L); } else Odcinek(0f, L);
    }

    // Niska balustrada (1.1 m × k): pochwyt z rury, poręcz środkowa, słupki co 2 m; kolizja.
    static void Balustrada(Transform root, Vector3 a, Vector3 b)
    {
        float k = K, wys = 1.1f * k; var s = BraxUliceKit.S(Ziel); var dir = b - a; float L = dir.magnitude; if (L < .3f) return; dir /= L;
        int seg = Mathf.Max(1, Mathf.CeilToInt(L / 2f));
        for (int i = 0; i <= seg; i++) { var p = a + dir * (L * i / seg); s.Rura(p, p + Vector3.up * wys, .04f, 8); }
        s.Rura(a + Vector3.up * wys, b + Vector3.up * wys, .035f, 8); s.Rura(a + Vector3.up * wys * .5f, b + Vector3.up * wys * .5f, .022f, 6);
        var c = new GameObject("kolizja: balustrada boiska"); c.transform.SetParent(root, false); c.transform.position = (a + b) * .5f + Vector3.up * wys * .5f;
        c.transform.rotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, dir)); c.AddComponent<BoxCollider>().size = new Vector3(L, wys, .1f);
    }

    // Ławka parkowa × k: żeliwne boki, siedzisko i oparcie z listew; osobny obiekt (BraxRegionSeats robi z niej siedzisko).
    static readonly Dictionary<string, Mesh> siatki = new Dictionary<string, Mesh>();
    public static GameObject Lawka(Transform root, Vector3 at, float yaw, string nazwa)
    {
        float k = K;
        var drewno = BraxUliceKit.Mat("ławka: listwy dębowe olejowane", new Color(.48f, .33f, .20f), .6f);
        var zeliwo = BraxUliceKit.Mat("ławka: żeliwo grafit", new Color(.08f, .085f, .09f), .55f, .45f);
        Mesh M(string kl, Action<UlSiatka> f)
        {
            if (siatki.TryGetValue(kl, out var m) && m) return m;
            var s = new UlSiatka(); f(s); m = s.Zbuduj("Ulice: " + kl); Directory.CreateDirectory(BraxUliceKit.Out);
            BraxZapisAssetu.Utworz(m, BraxUliceKit.Out + "/" + BraxUliceKit.Bezpieczna(kl) + ".asset"); siatki[kl] = m; return m;
        }
        float L = 1.8f * k, H = .45f * k, D = .45f * k;
        var listwy = M("ławka listwy", s =>
        {
            for (int i = 0; i < 4; i++) { float z = -D * .5f + .02f + i * (D - .04f) / 3.5f; s.Box(new Vector3(-L * .5f, H - .035f, z - .035f), new Vector3(L * .5f, H, z + .035f)); }
            for (int i = 0; i < 3; i++) { float y = H + .1f + i * .13f * k; float z = D * .5f + .02f + i * .03f; s.Box(new Vector3(-L * .5f, y, z - .02f), new Vector3(L * .5f, y + .08f * k, z + .02f)); }
        });
        var boki = M("ławka boki żeliwne", s =>
        {
            foreach (float x in new[] { -L * .5f + .18f, L * .5f - .18f })
            {
                s.Box(new Vector3(x - .03f, 0, -D * .5f), new Vector3(x + .03f, .04f, D * .5f + .05f));                      // stopa
                s.Box(new Vector3(x - .03f, 0, -D * .5f + .03f), new Vector3(x + .03f, H - .035f, -D * .5f + .09f));         // noga przednia
                s.Box(new Vector3(x - .03f, 0, D * .5f - .02f), new Vector3(x + .03f, H + .5f * k, D * .5f + .05f));         // noga tylna z oparciem
                s.Box(new Vector3(x - .03f, H - .07f, -D * .5f + .03f), new Vector3(x + .03f, H - .035f, D * .5f));          // wspornik siedziska
                s.Box(new Vector3(x - .03f, H + .2f * k, -D * .5f + .03f), new Vector3(x + .03f, H + .24f * k, -D * .5f + .2f));   // podłokietnik
                s.Box(new Vector3(x - .03f, H, -D * .5f + .03f), new Vector3(x + .03f, H + .2f * k, -D * .5f + .07f));
            }
        });
        var g = new GameObject(nazwa); g.transform.SetParent(root, false); g.transform.position = at; g.transform.rotation = Quaternion.Euler(0, yaw, 0);
        foreach (var (m, mat, n) in new[] { (listwy, drewno, "listwy"), (boki, zeliwo, "boki") })
        {
            var c = new GameObject(nazwa + ": " + n); c.transform.SetParent(g.transform, false);
            c.AddComponent<MeshFilter>().sharedMesh = m; c.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }
        var bc = g.AddComponent<BoxCollider>(); bc.center = new Vector3(0, H * .5f, 0); bc.size = new Vector3(L, H, D);
        return g;
    }

    static void Tablica(Transform root, Vector3 at, float yaw, string tekst)
    {
        float k = K; var q = Quaternion.Euler(0, yaw, 0); var R = q * Vector3.right; var N = q * Vector3.back;
        var s = BraxUliceKit.S(Ziel); var tlo = BraxUliceKit.S(BraxUliceKit.Mat("tablica regulaminu: lico białe", new Color(.93f, .93f, .9f), .5f));
        float w = 1.0f * k, hh = .7f * k, y0 = 1.0f * k;
        foreach (float sg in new[] { -1f, 1f }) s.Rura(at + R * sg * w * .5f, at + R * sg * w * .5f + Vector3.up * (y0 + hh + .05f), .03f, 8);
        s.OBox(at + Vector3.up * (y0 + hh * .5f), R, Vector3.up, N, w + .06f, hh + .06f, .03f);
        // lico tablicy: biała płyta 1.2 cm PRZED ramą (bryła, nie naklejka 1 mm), tekst i wiersze regulaminu jako bryły 1 cm
        var f = at + Vector3.up * (y0 + hh * .5f) + N * (.015f + .012f);
        tlo.OBox(f - N * .006f, R, Vector3.up, N, w, hh, .012f, true);
        var czarne = BraxUliceKit.Mat("tablica regulaminu: litery", new Color(.08f, .08f, .09f), .5f);
        BraxUliceKit.Napis3D(tekst, f + Vector3.up * (hh * .32f), N, .07f * k, .01f, czarne, czarne);
        for (int i = 0; i < 6; i++)
        {
            float dl = w * (.8f - (i % 3) * .1f);
            BraxUliceKit.S(czarne).OBox(f + N * .005f - R * w * .4f + R * dl * .5f + Vector3.up * (hh * .15f - i * .07f * k), R, Vector3.up, N, dl, .012f, .01f, true);
        }
    }

    // ---------------- koszykówka ----------------
    static void Koszykowka(Transform root, float g, Report rep)
    {
        float k = K, cx = SrodekKosza.x, cz = SrodekKosza.z, hl = 18f * k * .5f, hw = 11f * k * .5f, w = .05f * k, h = w * .5f;
        float yS = g + .02f, yL = yS + .008f;
        float sx0 = cx - hl - 1.5f, sx1 = cx + hl + 1.5f, sz0 = cz - hw - 1.5f, sz1 = cz + hw + 1.5f;
        var akryl = BraxUliceKit.Mat("boisko do kosza: akryl (kolory z wierzchołków)", Color.white, .7f);
        var s = new UlSiatka();     // nawierzchnia: materiał z maską linii po narysowaniu linii
        Color32 wybieg = BraxUliceKit.C(.20f, .42f, .30f), pole = BraxUliceKit.C(.16f, .30f, .52f), trzy = BraxUliceKit.C(.80f, .42f, .18f);
        float kD = 5.8f * k, kW = 4.9f * k * .5f;      // pole trzech sekund
        // nawierzchnia jedną płaszczyzną: siatka komórek, kolor wg obszaru (bez nakładania)
        var xs = new SortedSet<float> { sx0, sx1, cx - hl, cx + hl, cx - hl + kD, cx + hl - kD }; var zs = new SortedSet<float> { sz0, sz1, cz - hw, cz + hw, cz - kW, cz + kW };
        var X = xs.ToArray(); var Z = zs.ToArray();
        for (int i = 0; i < X.Length - 1; i++) for (int j = 0; j < Z.Length - 1; j++)
        {
            float mx = (X[i] + X[i + 1]) * .5f, mz = (Z[j] + Z[j + 1]) * .5f;
            bool wPolu = Mathf.Abs(mx - cx) < hl && Mathf.Abs(mz - cz) < hw;
            bool wTrzech = wPolu && Mathf.Abs(mz - cz) < kW && Mathf.Abs(mx - cx) > hl - kD;
            s.kolor = wTrzech ? trzy : wPolu ? pole : wybieg;
            s.Quad(new Vector3(X[i], yS, Z[j]), new Vector3(X[i + 1], yS, Z[j]), new Vector3(X[i + 1], yS, Z[j + 1]), new Vector3(X[i], yS, Z[j + 1]), Vector3.up);
        }
        s.kolor = BraxUliceKit.Bialy;
        var ob = BraxUliceKit.S(Obrzeze);
        ob.Box(new Vector3(sx0 - .12f, g - .02f, sz0 - .12f), new Vector3(sx1 + .12f, yS + .02f, sz0), 4);
        ob.Box(new Vector3(sx0 - .12f, g - .02f, sz1), new Vector3(sx1 + .12f, yS + .02f, sz1 + .12f), 4);
        ob.Box(new Vector3(sx0 - .12f, g - .02f, sz0), new Vector3(sx0, yS + .02f, sz1), 4);
        ob.Box(new Vector3(sx1, g - .02f, sz0), new Vector3(sx1 + .12f, yS + .02f, sz1), 4);
        var kol = new GameObject("Boisko do koszykówki: nawierzchnia (kolizja)"); kol.transform.SetParent(root, false);
        var bc = kol.AddComponent<BoxCollider>(); bc.center = new Vector3(cx, yS - .05f, cz); bc.size = new Vector3(sx1 - sx0, .1f, sz1 - sz0);
        // linie (białe, 5 cm × k), styk krawędziami — do maski w teksturze nawierzchni
        var l = new UlSiatka();
        float x0 = cx - hl, x1 = cx + hl, z0 = cz - hw, z1 = cz + hw;
        Pas(l, x0 - h, z0 - h, x1 + h, z0 + h, yL); Pas(l, x0 - h, z1 - h, x1 + h, z1 + h, yL);
        Pas(l, x0 - h, z0 + h, x0 + h, z1 - h, yL); Pas(l, x1 - h, z0 + h, x1 + h, z1 - h, yL);
        float Rc = 1.8f * k;
        Pas(l, cx - h, z0 + h, cx + h, cz - Rc - h, yL); Pas(l, cx - h, cz + Rc + h, cx + h, z1 - h, yL); Pas(l, cx - h, cz - Rc + h, cx + h, cz + Rc - h, yL);
        l.Pierscien(new Vector3(cx, yL, cz), Rc - h, Rc + h, 0, 360, 72);
        var kosze = new List<Vector3>(); var tablice = new List<Vector3>();
        foreach (int sg in new[] { -1, 1 })
        {
            float xe = sg < 0 ? x0 : x1, xf = xe - sg * kD;                 // linia końcowa, linia rzutów wolnych
            Pas(l, Mathf.Min(xe, xf) + (sg < 0 ? h : -h) * 0f + (sg < 0 ? h : 0f) - (sg < 0 ? 0f : 0f), cz - kW - h, Mathf.Max(xe, xf), cz - kW + h, yL);
            Pas(l, Mathf.Min(xe, xf) + (sg < 0 ? h : 0f), cz + kW - h, Mathf.Max(xe, xf) - (sg > 0 ? h : 0f), cz + kW + h, yL);
            Pas(l, xf - h, cz - kW + h, xf + h, cz + kW - h, yL);
            // półkole rzutów wolnych (na zewnątrz pola trzech sekund)
            float a0 = sg < 0 ? -90f : 90f; l.Pierscien(new Vector3(xf, yL, cz), Rc - h, Rc + h, a0 + 3f, a0 + 177f, 40);
            // linia za 3 pkt: proste przy bokach (0.9 m × k od linii bocznej) i łuk r 6.75 m × k od środka kosza
            float xb = xe - sg * 1.575f * k, R3 = 6.75f * k, zc = hw - .9f * k, dx = Mathf.Sqrt(Mathf.Max(0f, R3 * R3 - zc * zc));
            float xs3 = xb - sg * dx;
            Pas(l, Mathf.Min(xe, xs3) + (sg < 0 ? h : 0f), cz - zc - h, Mathf.Max(xe, xs3) - (sg > 0 ? h : 0f), cz - zc + h, yL);
            Pas(l, Mathf.Min(xe, xs3) + (sg < 0 ? h : 0f), cz + zc - h, Mathf.Max(xe, xs3) - (sg > 0 ? h : 0f), cz + zc + h, yL);
            float th = Mathf.Asin(zc / R3) * Mathf.Rad2Deg, srod = sg < 0 ? 0f : 180f;
            l.Pierscien(new Vector3(xb, yL, cz), R3 - h, R3 + h, srod - th, srod + th, 64);
            // kosz: słup 1 m × k za linią końcową, wysięgnik, tablica (lico 1.2 m × k od linii), obręcz na 3.05 m × k
            float xSl = xe + sg * 1.0f * k, xt = xe - sg * 1.2f * k, yr = g + 3.05f * k, yt0 = g + 2.9f * k, tbW = 1.8f * k, tbH = 1.05f * k;
            var stal = BraxUliceKit.S(Stal);
            stal.Box(new Vector3(xSl - .1f, g, cz - .1f), new Vector3(xSl + .1f, yt0 + .5f, cz + .1f), 4);
            stal.Box(new Vector3(Mathf.Min(xSl, xt) + (sg < 0 ? .1f : .03f), yt0 + .25f, cz - .06f), new Vector3(Mathf.Max(xSl, xt) - (sg < 0 ? .03f : .1f), yt0 + .37f, cz + .06f));
            stal.Rura(new Vector3(xSl, g + 2.2f * k, cz), new Vector3(xt + sg * .06f, yt0 + .3f, cz), .035f, 8);   // zastrzał
            var oslona = BraxUliceKit.S(BraxUliceKit.Mat("kosz: osłona słupa (pianka granatowa)", new Color(.10f, .16f, .35f), .8f));
            oslona.Box(new Vector3(xSl - .16f, g + .05f, cz - .16f), new Vector3(xSl + .16f, g + 1.9f * k, cz + .16f), 4);
            var tbl = BraxUliceKit.S(BraxUliceKit.Mat("kosz: tablica (laminat biały)", new Color(.94f, .94f, .93f), .3f));
            tbl.Box(new Vector3(xt - .025f, yt0, cz - tbW * .5f), new Vector3(xt + .025f, yt0 + tbH, cz + tbW * .5f));
            var ram = BraxUliceKit.S(BraxUliceKit.Mat("kosz: ramka i kwadrat tablicy (czerwone)", new Color(.72f, .16f, .12f), .5f));
            float fx = xt - sg * .031f, kw = .59f * k * .5f, ky0 = yr + .15f * k * 0f, ky1 = ky0 + .45f * k, t = .05f;
            ram.Box(new Vector3(fx - .006f, ky0, cz - kw), new Vector3(fx + .006f, ky0 + t, cz + kw)); ram.Box(new Vector3(fx - .006f, ky1 - t, cz - kw), new Vector3(fx + .006f, ky1, cz + kw));
            ram.Box(new Vector3(fx - .006f, ky0 + t, cz - kw), new Vector3(fx + .006f, ky1 - t, cz - kw + t)); ram.Box(new Vector3(fx - .006f, ky0 + t, cz + kw - t), new Vector3(fx + .006f, ky1 - t, cz + kw));
            // obręcz Ø 0.45 m × k, 0.15 m × k przed tablicą, mocowanie, siatka z linek
            var obr = BraxUliceKit.S(BraxUliceKit.Mat("kosz: obręcz (stal pomarańczowa)", new Color(.88f, .36f, .08f), .4f, .6f));
            float rr = .225f * k, xr = xt - sg * (.15f * k + rr);
            for (int i = 0; i < 24; i++) { float a = i * Mathf.PI * 2 / 24f, b2 = (i + 1) * Mathf.PI * 2 / 24f; obr.Rura(new Vector3(xr + Mathf.Cos(a) * rr, yr, cz + Mathf.Sin(a) * rr), new Vector3(xr + Mathf.Cos(b2) * rr, yr, cz + Mathf.Sin(b2) * rr), .011f * k, 6, false); }
            obr.Box(new Vector3(Mathf.Min(xt, xr + sg * rr) - .01f, yr - .05f, cz - .08f), new Vector3(Mathf.Max(xt, xr + sg * rr) + .01f, yr, cz + .08f));
            var siat = BraxUliceKit.S(BraxUliceKit.Mat("kosz: siatka (linka biała)", new Color(.95f, .95f, .93f), .9f));
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2 / 12f, a2 = a + Mathf.PI / 12f * 2f;
                var top = new Vector3(xr + Mathf.Cos(a) * rr, yr - .01f, cz + Mathf.Sin(a) * rr);
                var bot = new Vector3(xr + Mathf.Cos(a2) * rr * .55f, yr - .42f * k, cz + Mathf.Sin(a2) * rr * .55f);
                siat.Rura(top, bot, .005f * k, 4, false);
                var bot2 = new Vector3(xr + Mathf.Cos(a - Mathf.PI / 6f) * rr * .55f, yr - .42f * k, cz + Mathf.Sin(a - Mathf.PI / 6f) * rr * .55f);
                siat.Rura(top, bot2, .005f * k, 4, false);
            }
            var cs = new GameObject("Kosz: słup (kolizja)"); cs.transform.SetParent(root, false); cs.transform.position = new Vector3(xSl, g + 1.2f, cz);
            cs.AddComponent<BoxCollider>().size = new Vector3(.34f, 2.4f, .34f);
            kosze.Add(new Vector3(xr, yr, cz)); tablice.Add(new Vector3(xt, yt0 + tbH * .5f, cz));
        }
        Dolacz(BraxUliceKit.S(NawierzchniaZLiniami(akryl, l, sx0, sx1, sz0, sz1, "kosz", rep)), s);
        // ogrodzenie dookoła (3 m × k) z furtką od południa (od strony pawilonu i wiaty)
        float fz0 = sz0 - .3f, fz1 = sz1 + .3f, fx0 = sx0 - .3f, fx1 = sx1 + .3f, fw = 3f * k; var krata = Siatka("krata", .125f, new Color(.2f, .22f, .2f));
        Ogrodzenie(root, new Vector3(fx0, g, fz0), new Vector3(fx1, g, fz0), fw, krata, "ogrodzenie kosza", (cx - fx0) - 1.0f, (cx - fx0) + 1.0f);
        Ogrodzenie(root, new Vector3(fx0, g, fz1), new Vector3(fx1, g, fz1), fw, krata, "ogrodzenie kosza");
        Ogrodzenie(root, new Vector3(fx0, g, fz0), new Vector3(fx0, g, fz1), fw, krata, "ogrodzenie kosza");
        Ogrodzenie(root, new Vector3(fx1, g, fz0), new Vector3(fx1, g, fz1), fw, krata, "ogrodzenie kosza");
        // ławki wzdłuż północnego ogrodzenia, przodem do boiska
        foreach (float dx in new[] { -4f, 4f }) { Lawka(root, new Vector3(cx + dx, yS, fz1 - .65f), 180f, "Ławka przy boisku do koszykówki"); rep.benches++; }
        rep.basketball = true; rep.court2 = new Vector3(cx, yS, cz); rep.size2 = new Vector2(hl * 2, hw * 2);
        var b = new GameObject("Gra: koszykówka (podwórko)").AddComponent<BraxSportBoisko>(); b.transform.SetParent(root, false);
        b.gra = BraxSportBoisko.Gra.Kosz; b.srodek = new Vector3(cx, yS, cz); b.osX = Vector3.right; b.pol = new Vector2(hl, hw); b.strefa = new Vector2(hl + 1.5f, hw + 1.5f);
        b.cele = kosze.ToArray(); b.tablice = tablice.ToArray();
    }

    // ---------------- wiata śmietnikowa ----------------
    static void Wiata(Transform root, float g, Report rep)
    {
        float k = K; var r = ObrysWiaty; float x0 = r.xMin, x1 = r.xMax, z0 = r.yMin, z1 = r.yMax, H = 2.2f * k, t = .24f;
        var blok = BraxUliceKit.TexPlik("wiata: mur z bloczków", OS, "rough_block_wall", 1.6f, new Color(.86f, .85f, .82f), new Color(.60f, .59f, .56f), .9f);
        var mb = BraxUliceKit.S(blok);
        mb.Box(new Vector3(x0, g, z0), new Vector3(x1, H, z0 + t));                         // ściana tylna (południe)
        mb.Box(new Vector3(x0, g, z0 + t), new Vector3(x0 + t, H, z1));                     // boki
        mb.Box(new Vector3(x1 - t, g, z0 + t), new Vector3(x1, H, z1));
        float scD = (x1 - x0) / 3f;                                                          // ścianki działowe (3 boksy)
        foreach (float sx in new[] { x0 + scD, x0 + 2 * scD }) mb.Box(new Vector3(sx - .06f, g, z0 + t), new Vector3(sx + .06f, H * .75f, z1 - .3f));
        var czapa = BraxUliceKit.S(Obrzeze);
        czapa.Box(new Vector3(x0 - .03f, H, z0 - .03f), new Vector3(x1 + .03f, H + .05f, z0 + t + .03f));
        czapa.Box(new Vector3(x0 - .03f, H, z0 + t + .03f), new Vector3(x0 + t + .03f, H + .05f, z1 + .03f));
        czapa.Box(new Vector3(x1 - t - .03f, H, z0 + t + .03f), new Vector3(x1 + .03f, H + .05f, z1 + .03f));
        // posadzka z kostki (podniesiona 3 cm nad asfaltem — bez wspólnej płaszczyzny)
        var kostka = BraxUliceKit.TexPlik("wiata: posadzka z kostki", OS, "concrete_pavers_02", 1.4f, null, new Color(.55f, .54f, .52f), .9f);
        BraxUliceKit.S(kostka).Box(new Vector3(x0 + t, g, z0 + t), new Vector3(x1 - t, g + .03f, z1 + .6f), 4);
        // dach: blacha trapezowa na stalowych płatwiach, spadek do tyłu
        var blacha = BraxUliceKit.Mat("wiata: blacha trapezowa grafit", new Color(.22f, .23f, .24f), .55f, .6f);
        var stal = BraxUliceKit.Mat("wiata: stal ocynkowana (ramy, kraty)", new Color(.58f, .59f, .60f), .45f, .85f);
        var bs = BraxUliceKit.S(blacha); var ss = BraxUliceKit.S(stal);
        float yP = H + .3f, yT = H + .1f;                                                   // przód wyżej, tył niżej
        int fale = Mathf.CeilToInt((x1 - x0 + .4f) / .2f);
        for (int i = 0; i < fale; i++)
        {
            float a = x0 - .2f + i * .2f, b = a + .1f, c = a + .2f; float dz0 = z0 - .15f, dz1 = z1 + .35f;
            Vector3 P(float x, float zz, float up) => new Vector3(x, Mathf.Lerp(yT, yP, (zz - dz0) / (dz1 - dz0)) + up, zz);
            bs.Quad(P(a, dz0, 0), P(b, dz0, .035f), P(b, dz1, .035f), P(a, dz1, 0), Vector3.up);
            bs.Quad(P(b, dz0, .035f), P(c, dz0, 0), P(c, dz1, 0), P(b, dz1, .035f), Vector3.up);
            bs.Quad(P(a, dz0, -.004f), P(c, dz0, -.004f), P(c, dz1, -.004f), P(a, dz1, -.004f), Vector3.down);
        }
        foreach (float zz in new[] { z0 + .1f, (z0 + z1) * .5f, z1 + .2f }) { float y = Mathf.Lerp(yT, yP, (zz - z0 + .15f) / (z1 - z0 + .5f)) - .07f; ss.Box(new Vector3(x0 - .15f, y - .06f, zz - .04f), new Vector3(x1 + .15f, y, zz + .04f)); }
        foreach (float xx in new[] { x0 + .12f, x1 - .12f }) ss.Box(new Vector3(xx - .05f, g, z1 + .15f), new Vector3(xx + .05f, yP - .1f, z1 + .25f));
        // kraty frontowe: dwie bramy uchylone (pręty pionowe w ramie), środek otwarty — dostęp do pojemników
        foreach (var (bx0, bx1, kat) in new[] { (x0 + t, x0 + scD - .1f, 35f), (x1 - scD + .1f, x1 - t, -35f) })
        {
            var pivot = new Vector3(kat > 0 ? bx0 : bx1, g, z1 + .05f); float L = bx1 - bx0; var dir = Quaternion.Euler(0, kat > 0 ? -kat : -kat, 0) * (kat > 0 ? Vector3.right : Vector3.left);
            Vector3 Q(float u, float y) => pivot + dir * u + Vector3.up * y;
            float gh = H * .85f;
            ss.Rura(Q(0, .08f), Q(L, .08f), .025f, 6); ss.Rura(Q(0, gh), Q(L, gh), .025f, 6); ss.Rura(Q(0, .08f), Q(0, gh), .03f, 6); ss.Rura(Q(L, .08f), Q(L, gh), .025f, 6);
            for (float u = .12f; u < L - .05f; u += .12f) ss.Rura(Q(u, .08f), Q(u, gh), .01f, 4, false);
        }
        foreach (var (a, b) in new[] { (new Vector3(x0, g, z0), new Vector3(x1, H, z0 + t)), (new Vector3(x0, g, z0), new Vector3(x0 + t, H, z1)), (new Vector3(x1 - t, g, z0), new Vector3(x1, H, z1)) })
        { var c = new GameObject("kolizja: wiata śmietnikowa"); c.transform.SetParent(root, false); var bc = c.AddComponent<BoxCollider>(); bc.center = (a + b) * .5f; bc.size = b - a; }
        // pojemniki: boks 1 — zmieszane 1100 l; boks 2 — papier 1100 l i metale/tworzywa 1100 l; boks 3 — szkło 240 l, bio 240 l
        float yp = g + .03f, zp = z0 + t + .12f;
        var poj = new List<(float x, float w, float d, float h, Color kol, string co)>
        {
            (x0 + scD * .5f, 1.37f * k, 1.07f * k, 1.33f * k, new Color(.16f, .17f, .18f), "zmieszane"),
            (x0 + scD * 1.5f - .48f * k, .58f * k * 1.6f, 1.07f * k * .85f, 1.25f * k, new Color(.12f, .32f, .65f), "papier"),
            (x0 + scD * 1.5f + .48f * k, .58f * k * 1.6f, 1.07f * k * .85f, 1.25f * k, new Color(.90f, .76f, .10f), "metale i tworzywa"),
            (x0 + scD * 2.5f - .45f * k, .58f * k, .73f * k, 1.07f * k, new Color(.13f, .45f, .20f), "szkło"),
            (x0 + scD * 2.5f + .45f * k, .58f * k, .73f * k, 1.07f * k, new Color(.40f, .26f, .14f), "bio"),
        };
        foreach (var p in poj)
        {
            var at = new Vector3(p.x, yp, zp + p.d * .5f);
            Pojemnik(root, at, p.w, p.d, p.h, p.kol, p.co); rep.containers++;
        }
        // tablica segregacji na murze
        var tl = BraxUliceKit.Mat("wiata: tablica segregacji (biała)", new Color(.94f, .94f, .92f), .5f);
        // tablica: płyta 1.2 cm na murze (tył przy murze — bryła, nie naklejka 5 mm), litery i pola kolorów jako bryły 6–10 mm
        var ts = BraxUliceKit.S(tl); var cz = (z0 + t) + .012f;
        ts.OBox(new Vector3(x0 + scD * .5f, H - .625f, cz - .006f), Vector3.right, Vector3.up, Vector3.forward, 1.4f, .55f, .012f, true);
        var czarne = BraxUliceKit.Mat("tablica regulaminu: litery", new Color(.08f, .08f, .09f), .5f);
        BraxUliceKit.Napis3D("SEGREGUJ", new Vector3(x0 + scD * .5f, H - .58f, cz), Vector3.forward, .16f, .01f, czarne, czarne);
        int ci = 0; foreach (var p in poj)
        {
            var m = BraxUliceKit.Mat("pojemnik: " + p.co, p.kol, .5f); float xa = x0 + scD * .5f - .62f + ci * .26f;
            BraxUliceKit.S(m).OBox(new Vector3(xa + .11f, H - .77f, cz + .004f), Vector3.right, Vector3.up, Vector3.forward, .22f, .14f, .008f, true); ci++;
        }
        rep.shelter = true;
    }

    // Pojemnik na odpady (1100 l albo 240 l) × k: korpus zwężany ku dołowi, kołnierz, pokrywa, uchwyty, kółka. Osobny obiekt z kolizją.
    static void Pojemnik(Transform root, Vector3 at, float w, float d, float h, Color kolor, string co)
    {
        var g = new GameObject("Pojemnik na odpady: " + co); g.transform.SetParent(root, false); g.transform.position = at;
        var s = new UlSiatka(); float kol = h * .16f, yk = kol, yt = h - .05f; bool duzy = w > 1f;
        // korpus: ostrosłup ścięty (dół węższy o 8 %)
        float dw = w * .46f, dd = d * .46f, gw = w * .5f, gd = d * .5f;
        Vector3[] dol = { new Vector3(-dw, yk, -dd), new Vector3(dw, yk, -dd), new Vector3(dw, yk, dd), new Vector3(-dw, yk, dd) };
        Vector3[] gor = { new Vector3(-gw, yt, -gd), new Vector3(gw, yt, -gd), new Vector3(gw, yt, gd), new Vector3(-gw, yt, gd) };
        for (int i = 0; i < 4; i++)
        {
            int j = (i + 1) % 4; var n = Vector3.Cross(gor[i] - dol[i], dol[j] - dol[i]).normalized; if (Vector3.Dot(n, (dol[i] + dol[j]) * .5f) < 0) n = -n;
            s.Quad(dol[i], dol[j], gor[j], gor[i], n);
        }
        s.Quad(dol[0], dol[1], dol[2], dol[3], Vector3.down);
        s.Box(new Vector3(-gw - .02f, yt - .06f, -gd - .02f), new Vector3(gw + .02f, yt, gd + .02f));                 // kołnierz
        // pokrywa (lekko wypukła: dwa skosy)
        s.Quad(new Vector3(-gw - .03f, yt, -gd - .03f), new Vector3(gw + .03f, yt, -gd - .03f), new Vector3(gw + .03f, yt + .07f, 0), new Vector3(-gw - .03f, yt + .07f, 0), new Vector3(0, .9f, -.4f));
        s.Quad(new Vector3(-gw - .03f, yt + .07f, 0), new Vector3(gw + .03f, yt + .07f, 0), new Vector3(gw + .03f, yt, gd + .03f), new Vector3(-gw - .03f, yt, gd + .03f), new Vector3(0, .9f, .4f));
        foreach (float sx in new[] { -1f, 1f }) s.Tri(new Vector3(sx * (gw + .03f), yt, -gd - .03f), new Vector3(sx * (gw + .03f), yt + .07f, 0), new Vector3(sx * (gw + .03f), yt, gd + .03f), new Vector3(sx, 0, 0));
        var korpus = BraxUliceKit.Mat("pojemnik: " + co, kolor, .55f);
        var czarny = new UlSiatka();
        // uchwyty z przodu i z boków, kółka (4 — duży, 2 — mały)
        czarny.Rura(new Vector3(-gw * .6f, yt - .12f, -gd - .05f), new Vector3(gw * .6f, yt - .12f, -gd - .05f), .018f, 6);
        foreach (float sx in new[] { -gw * .6f, gw * .6f }) czarny.Box(new Vector3(sx - .02f, yt - .14f, -gd - .05f), new Vector3(sx + .02f, yt - .1f, -gd));
        float kr = duzy ? .1f : .1f;
        var kola = duzy ? new[] { new Vector3(-dw + .1f, 0, -dd + .1f), new Vector3(dw - .1f, 0, -dd + .1f), new Vector3(-dw + .1f, 0, dd - .1f), new Vector3(dw - .1f, 0, dd - .1f) }
                        : new[] { new Vector3(-dw + .02f, 0, dd - .05f), new Vector3(dw - .02f, 0, dd - .05f) };
        foreach (var c in kola)
        {
            czarny.Rura(c + new Vector3(-.03f, kr, 0), c + new Vector3(.03f, kr, 0), kr, 10);
            czarny.Box(c + new Vector3(-.035f, kr, -.03f), c + new Vector3(.035f, yk, .03f));
        }
        Mesh Zapis(UlSiatka x, string n) { var m = x.Zbuduj("Ulice: pojemnik " + co + " " + n); Directory.CreateDirectory(BraxUliceKit.Out); BraxZapisAssetu.Utworz(m, BraxUliceKit.Out + "/pojemnik_" + BraxUliceKit.Bezpieczna(co + "_" + n) + ".asset"); return m; }
        var a = new GameObject("korpus"); a.transform.SetParent(g.transform, false); a.AddComponent<MeshFilter>().sharedMesh = Zapis(s, "korpus"); a.AddComponent<MeshRenderer>().sharedMaterial = korpus;
        var b = new GameObject("kółka i uchwyty"); b.transform.SetParent(g.transform, false); b.AddComponent<MeshFilter>().sharedMesh = Zapis(czarny, "kola"); b.AddComponent<MeshRenderer>().sharedMaterial = BraxUliceKit.Mat("pojemnik: guma i tworzywo czarne", new Color(.05f, .05f, .05f), .7f);
        var bc = g.AddComponent<BoxCollider>(); bc.center = new Vector3(0, h * .5f, 0); bc.size = new Vector3(w, h, d);
    }
}
