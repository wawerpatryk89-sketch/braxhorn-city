using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// E23 (runda 6): AUTOBUSY OSIEDLA. Drogi A↔B przez parki jeszcze nie ma (E17 — plan), więc linie są dwie:
//  • linia A — pętla „A pętla Poznańska–Bydgoska (zgodnie)” z tras ruchu: przystanki OSIEDLE A 1 i OSIEDLE A 3 (kierunek
//    północny Poznańskiej); OSIEDLE A 2 (kierunek przeciwny) stoi dla pary, bez autobusu w tę stronę;
//  • linia B — ulica główna B w obie strony (osobny autobus na każdy kierunek, trasy otwarte jak auta B): OSIEDLE B 1
//    (na wschód) i OSIEDLE B 2 (na zachód); na końcu ulicy autobus czeka poza kadrem i wjeżdża od nowa.
// Model autobusu w kodzie (paczki aut nie mają autobusu): midi 8 × 2.4 × 2.9 m × K, nadwozie z fazowanymi narożnikami,
// nadkola z wnękami, pas szyb z ciemnymi słupkami, okap dachu, klimatyzator, zderzaki, lusterka na wysięgnikach, reflektory,
// lampy tylne, wyświetlacz linii, pas w kolorze linii, 2 drzwi dwuskrzydłowe po prawej (skrzydła ruchome, ciemny otwór),
// 4 koła z kodu (205/55 R16 przeskalowane do Ø ≈ 1.0 m × K). Ruch, drzwi i pasażerowie: BraxAutobus (gra).
// Logi: BRAXHORN_AUTOBUS, BRAXHORN_AUTOBUS_TRASA (próbki ścieżki poza jezdnią — limit 2000 próbek).
public static partial class BraxClaudeOsiedle
{
    static Mesh autobusMesh, skrzydloMesh; static Material[] autobusMats;
    static readonly List<BraxAutobus> autobusy = new List<BraxAutobus>();
    static float BusL => 8.0f * K; static float BusW => 2.4f * K; static float BusH => 2.9f * K;
    static float BusOsP => BusL * .5f - 2.6f; static float BusOsT => BusOsP - 4.8f;            // osie (przednia, tylna)
    static float BusDrzwi1 => BusL * .5f - 1.0f; static float BusDrzwi2 => (BusOsP + BusOsT) * .5f + .2f; const float BusDrzwiSzer = 1.5f;

    static void AutobusModel(Color pas)
    {
        float hw = BusW * .5f, hl = BusL * .5f, ch = .28f;
        var sb = new SiatkaSalonu();
        // pryzmat o rzucie z fazowanymi narożnikami: od z0 do z1, y0..y1, faza przy końcach (fz0/fz1)
        void Pryzmat(int sub, float z0, float z1, float y0, float y1, float w, bool fz0, bool fz1, bool gora, bool dol)
        {
            var obw = new List<Vector2>();
            float c0 = fz0 ? ch : 0f, c1 = fz1 ? ch : 0f;
            obw.Add(new Vector2(w, z0 + c0)); obw.Add(new Vector2(w, z1 - c1)); if (c1 > 0) obw.Add(new Vector2(w - c1, z1));
            obw.Add(new Vector2(-w + c1, z1)); if (c1 > 0) obw.Add(new Vector2(-w, z1 - c1)); obw.Add(new Vector2(-w, z0 + c0));
            if (c0 > 0) obw.Add(new Vector2(-w + c0, z0)); obw.Add(new Vector2(w - c0, z0)); if (c0 > 0) obw.Add(new Vector2(w, z0 + c0));
            for (int i = 0; i + 1 < obw.Count; i++)
            {
                var a = obw[i]; var b = obw[i + 1]; if ((a - b).sqrMagnitude < 1e-6f) continue;
                var n = new Vector3(b.y - a.y, 0, -(b.x - a.x)).normalized; if (Vector3.Dot(n, new Vector3((a.x + b.x) * .5f, 0, (a.y + b.y) * .5f - (z0 + z1) * .5f)) < 0) n = -n;
                sb.Quad(sub, new Vector3(a.x, y0, a.y), new Vector3(b.x, y0, b.y), new Vector3(b.x, y1, b.y), new Vector3(a.x, y1, a.y), n);
            }
            var srodek = new Vector3(0, 0, (z0 + z1) * .5f);
            for (int i = 0; i + 1 < obw.Count; i++)
            {
                var a = obw[i]; var b = obw[i + 1]; if ((a - b).sqrMagnitude < 1e-6f) continue;
                if (gora) sb.Quad(sub, srodek + Vector3.up * y1, new Vector3(a.x, y1, a.y), new Vector3(b.x, y1, b.y), srodek + Vector3.up * y1, Vector3.up);
                if (dol) sb.Quad(sub, srodek + Vector3.up * y0, new Vector3(b.x, y0, b.y), new Vector3(a.x, y0, a.y), srodek + Vector3.up * y0, Vector3.down);
            }
        }
        float yS = .32f, yA = 1.15f, yP = 1.3f, yO = 3.05f, yD = 3.4f, rW = .41f * 1.55f;
        // dół z nadkolami: segmenty między kołami (pełna wysokość) i nad kołami (od yA)
        float[] kz = { BusOsP, BusOsT }; float ok = rW + .12f;
        Pryzmat(0, BusOsP + ok, hl, yS, yP, hw, false, true, false, true);
        Pryzmat(0, BusOsT + ok, BusOsP - ok, yS, yP, hw, false, false, false, true);
        Pryzmat(0, -hl, BusOsT - ok, yS, yP, hw, true, false, false, true);
        foreach (float z in kz)
        {
            Pryzmat(0, z - ok, z + ok, yA, yP, hw, false, false, false, true);
            foreach (float sx in new[] { -1f, 1f })
            {
                sb.Box(2, new Vector3(sx > 0 ? hw - .75f : -hw + .7f, yS, z - ok), new Vector3(sx > 0 ? hw - .7f : -hw + .75f, yA, z + ok));   // ściana wnęki (za kołem)
                sb.Box(2, new Vector3(sx > 0 ? hw - .75f : -hw, yA - .02f, z - ok), new Vector3(sx > 0 ? hw : -hw + .75f, yA, z + ok));          // nadkole od spodu
            }
        }
        // pas szyb, słupki, dach z okapem, klimatyzator
        Pryzmat(1, -hl, hl, yP, yO, hw - .01f, true, true, false, false);
        Pryzmat(0, -hl, hl, yO, yD, hw, true, true, false, false);
        Pryzmat(0, -hl + .1f, hl - .1f, yD, yD + .08f, hw - .1f, true, true, true, false);
        sb.Box(3, new Vector3(-.75f, yD + .08f, -1.4f), new Vector3(.75f, yD + .32f, .9f));
        for (float z = -hl + 1.2f; z < hl - 1.0f; z += 1.25f)
            foreach (float sx in new[] { -1f, 1f })
            {
                if (sx > 0 && (Mathf.Abs(z - BusDrzwi1) < BusDrzwiSzer * .5f + .1f || Mathf.Abs(z - BusDrzwi2) < BusDrzwiSzer * .5f + .1f)) continue;
                sb.Box(2, new Vector3(sx > 0 ? hw - .02f : -hw - .005f, yP, z - .05f), new Vector3(sx > 0 ? hw + .005f : -hw + .02f, yO, z + .05f));
            }
        sb.Box(2, new Vector3(-hw + ch, yP, hl - .02f), new Vector3(hw - ch, yP + .08f, hl + .005f));          // dolna ramka szyby przedniej
        // pas w kolorze linii, zderzaki, reflektory, lampy tylne, wyświetlacz, lusterka
        sb.Box(4, new Vector3(-hw - .006f, 1.0f, -hl + ch), new Vector3(-hw, 1.12f, hl - ch));
        {   // prawa strona: pas przerwany na drzwiach
            float d1a = BusDrzwi2 - BusDrzwiSzer * .5f - .05f, d1b = BusDrzwi2 + BusDrzwiSzer * .5f + .05f, d2a = BusDrzwi1 - BusDrzwiSzer * .5f - .05f;
            foreach (var (a0, a1) in new[] { (-hl + ch, d1a), (d1b, d2a) }) if (a1 > a0) sb.Box(4, new Vector3(hw, 1.0f, a0), new Vector3(hw + .006f, 1.12f, a1));
        }
        sb.Box(2, new Vector3(-hw + .05f, yS - .02f, hl - .02f), new Vector3(hw - .05f, yS + .26f, hl + .07f));
        sb.Box(2, new Vector3(-hw + .05f, yS - .02f, -hl - .07f), new Vector3(hw - .05f, yS + .26f, -hl + .02f));
        foreach (float sx in new[] { -1f, 1f })
        {
            sb.Box(5, new Vector3(sx > 0 ? hw - .55f : -hw + .2f, .68f, hl), new Vector3(sx > 0 ? hw - .2f : -hw + .55f, .84f, hl + .012f));
            sb.Box(6, new Vector3(sx > 0 ? hw - .3f : -hw + .12f, .8f, -hl - .012f), new Vector3(sx > 0 ? hw - .12f : -hw + .3f, 1.5f, -hl));
            var ram = new Vector3(sx * (hw + .02f), 2.9f, hl - .3f);
            sb.Box(2, ram + new Vector3(sx > 0 ? 0f : -.32f, -.03f, -.03f), ram + new Vector3(sx > 0 ? .32f : 0f, .03f, .03f));
            sb.Box(2, new Vector3(sx * (hw + .3f) - .06f, 2.35f, hl - .38f), new Vector3(sx * (hw + .3f) + .06f, 2.9f, hl - .28f));
        }
        sb.Box(7, new Vector3(-.95f, 2.72f, hl - .005f), new Vector3(.95f, 2.98f, hl + .008f));
        // ciemne otwory drzwi po prawej (widoczne przy odsuniętych skrzydłach)
        foreach (float dz in new[] { BusDrzwi1, BusDrzwi2 }) sb.Box(2, new Vector3(hw + .001f, yS + .05f, dz - BusDrzwiSzer * .5f), new Vector3(hw + .004f, yO - .05f, dz + BusDrzwiSzer * .5f));
        autobusMesh = sb.Zapisz("autobus midi");
        // skrzydło drzwi: rama + szyba (2 podsiatki)
        var sk = new SiatkaSalonu(); float lw = BusDrzwiSzer * .5f - .02f, lh = yO - .05f - (yS + .05f);
        sk.Box(0, new Vector3(0, 0, -lw * .5f), new Vector3(.03f, lh, lw * .5f));
        sk.Box(1, new Vector3(.031f, .12f, -lw * .5f + .06f), new Vector3(.034f, lh - .1f, lw * .5f - .06f));
        sk.Box(0, new Vector3(.031f, lh * .45f, -lw * .5f + .06f), new Vector3(.05f, lh * .45f + .04f, lw * .5f - .06f));   // poręcz
        skrzydloMesh = sk.Zapisz("autobus skrzydło drzwi");
        autobusMats = new[]
        {
            SalonMatOsobny("autobus lakier", new Color(.93f, .94f, .95f), .7f, .2f),
            SalonMatOsobny("autobus szyby", new Color(.06f, .08f, .1f), .92f, .5f),
            SalonMatOsobny("autobus czarne", new Color(.04f, .04f, .045f), .35f, .1f),
            SalonMatOsobny("autobus klimatyzator", new Color(.7f, .72f, .74f), .5f, .4f),
            SalonMatOsobny("autobus pas linii", pas, .6f, .2f),
            SalonMatOsobny("autobus reflektory", new Color(.95f, .95f, .9f), .8f, 0f, new Color(1f, 1f, .92f) * .8f),
            SalonMatOsobny("autobus lampy tylne", new Color(.5f, .03f, .03f), .7f, 0f, new Color(.8f, .05f, .03f) * .6f),
            SalonMatOsobny("autobus wyświetlacz", new Color(.2f, .12f, .02f), .5f, 0f, new Color(1f, .6f, .05f) * 1.2f),
        };
    }

    static BraxAutobus AutobusNowy(string nazwa, Transform rodzic, Color pas)
    {
        if (!autobusMesh) AutobusModel(pas);
        var root = new GameObject(nazwa); root.transform.SetParent(rodzic, false);
        var g = SalonObiekt("nadwozie", root.transform, autobusMesh, autobusMats, Vector3.zero);
        var mats = (Material[])autobusMats.Clone(); mats[4] = SalonMatOsobny("autobus pas " + nazwa, pas, .6f, .2f); g.GetComponent<MeshRenderer>().sharedMaterials = mats;
        var bus = root.AddComponent<BraxAutobus>();
        float hw = BusW * .5f, yS = .32f;
        var skrzydla = new List<Transform>();
        foreach (float dz in new[] { BusDrzwi1, BusDrzwi2 })
            foreach (int q in new[] { 1, -1 })
            {
                var s = SalonObiekt("skrzydło drzwi", root.transform, skrzydloMesh, new[] { autobusMats[2], autobusMats[1] }, Vector3.zero);
                s.transform.localPosition = new Vector3(hw + .006f, yS + .05f, dz + q * BusDrzwiSzer * .25f); skrzydla.Add(s.transform);
            }
        var kola = new List<Transform>();
        foreach (float z in new[] { BusOsP, BusOsT })
            foreach (float sx in new[] { -1f, 1f })
            {
                var k = Kolo(true, Vector3.zero, Vector3.right * sx, Vector3.up, "koło autobusu", root.transform, false);
                k.transform.localScale = Vector3.one * 1.55f; k.transform.localPosition = new Vector3(sx * (hw - .42f), .41f * 1.55f, z);
                k.transform.localRotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, Vector3.right * sx), Vector3.right * sx);
                kola.Add(k.transform);
            }
        var dp = new GameObject("punkt drzwi (chodnik)").transform; dp.SetParent(root.transform, false); dp.localPosition = new Vector3(hw + 1.1f, .14f, BusDrzwi1);
        var bc = root.AddComponent<BoxCollider>(); bc.center = new Vector3(0, BusH * .5f, 0); bc.size = new Vector3(BusW, BusH, BusL);
        var rb = root.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
        bus.skrzydla = skrzydla.ToArray(); bus.kola = kola.ToArray(); bus.drzwiPunkt = dp; bus.obrys = bc;
        bus.dl = BusL; bus.szer = BusW; bus.rKola = .41f * 1.55f; bus.vmax = 30f / 3.6f * K; bus.vZakret = 12f / 3.6f * K;
        return bus;
    }

    // wołane z Auta() po AutaWRuchu: ścieżki z tras ruchu, przystanki na ścieżce, kontrola „na jezdni”
    static void Autobusy()
    {
        autobusy.Clear();
        BraxRuchAut[] tab = Object.FindObjectsByType<BraxRuchAut>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var ra = tab.FirstOrDefault(); if (!ra || ra.trasy == null) { Debug.LogError("BRAXHORN_AUTOBUS DO POPRAWKI: brak tras ruchu"); return; }
        var rodzic = Group("Osiedle / autobusy");
        Vector3[] Sciezka(string nazwa, out bool petla)
        {
            petla = false; var t = ra.trasy.FirstOrDefault(q => q.nazwa == nazwa); if (t.ile < 2) return null;
            petla = t.petla; var pts = new Vector3[t.ile]; System.Array.Copy(ra.punkty, t.start, pts, 0, t.ile);
            if (petla && (pts[0] - pts[pts.Length - 1]).sqrMagnitude > .01f) { var l = pts.ToList(); l.Add(pts[0]); pts = l.ToArray(); }
            return pts;
        }
        var opis = new List<string>(); int poza = 0, probki = 0;
        foreach (var (nazwa, trasa, kolor) in new[] { ("Autobus linii A", "A pętla Poznańska–Bydgoska (zgodnie)", new Color(.1f, .3f, .7f)),
                                                      ("Autobus linii B (na wschód)", "B zachód → wschód", new Color(.75f, .1f, .1f)),
                                                      ("Autobus linii B (na zachód)", "B wschód → zachód", new Color(.75f, .1f, .1f)) })
        {
            var pts = Sciezka(trasa, out bool petla); if (pts == null) { opis.Add(nazwa + ": brak trasy „" + trasa + "”"); continue; }
            float[] odc = new float[pts.Length]; for (int i = 1; i < pts.Length; i++) odc[i] = odc[i - 1] + Vector3.Distance(pts[i - 1], pts[i]);
            var bus = AutobusNowy(nazwa, rodzic, kolor); bus.sciezka = pts; bus.petla = petla;
            // przystanki: wiata do 9 m od ścieżki, kierunek ruchu zgodny; postój tak, by przednie drzwi były przy wiacie
            var lista = new List<BraxAutobus.Przystanek>();
            foreach (var p in przystankiLinii)
            {
                float best = 9f, sb = -1f;
                for (int i = 1; i < pts.Length; i++)
                {
                    var a = pts[i - 1]; var b = pts[i]; var d = b - a; float dl = d.magnitude; if (dl < 1e-3f) continue; d /= dl;
                    if (Vector3.Dot(d, p.ruch) < .9f) continue;
                    float t = Mathf.Clamp(Vector3.Dot(p.c - a, d), 0f, dl); float od = Vector3.Distance(new Vector3(p.c.x, a.y, p.c.z), a + d * t);
                    if (od < best) { best = od; sb = odc[i - 1] + t; }
                }
                if (sb < 0f) continue;
                lista.Add(new BraxAutobus.Przystanek { nazwa = p.nazwa, s = Mathf.Max(0f, sb - BusDrzwi1), wiata = p.c + Vector3.up * .14f });
            }
            bus.przystanki = lista.OrderBy(q => q.s).ToArray(); bus.s = 0f;
            var f0 = pts[1] - pts[0]; f0.y = 0f; bus.transform.position = pts[0]; if (f0.sqrMagnitude > 1e-6f) bus.transform.rotation = Quaternion.LookRotation(f0.normalized);
            // kontrola: środek autobusu i narożniki przodu na jezdni (próbki co 2 m, limit 2000)
            for (float s = 0; s < odc[odc.Length - 1] && probki < 2000; s += 2f, probki++)
            {
                int i = System.Array.FindIndex(odc, q => q >= s); if (i < 1) i = 1;
                var p = Vector3.Lerp(pts[i - 1], pts[i], Mathf.InverseLerp(odc[i - 1], odc[i], s));
                if (!NaJezdniAut(p)) poza++;
            }
            autobusy.Add(bus);
            opis.Add(nazwa + ": trasa „" + trasa + "” " + odc[odc.Length - 1].ToString("F0") + " m" + (petla ? " (pętla)" : " (otwarta)") + ", przystanki: " + (lista.Count > 0 ? string.Join(", ", lista.Select(q => q.nazwa)) : "BRAK — DO POPRAWKI"));
        }
        Debug.Log("BRAXHORN_AUTOBUS autobusy=" + autobusy.Count + " model=" + BusL.ToString("F2") + "×" + BusW.ToString("F2") + "×" + BusH.ToString("F2") + " m (midi 8 × 2.4 × 2.9 m × K) drzwi=2 (dwuskrzydłowe) koła=4 [" + string.Join("; ", opis) + "] — linii A↔B brak: droga przez parki (E17) niezbudowana");
        Debug.Log("BRAXHORN_AUTOBUS_TRASA próbki=" + probki + " poza_jezdnią=" + poza);
    }

    // po kroku ludzi: czekający przy każdym przystanku linii (2) i wysiadający (1, ukryty do przyjazdu)
    static void AutobusPasazerowie()
    {
        if (autobusy.Count == 0 || rbPostacie == null || rbPostacie.Count == 0) { Debug.Log("BRAXHORN_AUTOBUS pasażerowie=0 (brak autobusów albo paczki ludzi)"); return; }
        var rnd = new System.Random(7171); int n = 0; var rodz = Group("Osiedle / autobusy — pasażerowie");
        var juz = new Dictionary<string, (Transform[] cz, Transform[] wy)>();
        foreach (var bus in autobusy)
            foreach (var p in bus.przystanki)
            {
                if (juz.TryGetValue(p.nazwa, out var gotowi)) { p.czekajacy = gotowi.cz; p.wysiadajacy = gotowi.wy; continue; }
                var q = przystankiLinii.First(x => x.nazwa == p.nazwa);
                var doUlicy = -Vector3.Cross(Vector3.up, q.ruch).normalized;   // od wiaty w stronę jezdni (prawa strona ruchu)
                var cz = new List<Transform>(); var wy = new List<Transform>();
                for (int k = 0; k < 2; k++)
                {
                    var pos = q.c + doUlicy * 1.2f + q.ruch * (k == 0 ? -.7f : .7f); pos.y = .14f;
                    var m = NowyMieszkaniec(RolaPrzechodzien, rodz, "przystanek " + p.nazwa + " — czeka", pos, Quaternion.LookRotation(-q.ruch).eulerAngles.y, rnd, KlipyPostoj);
                    if (m) { cz.Add(m.transform); n++; }
                }
                var mw = NowyMieszkaniec(RolaPrzechodzien, rodz, "przystanek " + p.nazwa + " — wysiada", q.c + doUlicy * 1.5f + Vector3.up * .14f, Quaternion.LookRotation(-doUlicy).eulerAngles.y, rnd, KlipyPostoj);
                if (mw) { wy.Add(mw.transform); n++; }
                p.czekajacy = cz.ToArray(); p.wysiadajacy = wy.ToArray(); juz[p.nazwa] = (p.czekajacy, p.wysiadajacy);
            }
        Debug.Log("BRAXHORN_AUTOBUS pasażerowie=" + n + " (2 czekających + 1 wysiadający na przystanek; wsiadanie = znikanie przy drzwiach, wysiadanie = pojawienie się przy drzwiach)");
    }
}
