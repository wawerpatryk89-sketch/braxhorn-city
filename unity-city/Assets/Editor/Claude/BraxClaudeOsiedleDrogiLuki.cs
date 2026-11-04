using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// E3b (runda 3) — ŁUKI KRAWĘŻNIKÓW NA SKRZYŻOWANIACH A (było: narożnik pod kątem prostym — auta „pierdolą się w zakrętach”).
// Promień na wewnętrznej krawędzi (krawędź jezdni) dobierany dla każdego narożnika: cel 6 m × K, mniej, gdy brakuje miejsca:
//  • prosty krawężnik do sąsiedniego skrzyżowania dzielony po połowie między oba narożniki, z miejscem na zebrę (zebra stoi
//    za końcem łuku, na prostym krawężniku) i 1 m prostego przed końcem bulwaru (wjazd na parking przed budynkiem);
//  • łuk chodnika nie wchodzi na działkę ani w budynek (obrysy z kodu) ani w kolizję (płot, mur, słup);
//  • najmniejszy promień = pas krawężnika + zieleń + chodnik + 0.8 m (chodnik po łuku musi się zmieścić) — inaczej narożnik
//    prosty jak dotąd (z logiem).
// Łuk: asfalt narożnika (wachlarz od rogu jezdni do cięciw łuku), krawężnik po łuku z kamieni ~1 m × K z fugą 1 cm, zieleń
// i chodnik po łuku (szerokości płynnie z jednej ulicy na drugą), kolizje (obrócone prostopadłościany na każdym kamieniu i płycie).
// Za łukiem: zebry (od końca łuku + 0.3 m), linia zatrzymania i słupy świateł, tablica ulicy (w łuku chodnika), sieć pieszych
// (cięciwy łuku jako odcinki chodnika), trasy aut (skręt w prawo koncentrycznie z łukiem, linia zatrzymania przed zebrą).
// Log: BRAXHORN_DROGI_LUKI (każdy narożnik: promień albo powód), BRAXHORN_TOR_SKRETU (kolizje toru aut po zbudowaniu wszystkiego).
public static partial class BraxClaudeOsiedle
{
    static readonly Dictionary<(WezelA w, int sx, int sz), float> lukNaroznika = new Dictionary<(WezelA, int, int), float>();
    static int lukiPelne, lukiCzesc, lukiBrak, lukiKamienie;

    static float LukNaroznika(WezelA w, int sx, int sz) => w != null && lukNaroznika.TryGetValue((w, sx, sz), out var r) ? r : 0f;
    // największy promień łuków przy ramieniu r (dwa narożniki po obu stronach ramienia)
    static float LukRamienia(WezelA w, Ramie r)
    {
        if (Mathf.Abs(r.dir.x) > .5f) { int sx = r.dir.x > 0 ? 1 : -1; return Mathf.Max(LukNaroznika(w, sx, -1), LukNaroznika(w, sx, 1)); }
        int sz = r.dir.z > 0 ? 1 : -1; return Mathf.Max(LukNaroznika(w, -1, sz), LukNaroznika(w, 1, sz));
    }
    // zebra na ramieniu: dawniej w pasie chodnika ulicy poprzecznej (a0 = jezdnia + krawężnik + 0.25, a1 = półszerokość);
    // przy łuku — za końcem łuku (na prostym krawężniku), ta sama szerokość pasów
    static (float a0, float a1) ZebraRamienia(WezelA w, Ramie r)
    {
        float a0 = r.o.cw + r.o.kerb + .25f, a1 = r.o.HalfS(r.s), R = LukRamienia(w, r);
        if (R > 0f) { float szer = a1 - a0; a0 = r.o.cw + R + .3f; a1 = a0 + szer; }
        return (a0, a1);
    }
    static bool ZebraNaRamieniuJest(WezelA w, Ramie r) => w.rodzaj == RodzajWezla.Swiatla || w.Pelne || r.Trzon(w);

    // ---------------- dobór promieni (przed budową ulic) ----------------
    static void LukiNaroznikow()
    {
        lukNaroznika.Clear(); lukiPelne = lukiCzesc = lukiBrak = lukiKamienie = 0;
        Physics.SyncTransforms();
        float cel = 6f * K;
        var prost = PrzeszkodyDrog();
        var opis = new List<string>();
        // ramię wzdłuż ulicy u od węzła w w kierunku sgn: dostępna długość prostego krawężnika (połowa do sąsiedniego narożnika)
        float Dostepne(WezelA w, UlicaA u, UlicaA o, int sgn, int strona, out string co)
        {
            float pos = u.alongX ? w.x : w.z;
            // najbliższy węzeł na tej ulicy w tym kierunku, który ma ulicę poprzeczną PO TEJ STRONIE (węzeł T z drugiej strony
            // nie przerywa krawężnika — ten biegnie dalej) albo jest rondem
            WezelA sas = null; float best = float.MaxValue;
            foreach (var w2 in wezly)
            {
                if (w2 == w) continue;
                bool naTej = u.alongX ? Mathf.Abs(w2.z - w.z) < .1f : Mathf.Abs(w2.x - w.x) < .1f;
                if (!naTej) continue;
                bool poTejStronie = w2.rondo || (u.alongX ? (strona > 0 ? w2.N : w2.S) : (strona > 0 ? w2.E : w2.W));
                if (!poTejStronie) continue;
                float p2 = u.alongX ? w2.x : w2.z, d = (p2 - pos) * sgn;
                if (d > .1f && d < best) { best = d; sas = w2; }
            }
            if (sas == null) { co = "koniec ulicy"; return ((sgn > 0 ? u.a1 : u.a0) - pos) * sgn - o.cw - 1f; }
            var o2 = u.alongX ? sas.uz : sas.ux;
            if (sas.rondo) { co = "rondo"; return best - o.cw - RondoChodnik - .5f; }
            // czy sąsiad ma narożnik po tej stronie zwrócony do nas (ramię w naszą stronę i ramię poprzeczne po tej stronie)
            bool armK = u.alongX ? (sgn > 0 ? sas.W : sas.E) : (sgn > 0 ? sas.S : sas.N);
            bool armP = u.alongX ? (strona > 0 ? sas.N : sas.S) : (strona > 0 ? sas.E : sas.W);
            float gap = best - o.cw - o2.cw;
            co = "sąsiednie skrzyżowanie (" + sas.x.ToString("F1") + ", " + sas.z.ToString("F1") + ")";
            return armK && armP ? gap * .5f : gap - .5f;
        }
        foreach (var w in wezly)
        {
            if (w.rondo) continue;
            var ux = w.ux; var uz = w.uz;
            foreach (int sx in new[] { -1, 1 })
                foreach (int sz in new[] { -1, 1 })
                {
                    bool armX = sx > 0 ? w.E : w.W, armZ = sz > 0 ? w.N : w.S;
                    if (!armX || !armZ) continue;
                    float wX = ux.WalkS(sz), wZ = uz.WalkS(sx);
                    if (wX <= 0 && wZ <= 0) continue;
                    float hX = wX > 0 ? ux.GreenS(sz) + wX : 0f, hZ = wZ > 0 ? uz.GreenS(sx) + wZ : 0f;
                    float rMin = ux.kerb + Mathf.Max(hX, hZ) + .8f;
                    string nazwa = "(" + w.x.ToString("F1") + ", " + w.z.ToString("F1") + ") " + (sz > 0 ? "N" : "S") + (sx > 0 ? "E" : "W");
                    // długości prostych: wzdłuż ux (kierunek sx) i wzdłuż uz (kierunek sz); zebra na ramieniu → miejsce na nią
                    var ramX = new Ramie { dir = new Vector3(sx, 0, 0), u = ux, o = uz, s = sx }; var ramZ = new Ramie { dir = new Vector3(0, 0, sz), u = uz, o = ux, s = sz };
                    float lX = Dostepne(w, ux, uz, sx, sz, out var coX), lZ = Dostepne(w, uz, ux, sz, sx, out var coZ);
                    if (ZebraNaRamieniuJest(w, ramX)) lX -= (uz.HalfS(sx) - uz.cw - uz.kerb - .25f) + .6f;
                    if (ZebraNaRamieniuJest(w, ramZ)) lZ -= (ux.HalfS(sz) - ux.cw - ux.kerb - .25f) + .6f;
                    float rMax = Mathf.Min(cel, Mathf.Min(lX, lZ));
                    string pow = lX < lZ ? coX : coZ;
                    float R = 0f; string kolizja = null;
                    var O = new Vector3(w.x + sx * uz.cw, 0f, w.z + sz * ux.cw);
                    for (float r = rMax; r >= rMin - .001f; r -= .25f)
                    {
                        var C = O + new Vector3(sx * r, 0f, sz * r);
                        // próbki: zewnętrzna krawędź chodnika po łuku i środek pasa chodnika
                        bool ok = true;
                        for (int k = 0; k <= 8 && ok; k++)
                        {
                            float t = k / 8f; var d = KierunekLuku(sx, sz, t);
                            float h = Mathf.Lerp(hX, hZ, t);
                            foreach (float rr in new[] { r - ux.kerb - h, r - ux.kerb - h * .5f })
                            {
                                var p = C + d * rr;
                                foreach (var q in prost) if (p.x > q.x0 - .1f && p.x < q.x1 + .1f && p.z > q.z0 - .1f && p.z < q.z1 + .1f) { ok = false; kolizja = q.co; break; }
                                if (!ok) break;
                                // Stare wyposażenie ulic jest usuwane dopiero po
                                // wyborze łuków; nie ogranicza nowego narożnika.
                                // Działki i budynki sprawdzono powyżej.
                            }
                        }
                        if (ok) { R = r; break; }
                    }
                    if (R > 0f)
                    {
                        lukNaroznika[(w, sx, sz)] = R;
                        if (R >= cel - .01f) lukiPelne++; else lukiCzesc++;
                        opis.Add(nazwa + " R=" + R.ToString("F2") + (R < cel - .01f ? " (ogranicza: " + (rMax < cel - .01f ? pow : kolizja ?? "—") + ")" : ""));
                    }
                    else { lukiBrak++; opis.Add(nazwa + " prosty (R max " + rMax.ToString("F2") + " < min " + rMin.ToString("F2") + " albo " + (kolizja ?? pow) + ")"); }
                }
        }
        Debug.Log("BRAXHORN_DROGI_LUKI cel=" + cel.ToString("F2") + " m łuki_pełne=" + lukiPelne + " łuki_mniejsze=" + lukiCzesc + " narożniki_proste=" + lukiBrak + " [" + string.Join("; ", opis) + "]");
    }

    // kierunek od środka łuku C do punktu łuku dla t ∈ [0, 1]: t = 0 — styk z krawężnikiem ulicy wzdłuż x, t = 1 — wzdłuż z
    static Vector3 KierunekLuku(int sx, int sz, float t)
    {
        float a0 = Mathf.Atan2(-sz, 0f), a1 = Mathf.Atan2(0f, -sx);
        float da = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
        float a = a0 + da * t;
        return new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
    }

    // ---------------- geometria łuku narożnika ----------------
    static void NaroznikLuk(WezelA w, int sx, int sz, float R)
    {
        var ux = w.ux; var uz = w.uz;
        var O = new Vector3(w.x + sx * uz.cw, 0f, w.z + sz * ux.cw);
        var C = O + new Vector3(sx * R, 0f, sz * R);
        float kerb = ux.kerb;
        float wX = ux.WalkS(sz), wZ = uz.WalkS(sx);
        float gX = wX > 0 ? ux.GreenS(sz) : 0f, gZ = wZ > 0 ? uz.GreenS(sx) : 0f;
        float dl = R * Mathf.PI * .5f;
        int n = Mathf.Max(6, Mathf.CeilToInt(dl / K));          // kamienie krawężnika ~1 m × K
        float fug = .01f / Mathf.Max(R, .5f);                    // kąt fugi 1 cm
        var fuga = Plain("fuga krawężnika łuku", new Color(.40f, .40f, .39f), .1f);
        Vector3 P(float t, float r, float y) => C + KierunekLuku(sx, sz, t) * r + Vector3.up * y;
        for (int k = 0; k < n; k++)
        {
            float t0 = k / (float)n, t1 = (k + 1) / (float)n;
            // asfalt narożnika: trójkąt od rogu jezdni do cięciwy łuku (bez zakładki z krawężnikiem — ta sama cięciwa)
            Tri3(Asphalt, O + Vector3.up * YAsf, P(t0, R, YAsf), P(t1, R, YAsf), Vector3.up);
            // kamień krawężnika (z fugą na końcu kamienia) i fuga
            float tf = Mathf.Clamp01(t1 - fug / (Mathf.PI * .5f) * (k < n - 1 ? 1f : 0f));
            KamienLuku(KerbStone, P, t0, tf, R - kerb, R, YAsf, .16f, gX + wX > 0 || gZ + wZ > 0);
            if (k < n - 1) KamienLuku(fuga, P, tf, t1, R - kerb + .005f, R - .005f, YAsf, .155f, false);
            lukiKamienie++;
            // zieleń (trawa podłoża — bez geometrii) i chodnik po łuku: szerokości płynnie od ulicy wzdłuż x do ulicy wzdłuż z
            float ga = Mathf.Lerp(gX, gZ, t0), gb = Mathf.Lerp(gX, gZ, t1), wa = Mathf.Lerp(wX, wZ, t0), wb = Mathf.Lerp(wX, wZ, t1);
            if (wa + wb > .02f)
            {
                var a0 = P(t0, R - kerb - ga, .14f); var b0 = P(t1, R - kerb - gb, .14f);
                var a1 = P(t0, R - kerb - ga - wa, .14f); var b1 = P(t1, R - kerb - gb - wb, .14f);
                Quad(Pavers, a0, b0, b1, a1, Vector3.up);
                // lico od strony działki (zewnętrzne) i od zieleni (gdy jest zieleń)
                Quad(Pavers, a1, b1, b1 - Vector3.up * .14f, a1 - Vector3.up * .14f, -KierunekLuku(sx, sz, (t0 + t1) * .5f));
                if (ga + gb > .02f) Quad(Pavers, a0 - Vector3.up * .14f, b0 - Vector3.up * .14f, b0, a0, KierunekLuku(sx, sz, (t0 + t1) * .5f));
                var mid = (a0 + b0 + a1 + b1) * .25f; float szer = ((wa + wb) * .5f); float dlu = (P(t1, R - kerb - (ga + gb) * .5f - szer * .5f, 0) - P(t0, R - kerb - (ga + gb) * .5f - szer * .5f, 0)).magnitude;
                var dm = KierunekLuku(sx, sz, (t0 + t1) * .5f); float yaw = -Mathf.Atan2(dm.z, dm.x) * Mathf.Rad2Deg;
                ColliderRot(new Vector3(mid.x, .07f, mid.z), new Vector3(Mathf.Max(.05f, szer), .14f, dlu + .02f), yaw);
            }
        }
        // płaskie obrysy dla roślin i kontroli (cały kwadrat narożnika: asfalt, krawężnik, chodnik)
        float x0 = Mathf.Min(O.x, C.x), x1 = Mathf.Max(O.x, C.x), z0 = Mathf.Min(O.z, C.z), z1 = Mathf.Max(O.z, C.z);
        Hard(x0, x1, z0, z1);
        // chodnik po łuku w sieci pieszych: cięciwy (≥ 3 m) po osi chodnika
        float hm = (Mathf.Max(gX, gZ) + Mathf.Max(wX, wZ) * .5f);
        int nc = Mathf.Max(1, Mathf.FloorToInt((R - kerb - hm) * Mathf.PI * .5f / 3.2f));
        for (int k = 0; k < nc; k++)
        {
            float t0 = k / (float)nc, t1 = (k + 1) / (float)nc;
            float r0 = R - kerb - Mathf.Lerp(gX, gZ, t0) - Mathf.Lerp(wX, wZ, t0) * .5f, r1 = R - kerb - Mathf.Lerp(gX, gZ, t1) - Mathf.Lerp(wX, wZ, t1) * .5f;
            chodnikiA.Add((P(t0, r0, .14f), P(t1, r1, .14f)));
        }
    }
    // kamień po łuku (t0..t1, promienie rIn..rOut od środka łuku): wierzch, lico od jezdni (rOut), lico od chodnika (rIn), końce, kolizja
    static void KamienLuku(Material m, Func<float, float, float, Vector3> P, float t0, float t1, float rIn, float rOut, float yDol, float yGora, bool kolizja)
    {
        var a0 = P(t0, rOut, yGora); var b0 = P(t1, rOut, yGora); var a1 = P(t0, rIn, yGora); var b1 = P(t1, rIn, yGora);
        var nOut = (P((t0 + t1) * .5f, rOut, 0) - P((t0 + t1) * .5f, rIn, 0)).normalized;
        Quad(m, a0, b0, b1, a1, Vector3.up);
        Quad(m, a0, b0, P(t1, rOut, yDol), P(t0, rOut, yDol), nOut);
        Quad(m, a1, b1, P(t1, rIn, 0f), P(t0, rIn, 0f), -nOut);
        var tang = (P(t1, (rIn + rOut) * .5f, 0) - P(t0, (rIn + rOut) * .5f, 0)).normalized;
        Quad(m, a0, a1, P(t0, rIn, 0f), P(t0, rOut, yDol), -tang); Quad(m, b0, b1, P(t1, rIn, 0f), P(t1, rOut, yDol), tang);
        if (!kolizja) return;
        var mid = (P(t0, (rIn + rOut) * .5f, 0) + P(t1, (rIn + rOut) * .5f, 0)) * .5f;
        float yaw = -Mathf.Atan2(nOut.z, nOut.x) * Mathf.Rad2Deg;
        ColliderRot(new Vector3(mid.x, yGora * .5f, mid.z), new Vector3(rOut - rIn, yGora, (P(t1, rOut, 0) - P(t0, rOut, 0)).magnitude + .01f), yaw);
    }
    // tablica ulicy w łuku chodnika (tylna część chodnika na dwusiecznej narożnika)
    static Vector3? TablicaWLuku(WezelA w, int sx, int sz)
    {
        float R = LukNaroznika(w, sx, sz); if (R <= 0f) return null;
        var ux = w.ux; var uz = w.uz;
        var O = new Vector3(w.x + sx * uz.cw, 0f, w.z + sz * ux.cw); var C = O + new Vector3(sx * R, 0f, sz * R);
        float g = (Mathf.Max(0f, ux.WalkS(sz) > 0 ? ux.GreenS(sz) : 0f) + (uz.WalkS(sx) > 0 ? uz.GreenS(sx) : 0f)) * .5f, wk = (ux.WalkS(sz) + uz.WalkS(sx)) * .5f;
        return C + KierunekLuku(sx, sz, .5f) * (R - ux.kerb - g - wk + .35f);
    }

    // ---------------- kontrola toru skrętu (po zbudowaniu wszystkiego, przed autami) ----------------
    // Obrys auta osobowego × K wzdłuż każdej trasy (co ~1 m, od 8 cm nad asfaltem — krawężnik 16 cm i chodnik 14 cm się liczą):
    // kolizja = krawężnik, chodnik, słupek, latarnia, znak, wyspa ronda w torze jazdy.
    static void KontrolaToruSkretu(BraxTrasyAut.Wynik wy, float szer, float dl)
    {
        Physics.SyncTransforms();
        int punkty = 0, kolizje = 0; var przyklady = new Dictionary<string, int>();
        const int maska = ~((1 << 8) | (1 << 29) | (1 << 2));
        var buf = new Collider[64];
        foreach (var t in wy.trasy)
        {
            for (int i = t.start; i + 2 < t.start + t.ile; i += 2)
            {
                var p = wy.punkty[i]; var q = wy.punkty[i + 2]; var f = q - p; f.y = 0f; if (f.sqrMagnitude < 1e-4f) continue;
                punkty++;
                var c = (p + q) * .5f + Vector3.up * (.08f + .7f);
                // Pełny samochód: dawny limit 1,2 m badał tylko środkowe 2,4 m
                // i pomijał zderzaki oraz narożniki auta długości ponad 6 m.
                int nh = Physics.OverlapBoxNonAlloc(c, new Vector3(szer * .5f, .7f, dl * .5f), buf, Quaternion.LookRotation(f.normalized), maska, QueryTriggerInteraction.Ignore);
                for (int k = 0; k < nh; k++)
                {
                    var col = buf[k]; if (!col || col.bounds.size.x > 60f || col.bounds.size.z > 60f) continue;   // podłogi
                    if (col.GetComponentInParent<BraxAuto>()) continue;
                    kolizje++;
                    string key = t.nazwa + " @ " + BraxKontrola.F(c) + " — " + col.name + " (" + col.bounds.size.y.ToString("F2") + " m)";
                    if (przyklady.Count < 12) przyklady[key] = 1;
                    break;
                }
            }
        }
        Debug.Log("BRAXHORN_TOR_SKRETU punkty=" + punkty + " kolizje=" + kolizje + " auto " + szer.ToString("F2") + " × " + dl.ToString("F2") + " m (od 8 cm nad asfaltem)" + (przyklady.Count > 0 ? " [" + string.Join("; ", przyklady.Keys) + "]" : ""));
    }
}
