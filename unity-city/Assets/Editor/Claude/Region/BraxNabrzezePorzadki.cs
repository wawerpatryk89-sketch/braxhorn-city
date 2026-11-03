using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// PORZĄDKI PRZY MARINIE I BULWARZE (właściciel 3.11 10:49: „te sklepy też mają zniknąć”; zdjęcia fe1bd7d6, 8ecc465c, 7af35aa2,
// aa5d979f). Wszystko poniżej pochodzi ze ŹRÓDŁA MIASTA (eksport gry przeglądarkowej, dev/src/district.js @260a589 → glTF;
// glTFast odbija X, więc x Unity = −x źródła) — nie z naszego C#. Zdejmowane wycięciem geometrii źródła i jego kolizji
// (BraxNabrzeze.Wytnij = BraxCityMeshCut: przycięcie trójkątów do bryły, posadzka y 0.15 zostaje — bryły zaczynają się 2 mm wyżej):
//  • PUNKT DOSTAW — kontener-biuro (contGreen, dach, drzwi, okienko z ladą, szyld „PUNKT DOSTAW / ODBIÓR PRZESYŁEK”):
//    źródło x −82.6…−80.2, z 79.6…85.8 (kolizja „cont”) → Unity x 80.2…82.6 (z ladą okienka 79.7), z 79.6…85.8;
//  • „wiata z zielonym dachem” ze zdjęcia 8ecc465c = DWA GARAŻE ze źródła (garages.glb, „garages along the west wall”):
//    źródło x −60.7, z 84.5 i 91.5 → Unity x 58.9…62.5, z 81.5…95.1 (kolizje „garage”), za ceglanym murem podwórka.
//  Palety, skrzynki i wózek przy punkcie dostaw ZOSTAJĄ (nie były w zgłoszeniu).
// MUR PODWÓRKA OD BULWARU (zdjęcia 7af35aa2, aa5d979f: „cegły kilka razy za duże”): źródło — mats.brick z kanwy streetBrick
// (4 cegły × 8 warstw na kafel 2 m → cegła ~0.5 × 0.25 m, 2–4 × za duża). Mur (x 64…64.5, z 60…74 i 80…100, h 3.2 + czapa)
// i słupy bramy (z 74 i 80, h 3.7) WYCIĘTE i postawione od nowa w tym samym obrysie: cegła PBR Poly Haven red_brick_03
// (kafel 1 × 1 m skanu = cegła ~21.5 × 6.5 cm, spoina ~1 cm) w skali × k, czapa muru i nakrywy słupów z prefabrykatu
// (profil ze spadkiem i kapinosem), kolizje jak w źródle. Skrzydła bramy (stal) zostają.
public static class BraxNabrzezePorzadki
{
    static float K => BraxNabrzeze.K;
    static Bounds MM(float x0, float y0, float z0, float x1, float y1, float z1) { var b = new Bounds(); b.SetMinMax(new Vector3(x0, y0, z0), new Vector3(x1, y1, z1)); return b; }

    public static void Apply(Transform portRoot)
    {
        UsunKioski();
        MurPodworka(portRoot);
    }

    static void UsunKioski()
    {
        var src = BraxClaudeGround.SourceCity; var lista = new List<string>();
        void Usun(string co, Bounds b)
        {
            var siatki = new List<string>();
            if (src) foreach (var r in src.GetComponentsInChildren<MeshRenderer>(true))
                    if (r.enabled && r.bounds.Intersects(b)) siatki.Add(r.name + "[" + (r.sharedMaterial ? r.sharedMaterial.name : "-") + "]");
            BraxNabrzeze.Wytnij(b, co);
            lista.Add(co + " @ x " + b.min.x.ToString("F2") + "…" + b.max.x.ToString("F2") + ", z " + b.min.z.ToString("F2") + "…" + b.max.z.ToString("F2")
                      + ", y do " + b.max.y.ToString("F2") + " (siatki źródła przycięte: " + string.Join(", ", siatki.Take(14)) + (siatki.Count > 14 ? " …" : "") + ")");
        }
        Usun("PUNKT DOSTAW (kontener-biuro, szyld, okienko z ladą, drzwi)", MM(79.65f, .152f, 79.5f, 82.7f, 2.9f, 85.9f));
        Usun("wiata z zielonym dachem = 2 garaże źródła za murem podwórka", MM(58.8f, .152f, 81.3f, 62.6f, 3.1f, 95.2f));
        Debug.Log("BRAXHORN_USUNIETE_KIOSKI n=" + lista.Count + (src ? "" : " (BRAK źródła miasta — nic nie wycięte)") + " | " + string.Join(" | ", lista));
    }

    static void MurPodworka(Transform portRoot)
    {
        // 1) zdjęcie starego muru (bez skrzydeł bramy x 63.82…63.90 i bez budynku przy z 60)
        var b1 = MM(63.91f, .152f, 60.005f, 64.59f, 3.36f, 73.65f);
        var b2 = MM(63.91f, .152f, 80.35f, 64.59f, 3.36f, 99.95f);
        var s1 = MM(63.849f, .152f, 73.649f, 64.651f, 3.75f, 74.351f);
        var s2 = MM(63.849f, .152f, 79.649f, 64.651f, 3.75f, 80.351f);
        var b3 = MM(63.125f, .152f, 99.95f, 64.6f, 4.2f, 100.56f);   // mur poprzeczny przy bloku (z 100…100.5, h 4) — lico bloku x 63.12 zostaje
        foreach (var (bb, nz) in new[] { (b1, "mur podwórka płd."), (b2, "mur podwórka płn."), (s1, "słup bramy płd."), (s2, "słup bramy płn."), (b3, "mur przy bloku (z 100)") }) BraxNabrzeze.Wytnij(bb, nz);
        // 2) nowy mur
        var root = new GameObject("CLAUDE plaża: mur podwórka od bulwaru (cegła PBR × k)").transform; root.SetParent(portRoot, false);
        Material cegla;
        try { cegla = BraxRegion.Pbr("Mur podwórka: cegła czerwona (red_brick_03, × k)", 1.0f * K, new Color(.97f, .95f, .93f), "red_brick_03", "brick_wall_001"); }
        catch (System.Exception) { cegla = BraxNabrzeze.Kolor("Mur podwórka: cegła (brak tekstury)", new Color(.56f, .25f, .19f), .85f); BraxNabrzeze.Log("mur podwórka: brak red_brick_03 — kolor zastępczy"); }
        var beton = BraxNabrzeze.Mat("Mur podwórka: czapa z prefabrykatu", 2f, new Color(.8f, .79f, .76f), "concrete_wall_008", "concrete_floor_02");
        var z = new BraxNabrzeze.Zbior(); var czapy = new UlSiatka();
        const float x0 = 64f, x1 = 64.5f, yB = .15f, yM = 3.2f, yS = 3.7f;
        // czapa muru: prefabrykat ze spadkiem dwustronnym (wystaje 8 cm po obu stronach), profil w osi x
        var czapa = new[] { new Vector2(-.33f, 0), new Vector2(-.33f, .07f), new Vector2(0, .14f), new Vector2(.33f, .07f), new Vector2(.33f, 0) };
        foreach (var (za, zb) in new[] { (60.005f, 73.65f), (80.35f, 100f) })
        {
            z[cegla].BoxMM(new Vector3(x0, yB, za), new Vector3(x1, yM, zb));
            BraxMiastoWyspa.Wyciag(czapy, czapa, new Vector3((x0 + x1) * .5f, yM, za), Vector3.forward, Vector3.up, Vector3.right, zb - za);
            BraxNabrzeze.KolMM(root, "Mur podwórka", new Vector3(x0, yB, za), new Vector3(x1, 3.3f, zb));
        }
        foreach (float zc in new[] { 74f, 80f })
        {
            z[cegla].BoxMM(new Vector3(63.85f, yB, zc - .35f), new Vector3(64.65f, yS, zc + .35f));
            // nakrywa słupa: płyta z kapinosem i ostrosłup ścięty (spadek na 4 strony)
            var c = new Vector3(64.25f, yS, zc); float a = .46f, h = .06f, t = .18f;
            czapy.Quad(c + new Vector3(-a, 0, -a), c + new Vector3(a, 0, -a), c + new Vector3(a, h, -a), c + new Vector3(-a, h, -a), Vector3.back);
            czapy.Quad(c + new Vector3(-a, 0, a), c + new Vector3(a, 0, a), c + new Vector3(a, h, a), c + new Vector3(-a, h, a), Vector3.forward);
            czapy.Quad(c + new Vector3(-a, 0, -a), c + new Vector3(-a, 0, a), c + new Vector3(-a, h, a), c + new Vector3(-a, h, -a), Vector3.left);
            czapy.Quad(c + new Vector3(a, 0, -a), c + new Vector3(a, 0, a), c + new Vector3(a, h, a), c + new Vector3(a, h, -a), Vector3.right);
            czapy.Quad(c + new Vector3(-a, 0, -a), c + new Vector3(a, 0, -a), c + new Vector3(a, 0, a), c + new Vector3(-a, 0, a), Vector3.down);
            var top = c + Vector3.up * (h + t); float r = .08f;
            Vector3 Q(float sx, float sz) => c + new Vector3(sx * a, h, sz * a); Vector3 Tq(float sx, float sz) => top + new Vector3(sx * r, 0, sz * r);
            czapy.Quad(Q(-1, -1), Q(1, -1), Tq(1, -1), Tq(-1, -1), new Vector3(0, .9f, -.45f)); czapy.Quad(Q(1, 1), Q(-1, 1), Tq(-1, 1), Tq(1, 1), new Vector3(0, .9f, .45f));
            czapy.Quad(Q(-1, 1), Q(-1, -1), Tq(-1, -1), Tq(-1, 1), new Vector3(-.45f, .9f, 0)); czapy.Quad(Q(1, -1), Q(1, 1), Tq(1, 1), Tq(1, -1), new Vector3(.45f, .9f, 0));
            czapy.Quad(Tq(-1, -1), Tq(1, -1), Tq(1, 1), Tq(-1, 1), Vector3.up);
            BraxNabrzeze.KolMM(root, "Słup bramy podwórka", new Vector3(63.85f, yB, zc - .35f), new Vector3(64.65f, yS, zc + .35f));
        }
        // mur poprzeczny przy bloku: x 63.125…64.5, z 100…100.5, h 4 + czapa (profil w osi z, wyciągnięty wzdłuż x)
        z[cegla].BoxMM(new Vector3(63.125f, yB, 100f), new Vector3(64.5f, 4f, 100.5f));
        BraxMiastoWyspa.Wyciag(czapy, czapa.Select(q => new Vector2(q.x * .91f, q.y)).ToArray(), new Vector3(63.125f, 4f, 100.25f), Vector3.right, Vector3.up, Vector3.forward, 64.55f - 63.125f);
        BraxNabrzeze.KolMM(root, "Mur przy bloku", new Vector3(63.125f, yB, 100f), new Vector3(64.5f, 4f, 100.5f));
        int n = z.Emit(root, "Mur podwórka", true);
        BraxNabrzeze.Obiekt(root, "Mur podwórka: czapy (prefabrykat)", czapy.Zbuduj("Mur podwórka: czapy"), beton, false, true);
        Debug.Log("BRAXHORN_MUR_PODWORKA cegła " + cegla.name + " (kafel " + (1.0f * K).ToString("F3") + " m = skan 1 m × k → cegła ~" + (21.5f * K).ToString("F0") + " × " + (6.5f * K).ToString("F1")
                  + " cm), mur x 64…64.5 z 60…73.65 i 80.35…100 h 3.2 + czapa, mur przy bloku z 100…100.5 h 4, słupy z 74 i 80 h 3.7 + nakrywy, siatek " + (n + 1));
    }
}
