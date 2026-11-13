using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// WARSZTAT + WULKANIZACJA (runda 5; właściciel 01:08: „opony to nie były opony w ogóle”; koordynator: dwie strefy, prawdziwe
// koła, zakaz klocków, pełne detale). Budynek warsztatu E32 (x 23…47, z −54…−42) dzielony na strefy:
//  • WARSZTAT (stanowiska 1–2): podnośnik dwukolumnowy z autem w górze (kolumny z ceowników, wózki, siłownik, zapadki,
//    belka górna z wyłącznikiem, ramiona teleskopowe z gumowymi talerzami, agregat), kanał w posadzce (wycięty: ściany
//    z płytek, dno z odpływem, schody stalowe, lampy w ścianach, pas żółto-czarny i kątownik na krawędzi), wózki i szafy
//    narzędziowe (modele Poly Haven), kompresor, tablica narzędzi, beczki i kanistry oleju, regał z częściami, stół z imadłem;
//  • WULKANIZACJA (stanowisko 3, ścianka działowa z przeszkleniem, brama podniesiona, szyld WULKANIZACJA): montażownica,
//    wyważarka z kołem i osłoną, podnośnik nożycowy, klucz pneumatyczny na zwijaczu węża, stojaki i stosy opon, wanienka do
//    szukania przebić z kołem w wodzie, pompka (model);
//  • KOŁA w kodzie (do czasu modelu ze Sketchfaba): 205/55 R16 × K (średnica 0.632 m × K = 0.82 m): opona z bieżnikiem
//    (3 rowki obwodowe, rowki poprzeczne), bok z wybrzuszeniem i napisem rozmiaru (bez marek), felga aluminiowa 5 ramion
//    z piastą, 5 nakrętkami, otworem centralnym, rantami i bębnem; sama opona z wewnętrzną ścianą;
//  • ZŁOM na zapleczu (za budynkiem): kontener z rdzawymi felgami i starą oponą (Poly Haven CC0: rusted_wheel_rim_01/02, old_tyre).
// Logi: BRAXHORN_WARSZTAT_STREFY, BRAXHORN_KOLA, BRAXHORN_DETALE obiekt=warsztat+wulkanizacja. Ujęcia DETAL_warsztat_*, DETAL_wulkanizacja_*.
public static partial class BraxClaudeOsiedle
{
    static float KoloRo => .632f * .5f * K; static float KoloRr => .2032f * K; static float KoloW => .205f * K;
    static Mesh koloAluMesh, oponaMesh; static Material[] koloMats, oponaMats; static Material koloNapisMat;
    static int wkKola, wkOpony, wkNapisy, wkDetale; static readonly List<string> wkOpis = new List<string>();
    static void WkDetal(string obiekt, string co, int n = 1) { wkDetale += n; wkOpis.Add(obiekt + ":" + co + (n > 1 ? "×" + n : "")); }
    const string PhModele = "Assets/Source/Claude/PolyHaven/models/";
    static GameObject ModelPH(string id)
    {
        var p = PhModele + id + "/" + id + ".gltf";
        if (!System.IO.File.Exists(p)) { rep.notes.Add("brak modelu Poly Haven " + id + " (fetch_polyhaven.py, blok osiedle — warsztat)"); return null; }
        AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<GameObject>(p) ?? AssetDatabase.LoadAllAssetsAtPath(p).OfType<GameObject>().FirstOrDefault();
    }
    static GameObject WkModel(string id, string nazwa, Vector3 at, float yaw, bool solid = true)
    {
        var src = ModelPH(id); if (!src) return null;
        var o = SalonModel(src, nazwa, at, yaw, 1f, solid); if (o) { o.name = "Warsztat: " + nazwa + " (" + id + ", Poly Haven CC0)"; WkDetal("model", id); }
        return o;
    }

    // ---------------- koła ----------------
    static Vector3 PB(float r, float y, float a) => new Vector3(r * Mathf.Cos(a), y, r * Mathf.Sin(a));
    static void KolaPrzygotuj()
    {
        if (koloAluMesh && oponaMesh) return;
        float Ro = KoloRo, Rr = KoloRr, hw = KoloW * .5f;
        var guma = SalonMatOsobny("opona guma", new Color(.045f, .045f, .05f), .28f, 0f);
        var alu = SalonMatOsobny("felga aluminium", new Color(.78f, .79f, .81f), .72f, .9f);
        var ciem = SalonMatOsobny("felga wnętrze", new Color(.1f, .1f, .11f), .4f, .5f);
        koloNapisMat = SalonMatOsobny("napis na boku opony", new Color(.16f, .16f, .17f), .3f, 0f);
        koloMats = new[] { guma, alu, ciem }; oponaMats = new[] { guma, ciem };
        // profil opony (r, y): stopka → bok z wybrzuszeniem → bark → bieżnik z 3 rowkami → bark → bok → stopka
        var lewy = new List<Vector2> { new Vector2(Rr + .012f, -hw + .012f), new Vector2(Rr + .05f, -hw - .004f), new Vector2(Rr + .1f, -hw - .01f), new Vector2(Ro - .05f, -hw - .006f), new Vector2(Ro - .018f, -hw + .006f), new Vector2(Ro - .004f, -hw + .03f) };
        var biez = new List<Vector2> { new Vector2(Ro, -hw + .045f) };
        foreach (float g in new[] { -.06f, 0f, .06f }) { biez.Add(new Vector2(Ro, g - .006f)); biez.Add(new Vector2(Ro - .01f, g - .006f)); biez.Add(new Vector2(Ro - .01f, g + .006f)); biez.Add(new Vector2(Ro, g + .006f)); }
        biez.Add(new Vector2(Ro, hw - .045f));
        var prof = new List<Vector2>(lewy); prof.AddRange(biez); prof.AddRange(lewy.AsEnumerable().Reverse().Select(q => new Vector2(q.x, -q.y)));
        int j0 = lewy.Count - 1, j1 = lewy.Count + biez.Count;   // zakres bieżnika (z barkami)
        Mesh Buduj(bool felga, string nazwa)
        {
            var sb = new SiatkaSalonu(); int seg = 48; float rc = (Rr + Ro) * .5f;
            for (int k = 0; k < seg; k++)
            {
                float a0 = k * Mathf.PI * 2f / seg, a1 = (k + 1) * Mathf.PI * 2f / seg; bool poprz = k % 4 == 0;
                Vector2 Low(int j) => poprz && j > j0 && j < j1 ? new Vector2(Mathf.Min(prof[j].x, Ro - .008f), prof[j].y) : prof[j];
                for (int j = 0; j + 1 < prof.Count; j++)
                {
                    Vector2 p = Low(j), q = Low(j + 1);
                    Vector3 a = PB(p.x, p.y, a0), b = PB(q.x, q.y, a0), c = PB(q.x, q.y, a1), d = PB(p.x, p.y, a1);
                    var n = Vector3.Cross(b - a, d - a); if (n.sqrMagnitude < 1e-12f) n = Vector3.Cross(c - b, a - b);
                    var mid = (a + c) * .5f; var hint = mid - PB(rc, 0f, (a0 + a1) * .5f); if (Vector3.Dot(n, hint) < 0) n = -n;
                    sb.Quad(0, a, b, c, d, n.normalized);
                }
                if (poprz)   // ściany rowka poprzecznego
                    for (int j = j0 + 1; j + 1 < j1; j++)
                        foreach (var (ang, sgn) in new[] { (a0, -1f), (a1, 1f) })
                        {
                            Vector2 p = prof[j], q = prof[j + 1], pl = Low(j), ql = Low(j + 1);
                            if (Mathf.Abs(p.x - pl.x) < 1e-5f && Mathf.Abs(q.x - ql.x) < 1e-5f) continue;
                            var tn = new Vector3(-Mathf.Sin(ang), 0, Mathf.Cos(ang)) * -sgn;
                            sb.Quad(0, PB(p.x, p.y, ang), PB(q.x, q.y, ang), PB(ql.x, ql.y, ang), PB(pl.x, pl.y, ang), tn);
                        }
                if (!felga)
                {   // wewnętrzna ściana opony (widać ją przez otwór)
                    float r = Rr + .012f;
                    sb.Quad(1, PB(r, -hw + .012f, a0), PB(r, hw - .012f, a0), PB(r, hw - .012f, a1), PB(r, -hw + .012f, a1), -PB(1f, 0, (a0 + a1) * .5f));
                    continue;
                }
                // felga: bęben (ciemny, do osi), ranty (alu), tylna tarcza (ciemna)
                float rb = Rr - .015f, yf = hw - .035f, yt = -hw + .03f;
                sb.Quad(2, PB(rb, yt, a0), PB(rb, yf, a0), PB(rb, yf, a1), PB(rb, yt, a1), -PB(1f, 0, (a0 + a1) * .5f));
                foreach (float yy in new[] { -hw + .012f, hw - .012f })
                    sb.Quad(1, PB(rb, yy, a0), PB(Rr + .014f, yy, a0), PB(Rr + .014f, yy, a1), PB(rb, yy, a1), yy > 0 ? Vector3.up : Vector3.down);
                sb.Quad(1, PB(rb, hw - .012f, a0), PB(rb, yf, a0), PB(rb, yf, a1), PB(rb, hw - .012f, a1), -PB(1f, 0, (a0 + a1) * .5f));
                sb.Quad(2, PB(0f, yt, a0), PB(rb, yt, a0), PB(rb, yt, a1), PB(0f, yt, a1), Vector3.up);
                sb.Quad(1, PB(Rr - .045f, yf, a0), PB(rb, yf, a0), PB(rb, yf, a1), PB(Rr - .045f, yf, a1), Vector3.up);     // pierścień czoła
                float rh = .1f;
                sb.Quad(1, PB(0f, yf + .012f, a0), PB(rh, yf + .012f, a0), PB(rh, yf + .012f, a1), PB(0f, yf + .012f, a1), Vector3.up);   // piasta
                sb.Quad(1, PB(rh, yf + .012f, a0), PB(rh, yf - .01f, a0), PB(rh, yf - .01f, a1), PB(rh, yf + .012f, a1), PB(1f, 0, (a0 + a1) * .5f));
                sb.Quad(2, PB(0f, yf + .016f, a0), PB(.032f, yf + .016f, a0), PB(.032f, yf + .016f, a1), PB(0f, yf + .016f, a1), Vector3.up);   // otwór/dekielek
            }
            // ramiona i nakrętki dokładane osobno (zamknięte w funkcji niżej)
            if (felga) KoloRamionaINakretki(sb);
            return sb.Zapisz(nazwa);
        }
        koloAluMesh = Buduj(true, "koło 205-55 R16 felga aluminiowa"); oponaMesh = Buduj(false, "opona 205-55 R16");
    }
    static void KoloRamionaINakretki(SiatkaSalonu sb)
    {
        float yf = KoloW * .5f - .035f, rh = .1f, re = KoloRr - .045f, gr = .025f;
        for (int s = 0; s < 5; s++)
        {
            float c = s * Mathf.PI * 2f / 5f + Mathf.PI * .1f, w0 = .035f / rh, w1 = .024f / re;
            Vector3 a = PB(rh - .005f, yf + .012f, c - w0), b = PB(re + .005f, yf + .002f, c - w1), cc = PB(re + .005f, yf + .002f, c + w1), d = PB(rh - .005f, yf + .012f, c + w0);
            var dn = Vector3.down * gr;
            sb.Quad(1, a, b, cc, d, Vector3.up);
            sb.Quad(1, a, a + dn, b + dn, b, -PB(1f, 0, c + Mathf.PI * .5f)); sb.Quad(1, d, cc, cc + dn, d + dn, PB(1f, 0, c + Mathf.PI * .5f));
        }
        // nakrętki: 5 sześciokątów na rozstawie 114.3 mm × K
        float pcd = .05715f * K;
        for (int s = 0; s < 5; s++)
        {
            float c = s * Mathf.PI * 2f / 5f + Mathf.PI * .3f; var m = PB(pcd, 0, c); float rn = .011f, y0 = yf + .012f, y1 = y0 + .02f;
            for (int k = 0; k < 6; k++)
            {
                float a0 = k * Mathf.PI / 3f, a1 = (k + 1) * Mathf.PI / 3f;
                Vector3 p0 = m + PB(rn, y0, a0), p1 = m + PB(rn, y0, a1), q0 = m + PB(rn, y1, a0), q1 = m + PB(rn, y1, a1);
                sb.Quad(1, p0, q0, q1, p1, PB(1f, 0, (a0 + a1) * .5f)); sb.Quad(1, q0, m + new Vector3(0, y1, 0), m + new Vector3(0, y1, 0), q1, Vector3.up);
            }
        }
    }
    // Koło/opona: oś obrotu = os (lokalne Y siatki), „gora” = kierunek do napisu na boku; napis rozmiaru po stronie +os
    static GameObject Kolo(bool felga, Vector3 c, Vector3 os, Vector3 gora, string nazwa, Transform rodzic, bool napis)
    {
        KolaPrzygotuj();
        os.Normalize(); gora = Vector3.ProjectOnPlane(gora, os).normalized; if (gora.sqrMagnitude < .5f) gora = Vector3.ProjectOnPlane(Vector3.forward, os).normalized;
        var g = new GameObject(nazwa); g.transform.SetParent(rodzic, false); g.transform.position = c;
        g.transform.rotation = Quaternion.LookRotation(Vector3.Cross(gora, os), os);   // lokalne Y = oś
        g.AddComponent<MeshFilter>().sharedMesh = felga ? koloAluMesh : oponaMesh;
        var r = g.AddComponent<MeshRenderer>(); r.sharedMaterials = felga ? koloMats : oponaMats;
        var lod = g.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(.012f, new Renderer[] { r }) }); lod.RecalculateBounds();
        if (felga) wkKola++; else wkOpony++;
        if (napis)
        {
            float rt = (KoloRr + KoloRo) * .5f + .01f, y = KoloW * .5f + .011f;
            var prawo = Vector3.Cross(os, gora).normalized;
            Napis3D("205/55 R16 91V", c + os * y + gora * rt, os, .024f, .003f, koloNapisMat, koloNapisMat, .15f, .14f, true, -prawo, gora);
            wkNapisy++;
        }
        return g;
    }

    // ---------------- kanał w posadzce (stanowisko 2) ----------------
    static (float x0, float x1, float z0, float z1) KanalObrys(float cx) => (cx - .5f * K, cx + .5f * K, -51.5f, -45.0f);
    static float KanalDno => .15f - 1.6f * K;
    static void WarsztatKanalWytnij(Transform estate, Transform physics, float cx)
    {
        var (x0, x1, z0, z1) = KanalObrys(cx);
        var v = MinMax(x0 - .16f, x1 + .16f, KanalDno - .4f, .14f, z0 - .16f, z1 + .16f);
        if (estate) CutVolumes(estate, physics, new List<Bounds> { v });
        int w = WytnijZGrupy((gr, m) => !gr.StartsWith("Osiedle A / warsztat"), v);
        PodzielKolizje(colliders, v);
        if (physics) PodzielKolizje(physics, v);
        Debug.Log("BRAXHORN_WARSZTAT_KANAL wycięcie x " + x0.ToString("F2") + "…" + x1.ToString("F2") + " z " + z0 + "…" + z1 + " dno=" + KanalDno.ToString("F2") + " trójkąty_generatora=" + w);
    }
    // posadzka hali z otworem kanału i pasem żółto-czarnym w warstwie wykończenia (bez nakładek)
    static void WarsztatPosadzka(float xa, float xb, float za, float zb, float cx, Material posadz)
    {
        var (x0, x1, z0, z1) = KanalObrys(cx); float p = .12f;   // pas ostrzegawczy
        void Plaster(Material m, float y0, float y1, float ox0, float ox1, float oz0, float oz1)
        {
            BoxMinMax(m, xa, ox0, y0, y1, za, zb, true); BoxMinMax(m, ox1, xb, y0, y1, za, zb, true);
            BoxMinMax(m, ox0, ox1, y0, y1, za, oz0, true); BoxMinMax(m, ox0, ox1, y0, y1, oz1, zb, true);
        }
        Plaster(Concrete, 0f, .13f, x0, x1, z0, z1);
        Plaster(posadz, .13f, .15f, x0 - p, x1 + p, z0 - p, z1 + p);
        var zolty = StMat("pas ostrzegawczy żółty", new Color(.95f, .75f, .05f), .5f); var czarny = StMat("pas ostrzegawczy czarny", new Color(.05f, .05f, .05f), .4f);
        int n = 0;
        void Pas(bool wzdluzX, float c0, float c1, float a0, float a1)
        {
            for (float a = a0; a < a1 - .005f; a += .2f, n++)
            {
                float e = Mathf.Min(a1, a + .2f); var m = n % 2 == 0 ? zolty : czarny;
                if (wzdluzX) BoxMinMax(m, a, e, .13f, .15f, c0, c1, true); else BoxMinMax(m, c0, c1, .13f, .15f, a, e, true);
            }
        }
        Pas(true, z0 - p, z0, x0 - p, x1 + p); Pas(true, z1, z1 + p, x0 - p, x1 + p); Pas(false, x0 - p, x0, z0, z1); Pas(false, x1, x1 + p, z0, z1);
        WkDetal("kanał", "pas żółto-czarny w warstwie posadzki", n);
    }
    static void WarsztatKanalBuduj(float cx)
    {
        var (x0, x1, z0, z1) = KanalObrys(cx); float dno = KanalDno, sc = .15f;
        var plytki = Pbr("warsztat: płytki kanału", "large_grey_tiles", .6f, new Color(.95f, .95f, .94f));
        BoxMinMax(plytki, x0 - sc, x0, dno, 0f, z0 - sc, z1 + sc, true); BoxMinMax(plytki, x1, x1 + sc, dno, 0f, z0 - sc, z1 + sc, true);
        BoxMinMax(plytki, x0, x1, dno, 0f, z0 - sc, z0, true); BoxMinMax(plytki, x0, x1, dno, 0f, z1, z1 + sc, true);
        BoxMinMax(Concrete, x0, x1, dno - .15f, dno, z0, z1, true);
        // kątownik stalowy na krawędzi (lico posadzki), odpływ w dnie, lampy w ścianach, schody stalowe z blachy ryflowanej
        var kat = StMat("kątownik ocynk", new Color(.62f, .63f, .65f), .5f, .7f);
        BoxMinMax(kat, x0 - .005f, x0 + .03f, .1f, .15f, z0, z1, false); BoxMinMax(kat, x1 - .03f, x1 + .005f, .1f, .15f, z0, z1, false);
        BoxMinMax(kat, x0, x1, .1f, .15f, z1 - .03f, z1 + .005f, false);
        BoxMinMax(StMat("kratka odpływu", new Color(.12f, .12f, .13f), .3f, .5f), cx - .15f, cx + .15f, dno, dno + .01f, z1 - 1.2f, z1 - .9f, false);
        var lamp = StSwiatlo("lampa kanału", new Color(1f, .97f, .9f));
        foreach (float lz in new[] { -49f, -46.5f }) foreach (float lx in new[] { x0, x1 })
            { float s = lx < cx ? 1f : -1f; BoxMinMax(Frame, lx, lx + s * .02f, -.9f, -.6f, lz - .3f, lz + .3f, false); BoxMinMax(lamp, lx + s * .02f, lx + s * .025f, -.86f, -.64f, lz - .26f, lz + .26f, false); }
        var ryf = StMat("blacha ryflowana", new Color(.55f, .56f, .58f), .45f, .8f);
        int ns = 7; float rs = (.15f - dno) / (ns + 1), gs = .26f;
        for (int k = 0; k < ns; k++)
        {
            float y = dno + (k + 1) * rs, zz = z0 + k * gs;
            BoxMinMax(ryf, x0 + .02f, x1 - .02f, y - .04f, y, zz, zz + gs + .02f, true);
        }
        foreach (float px in new[] { x0 + .02f, x1 - .06f }) Pochyla(Steel, px, px + .04f, z0, dno + rs + .05f, z0 + ns * gs, dno + ns * rs + .05f, .2f);
        WkDetal("kanał", "ściany z płytek, dno z odpływem, kątownik, 4 lampy, schody z blachy ryflowanej na policzkach", ns);
    }

    // ---------------- wyposażenie stref ----------------
    static Transform warsztatPodnosnikC; static float warsztatPodnosnikZ;
    static void WarsztatWyposazenie(float x0, float x1, float z0, float z1, float xb, float bw, float t, float yE)
    {
        var rodz = Group("Osiedle A / warsztat — koła"); var czerw = StMat("podnośnik czerwony", new Color(.7f, .1f, .08f), .5f, .3f);
        float c1 = xb + bw * .5f, c2 = xb + bw * 1.5f, c3 = xb + bw * 2.5f, lz = -48.5f;
        // ===== WARSZTAT 1: podnośnik dwukolumnowy =====
        foreach (int s in new[] { -1, 1 })
        {
            float kx = c1 + s * 2.1f, h = 3.6f * K;
            // kolumna z ceownika: plecy + 2 półki, wózek z ramionami, zapadki (drabinka), stopa z 4 kotwami
            BoxMinMax(czerw, kx + s * .14f, kx + s * .2f, .15f, h, lz - .2f, lz + .2f, true);
            BoxMinMax(czerw, kx - .2f, kx + .2f, .15f, h, lz - .2f, lz - .15f, false); BoxMinMax(czerw, kx - .2f, kx + .2f, .15f, h, lz + .15f, lz + .2f, false);
            BoxMinMax(Steel, kx - .32f, kx + .32f, .15f, .18f, lz - .4f, lz + .4f, false);
            foreach (float ax in new[] { kx - .25f, kx + .25f }) foreach (float az in new[] { lz - .33f, lz + .33f }) Cylinder(Steel, new Vector3(ax, .18f, az), .018f, .03f, 8, true, false);
            for (float yy = .5f; yy < h - .3f; yy += .12f) BoxMinMax(Steel, kx + s * .1f, kx + s * .13f, yy, yy + .03f, lz - .05f, lz + .05f, false);
            BoxMinMax(StMat("wózek podnośnika", new Color(.22f, .23f, .25f), .5f, .5f), kx - .12f, kx + .12f, 1.3f, 1.95f, lz - .14f, lz + .14f, false);
            Cylinder(StMat("siłownik chrom", new Color(.8f, .81f, .83f), .85f, .9f), new Vector3(kx + s * .05f, .2f, lz), .045f, 1.1f, 12, true, false);
            foreach (int q in new[] { -1, 1 })
            {   // ramiona teleskopowe (dwa stopnie) i talerze gumowe
                var a = new Vector3(kx, 1.5f, lz + q * .14f); var m = new Vector3(c1 + s * 1.2f, 1.5f, lz + q * .8f); var e = new Vector3(c1 + s * .7f, 1.5f, lz + q * 1.25f);
                Tube(czerw, a, m, .06f, 8); Tube(Steel, m, e, .045f, 8);
                foreach (var (od, koniec, promien) in new[] { (a, m, .06f), (m, e, .045f) })
                {
                    var os = koniec - od;
                    var ramie = new GameObject("Kolizja ramienia podnośnika").AddComponent<CapsuleCollider>();
                    ramie.transform.SetParent(rodz, false);
                    ramie.transform.SetPositionAndRotation((od + koniec) * .5f, Quaternion.FromToRotation(Vector3.up, os));
                    ramie.direction = 1; ramie.radius = promien; ramie.height = os.magnitude + 2f * promien;
                }
                Cylinder(Steel, e - Vector3.up * .02f, .06f, .08f, 12, true, false); Cylinder(StMat("talerz gumowy", new Color(.08f, .08f, .08f), .3f), e + Vector3.up * .06f, .09f, .03f, 14, true, false);
            }
            if (s < 0)
            {   // agregat hydrauliczny z silnikiem i zbiornikiem
                BoxMinMax(StMat("agregat", new Color(.25f, .27f, .3f), .5f, .4f), kx - .55f, kx - .22f, .9f, 1.25f, lz - .15f, lz + .15f, false);
                Cylinder(StMat("silnik agregatu", new Color(.15f, .3f, .55f), .5f, .3f), new Vector3(kx - .4f, 1.25f, lz), .1f, .28f, 14, true, false);
                BoxMinMax(StMat("zbiornik oleju przezroczysty", new Color(.85f, .7f, .2f), .7f), kx - .52f, kx - .28f, .6f, .9f, lz - .1f, lz + .1f, false);
            }
            Collider(new Vector3(kx, h * .5f, lz), new Vector3(.42f, h, .42f));
        }
        float hb = 3.6f * K;
        BoxMinMax(czerw, c1 - 2.3f, c1 + 2.3f, hb, hb + .18f, lz - .1f, lz + .1f, false, true);                         // belka górna
        BoxMinMax(StMat("listwa wyłącznika", new Color(.85f, .85f, .2f), .5f), c1 - 1.6f, c1 + 1.6f, hb - .08f, hb - .05f, lz - .03f, lz + .03f, false, true);
        Tube(Steel, new Vector3(c1 - 2.1f, hb - .1f, lz + .05f), new Vector3(c1 + 2.1f, hb - .1f, lz + .05f), .012f, 6);   // linki wyrównujące
        warsztatPodnosnikC = rodz; warsztatPodnosnikZ = lz;
        WkDetal("warsztat", "podnośnik: ceowniki, zapadki, stopy z kotwami, wózki, siłowniki, ramiona teleskopowe, talerze, agregat, belka, wyłącznik, linki");
        // szafa narzędziowa i wózek (modele) przy tylnej ścianie stanowiska 1; tablica narzędzi nad stołem
        if (!WkModel("metal_tool_chest", "szafa narzędziowa", new Vector3(c1 - 1.6f, .15f, z0 + t + .45f), 0f)) WkDetal("warsztat", "brak modelu szafy");
        if (!WkModel("tool_cart", "wózek narzędziowy", new Vector3(c1 + 1.5f, .15f, -51.2f), 20f)) WkDetal("warsztat", "brak modelu wózka");
        {   // tablica narzędziowa (perforowana płyta, klucze, młotek, śrubokręty) nad stołem stanowiska 2
            float px0 = c2 - 1.5f, px1 = c2 + 1.1f, py0 = 1.45f, py1 = 2.4f, pz = z0 + t;
            BoxMinMax(StMat("tablica perforowana", new Color(.55f, .58f, .62f), .4f, .3f), px0, px1, py0, py1, pz, pz + .02f, false);
            for (float yy = py0 + .06f; yy < py1 - .04f; yy += .06f) for (float xx = px0 + .06f; xx < px1 - .04f; xx += .12f) BoxMinMax(StMat("otwór tablicy", new Color(.08f, .08f, .09f), .1f), xx, xx + .012f, yy, yy + .012f, pz + .02f, pz + .021f, false);
            var chrom = StMat("klucz chrom", new Color(.78f, .79f, .8f), .8f, .9f);
            for (int k = 0; k < 8; k++) { float xx = px0 + .15f + k * .11f, l = .14f + k * .02f; BoxMinMax(chrom, xx, xx + .022f, py1 - .12f - l, py1 - .12f, pz + .025f, pz + .035f, false); Cylinder(chrom, new Vector3(xx + .011f, py1 - .12f - l, pz + .03f), .018f, .008f, 8, true, false); }
            Tube(StMat("trzonek młotka", new Color(.55f, .35f, .18f), .4f), new Vector3(px1 - .5f, py0 + .15f, pz + .04f), new Vector3(px1 - .5f, py0 + .5f, pz + .04f), .015f, 8);
            BoxMinMax(Steel, px1 - .56f, px1 - .44f, py0 + .5f, py0 + .55f, pz + .02f, pz + .07f, false);
            for (int k = 0; k < 5; k++) { float xx = px1 - .3f + k * .05f; Tube(StMat("rękojeść śrubokręta", new Color(.8f, .15f, .1f), .5f), new Vector3(xx, py0 + .3f, pz + .04f), new Vector3(xx, py0 + .42f, pz + .04f), .012f, 6); Tube(chrom, new Vector3(xx, py0 + .1f, pz + .04f), new Vector3(xx, py0 + .3f, pz + .04f), .004f, 4); }
            WkDetal("warsztat", "tablica narzędzi: perforacja, 8 kluczy, młotek, 5 śrubokrętów");
        }
        // stół warsztatowy z nogami, szufladami, blatem bukowym i imadłem (+ skrzynka narzędziowa z modelu)
        {
            float sx0 = c2 - 1.6f, sx1 = c2 + 1.2f, sz0 = z0 + t + .05f, sz1 = sz0 + .8f, h = .9f * K;
            foreach (float lx in new[] { sx0 + .04f, sx1 - .1f }) foreach (float lzz in new[] { sz0 + .04f, sz1 - .1f }) BoxMinMax(Steel, lx, lx + .06f, .15f, h - .05f, lzz, lzz + .06f, false);
            BoxMinMax(Steel, sx0 + .04f, sx1 - .04f, .3f, .34f, sz0 + .04f, sz1 - .04f, false);                          // półka dolna
            BoxMinMax(StMat("szuflady stołu", new Color(.2f, .32f, .55f), .5f, .3f), sx0 + .1f, sx0 + .8f, h - .5f, h - .05f, sz0 + .05f, sz1 - .05f, false);
            for (float yy = h - .45f; yy < h - .1f; yy += .14f) BoxMinMax(Frame, sx0 + .35f, sx0 + .55f, yy, yy + .02f, sz1 - .05f, sz1 - .03f, false);
            BoxMinMax(StMat("blat stołu buk", new Color(.68f, .5f, .32f), .4f), sx0, sx1, h - .05f, h, sz0, sz1 + .05f, true);
            BoxMinMax(Steel, sx1 - .45f, sx1 - .2f, h, h + .14f, sz1 - .2f, sz1 + .02f, false); BoxMinMax(Steel, sx1 - .43f, sx1 - .22f, h + .14f, h + .2f, sz1 - .18f, sz1 - .1f, false);   // imadło
            Tube(Steel, new Vector3(sx1 - .32f, h + .08f, sz1 + .02f), new Vector3(sx1 - .32f, h + .08f, sz1 + .2f), .01f, 6);
            WkModel("metal_toolbox", "skrzynka narzędziowa na stole", new Vector3(sx0 + 1.3f, h, sz0 + .4f), 10f, false);
            WkDetal("warsztat", "stół: nogi, półka, szuflady z uchwytami, blat, imadło z pokrętłem");
        }
        // kompresor (model) i olej: beczki z rantami, kanistry na regale; regał z częściami (filtry, tarcze, kartony)
        WkModel("old_military_compressor", "kompresor", new Vector3(c2 + 2.2f, .15f, z0 + t + .55f), 0f);
        var bk = StMat("beczka oleju", new Color(.12f, .25f, .45f), .5f, .4f);
        foreach (float bz in new[] { -51.6f, -50.9f })
        {
            var b0 = new Vector3(xb + .45f, .15f, bz); Cylinder(bk, b0, .29f, .88f, 20, true, true);
            foreach (float yy in new[] { .3f, .6f }) Cylinder(Steel, b0 + Vector3.up * yy, .3f, .03f, 20, false, false);
            Cylinder(Steel, b0 + Vector3.up * .88f, .03f, .02f, 8, true, false);
        }
        {
            float rx0 = xb + .1f, rx1 = xb + .5f, rz0 = -47.6f, rz1 = -44.6f;
            foreach (float yy in new[] { .2f, .75f, 1.3f, 1.85f }) BoxMinMax(StMat("półka regału", new Color(.35f, .37f, .4f), .4f, .5f), rx0, rx1, yy, yy + .03f, rz0, rz1, false);
            foreach (float pz in new[] { rz0, rz1 - .05f }) foreach (float px in new[] { rx0, rx1 - .05f }) BoxMinMax(Steel, px, px + .05f, .15f, 2.1f, pz, pz + .05f, false);
            var kan = StMat("kanister oleju", new Color(.85f, .55f, .05f), .5f); var filtr = StMat("filtr", new Color(.1f, .3f, .7f), .5f, .3f); var karton = StMat("karton części", new Color(.6f, .45f, .28f), .3f);
            for (int k = 0; k < 6; k++) { float pz = rz0 + .2f + k * .45f; BoxMinMax(kan, rx0 + .08f, rx0 + .3f, .78f, 1.1f, pz, pz + .32f, false); Cylinder(StMat("korek", new Color(.1f, .1f, .1f), .3f), new Vector3(rx0 + .19f, 1.1f, pz + .25f), .025f, .03f, 8, true, false); }
            for (int k = 0; k < 8; k++) Cylinder(filtr, new Vector3(rx0 + .2f, 1.33f, rz0 + .25f + k * .35f), .05f, .14f, 12, true, false);
            for (int k = 0; k < 4; k++) Cylinder(Steel, new Vector3(rx0 + .2f, 1.88f + k * .03f, rz0 + .5f), .14f, .025f, 20, true, false);
            for (int k = 0; k < 4; k++) BoxMinMax(karton, rx0 + .05f, rx1 - .05f, .23f, .5f, rz0 + .1f + k * .72f, rz0 + .7f + k * .72f, false);
            Collider(new Vector3((rx0 + rx1) * .5f, 1.1f, (rz0 + rz1) * .5f), new Vector3(rx1 - rx0, 2f, rz1 - rz0));
            WkDetal("warsztat", "olej: 2 beczki z rantami, 6 kanistrów; regał części: filtry, tarcze, kartony");
        }
        // ===== WULKANIZACJA (stanowisko 3) =====
        float xw = xb + bw * 2f;
        {   // ścianka działowa: murek 1.2 m + przeszklenie do 3 m, przejście przy tylnej ścianie
            var mur = Pbr("warsztat: tynk", "white_stucco_02", 2.5f, new Color(.86f, .87f, .86f)); float pz0 = z0 + t + 1.3f, pz1 = z1 - t;
            BoxMinMax(mur, xw - .06f, xw + .06f, .15f, 1.2f, pz0, pz1, true);
            BoxMinMax(Frame, xw - .05f, xw + .05f, 1.2f, 1.26f, pz0, pz1, false); BoxMinMax(Frame, xw - .05f, xw + .05f, 2.94f, 3f, pz0, pz1, false);
            for (float zz = pz0; zz < pz1 + .01f; zz += (pz1 - pz0) / 4f) BoxMinMax(Frame, xw - .05f, xw + .05f, 1.26f, 2.94f, Mathf.Min(zz, pz1 - .06f), Mathf.Min(zz + .06f, pz1), false);
            BoxMinMax(GlassClear, xw - .01f, xw + .01f, 1.26f, 2.94f, pz0, pz1, false);
            Collider(new Vector3(xw, 1.6f, (pz0 + pz1) * .5f), new Vector3(.12f, 2.9f, pz1 - pz0));
            BoxMinMax(Frame, xw - .06f, xw + .06f, .15f, 2.1f * K + .15f, pz0 - .06f, pz0, false);   // ościeżnica przejścia
            WkDetal("wulkanizacja", "ścianka działowa: murek, przeszklenie w ramach, ościeżnica przejścia");
        }
        // podnośnik nożycowy (najazdy, nożyce, siłownik), uniesiony 0.55 m
        {
            float ph = .55f; var nb = StMat("podnośnik nożycowy niebieski", new Color(.1f, .25f, .55f), .5f, .3f);
            foreach (int s in new[] { -1, 1 })
            {
                float rx = c3 + s * .75f;
                BoxMinMax(nb, rx - .3f, rx + .3f, .15f + ph, .15f + ph + .12f, -51.6f, -45.0f, true);
                BoxMinMax(StMat("blacha ryflowana", new Color(.55f, .56f, .58f), .45f, .8f), rx - .28f, rx + .28f, .15f + ph + .12f, .15f + ph + .125f, -51.5f, -45.1f, false);
                BoxMinMax(nb, rx - .3f, rx + .3f, .15f, .2f, -51.4f, -45.2f, false);
                Tube(nb, new Vector3(rx, .2f, -51.2f), new Vector3(rx, .15f + ph, -45.5f), .05f, 8); Tube(nb, new Vector3(rx, .15f + ph, -51.2f), new Vector3(rx, .2f, -45.5f), .05f, 8);
                Cylinder(StMat("siłownik chrom", new Color(.8f, .81f, .83f), .85f, .9f), new Vector3(rx, .2f, -48.4f), .04f, ph - .05f, 10, true, false);
                BoxMinMax(nb, rx - .3f, rx + .3f, .15f + ph, .15f + ph + .12f, -45.0f, -44.6f, false);   // najazd
            }
            WkDetal("wulkanizacja", "podnośnik nożycowy: najazdy z blachą ryflowaną, nożyce, siłowniki, podstawy");
        }
        // montażownica: korpus, talerz z 4 szczękami, kolumna z ramieniem i głowicą, łyżka zbijaka, pedały, manometr + koło na talerzu
        {
            var mx = new Vector3(xw + .9f, .15f, -44.0f); var nieb = StMat("montażownica", new Color(.1f, .3f, .6f), .5f, .3f);
            BoxMinMax(nieb, mx.x - .35f, mx.x + .35f, .15f, .85f, mx.z - .45f, mx.z + .35f, true);
            Cylinder(Steel, mx + Vector3.up * .7f, .32f, .05f, 24, true, false);
            for (int k = 0; k < 4; k++) { float a = k * Mathf.PI * .5f; var p = mx + new Vector3(Mathf.Cos(a) * .27f, .75f, Mathf.Sin(a) * .27f); BoxMinMax(StMat("szczęka", new Color(.85f, .2f, .1f), .5f), p.x - .03f, p.x + .03f, p.y, p.y + .06f, p.z - .03f, p.z + .03f, false); }
            BoxMinMax(nieb, mx.x - .1f, mx.x + .1f, .85f, 1.95f, mx.z - .45f, mx.z - .3f, false);
            Tube(Steel, new Vector3(mx.x, 1.85f, mx.z - .38f), new Vector3(mx.x, 1.85f, mx.z + .05f), .035f, 8);
            Tube(Steel, new Vector3(mx.x, 1.85f, mx.z + .05f), new Vector3(mx.x, 1.2f, mx.z + .05f), .03f, 8);
            BoxMinMax(StMat("głowica montażowa", new Color(.15f, .15f, .15f), .5f), mx.x - .04f, mx.x + .04f, 1.1f, 1.2f, mx.z + .01f, mx.z + .12f, false);
            Tube(Steel, new Vector3(mx.x + .35f, .5f, mx.z - .1f), new Vector3(mx.x + .75f, .5f, mx.z - .1f), .03f, 8);   // łyżka zbijaka
            BoxMinMax(Steel, mx.x + .7f, mx.x + .78f, .3f, .55f, mx.z - .2f, mx.z, false);
            foreach (float px in new[] { -.15f, 0f, .15f }) BoxMinMax(StMat("pedał", new Color(.1f, .1f, .1f), .3f), mx.x + px - .05f, mx.x + px + .05f, .15f, .2f, mx.z + .35f, mx.z + .55f, false);
            Cylinder(StMat("manometr", new Color(.95f, .95f, .95f), .6f), new Vector3(mx.x - .25f, 1.0f, mx.z + .36f), .05f, .02f, 14, true, false);
            Kolo(true, mx + Vector3.up * (.75f + KoloW * .5f), Vector3.up, Vector3.back, "koło na montażownicy", rodz, true);
            warsztatMechanicy.Add((new Vector3(mx.x - .1f, .15f, mx.z + 1.0f), 180f));
            WkDetal("wulkanizacja", "montażownica: korpus, talerz, 4 szczęki, kolumna, ramię, głowica, zbijak, 3 pedały, manometr, koło");
        }
        // wyważarka: korpus, wał, koło, osłona (pałąk), panel z wyświetlaczem, półka z ciężarkami
        {
            var wx = new Vector3(xw + .9f, .15f, -47.4f); var szary = StMat("wyważarka", new Color(.3f, .32f, .35f), .5f, .3f);
            BoxMinMax(szary, wx.x - .3f, wx.x + .3f, .15f, 1.0f, wx.z - .35f, wx.z + .35f, true);
            BoxMinMax(StMat("półka ciężarków", new Color(.15f, .15f, .16f), .4f), wx.x - .3f, wx.x + .3f, 1.0f, 1.04f, wx.z - .35f, wx.z + .35f, false);
            for (int k = 0; k < 10; k++) BoxMinMax(Steel, wx.x - .25f + (k % 5) * .1f, wx.x - .2f + (k % 5) * .1f, 1.04f, 1.06f, wx.z - .25f + (k / 5) * .2f, wx.z - .15f + (k / 5) * .2f, false);
            BoxMinMax(szary, wx.x - .2f, wx.x + .2f, 1.0f, 1.4f, wx.z + .3f, wx.z + .4f, false);
            BoxMinMax(StSwiatlo("wyświetlacz wyważarki", new Color(.2f, .9f, .4f)), wx.x - .15f, wx.x + .15f, 1.2f, 1.35f, wx.z + .4f, wx.z + .405f, false);
            Tube(StMat("wał chrom", new Color(.8f, .81f, .83f), .85f, .9f), new Vector3(wx.x + .3f, .75f, wx.z), new Vector3(wx.x + .7f, .75f, wx.z), .025f, 10);
            Kolo(true, new Vector3(wx.x + .3f + .05f + KoloW * .5f, .75f, wx.z), Vector3.right, Vector3.up, "koło na wyważarce", rodz, true);
            for (int k = 0; k <= 8; k++)
            {
                float a0 = Mathf.PI * (k / 8f), a1 = Mathf.PI * ((k + 1) / 8f); if (k == 8) break;
                var p0 = new Vector3(wx.x + .45f, .75f + Mathf.Sin(a0) * .52f, wx.z + Mathf.Cos(a0) * .52f); var p1 = new Vector3(wx.x + .45f, .75f + Mathf.Sin(a1) * .52f, wx.z + Mathf.Cos(a1) * .52f);
                Tube(Steel, p0, p1, .015f, 6);
            }
            WkDetal("wulkanizacja", "wyważarka: korpus, półka z 10 ciężarkami, panel z wyświetlaczem, wał, koło, osłona");
        }
        // klucz pneumatyczny na zwijaczu węża (ściana boczna), pompka (model)
        {
            float kx = x1 - t - .02f, ky = 1.9f, kz = -50.4f;
            Cylinder(StMat("zwijacz", new Color(.85f, .2f, .1f), .5f), new Vector3(kx - .15f, ky - .25f, kz), .25f, .02f, 20, true, false);
            for (int k = 0; k < 6; k++) Cylinder(StMat("wąż pneumatyczny", new Color(.95f, .75f, .1f), .4f), new Vector3(kx - .15f, ky - .25f, kz), .2f - k * .015f, .015f, 18, false, false);
            Tube(StMat("wąż pneumatyczny", new Color(.95f, .75f, .1f), .4f), new Vector3(kx - .15f, ky - .45f, kz), new Vector3(kx - .3f, 1.0f, kz + .3f), .01f, 6);
            Cylinder(StMat("klucz pneumatyczny", new Color(.1f, .1f, .11f), .5f, .3f), new Vector3(kx - .3f, .82f, kz + .3f), .045f, .18f, 12, true, false);
            BoxMinMax(StMat("klucz pneumatyczny", new Color(.1f, .1f, .11f), .5f, .3f), kx - .34f, kx - .26f, .72f, .84f, kz + .26f, kz + .45f, false);
            WkModel("tire_pump", "pompka do opon", new Vector3(x1 - t - .4f, .15f, -52.6f), 200f, false);
            WkDetal("wulkanizacja", "klucz pneumatyczny na zwijaczu z wężem");
        }
        // wanienka do szukania przebić: stelaż, wanna z blachy (rant), woda, koło do połowy w wodzie
        {
            var wc = new Vector3(x1 - t - .65f, .15f, -45.6f); float ww = .5f, wd = .45f, wh = .7f;
            foreach (float lx in new[] { -ww + .03f, ww - .07f }) foreach (float lzz in new[] { -wd + .03f, wd - .07f }) BoxMinMax(Steel, wc.x + lx, wc.x + lx + .04f, .15f, wh - .2f, wc.z + lzz, wc.z + lzz + .04f, false);
            var bl = StMat("wanna ocynk", new Color(.65f, .67f, .7f), .5f, .7f);
            BoxMinMax(bl, wc.x - ww, wc.x + ww, wh - .2f, wh - .17f, wc.z - wd, wc.z + wd, true);
            BoxMinMax(bl, wc.x - ww, wc.x + ww, wh - .17f, wh + .25f, wc.z - wd, wc.z - wd + .02f, false); BoxMinMax(bl, wc.x - ww, wc.x + ww, wh - .17f, wh + .25f, wc.z + wd - .02f, wc.z + wd, false);
            BoxMinMax(bl, wc.x - ww, wc.x - ww + .02f, wh - .17f, wh + .25f, wc.z - wd + .02f, wc.z + wd - .02f, false); BoxMinMax(bl, wc.x + ww - .02f, wc.x + ww, wh - .17f, wh + .25f, wc.z - wd + .02f, wc.z + wd - .02f, false);
            Top(Water, wc.x - ww + .02f, wc.x + ww - .02f, wc.z - wd + .02f, wc.z + wd - .02f, wh + .15f);
            Kolo(true, new Vector3(wc.x, wh + .15f, wc.z), Vector3.right, Vector3.up, "koło w wanience (szukanie przebicia)", rodz, false);
            WkDetal("wulkanizacja", "wanienka: stelaż, wanna z blachy z rantami, woda, koło");
        }
        // stojaki z oponami (2 poziomy, opony pionowo bieżnikiem do przodu) przy tylnej ścianie strefy i stosy opon na posadzce
        int kola0 = wkOpony;
        {
            float sx0 = xw + .4f, sx1 = x1 - t - .1f, sz = z0 + t + .55f;
            foreach (float yy in new[] { .15f + .05f, 1.15f }) { BoxMinMax(Steel, sx0, sx1, yy, yy + .04f, sz - .32f, sz - .28f, false); BoxMinMax(Steel, sx0, sx1, yy, yy + .04f, sz + .28f, sz + .32f, false); }
            for (float px = sx0; px < sx1 + .01f; px += (sx1 - sx0) / 3f) foreach (float pz in new[] { sz - .34f, sz + .3f }) BoxMinMax(Steel, Mathf.Min(px, sx1 - .04f), Mathf.Min(px, sx1 - .04f) + .04f, .15f, 2.1f, pz, pz + .04f, false);
            int n = Mathf.FloorToInt((sx1 - sx0 - .1f) / (KoloW + .03f));
            foreach (float yy in new[] { .24f + KoloRo, 1.19f + KoloRo })
                for (int k = 0; k < n; k++) Kolo(false, new Vector3(sx0 + .08f + KoloW * .5f + k * (KoloW + .03f), yy, sz), Vector3.right, Vector3.up, "opona w stojaku", rodz, false);
            Collider(new Vector3((sx0 + sx1) * .5f, 1.1f, sz), new Vector3(sx1 - sx0, 2f, .7f));
            // stosy opon (leżące, napis na górnej)
            foreach (var (stx, stz, ile) in new[] { (x1 - t - .8f, -48.0f, 5), (x1 - t - .8f, -49.0f, 4), (xb + bw * 1.5f + 2.3f, -45.0f, 4) })
            {
                for (int k = 0; k < ile; k++) Kolo(false, new Vector3(stx, .15f + KoloW * .5f + k * KoloW, stz), Vector3.up, Vector3.back, "opona w stosie", rodz, k == ile - 1);
                Collider(new Vector3(stx, .15f + ile * KoloW * .5f, stz), new Vector3(KoloRo * 2f, ile * KoloW, KoloRo * 2f));
            }
            WkDetal("wulkanizacja", "stojak 2-poziomowy z oponami + 3 stosy opon", wkOpony - kola0);
        }
        // komplet kół do wydania przy bramie (koła na felgach stojące), szyld wewnątrz
        for (int k = 0; k < 4; k++) Kolo(true, new Vector3(45.0f + k * (KoloW + .04f), .15f + KoloRo, -43.3f), Vector3.right, Vector3.up, "koło do wydania", rodz, k == 3);
        Collider(new Vector3(45.0f + 1.5f * (KoloW + .04f), .15f + KoloRo, -43.3f), new Vector3(4f * (KoloW + .04f), KoloRo * 2f, KoloRo * 2f));
        WkDetal("wulkanizacja", "komplet 4 kół do wydania");
    }

    // auto na podnośniku (stanowisko 1): wołane z Auta() — szablony z etapu ruchu; bez dostawczaka
    static void WarsztatAuto(List<AutoSzablon> szablony)
    {
        if (!WarsztatWlaczony || szablony == null || szablony.Count == 0 || !warsztatPodnosnikC) return;
        var sz = new[] { "car_s4", "car_gmc", "car_dc", "car_mus" }.Select(id => szablony.FirstOrDefault(s => s.id == id)).FirstOrDefault(s => s != null && s.szer < 2.6f) ?? szablony.FirstOrDefault(s => s.id != "car_zuk");
        if (sz == null) return;
        float xb = 29.5f, bw = (47f - .3f - xb) / 3f, c1 = xb + bw * .5f;
        var o = AutoInstancja(sz, warsztatPodnosnikC, "Warsztat · auto na podnośniku · " + sz.id + (AutaNC.Contains(sz.id) ? " (DO WYMIANY PRZED SPRZEDAŻĄ)" : ""), new System.Random(707), out var kolor);
        o.transform.SetPositionAndRotation(new Vector3(c1, 1.62f - .25f, warsztatPodnosnikZ), Quaternion.LookRotation(Vector3.back));
        var nad = o.transform.Find("nadwozie");
        if (nad) foreach (var t in nad.Cast<Transform>().Where(t => t.name == "spaliny" || t.name.StartsWith("światło") || t.name.StartsWith("kierunkowskaz")).ToList()) Object.DestroyImmediate(t.gameObject);
        var bc = o.AddComponent<BoxCollider>(); bc.center = sz.obrys.center; bc.size = sz.obrys.size;
        LodAuta(o, .02f);
        Debug.Log("BRAXHORN_WARSZTAT auto_na_podnośniku=" + sz.id + " " + kolor + " wysokość_progu≈1.62");
    }

    // złom na zapleczu (za ścianą południową): kontener otwarty z rdzawymi felgami i starą oponą (Poly Haven)
    static void WarsztatZlom()
    {
        var g0 = group; group = "Osiedle A / warsztat — złom";
        float cx = 30f, cz = -56.2f, w = 1.6f, d = .9f, h = .9f; var kont = StMat("kontener złomu", new Color(.35f, .22f, .12f), .4f, .5f);
        BoxMinMax(kont, cx - w, cx + w, 0, .08f, cz - d, cz + d, true);
        BoxMinMax(kont, cx - w, cx + w, .08f, h, cz - d, cz - d + .04f, true); BoxMinMax(kont, cx - w, cx + w, .08f, h, cz + d - .04f, cz + d, true);
        BoxMinMax(kont, cx - w, cx - w + .04f, .08f, h, cz - d + .04f, cz + d - .04f, true); BoxMinMax(kont, cx + w - .04f, cx + w, .08f, h, cz - d + .04f, cz + d - .04f, true);
        for (float rx = cx - w + .4f; rx < cx + w - .2f; rx += .5f) foreach (float rz in new[] { cz - d - .03f, cz + d }) BoxMinMax(kont, rx, rx + .06f, .1f, h - .05f, rz, rz + .03f, false);   // żebra
        int n = 0;
        if (WkModel("rusted_wheel_rim_01", "rdzawa felga (złom)", new Vector3(cx - .8f, .08f, cz), 20f, false)) n++;
        if (WkModel("rusted_wheel_rim_02", "rdzawa felga (złom)", new Vector3(cx + .2f, .08f, cz + .2f), 75f, false)) n++;
        if (WkModel("old_tyre", "stara opona (złom)", new Vector3(cx + .9f, .08f, cz - .2f), 140f, false)) n++;
        Napis3D("ZŁOM", new Vector3(cx, h - .3f, cz + d + .01f), Vector3.forward, .14f, .01f, StMat("napis biały", new Color(.92f, .92f, .9f), .3f), kont, .15f, .14f, true);
        group = g0;
        WkDetal("zaplecze", "kontener złomu z żebrami i napisem, modele złomu", n);
    }
}
