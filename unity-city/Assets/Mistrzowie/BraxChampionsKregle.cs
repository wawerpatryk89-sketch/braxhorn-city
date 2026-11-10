using UnityEngine;
#if UNITY_6000_0_OR_NEWER
using FizMat = UnityEngine.PhysicsMaterial;
using FizLacz = UnityEngine.PhysicsMaterialCombine;
#else
using FizMat = UnityEngine.PhysicMaterial;
using FizLacz = UnityEngine.PhysicMaterialCombine;
#endif

// Owner 10.11 20:45-20:50 ("Jak robisz kręgle, to rób konkretnie, funkcjonalne ... nie na wystawę"): the 24 lanes of E1
// Centrum Rozrywki are PLAYED. One component for the whole bowling centre (BraxChampionsE1 writes its configuration):
//   - AKCJA at a ball return = take a ball (the lane on your side of the return); the ball rides at your hip,
//   - AKCJA on the approach = roll: the ball leaves at your feet (behind the foul line) toward where you face (at most
//     6 deg off the lane), 8 m/s real; slides, rolls, hooks nothing - physics does the rest: 10 pins with real mass
//     (1.5 kg, low centre of gravity) against a 7.3 kg ball,
//   - off the lane bed before the pins = GUTTER: the ball drops into the channel and runs to the pit,
//   - the pinsetter: after the first ball the fallen pins are swept, the standing ones lifted and set back on their spots;
//     after a frame (or a strike) a full new rack comes down; the ball comes back on the return,
//   - real ten-pin scoring (10 frames, strike = 10 + next two balls, spare = 10 + next ball, 10th frame up to 3 balls)
//     painted on the lane's own overhead screen, the panel at the top of the screen, STRIKE! / SPARE! / RYNNA flashes.
// Only the lane being played has live physics; all the other pins are kinematic and asleep. Nothing runs when the
// player is further than 70 m from the bowling centre.
public sealed class BraxChampionsKregle : MonoBehaviour, IBraxInterakcja
{
    // ---- configuration (BraxChampionsE1, world space; lanes run along +z)
    public Vector3[] faul = new Vector3[0];          // foul-line centre of every lane, y = lane surface
    public Vector3[] zwrotnica = new Vector3[0];     // centre of the ball-return hood of every lane (both lanes of a pair share it)
    public Vector3[] kulaNaZwrotnicy = new Vector3[0];   // where the game ball waits on that return
    public Transform[] kregle = new Transform[0];    // 10 per lane: lane * 10 + pin
    public Rigidbody kula;
    public Renderer[] ekrany = new Renderer[0];      // overhead score screen of every lane (uv 0..1)
    public Renderer[] konsole = new Renderer[0];     // the scorer console's screen of every lane, by the settee (same scorecard)
    public float rozbieg = 5.9f, glowa = 23.7f, koniecPola = 25.1f, polowaToru = .69f, rynna = .30f, promien = .141f;
    public float glebRynny = .078f;                   // the gutter channel lies this far under the lane surface
    public float predkosc = 10.4f, maxKat = 6f;
    public float srodekCiezkosci = .175f;            // a real pin's centre of gravity sits ~14 cm over its base (world)

    const int Pinow = 10;
    enum Faza { Wolne, Trzyma, Toczy, Rynna, Liczy, Maszyna, Powrot }
    Faza faza = Faza.Wolne;
    int tor = -1; float t0, czekaj; float stoi0;
    Vector3[] miejsca; Quaternion[] obroty; Rigidbody[] rb; Collider[] kol; Renderer[][] widok;
    bool[] stoiPrzed = new bool[Pinow]; bool pelny = true, gotowe, aktywne; float sprawdz;
    Collider kulaKol; Renderer kulaWidok; Vector3 rynnaPoz; float rynnaV;
    BraxGymPlayer gracz;

    sealed class Gra { public int[] rzuty = new int[21]; public int n; public Texture2D tex; public Color32[] px; }
    Gra[] gry;

    void OnEnable() { BraxInterakcje.Dodaj(this); }
    void OnDisable() { BraxInterakcje.Usun(this); }

    void Start()
    {
        int n = kregle.Length;
        miejsca = new Vector3[n]; obroty = new Quaternion[n]; rb = new Rigidbody[n]; kol = new Collider[n]; widok = new Renderer[n][];
        var pinMat = new FizMat("kręgiel") { bounciness = .35f, dynamicFriction = .32f, staticFriction = .40f, bounceCombine = FizLacz.Average, frictionCombine = FizLacz.Average };
        for (int i = 0; i < n; i++)
        {
            var k = kregle[i]; if (!k) continue;
            miejsca[i] = k.position; obroty[i] = k.rotation;
            rb[i] = k.GetComponent<Rigidbody>(); kol[i] = k.GetComponent<Collider>(); widok[i] = k.GetComponentsInChildren<Renderer>(true);
            if (kol[i]) kol[i].sharedMaterial = pinMat;
            if (rb[i]) { rb[i].isKinematic = true; rb[i].maxAngularVelocity = 60f; rb[i].centerOfMass = new Vector3(0f, srodekCiezkosci, 0f); }
        }
        gry = new Gra[faul.Length];
        for (int l = 0; l < faul.Length; l++) { gry[l] = new Gra(); Rysuj(l); }   // every screen shows its empty scorecard
        if (kula)
        {
            kulaKol = kula.GetComponent<Collider>(); kulaWidok = kula.GetComponentInChildren<Renderer>(true);
            if (kulaKol) kulaKol.sharedMaterial = new FizMat("kula do kręgli") { bounciness = .05f, dynamicFriction = .06f, staticFriction = .08f, frictionCombine = FizLacz.Minimum, bounceCombine = FizLacz.Minimum };
            kula.maxAngularVelocity = 120f; kula.isKinematic = true;
            Pokaz(kula, false);
        }
        gotowe = true;
    }

    // ---------------- interakcja ----------------
    int Zwrotnica(Vector3 p, out int lane)
    {
        lane = -1; int best = -1; float bd = 1.7f;
        for (int i = 0; i < zwrotnica.Length; i++)
        {
            var d = zwrotnica[i] - p; d.y = 0f; float m = d.magnitude;
            if (m < bd && Mathf.Abs(p.y - zwrotnica[i].y) < 2.5f) { bd = m; best = i; }
        }
        if (best < 0) return -1;
        // the pair's return serves both lanes: the lane on the player's side
        lane = best;
        int partner = best % 2 == 0 ? best + 1 : best - 1;
        if (partner >= 0 && partner < faul.Length && Mathf.Abs(p.x - faul[partner].x) < Mathf.Abs(p.x - faul[best].x)) lane = partner;
        return best;
    }
    bool NaRozbiegu(Vector3 p, int t)
    {
        if (t < 0) return false;
        var d = p - faul[t];
        return Mathf.Abs(d.x) < polowaToru + rynna && d.z < .05f && d.z > -rozbieg - .6f && Mathf.Abs(d.y) < 2.5f;
    }

    public string Kontekst(Vector3 p)
    {
        if (!gotowe || faul.Length == 0) return "";
        switch (faza)
        {
            case Faza.Wolne:
                if (Zwrotnica(p, out int lane) < 0) return "";
                return "KRĘGLE · WEŹ KULĘ · TOR " + (lane + 1) + (lane != tor || Koniec(lane) ? " · NOWA GRA" : "");
            case Faza.Trzyma:
                if (NaRozbiegu(p, tor)) return "KRĘGLE · RZUĆ KULĘ";
                if (Zwrotnica(p, out _) >= 0) return "KRĘGLE · ODŁÓŻ KULĘ";
                return "";
            default: return "";
        }
    }

    public bool Wykonaj(Vector3 p)
    {
        if (faza == Faza.Wolne)
        {
            if (Zwrotnica(p, out int lane) < 0) return false;
            if (lane != tor || Koniec(lane)) NowaGra(lane);
            faza = Faza.Trzyma; Pokaz(kula, true); kula.isKinematic = true;
            return true;
        }
        if (faza == Faza.Trzyma)
        {
            if (NaRozbiegu(p, tor)) { Rzut(p); return true; }
            if (Zwrotnica(p, out _) >= 0) { NaZwrotnice(); faza = Faza.Wolne; return true; }
        }
        return false;
    }

    void NowaGra(int lane)
    {
        if (tor >= 0 && tor != lane) Odstaw(tor);
        tor = lane;
        var g = gry[lane] ?? (gry[lane] = new Gra());
        g.n = 0; for (int i = 0; i < g.rzuty.Length; i++) g.rzuty[i] = 0;
        UstawWszystkie(lane, true);
        pelny = true;
        for (int i = 0; i < Pinow; i++) stoiPrzed[i] = true;
        Rysuj(lane);
        BraxChampionsGry.Pokaz("TOR " + (lane + 1) + " · NOWA GRA", 1.6f);
    }
    // a lane left in the middle of a game keeps its score; its pins go back on their spots, asleep
    void Odstaw(int lane) { UstawWszystkie(lane, true); }

    void UstawWszystkie(int lane, bool widac)
    {
        for (int i = 0; i < Pinow; i++)
        {
            int k = lane * Pinow + i; if (k >= kregle.Length || !kregle[k]) continue;
            var b = rb[k]; if (b) { if (!b.isKinematic) { Predkosc(b, Vector3.zero); b.angularVelocity = Vector3.zero; } b.isKinematic = true; }
            kregle[k].SetPositionAndRotation(miejsca[k], obroty[k]); if (b) { b.position = miejsca[k]; b.rotation = obroty[k]; }
            Widac(k, widac);
        }
    }
    void Widac(int k, bool on) { if (kol[k]) kol[k].enabled = on; foreach (var r in widok[k]) if (r) r.enabled = on; }
    static void Pokaz(Rigidbody b, bool on) { if (!b) return; foreach (var r in b.GetComponentsInChildren<Renderer>(true)) r.enabled = on; var c = b.GetComponent<Collider>(); if (c) c.enabled = on; }

    void NaZwrotnice()
    {
        if (!kula) return;
        if (!kula.isKinematic) { Predkosc(kula, Vector3.zero); kula.angularVelocity = Vector3.zero; }
        kula.isKinematic = true;
        var at = tor >= 0 && tor < kulaNaZwrotnicy.Length ? kulaNaZwrotnicy[tor] : transform.position;
        kula.position = at; kula.transform.position = at; kula.transform.rotation = Quaternion.identity;
        Pokaz(kula, true);
    }

    void Rzut(Vector3 p)
    {
        gracz = BraxChampionsGry.Gracz;
        var f = faul[tor];
        var przod = gracz ? BraxChampionsGry.Przod(gracz) : Vector3.forward;
        float kat = Vector3.SignedAngle(Vector3.forward, przod, Vector3.up);
        if (Mathf.Abs(kat) > 90f) kat = 0f;
        kat = Mathf.Clamp(kat, -maxKat, maxKat);
        var dir = Quaternion.Euler(0, kat, 0) * Vector3.forward;
        float x = Mathf.Clamp(p.x - f.x, -(polowaToru - promien - .02f), polowaToru - promien - .02f);
        float z = Mathf.Clamp(p.z - f.z, -rozbieg + .3f, -.25f);
        var start = new Vector3(f.x + x, f.y + promien + .003f, f.z + z);
        for (int i = 0; i < Pinow; i++)
        {
            int k = tor * Pinow + i; stoiPrzed[i] = kol[k] && kol[k].enabled;
            if (rb[k] && stoiPrzed[i]) { rb[k].isKinematic = false; rb[k].WakeUp(); }
        }
        kula.isKinematic = false; Pokaz(kula, true);
        kula.position = start; kula.transform.position = start;
        Predkosc(kula, dir * predkosc);
        kula.angularVelocity = Vector3.Cross(Vector3.up, dir) * (predkosc / promien) * .35f;
        if (gracz && gracz.body && kulaKol) Physics.IgnoreCollision(kulaKol, gracz.body, true);
        faza = Faza.Toczy; t0 = Time.time;
        BraxDzwiekKul(start);
    }
    static void BraxDzwiekKul(Vector3 p) { }   // (sound hook: the lane's rumble; the region has no bowling clip yet)

    // ---------------- pętla ----------------
    void Update()
    {
        if (!gotowe) return;
        if (Time.unscaledTime >= sprawdz)
        {
            sprawdz = Time.unscaledTime + .5f;
            gracz = BraxChampionsGry.Gracz;
            aktywne = gracz && (gracz.transform.position - transform.position).sqrMagnitude < 70f * 70f;
            if (aktywne && gracz.body) foreach (var c in kol) if (c && c.enabled) Physics.IgnoreCollision(c, gracz.body, true);
        }
        if (!aktywne) { if (faza == Faza.Trzyma) { NaZwrotnice(); faza = Faza.Wolne; } return; }
        if (tor >= 0) Panel();
        switch (faza)
        {
            case Faza.Liczy:
                if (Time.time >= czekaj) Licz();
                break;
            case Faza.Maszyna:
                Maszyna(Time.time - t0);
                break;
            case Faza.Powrot:
                if (Time.time >= czekaj) { NaZwrotnice(); faza = Faza.Wolne; }
                break;
        }
    }

    void LateUpdate()
    {
        if (faza != Faza.Trzyma || !kula || !gracz) return;
        var f = BraxChampionsGry.Przod(gracz); var r = Vector3.Cross(Vector3.up, f);
        var at = gracz.transform.position + r * .40f + f * .12f + Vector3.up * .98f;
        kula.position = at; kula.transform.position = at;
    }

    void FixedUpdate()
    {
        if (!gotowe || tor < 0) return;
        var f = faul[tor];
        if (faza == Faza.Toczy)
        {
            var p = kula.position; float u = p.z - f.z, v = p.x - f.x;
            if (u > -.1f && u < glowa - .45f && Mathf.Abs(v) > polowaToru - promien * .25f)
            {
                // into the gutter: the channel runs beside the lane bed to the pit
                faza = Faza.Rynna; kula.isKinematic = true;
                rynnaV = Mathf.Max(4f, Predkosc(kula).z);
                rynnaPoz = new Vector3(f.x + Mathf.Sign(v) * (polowaToru + rynna * .5f), f.y - glebRynny + promien, p.z);
                BraxChampionsGry.Pokaz("RYNNA", 1.4f);
            }
            else if (u > koniecPola + .25f || p.y < f.y - .6f) { Schowaj(); Czekaj(2.2f); }
            else if (Time.time - t0 > 9f) { Schowaj(); Czekaj(1.0f); }
            else if (u > glowa - 1f && Predkosc(kula).magnitude < .15f && Time.time - t0 > 2f) { Schowaj(); Czekaj(1.8f); }
        }
        else if (faza == Faza.Rynna)
        {
            rynnaPoz.z += rynnaV * Time.fixedDeltaTime;
            kula.MovePosition(rynnaPoz);
            if (rynnaPoz.z - f.z > koniecPola + .25f) { Schowaj(); Czekaj(1.6f); }
        }
    }
    void Schowaj() { if (!kula.isKinematic) { Predkosc(kula, Vector3.zero); kula.angularVelocity = Vector3.zero; } kula.isKinematic = true; Pokaz(kula, false); }
    void Czekaj(float s) { faza = Faza.Liczy; czekaj = Time.time + s; }

    bool Stoi(int k)
    {
        var t = kregle[k]; if (!t || !kol[k] || !kol[k].enabled) return false;
        if (Vector3.Dot(t.up, Vector3.up) < .8f) return false;
        var d = t.position - miejsca[k];
        if (Mathf.Abs(d.y) > .12f) return false;
        var f = faul[k / Pinow]; float u = t.position.z - f.z, v = t.position.x - f.x;
        return u > glowa - .6f && u < koniecPola && Mathf.Abs(v) < polowaToru + .03f;
    }

    void Licz()
    {
        var g = gry[tor];
        int przed = 0, po = 0; var stoiPo = new bool[Pinow];
        for (int i = 0; i < Pinow; i++)
        {
            int k = tor * Pinow + i;
            if (stoiPrzed[i]) { przed++; stoiPo[i] = Stoi(k); if (stoiPo[i]) po++; }
            if (rb[k] && !rb[k].isKinematic) { Predkosc(rb[k], Vector3.zero); rb[k].angularVelocity = Vector3.zero; rb[k].isKinematic = true; }
        }
        int zbite = przed - po;
        if (g.n < g.rzuty.Length) g.rzuty[g.n++] = zbite;
        Pozycja(g.rzuty, g.n, out int ramka, out int wRamce, out bool nowyKomplet, out bool koniec);
        // flash
        int prev = g.n >= 2 ? g.rzuty[g.n - 2] : -1;
        bool drugi = !pelny;
        if (!drugi && zbite == 10) BraxChampionsGry.Pokaz("STRIKE!", 2.2f);
        else if (drugi && po == 0) BraxChampionsGry.Pokaz("SPARE!", 2.2f);
        else if (zbite == 0) BraxChampionsGry.Pokaz("0 · PUDŁO", 1.4f);
        else BraxChampionsGry.Pokaz(zbite + (zbite == 1 ? " KRĘGIEL" : zbite < 5 ? " KRĘGLE" : " KRĘGLI"), 1.4f);
        if (koniec) BraxChampionsGry.Pokaz("KONIEC GRY · " + Suma(g) + " PKT", 4f);
        Rysuj(tor);
        // pinsetter
        for (int i = 0; i < Pinow; i++) stoiPrzed[i] = nowyKomplet || stoiPo[i];
        pelny = nowyKomplet;
        faza = Faza.Maszyna; t0 = Time.time;
        // the fallen (or, for a new rack, all) pins are swept at once
        for (int i = 0; i < Pinow; i++) if (nowyKomplet || !stoiPo[i]) Widac(tor * Pinow + i, false);
        _ = prev; _ = ramka; _ = wRamce;
    }

    // pinsetter: standing pins lifted (0..0.5 s), set on their spots (0.5..0.8), lowered (0.8..1.3); a new rack comes
    // down from above at 0.7..1.4 s
    void Maszyna(float t)
    {
        const float lift = .45f;
        for (int i = 0; i < Pinow; i++)
        {
            int k = tor * Pinow + i; var tr = kregle[k]; if (!tr) continue;
            if (pelny)
            {
                if (t < .7f) continue;
                Widac(k, true);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.7f, 1.4f, t));
                var pos = miejsca[k] + Vector3.up * lift * (1f - a);
                tr.SetPositionAndRotation(pos, obroty[k]); if (rb[k]) { rb[k].position = pos; rb[k].rotation = obroty[k]; }
            }
            else if (stoiPrzed[i])
            {
                var p0 = tr.position;
                float up = t < .5f ? Mathf.SmoothStep(0f, 1f, t / .5f) : t < .8f ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (t - .8f) / .5f);
                var spot = t < .5f ? new Vector3(p0.x, miejsca[k].y + lift * up, p0.z) : miejsca[k] + Vector3.up * lift * up;
                var rot = t < .5f ? tr.rotation : obroty[k];
                tr.SetPositionAndRotation(spot, rot); if (rb[k]) { rb[k].position = spot; rb[k].rotation = rot; }
            }
        }
        if (t > 1.45f)
        {
            for (int i = 0; i < Pinow; i++)
            {
                int k = tor * Pinow + i;
                if (pelny || stoiPrzed[i]) { kregle[k].SetPositionAndRotation(miejsca[k], obroty[k]); if (rb[k]) { rb[k].position = miejsca[k]; rb[k].rotation = obroty[k]; } }
            }
            faza = Faza.Powrot; czekaj = Time.time + .6f;
        }
    }

    // ---------------- punktacja: real ten-pin scoring (BraxChampionsPiksele) ----------------
    bool Koniec(int lane) => gry != null && lane >= 0 && lane < gry.Length && gry[lane] != null && BraxChampionsPiksele.Koniec(gry[lane].rzuty, gry[lane].n);
    static int Suma(Gra g) => BraxChampionsPiksele.Suma(g.rzuty, g.n);
    static void Pozycja(int[] r, int n, out int ramka, out int wRamce, out bool komplet, out bool koniec) => BraxChampionsPiksele.Pozycja(r, n, out ramka, out wRamce, out komplet, out koniec);
    // the status board (reception, offices): -1 = free lane, else its running total; whether that game is over
    public int Wynik(int lane) { var g = gry != null && lane >= 0 && lane < gry.Length ? gry[lane] : null; return g == null || g.n == 0 ? -1 : Suma(g); }
    public bool Skonczona(int lane) => Koniec(lane);
    public int Tory => faul.Length;

    void Panel()
    {
        var g = tor >= 0 ? gry[tor] : null; if (g == null) return;
        if (!gracz || (gracz.transform.position - faul[tor]).sqrMagnitude > 18f * 18f) return;
        Pozycja(g.rzuty, g.n, out int ramka, out int wRamce, out _, out bool koniec);
        BraxChampionsGry.Ustaw("KRĘGLE · TOR " + (tor + 1) + (koniec ? " · KONIEC" : " · RAMKA " + (ramka + 1) + " · RZUT " + (wRamce + 1)) + " · " + Suma(g) + " PKT");
    }

    // ---------------- ekran wyniku nad torem i na konsoli (400 x 128) ----------------
    void Rysuj(int lane)
    {
        var g = gry[lane]; if (g == null) return;
        bool ekran = lane < ekrany.Length && ekrany[lane], konsola = lane < konsole.Length && konsole[lane];
        if (!ekran && !konsola) return;
        if (!g.tex)
        {
            g.tex = new Texture2D(BraxChampionsPiksele.TorW, BraxChampionsPiksele.TorH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "E1 tor " + (lane + 1) };
            g.px = new Color32[BraxChampionsPiksele.TorW * BraxChampionsPiksele.TorH];
            var baza = ekran ? ekrany[lane].sharedMaterial : konsole[lane].sharedMaterial;
            var m = new Material(baza) { name = "E1 ekran toru " + (lane + 1) };
            m.SetTexture("_MainTex", g.tex); m.SetTexture("_EmissionMap", g.tex);
            m.SetColor("_Color", Color.white); m.SetColor("_EmissionColor", new Color(1.05f, 1.05f, 1.1f));
            if (ekran) ekrany[lane].sharedMaterial = m;
            if (konsola) konsole[lane].sharedMaterial = m;
        }
        BraxChampionsPiksele.TabelaToru(g.px, lane, g.rzuty, g.n);
        g.tex.SetPixels32(g.px); g.tex.Apply(false);
    }

    // ---- Unity 6 renamed velocity / drag (linearVelocity / linearDamping)
    static Vector3 Predkosc(Rigidbody b)
    {
#if UNITY_6000_0_OR_NEWER
        return b.linearVelocity;
#else
        return b.velocity;
#endif
    }
    static void Predkosc(Rigidbody b, Vector3 v)
    {
#if UNITY_6000_0_OR_NEWER
        b.linearVelocity = v;
#else
        b.velocity = v;
#endif
    }
}
