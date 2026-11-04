using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// SPORT: PIŁKI I GRY (właściciel 1.11, 18:16: „rozrywka na osiedlu ma być użytkowa — kosz, tenis, później misje”).
// Na każdym boisku komponent BraxSportBoisko (runtime: piłki, kolizje obręczy / tablic / siatek / bramek, wynik, AKCJA, CIOS/KOP).
//   • kosz — boisko FIBA w Parku Wiśniowym (x 14.1–41.9, z −90.4…−75.6, obręcze x 15.675 / 40.325, y 3.05);
//     dodatkowe wycięcie resztek starego kosza Codexa (zdjęcie właściciela 1.11: tablica bokiem, obręcz obok, ramię krzywo);
//   • siatkówka — pół boiska plażowego przy najlepszej willi (rogi zapisane w Villa, siatka 1.43–2.43 m);
//   • tenis — kort klubu tenisowego (osiedle B): wyrzutnia piłek za linią końcową od strony bez furtki;
//   • piłka nożna — na osiedlu nie było boiska: dwie małe bramki 3 × 1.5 m na wolnym trawniku Parku Centralnego
//     (szukane automatycznie: bez drzew, ścieżek, kolizji, placu zabaw, stołów), awaryjnie na boisku do kosza.
public static partial class BraxClaudeOsiedle
{
    // zapisywane w Villa (najlepsza willa): rogi boiska do siatkówki — (a0,w0), (a1,w0), (a0,w1) na wysokości piasku
    static readonly List<Vector3[]> siatkowkaRogi = new List<Vector3[]>();
    // prostokąty boisk na trawie (bez kwiatów — FlowerPatches)
    static readonly List<Rect> sportTrawa = new List<Rect>();
    static bool SportZajete(float x, float z, float m) { foreach (var r in sportTrawa) if (x > r.xMin - m && x < r.xMax + m && z > r.yMin - m && z < r.yMax + m) return true; return false; }

    static void Sport()
    {
        group = "Sport: piłki i gry";
        sportTrawa.Clear();
        var sport = new GameObject("BRAXHORN sport: boiska z piłkami").transform; sport.SetParent(root, false);
        var opis = new List<string>();
        Guard("sport: kosz (park Wiśniowy)", () => opis.Add(SportKosz(sport)));
        Guard("sport: siatkówka (willa)", () => opis.Add(SportSiatkowka(sport)));
        Guard("sport: tenis (kort, wyrzutnia)", () => opis.Add(SportTenis(sport)));
        Guard("sport: piłka nożna (bramki)", () => opis.Add(SportNozna(sport)));
        siatkowkaRogi.Clear();
        rep.notes.Add("sport: " + string.Join("; ", opis.Where(s => !string.IsNullOrEmpty(s))));
    }

    // Kontrola po wygenerowaniu sceny: sprawdza PRAWDZIWE komponenty i kości
    // Patryka, zamiast uznawać samą obecność plików C# za gotową grę.
    public static void SprawdzSportPatryka(BraxGymPlayer gracz)
    {
        if (!gracz || !gracz.visual || !gracz.clips)
            throw new System.InvalidOperationException("OSIEDLE SPORT: brak gracza Patryka lub animacji");
        if (gracz.walkOnly || gracz.clips.GetClip("Run") == null ||
            gracz.clips.GetClip("Punch") == null || gracz.clips.GetClip("Kick") == null)
            throw new System.InvalidOperationException("OSIEDLE SPORT: Patryk nie ma biegu/ciosu/kopnięcia");
        var wymaganeKosci = new[] { "Hips", "Spine", "LeftArm", "LeftForeArm", "LeftHand",
            "RightArm", "RightForeArm", "RightHand", "LeftUpLeg", "LeftLeg", "LeftFoot",
            "RightUpLeg", "RightLeg", "RightFoot" };
        var nazwy = new HashSet<string>(gracz.visual.GetComponentsInChildren<Transform>(true)
            .Select(t => PatrykKosc(t.name)));
        var braki = wymaganeKosci.Where(n => !nazwy.Contains(n)).ToArray();
        if (braki.Length > 0)
            throw new System.InvalidOperationException("OSIEDLE SPORT: brak kości Patryka " + string.Join(", ", braki));
        if (!gracz.GetComponent<BraxRuchySport>())
            throw new System.InvalidOperationException("OSIEDLE SPORT: ruchy kosza/tenisa nie są podłączone do Patryka");

        var boiska = UnityEngine.Object.FindObjectsByType<BraxSportBoisko>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var gra in new[] { BraxSportBoisko.Gra.Kosz, BraxSportBoisko.Gra.Nozna, BraxSportBoisko.Gra.Tenis })
        {
            if (!boiska.Any(b => b && b.gra == gra && b.cele != null && b.cele.Length > 0 &&
                                  b.strefa.x > 1f && b.strefa.y > 1f))
                throw new System.InvalidOperationException("OSIEDLE SPORT: nie utworzono grywalnego boiska " + gra);
        }
        Debug.Log("BRAXHORN_SPORT_PATRYK_OK boiska=" + boiska.Length +
            " (kosz/nożna/tenis), rig=" + wymaganeKosci.Length +
            " kości, Run/Punch/Kick=true, ruchy proceduralne=true, build wyłącznie osiedla");
    }

    static BraxSportBoisko NoweBoisko(Transform sport, string nazwa, BraxSportBoisko.Gra gra)
    {
        var go = new GameObject(nazwa); go.transform.SetParent(sport, false);
        var b = go.AddComponent<BraxSportBoisko>(); b.gra = gra; b.jednostkiNaMetr = K; return b;
    }

    // ---------------- koszykówka ----------------
    static string SportKosz(Transform sport)
    {
        group = "Park Wiśniowy / boisko do kosza";
        const float cx = 28f, cz = -83f, hx = 13.9f, hz = 7.4f;   // jak w ParkA (linie FIBA, kosze w osi boiska)
        // Kosz w ParkA (po poprawce z buildu 11): słup 1 m za linią końcową, wysięgnik wzdłuż osi, tablica 1.8 × 1.05 prostopadle
        // do osi (patrzy na środek boiska) 1.2 m przed linią, obręcz Ø 0.46 na 3.05 m, 0.15 m przed tablicą. Ze zdjęcia
        // właściciela (stary build) — tablica bokiem i ramię obok: wycinam jeszcze raz szerzej wszystko, co z kosza Codexa mogło
        // zostać nad 1.6 m (tablica, wysięgnik, obręcz obrócone o 90°), z kolizjami. Nowe kosze są w partiach „CLAUDE”, nie w estate.
        var estate = GameObject.Find("braxhorn-estate")?.transform; var physics = GameObject.Find("Original estate collisions")?.transform;
        int wylaczone = 0;
        foreach (int s in new[] { -1, 1 })
        {
            float xe = cx + s * hx, xa = Mathf.Min(xe - s * 3.0f, xe + s * 1.2f), xb = Mathf.Max(xe - s * 3.0f, xe + s * 1.2f);
            var vol = MinMax(xa, xb, 1.6f, 4.4f, cz - 1.4f, cz + 1.4f);
            if (estate) CutVolumes(estate, physics, new List<Bounds> { vol });
            if (physics) foreach (var c in physics.GetComponentsInChildren<Collider>(true))
                    if (c.enabled && vol.Contains(c.bounds.min) && vol.Contains(c.bounds.max)) { c.enabled = false; wylaczone++; }
        }
        var b = NoweBoisko(sport, "boisko do koszykówki (park Wiśniowy)", BraxSportBoisko.Gra.Kosz);
        b.srodek = new Vector3(cx, .08f, cz); b.osX = Vector3.right; b.pol = new Vector2(hx, hz); b.strefa = new Vector2(hx + 2.5f, hz + 2.5f);
        b.cele = new[] { new Vector3(cx - hx + 1.575f * K, .08f + 3.05f * K, cz), new Vector3(cx + hx - 1.575f * K, .08f + 3.05f * K, cz) };
        b.tablice = new[] { new Vector3(cx - hx + 1.2f * K, .08f + 3.425f * K, cz), new Vector3(cx + hx - 1.2f * K, .08f + 3.425f * K, cz) };
        return "kosz w Parku Wiśniowym (2 obręcze z kolizją, piłka" + (wylaczone > 0 ? ", " + wylaczone + " kolizji starego kosza wyłączonych" : "") + ")";
    }

    // ---------------- siatkówka ----------------
    static string SportSiatkowka(Transform sport)
    {
        int n = 0; var byly = new List<Vector3>();
        foreach (var rg in siatkowkaRogi)
        {
            if (rg == null || rg.Length < 3) continue;
            Vector3 c00 = rg[0], c10 = rg[1], c01 = rg[2];
            var c = (c10 + c01) * .5f;   // środek prostokąta (przekątna c10–c01)
            if (byly.Any(q => (q - c).sqrMagnitude < 1f)) continue; byly.Add(c);
            var poprz = c01 - c00; var wzdl = c10 - c00;   // poprz: w głąb (przez siatkę), wzdl: wzdłuż siatki
            var b = NoweBoisko(sport, "boisko do siatkówki (willa)", BraxSportBoisko.Gra.Siatkowka);
            b.srodek = new Vector3(c.x, c00.y, c.z); b.osX = poprz.normalized;
            b.pol = new Vector2(poprz.magnitude * .5f, wzdl.magnitude * .5f); b.strefa = b.pol + new Vector2(1.6f, 1.2f);
            b.siatkaDol = 1.43f * K; b.siatkaGora = 2.43f * K; b.siatkaPol = wzdl.magnitude * .5f + .45f;
            n++;
        }
        return n > 0 ? "siatkówka przy willi (" + n + ")" : "siatkówka: brak boiska (willa bez boiska?)";
    }

    // ---------------- tenis ----------------
    static string SportTenis(Transform sport)
    {
        if (tennisFence.width < 10f) return "tenis: brak kortu";
        group = "Osiedle B / klub tenisowy";
        float cx = tennisFence.center.x, cz = tennisFence.center.y;   // kort 23.77 × 10.97 wzdłuż x, siatka na x = cx; furtka przy x = cx + 14.8
        float lx = cx - 13.4f, ly = .04f;                             // wyrzutnia za zachodnią linią końcową (gracz wchodzi od wschodu)
        Wyrzutnia(new Vector3(lx, ly, cz));
        var b = NoweBoisko(sport, "kort tenisowy (wyrzutnia piłek)", BraxSportBoisko.Gra.Tenis);
        b.srodek = new Vector3(cx, ly, cz); b.osX = Vector3.right; b.pol = new Vector2(11.885f, 5.485f);
        b.strefa = new Vector2(tennisFence.width * .5f - .2f, tennisFence.height * .5f - .2f);
        b.cele = new[] { new Vector3(lx, ly, cz) };
        return "tenis (wyrzutnia x " + lx.ToString("0.0") + ", z " + cz.ToString("0.0") + ")";
    }

    // Wyrzutnia piłek: zgrabna skrzynka na kółkach (ciemna zieleń), kosz na piłki z żółtymi piłkami, rura wylotowa w stronę kortu (+x),
    // rączka do pchania. Nic nie świeci.
    static void Wyrzutnia(Vector3 p)
    {
        var body = Plain("wyrzutnia piłek zieleń", new Color(.10f, .27f, .19f), .45f, .1f);
        var trim = Plain("wyrzutnia piłek grafit", new Color(.08f, .085f, .09f), .35f, .3f);
        var balls = Plain("piłki tenisowe w koszu", new Color(.78f, .88f, .20f), .1f);
        var tyre = Plain("opona wyrzutni", new Color(.05f, .05f, .05f), .2f);
        float x = p.x, y = p.y, z = p.z;
        BoxMinMax(body, x - .32f, x + .32f, y + .2f, y + .72f, z - .25f, z + .25f, true, true);                 // skrzynia
        BoxMinMax(trim, x - .34f, x + .34f, y + .70f, y + .74f, z - .27f, z + .27f, false, true);               // listwa
        foreach (int s in new[] { -1, 1 })                                                                      // kosz na piłki (ścianki)
        {
            BoxMinMax(trim, x - .3f, x + .3f, y + .74f, y + 1.0f, z + s * .245f - .01f, z + s * .245f + .01f, false);
            BoxMinMax(trim, x + s * .29f - .01f, x + s * .29f + .01f, y + .74f, y + 1.0f, z - .245f, z + .245f, false);
        }
        Top(balls, x - .28f, x + .28f, z - .23f, z + .23f, y + .93f);
        foreach (int s in new[] { -1, 1 })                                                                      // kółka
        {
            Tube(tyre, new Vector3(x - .16f, y + .12f, z + s * .27f), new Vector3(x - .16f, y + .12f, z + s * .32f), .12f, 14);
            Tube(trim, new Vector3(x - .16f, y + .12f, z + s * .255f), new Vector3(x - .16f, y + .12f, z + s * .335f), .035f, 8);
        }
        Tube(tyre, new Vector3(x + .22f, y + .05f, z - .03f), new Vector3(x + .22f, y + .05f, z + .03f), .05f, 10);   // kółko skrętne
        Tube(trim, new Vector3(x + .22f, y + .1f, z), new Vector3(x + .22f, y + .2f, z), .02f, 6);
        Tube(trim, new Vector3(x + .18f, y + .6f, z), new Vector3(x + .46f, y + .78f, z), .05f, 12);                   // rura wylotowa
        foreach (int s in new[] { -1, 1 }) Tube(trim, new Vector3(x - .3f, y + .68f, z + s * .2f), new Vector3(x - .58f, y + 1.05f, z + s * .2f), .016f, 6);
        Tube(trim, new Vector3(x - .58f, y + 1.05f, z - .22f), new Vector3(x - .58f, y + 1.05f, z + .22f), .02f, 8);     // rączka
    }

    // ---------------- piłka nożna ----------------
    static string SportNozna(Transform sport)
    {
        group = "Park Centralny / bramki";
        const float L = 18f, W = 11f, gl = 1f;   // odległość linii bramkowych, szerokość pola, głębokość bramki
        float z0 = ParkZ0 + 3f, z1 = FenceA - 3.6f;
        var fc = new Vector2(AxisX, (ParkZ0 + FenceA - .6f) * .5f);
        var picnic = new[] { new Vector2(-80, -114), new Vector2(-48, -142), new Vector2(40, -113), new Vector2(75, -138) };
        Physics.SyncTransforms();
        bool Wolne(float cx, float cz)
        {
            float hx = L * .5f + gl + 1.5f, hz = W * .5f + 1.5f;
            if (cx - hx < X0 + 4f || cx + hx > X1 - 4f || cz - hz < z0 || cz + hz > z1) return false;
            if (new Vector2(cx - fc.x, cz - fc.y).magnitude < 16f + hx) return false;                         // plac z fontanną
            foreach (var q in picnic) if (Mathf.Abs(q.x - cx) < hx + 4f && Mathf.Abs(q.y - cz) < hz + 4f) return false;
            if (cx + hx > -84f && cx - hx < -60f && cz + hz > -149f && cz - hz < -126f) return false;         // plac zabaw
            if (cx + hx > 45f && cx - hx < 71f && cz + hz > -123f && cz - hz < -101f) return false;            // siłownia plenerowa
            foreach (var t in treeSpots) if (Mathf.Abs(t.x - cx) < hx + 2.5f && Mathf.Abs(t.z - cz) < hz + 2.5f) return false;   // pnie i korony
            for (float x = cx - hx; x <= cx + hx + .01f; x += 1f)
                for (float z = cz - hz; z <= cz + hz + .01f; z += 1f) if (IsHard(x, z, .2f)) return false;   // ścieżki, place, płoty
            foreach (var c in Physics.OverlapBox(new Vector3(cx, 1.05f, cz), new Vector3(hx, .9f, hz), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                if (c.bounds.size.x < 60f && c.bounds.size.z < 60f) return false;                             // ławki, latarnie, kosze, krzewy z kolizją
            return true;
        }
        float bx = 0, bz = 0; bool ok = false; float best = 1e9f;
        for (float cz = z0; cz <= z1; cz += 1.5f)
            for (float cx = X0 + 4f; cx <= X1 - 4f; cx += 2f)
            {
                float score = Mathf.Abs(cx - (AxisX + 45f)) + Mathf.Abs(cz - fc.y) * .5f;   // bliżej alei W-E i środka parku
                if (score >= best || !Wolne(cx, cz)) continue;
                best = score; bx = cx; bz = cz; ok = true;
            }
        float gy = 0f; string gdzie;
        if (ok) { gdzie = "trawnik Parku Centralnego (" + bx.ToString("0") + ", " + bz.ToString("0") + ")"; sportTrawa.Add(Rect.MinMaxRect(bx - L * .5f - gl - .5f, bz - W * .5f - .5f, bx + L * .5f + gl + .5f, bz + W * .5f + .5f)); }
        else { bx = 28f; bz = -83f; gy = .08f; gdzie = "boisko do kosza w Parku Wiśniowym (brak wolnego trawnika)"; }   // bramki na liniach pod koszami
        float half = ok ? L * .5f : 13.9f - gl;
        foreach (int s in new[] { -1, 1 }) Bramka(bx + s * half, gy, bz, s, 3f * K, 1.5f * K, gl);
        var b = NoweBoisko(sport, "bramki do piłki nożnej", BraxSportBoisko.Gra.Nozna);
        b.srodek = new Vector3(bx, gy, bz); b.osX = Vector3.right; b.pol = new Vector2(half, ok ? W * .5f : 7.4f);
        b.strefa = new Vector2(half + gl + 2.5f, (ok ? W * .5f : 7.4f) + 2.5f);
        b.cele = new[] { new Vector3(bx - half, gy, bz), new Vector3(bx + half, gy, bz) };
        b.bramkaSzer = 3f * K; b.bramkaWys = 1.5f * K; b.bramkaGl = gl;
        return "piłka nożna: 2 bramki — " + gdzie;
    }

    // Mała bramka (3 × 1.5 m, głębokość 1 m): białe słupki i poprzeczka z rur, rama tylna, siatka z oczkami 15 cm (cienkie listwy).
    // Linia bramkowa na x = gx, bramka wychodzi na zewnątrz pola w stronę ox (±1). Kolizje stawia BraxSportBoisko (warstwa gracza).
    static void Bramka(float gx, float gy, float gz, float ox, float w, float h, float d)
    {
        var white = Plain("bramka biała", new Color(.92f, .92f, .90f), .45f, .1f);
        var netM = Plain("siatka bramki", new Color(.86f, .86f, .84f), .2f);
        float xb = gx + ox * d, x0 = Mathf.Min(gx, xb), x1 = Mathf.Max(gx, xb), hw = w * .5f;
        const float t = .006f, k = .15f;
        foreach (int s in new[] { -1, 1 })
        {
            float zz = gz + s * hw;
            Tube(white, new Vector3(gx, gy, zz), new Vector3(gx, gy + h, zz), .05f, 10);                     // słupek
            Tube(white, new Vector3(xb, gy, zz), new Vector3(xb, gy + h, zz), .025f, 8);                     // słupek tylny
            Tube(white, new Vector3(gx, gy + h, zz), new Vector3(xb, gy + h, zz), .025f, 8);                 // górna boczna
            Tube(white, new Vector3(gx, gy + .025f, zz), new Vector3(xb, gy + .025f, zz), .025f, 8);         // dolna boczna
        }
        Tube(white, new Vector3(gx, gy + h, gz - hw - .05f), new Vector3(gx, gy + h, gz + hw + .05f), .05f, 10);   // poprzeczka
        Tube(white, new Vector3(xb, gy + h, gz - hw), new Vector3(xb, gy + h, gz + hw), .025f, 8);
        Tube(white, new Vector3(xb, gy + .025f, gz - hw), new Vector3(xb, gy + .025f, gz + hw), .025f, 8);
        for (float z = -hw + k; z < hw - .01f; z += k) BoxMinMax(netM, xb - t, xb + t, gy, gy + h, gz + z - t, gz + z + t, false);   // tył: pion
        for (float y = k; y < h - .01f; y += k) BoxMinMax(netM, xb - t, xb + t, gy + y - t, gy + y + t, gz - hw, gz + hw, false);     // tył: poziom
        foreach (int s in new[] { -1, 1 })
        {
            float zz = gz + s * hw;
            for (float x = k; x < d - .01f; x += k) BoxMinMax(netM, gx + ox * x - t, gx + ox * x + t, gy, gy + h, zz - t, zz + t, false);
            for (float y = k; y < h - .01f; y += k) BoxMinMax(netM, x0, x1, gy + y - t, gy + y + t, zz - t, zz + t, false);
        }
        for (float z = -hw + k; z < hw - .01f; z += k) BoxMinMax(netM, x0, x1, gy + h - t, gy + h + t, gz + z - t, gz + z + t, false);  // dach
        for (float x = k; x < d - .01f; x += k) BoxMinMax(netM, gx + ox * x - t, gx + ox * x + t, gy + h - t, gy + h + t, gz - hw, gz + hw, false);
    }
}
