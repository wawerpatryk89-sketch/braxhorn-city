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
// Owner 24.11 01:32 ("Centrum rozrywki, żeby było można grać tak samo [jak w kasynie]. Gdzie bilard i kręgle"): played the
// way the casino's games are (BraxChampionsStanowisko) and the mobile bowling games: AKCJA at a lane's ball return or on
// its approach opens the lane - the camera low behind the ball on the foul line, the player out of the picture:
//   - a finger near the ball moves it along the foul line; a finger down the lane aims (at most maxKat off the lane),
//     the aim drawn on the lane; PODKRĘĆ ◀ ▶ the hook (the ball curls in the back third of the lane, as a real hook),
//   - hold RZUĆ: the speed rises and falls; let go = the ball rolls; the camera rides behind it to the pins, watches the
//     pinsetter, comes back for the next ball; the scorecard in the panel and on the lane's screens; NOWA GRA at the end.
public sealed class BraxChampionsKregle : MonoBehaviour, IBraxInterakcja, IBraxStanowisko
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
    // the game at the lane (BraxChampionsStanowisko): the ball's place on the foul line, the aim, the hook, the speed held
    float startX, hakRzutu; int hak; Vector3 kier = Vector3.forward; bool trzymaSila; float silaOd, moc;
    public float hakSila = .35f;                      // the hook's sideways pull per step (world m/s2, back third of the lane)
    public float predkoscMin = 6.2f, predkoscMax = 11.6f;

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

    // the lane the player is at: its ball return (the lane on his side of it) or its approach
    int TorPrzy(Vector3 p)
    {
        if (Zwrotnica(p, out int lane) >= 0) return lane;
        for (int l = 0; l < faul.Length; l++) if (NaRozbiegu(p, l)) return l;
        return -1;
    }

    public string Kontekst(Vector3 p)
    {
        if (!gotowe || faul.Length == 0 || BraxChampionsStanowisko.Aktywne || faza != Faza.Wolne) return "";
        int lane = TorPrzy(p); if (lane < 0) return "";
        return "GRAJ · KRĘGLE · TOR " + (lane + 1) + (lane != tor || Koniec(lane) ? " · NOWA GRA" : "");
    }

    public bool Wykonaj(Vector3 p)
    {
        if (faza != Faza.Wolne || BraxChampionsStanowisko.Aktywne) return false;
        int lane = TorPrzy(p); if (lane < 0) return false;
        if (lane != tor || Koniec(lane)) NowaGra(lane);
        startX = 0f; kier = Vector3.forward; hak = 0; trzymaSila = false; moc = 0f;
        NaRozbieg();
        BraxChampionsStanowisko.Wejdz(this, lane);
        return true;
    }

    // the ball waits on the foul line where the player put it (the aim phase)
    void NaRozbieg()
    {
        if (!kula || tor < 0) return;
        if (!kula.isKinematic) { Predkosc(kula, Vector3.zero); kula.angularVelocity = Vector3.zero; }
        kula.isKinematic = true; Pokaz(kula, true);
        var at = StartRzutu(); kula.position = at; kula.transform.position = at;
        faza = Faza.Trzyma;
    }
    Vector3 StartRzutu()
    {
        var f = faul[tor]; float lim = polowaToru - promien - .02f;
        return new Vector3(f.x + Mathf.Clamp(startX, -lim, lim), f.y + promien + .003f, f.z - .35f);
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

    void Rzut(Vector3 dir, float v)
    {
        gracz = BraxChampionsGry.Gracz;
        float kat = Mathf.Clamp(Vector3.SignedAngle(Vector3.forward, dir, Vector3.up), -maxKat, maxKat);
        dir = Quaternion.Euler(0, kat, 0) * Vector3.forward;
        var start = StartRzutu(); hakRzutu = hak * hakSila;
        for (int i = 0; i < Pinow; i++)
        {
            int k = tor * Pinow + i; stoiPrzed[i] = kol[k] && kol[k].enabled;
            if (rb[k] && stoiPrzed[i]) { rb[k].isKinematic = false; rb[k].WakeUp(); }
        }
        kula.isKinematic = false; Pokaz(kula, true);
        kula.position = start; kula.transform.position = start;
        Predkosc(kula, dir * v);
        kula.angularVelocity = Vector3.Cross(Vector3.up, dir) * (v / promien) * .35f;
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
        if (tor >= 0 && !BraxChampionsStanowisko.Aktywne) Status();
        switch (faza)
        {
            case Faza.Liczy:
                if (Time.time >= czekaj) Licz();
                break;
            case Faza.Maszyna:
                Maszyna(Time.time - t0);
                break;
            case Faza.Powrot:
                if (Time.time >= czekaj)
                {
                    if (BraxChampionsStanowisko.Gra(this, tor) && !Koniec(tor)) NaRozbieg();
                    else { NaZwrotnice(); faza = Faza.Wolne; }
                }
                break;
        }
    }


    void FixedUpdate()
    {
        if (!gotowe || tor < 0) return;
        var f = faul[tor];
        if (faza == Faza.Toczy)
        {
            var p = kula.position; float u = p.z - f.z, v = p.x - f.x;
            // the hook: the ball grips the dry back end of the lane and curls (from about 2/3 of the way to the pins)
            if (hakRzutu != 0f && u > glowa * .6f && u < glowa) kula.AddForce(Vector3.right * hakRzutu, ForceMode.Acceleration);
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

    // ---------------- the game at the lane (BraxChampionsStanowisko) ----------------
    public string Tytul(int lane)
    {
        var g = gry != null && lane >= 0 && lane < gry.Length ? gry[lane] : null;
        if (g == null) return "E1 · KRĘGLE · TOR " + (lane + 1);
        Pozycja(g.rzuty, g.n, out int ramka, out int wRamce, out _, out bool koniec);
        return "E1 · KRĘGLE · TOR " + (lane + 1) + (koniec ? " · KONIEC" : " · RAMKA " + (ramka + 1) + " · RZUT " + (wRamce + 1));
    }
    // aiming: low behind the ball, the lane ahead; rolling: riding behind the ball, stopping short of the pins; the pins
    // falling and the pinsetter: over the pin deck
    public void Kadr(int lane, Camera cam, out Vector3 oko, out Quaternion obrot)
    {
        var f = faul[lane]; var talia = new Vector3(f.x, f.y, f.z + glowa + .45f);
        if (lane == tor && (faza == Faza.Toczy || faza == Faza.Rynna) && kula)
        {
            var b = kula.position; float z = Mathf.Min(b.z - 2.6f, talia.z - 4.2f);
            oko = new Vector3(Mathf.Lerp(f.x, b.x, .6f), f.y + 1.05f, z);
            obrot = Quaternion.LookRotation(new Vector3(b.x, f.y + .15f, Mathf.Max(b.z + 3f, talia.z)) - oko);
            return;
        }
        if (lane == tor && (faza == Faza.Liczy || faza == Faza.Maszyna))
        {
            oko = new Vector3(f.x, f.y + 1.25f, talia.z - 4.2f);
            obrot = Quaternion.LookRotation(talia + Vector3.up * .2f - oko);
            return;
        }
        oko = new Vector3(f.x, f.y + 1.55f, f.z - 3.4f);
        obrot = Quaternion.LookRotation(new Vector3(f.x, f.y, f.z + glowa * .55f) - oko);
    }
    public void Wstal(int lane)
    {
        trzymaSila = false; moc = 0f;
        if (lane == tor && faza == Faza.Trzyma) { NaZwrotnice(); faza = Faza.Wolne; }
    }

    public void Panel(int lane, BraxChampionsStanowisko.Ui ui)
    {
        if (lane != tor) return;
        var g = gry[lane]; var e = Event.current; var r = ui.dol; float p = ui.m, h = r.height - 2f * p, y = r.y + p;
        Pozycja(g.rzuty, g.n, out _, out _, out _, out bool koniec);
        bool moze = faza == Faza.Trzyma && !koniec;
        var wynR = new Rect(r.x + p, y, r.width * .15f, h);
        var hakL = new Rect(wynR.xMax + p * 2f, y + h * .12f, h * .76f, h * .76f);
        var hakP = new Rect(hakL.xMax + p * .5f + r.width * .13f, hakL.y, hakL.width, hakL.height);
        var hakPole = new Rect(hakL.xMax + p * .25f, y, hakP.x - hakL.xMax - p * .5f, h);
        var silaR = new Rect(r.xMax - p - r.width * .30f, y, r.width * .30f, h);
        var info = new Rect(hakP.xMax + p, y, silaR.x - hakP.xMax - 2f * p, h);
        ui.Pole(wynR, "WYNIK", Suma(g) + " PKT", koniec ? BraxChampionsStanowisko.Ui.Zloto : BraxChampionsStanowisko.Ui.Krem);
        if (ui.Przycisk(hakL, "◀", BraxChampionsStanowisko.Ui.Szary, BraxChampionsStanowisko.Ui.Krem, moze && hak > -3)) hak--;
        if (ui.Przycisk(hakP, "▶", BraxChampionsStanowisko.Ui.Szary, BraxChampionsStanowisko.Ui.Krem, moze && hak < 3)) hak++;
        ui.Pole(hakPole, "PODKRĘCENIE", hak == 0 ? "PROSTO" : (hak < 0 ? "W LEWO " : "W PRAWO ") + Mathf.Abs(hak), BraxChampionsStanowisko.Ui.Krem);
        string stan = faza == Faza.Toczy ? "KULA TOCZY SIĘ" : faza == Faza.Rynna ? "RYNNA" : faza == Faza.Liczy || faza == Faza.Maszyna || faza == Faza.Powrot ? "MASZYNA STAWIA KRĘGLE" :
            koniec ? "KONIEC GRY · " + Suma(g) + " PKT" : "PALEC PRZY KULI = MIEJSCE · NA TORZE = CEL";
        ui.Tekst(info, stan, .022f, BraxChampionsStanowisko.Ui.Krem);
        if (koniec)
        {
            if (ui.Przycisk(silaR, "NOWA GRA", BraxChampionsStanowisko.Ui.Zielen, BraxChampionsStanowisko.Ui.Krem, faza == Faza.Wolne || faza == Faza.Trzyma, true)) { NowaGra(lane); NaRozbieg(); }
            return;
        }
        // hold: the speed rises and falls; let go: the ball rolls
        if (e.type == EventType.MouseDown && silaR.Contains(e.mousePosition) && moze && BraxChampionsStanowisko.OdWejscia > .45f) { trzymaSila = true; silaOd = Time.unscaledTime; moc = 0f; }
        if (trzymaSila) moc = Mathf.PingPong((Time.unscaledTime - silaOd) / 1.2f, 1f);
        if (e.type == EventType.MouseUp && trzymaSila)
        {
            trzymaSila = false;
            if (moze) Rzut(kier, Mathf.Lerp(predkoscMin, predkoscMax, moc));
            moc = 0f;
        }
        if (e.type == EventType.Repaint)
        {
            BraxGui.Zaokraglony(silaR, h * .2f, moze ? BraxChampionsStanowisko.Ui.Czerwien : BraxChampionsStanowisko.Ui.Szary);
            if (trzymaSila) BraxGui.Zaokraglony(new Rect(silaR.x, silaR.y, Mathf.Max(h * .4f, silaR.width * moc), silaR.height), h * .2f, BraxChampionsStanowisko.Ui.Zloto);
            BraxGui.ObrysZaokraglony(silaR, h * .2f, BraxChampionsStanowisko.Ui.Zloto);
            ui.Tekst(silaR, trzymaSila ? "PUŚĆ = RZUĆ · " + Mathf.RoundToInt(Mathf.Lerp(predkoscMin, predkoscMax, moc) / 1.2944f * 3.6f) + " KM/H" : moze ? "PRZYTRZYMAJ · RZUĆ" : "CZEKAJ", .028f, trzymaSila ? Color.black : BraxChampionsStanowisko.Ui.Krem);
        }
        if (!moze) return;
        var f = faul[lane]; var start = StartRzutu();
        // a finger near the ball moves it along the foul line; down the lane it aims
        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && !trzymaSila && ui.kam && !ui.dol.Contains(e.mousePosition) && !ui.gora.Contains(e.mousePosition))
        {
            var ray = ui.kam.ScreenPointToRay(new Vector3(e.mousePosition.x, Screen.height - e.mousePosition.y, 0f));
            if (new Plane(Vector3.up, f).Raycast(ray, out float dist))
            {
                var w = ray.GetPoint(dist); float u = w.z - f.z;
                if (u < 2.2f) { startX = w.x - f.x; var at = StartRzutu(); kula.position = at; kula.transform.position = at; }
                else
                {
                    var d = w - start; d.y = 0f;
                    float kat = Mathf.Clamp(Vector3.SignedAngle(Vector3.forward, d, Vector3.up), -maxKat, maxKat);
                    kier = Quaternion.Euler(0f, kat, 0f) * Vector3.forward;
                }
            }
        }
        // the aim on the lane: from the ball to the pins (straight; the hook bends it in the back third)
        if (e.type == EventType.Repaint)
        {
            var a0 = new Vector3(start.x, f.y + .01f, start.z); var a1 = a0 + kier * (glowa + .35f - (start.z - f.z));
            if (ui.Rzut(a0, out var A) && ui.Rzut(a1, out var B)) BraxChampionsStanowisko.Ui.Linia(A, B, new Color(1f, 1f, 1f, .75f), ui.s * .004f);
            if (hak != 0)
            {
                var a2 = a0 + kier * (glowa * .6f); var a3 = a1 + Vector3.right * hak * .08f;
                if (ui.Rzut(a2, out var C) && ui.Rzut(a3, out var D)) BraxChampionsStanowisko.Ui.Linia(C, D, BraxChampionsStanowisko.Ui.Zloto, ui.s * .004f);
            }
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

    void Status()
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
