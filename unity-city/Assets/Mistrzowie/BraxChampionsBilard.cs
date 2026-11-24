using UnityEngine;
#if UNITY_6000_0_OR_NEWER
using FizMat = UnityEngine.PhysicsMaterial;
using FizLacz = UnityEngine.PhysicsMaterialCombine;
#else
using FizMat = UnityEngine.PhysicMaterial;
using FizLacz = UnityEngine.PhysicMaterialCombine;
#endif

// Owner 10.11 20:45-20:50 ("funkcjonalne, nie na wystawę ... jak najbardziej realistyczne"): the 18 billiard tables of E1
// are PLAYED. One component for the billiards hall (BraxChampionsE1 writes its configuration):
//   - every table holds the 15 object balls racked at the foot spot and the cue ball at the head spot (real phenolic
//     balls 57 mm, 170 g, numbered: solids 1-7, the 8, stripes 9-15),
//   - walk round the table: the cue stick lies behind the cue ball, pointing away from you through it - you aim by where
//     you stand. AKCJA = a normal shot, CIOS (punch) = a hard shot (the break),
//   - real physics: ball-to-ball and cushion bounces, rolling resistance on the cloth; a ball that reaches a pocket mouth
//     drops in; the cue ball in a pocket (or off the table) comes back on the head spot,
//   - WBITE x/15 and UDERZENIA on the panel, flashes for every pot; all 15 down = the partie ends, AKCJA re-racks.
// Only the table being played is live; the others sleep (kinematic). Nothing runs further than 60 m away.
// Owner 24.11 01:32 ("Centrum rozrywki, żeby było można grać tak samo [jak w kasynie]. Gdzie bilard i kręgle"): played the
// way the casino's games are (BraxChampionsStanowisko) and the mobile pool games: AKCJA at a table opens it - the camera
// over the table, the player out of the picture, the walking controls rest:
//   - a finger on the cloth aims (the cue ball goes toward it), ◀ ▶ turn the aim finely; the guide line runs from the cue
//     ball to the first ball or cushion it meets and shows where the struck ball goes,
//   - hold UDERZ: the power rises and falls (the cue draws back), let go = the shot,
//   - NOWA PARTIA when all 15 are down; WSTAŃ walks on.
public sealed class BraxChampionsBilard : MonoBehaviour, IBraxInterakcja, IBraxStanowisko
{
    public Vector3[] stoly = new Vector3[0];         // centre of each table's cloth (world), y = cloth surface
    public float polX = .757f, polZ = 1.527f;        // half sizes of the playing field (world; the long side along z)
    public float r = .037f;                          // ball radius (world)
    public Rigidbody[] bile = new Rigidbody[0];      // 16 per table: [t * 16] = cue ball, then 1..15
    public Vector3[] luzy = new Vector3[0];          // the 6 pocket centres relative to the table centre (world)
    public float promienLuzy = .084f;
    public Transform kij;                            // the cue (tip at its origin, the butt back along -z)
    public Collider[] bandy = new Collider[0];       // cushion colliders (bouncy rubber)
    public float sila = 3.6f, silaMocna = 6.6f;      // world m/s
    public float zasieg = 2.6f;                      // how far from the cue ball the player can take the shot

    const int Bil = 16;
    int stol = -1; bool ruch, gotowe, aktywne; float spokojOd, sprawdz, uderzenieDo;
    Vector3[] start;
    bool[] wbita; int wbite, uderzenia;
    Vector3 cel; bool celuje;
    Collider[] kol; Renderer[][] widok;
    BraxGymPlayer gracz;
    // the game at the table (BraxChampionsStanowisko): the aim (horizontal unit), the power held
    Vector3 kierunek = Vector3.forward; bool trzymaSila; float silaOd, moc;

    void OnEnable() { BraxInterakcje.Dodaj(this); }
    void OnDisable() { BraxInterakcje.Usun(this); }

    void Start()
    {
        int n = bile.Length;
        start = new Vector3[n]; wbita = new bool[n]; kol = new Collider[n]; widok = new Renderer[n][];
        var bila = new FizMat("bila") { bounciness = .92f, dynamicFriction = .05f, staticFriction = .06f, bounceCombine = FizLacz.Maximum, frictionCombine = FizLacz.Minimum };
        var banda = new FizMat("banda") { bounciness = .74f, dynamicFriction = .12f, staticFriction = .14f, bounceCombine = FizLacz.Average, frictionCombine = FizLacz.Average };
        for (int i = 0; i < n; i++)
        {
            var b = bile[i]; if (!b) continue;
            start[i] = b.position; kol[i] = b.GetComponent<Collider>(); widok[i] = b.GetComponentsInChildren<Renderer>(true);
            if (kol[i]) kol[i].sharedMaterial = bila;
            b.isKinematic = true; b.maxAngularVelocity = 250f;
        }
        foreach (var c in bandy) if (c) c.sharedMaterial = banda;
        if (kij) kij.gameObject.SetActive(false);
        gotowe = true;
    }

    // the status board (reception, offices): -1 = the table is free, else balls potted in the partie being played
    public int Wbite(int t) => gotowe && t == stol && uderzenia > 0 ? wbite : -1;
    public int Stoly => stoly.Length;

    // ---------------- interakcja ----------------
    int Stol(Vector3 p)
    {
        int best = -1; float bd = float.MaxValue;
        for (int t = 0; t < stoly.Length; t++)
        {
            var d = p - stoly[t]; if (Mathf.Abs(d.y) > 2.5f) continue;
            float dx = Mathf.Max(0f, Mathf.Abs(d.x) - polX), dz = Mathf.Max(0f, Mathf.Abs(d.z) - polZ);
            float m = Mathf.Sqrt(dx * dx + dz * dz);
            if (m < 1.4f && m < bd) { bd = m; best = t; }
        }
        return best;
    }
    Rigidbody Biala(int t) => bile[t * Bil];

    public string Kontekst(Vector3 p)
    {
        if (!gotowe || BraxChampionsStanowisko.Aktywne) return "";
        int t = Stol(p); if (t < 0) return "";
        return "GRAJ · BILARD · STÓŁ " + (t + 1);
    }

    public bool Wykonaj(Vector3 p)
    {
        int t = Stol(p); if (t < 0 || BraxChampionsStanowisko.Aktywne) return false;
        Przejmij(t);
        // the first aim: from the cue ball to the rack's apex
        var d = bile[t * Bil + 1].position - Biala(t).position; d.y = 0f;
        if (d.sqrMagnitude > 1e-6f && !(t == stol && uderzenia > 0)) kierunek = d.normalized;
        trzymaSila = false; moc = 0f;
        BraxChampionsStanowisko.Wejdz(this, t);
        return true;
    }

    // this table becomes the live one (a new partie racked, unless it is the one being played)
    void Przejmij(int t)
    {
        if (t == stol) return;
        if (stol >= 0) Usypiaj(stol);
        stol = t; wbite = 0; uderzenia = 0; ruch = false;
        for (int i = 0; i < Bil; i++) { int k = t * Bil + i; if (wbita[k]) { Widac(k, true); wbita[k] = false; } }
        Rozstaw(t);
    }

    void Rozstaw(int t)
    {
        for (int i = 0; i < Bil; i++)
        {
            int k = t * Bil + i; var b = bile[k]; if (!b) continue;
            if (!b.isKinematic) { Predkosc(b, Vector3.zero); b.angularVelocity = Vector3.zero; }
            b.isKinematic = true; b.position = start[k]; b.transform.position = start[k]; b.transform.rotation = Quaternion.identity;
            Widac(k, true); wbita[k] = false;
        }
        if (t == stol) { wbite = 0; uderzenia = 0; }
        BraxChampionsGry.Pokaz("STÓŁ " + (t + 1) + " · ROZSTAWIONE", 1.4f);
    }
    void Widac(int k, bool on) { if (kol[k]) kol[k].enabled = on; foreach (var w in widok[k]) if (w) w.enabled = on; }

    void Uderz(int t, Vector3 d, float v)
    {
        Przejmij(t);
        gracz = BraxChampionsGry.Gracz;
        for (int i = 0; i < Bil; i++)
        {
            int k = t * Bil + i; var b = bile[k]; if (!b || wbita[k]) continue;
            b.isKinematic = false; b.WakeUp();
            b.linearDampingCompat(.6f); b.angularDampingCompat(.9f);
            if (gracz && gracz.body && kol[k]) Physics.IgnoreCollision(kol[k], gracz.body, true);
        }
        var cue = Biala(t); d.y = 0f; d.Normalize();
        Predkosc(cue, d * v);
        cue.angularVelocity = Vector3.Cross(Vector3.up, d) * (v / r) * .2f;
        uderzenia++; ruch = true; spokojOd = 0f;
        cel = d; uderzenieDo = Time.time + .25f;
        BraxChampionsGry.Pokaz(v > silaMocna * .85f ? "MOCNE UDERZENIE" : "UDERZENIE " + uderzenia, .9f);
    }

    void Usypiaj(int t)
    {
        for (int i = 0; i < Bil; i++)
        {
            var b = bile[t * Bil + i]; if (!b) continue;
            if (!b.isKinematic) { Predkosc(b, Vector3.zero); b.angularVelocity = Vector3.zero; }
            b.isKinematic = true;
        }
    }

    // ---------------- pętla ----------------
    void Update()
    {
        if (!gotowe) return;
        if (Time.unscaledTime >= sprawdz)
        {
            sprawdz = Time.unscaledTime + .4f;
            gracz = BraxChampionsGry.Gracz;
            aktywne = gracz && (gracz.transform.position - transform.position).sqrMagnitude < 60f * 60f;
        }
        if (!aktywne || !gracz) { if (kij && kij.gameObject.activeSelf) kij.gameObject.SetActive(false); return; }
        bool gra = stol >= 0 && BraxChampionsStanowisko.Gra(this, stol);
        // the cue: behind the cue ball along the aim, drawn back by the power held (or striking right after a shot)
        celuje = gra && !ruch && wbite < 15;
        if (trzymaSila) moc = Mathf.PingPong((Time.unscaledTime - silaOd) / 1.3f, 1f);
        if (kij)
        {
            bool uderza = Time.time < uderzenieDo && stol >= 0;
            if (celuje || uderza)
            {
                var cue = Biala(stol).position;
                var d = uderza ? cel : kierunek; d.y = 0f; if (d.sqrMagnitude < 1e-6f) d = Vector3.forward; d.Normalize();
                float cofniecie = uderza ? Mathf.Lerp(.02f, .06f + .30f * moc, (uderzenieDo - Time.time) / .25f) : .06f + (trzymaSila ? .30f * moc : .02f * Mathf.Sin(Time.time * 3f) + .02f);
                var tip = cue - d * (r + cofniecie) + Vector3.up * .01f;
                kij.SetPositionAndRotation(tip, Quaternion.LookRotation(Quaternion.AngleAxis(6f, Vector3.Cross(Vector3.up, d)) * d));
                if (!kij.gameObject.activeSelf) kij.gameObject.SetActive(true);
            }
            else if (kij.gameObject.activeSelf) kij.gameObject.SetActive(false);
        }
    }

    // ---------------- the game at the table (BraxChampionsStanowisko) ----------------
    public string Tytul(int t) => "E1 · BILARD · STÓŁ " + (t + 1);
    // over the table, its long side across the screen, tilted a little toward the player's side
    public void Kadr(int t, Camera cam, out Vector3 oko, out Quaternion obrot)
    {
        var c = stoly[t]; float a = 14f * Mathf.Deg2Rad;
        var n = Vector3.up * Mathf.Cos(a) + Vector3.left * Mathf.Sin(a);
        BraxChampionsStanowisko.Kadruj(cam, c, n, Vector3.right, 2f * polZ + .55f, 2f * polX + .45f, out oko, out obrot);
    }
    public void Wstal(int t) { trzymaSila = false; moc = 0f; if (kij) kij.gameObject.SetActive(false); }

    public void Panel(int t, BraxChampionsStanowisko.Ui ui)
    {
        if (t != stol) return;
        var e = Event.current; var dolR = ui.dol; float p = ui.m, h = dolR.height - 2f * p, y = dolR.y + p;
        bool moze = !ruch && wbite < 15;
        var wynR = new Rect(dolR.x + p, y, dolR.width * .17f, h);
        var udR = new Rect(wynR.xMax + p, y, dolR.width * .15f, h);
        var silaR = new Rect(dolR.xMax - p - dolR.width * .30f, y, dolR.width * .30f, h);
        var lewoR = new Rect(udR.xMax + p * 2f, y + h * .12f, h * .76f, h * .76f);
        var prawoR = new Rect(lewoR.xMax + p, lewoR.y, lewoR.width, lewoR.height);
        var info = new Rect(prawoR.xMax + p, y, silaR.x - prawoR.xMax - 2f * p, h);
        ui.Pole(wynR, "WBITE", wbite + " / 15", wbite >= 15 ? BraxChampionsStanowisko.Ui.Zloto : BraxChampionsStanowisko.Ui.Krem);
        ui.Pole(udR, "UDERZENIA", uderzenia.ToString(), BraxChampionsStanowisko.Ui.Krem);
        if (ui.Przycisk(lewoR, "◀", BraxChampionsStanowisko.Ui.Szary, BraxChampionsStanowisko.Ui.Krem, moze)) kierunek = Quaternion.Euler(0f, -.6f, 0f) * kierunek;
        if (ui.Przycisk(prawoR, "▶", BraxChampionsStanowisko.Ui.Szary, BraxChampionsStanowisko.Ui.Krem, moze)) kierunek = Quaternion.Euler(0f, .6f, 0f) * kierunek;
        ui.Tekst(info, ruch ? "BILE W RUCHU" : wbite >= 15 ? "WSZYSTKIE WBITE · " + uderzenia + " UDERZEŃ" : "PALEC NA STOLE = CELUJ", .024f, BraxChampionsStanowisko.Ui.Krem);
        if (wbite >= 15)
        {
            if (ui.Przycisk(silaR, "NOWA PARTIA", BraxChampionsStanowisko.Ui.Zielen, BraxChampionsStanowisko.Ui.Krem, !ruch, true)) { Rozstaw(t); var d0 = bile[t * Bil + 1].position - Biala(t).position; d0.y = 0f; if (d0.sqrMagnitude > 1e-6f) kierunek = d0.normalized; }
        }
        else
        {
            // hold: the power rises and falls; let go: the shot
            if (e.type == EventType.MouseDown && silaR.Contains(e.mousePosition) && moze && BraxChampionsStanowisko.OdWejscia > .45f) { trzymaSila = true; silaOd = Time.unscaledTime; moc = 0f; }
            if (e.type == EventType.MouseUp && trzymaSila)
            {
                trzymaSila = false;
                if (moze) Uderz(t, kierunek, Mathf.Lerp(.5f, silaMocna * 1.15f, Mathf.Pow(Mathf.Max(moc, .04f), 1.4f)));
                moc = 0f;
            }
            if (e.type == EventType.Repaint)
            {
                BraxGui.Zaokraglony(silaR, h * .2f, moze ? BraxChampionsStanowisko.Ui.Czerwien : BraxChampionsStanowisko.Ui.Szary);
                if (trzymaSila) BraxGui.Zaokraglony(new Rect(silaR.x, silaR.y, Mathf.Max(h * .4f, silaR.width * moc), silaR.height), h * .2f, BraxChampionsStanowisko.Ui.Zloto);
                BraxGui.ObrysZaokraglony(silaR, h * .2f, BraxChampionsStanowisko.Ui.Zloto);
                ui.Tekst(silaR, trzymaSila ? "PUŚĆ = UDERZ · " + Mathf.RoundToInt(moc * 100f) + "%" : moze ? "PRZYTRZYMAJ · UDERZ" : "CZEKAJ", .028f, trzymaSila ? Color.black : BraxChampionsStanowisko.Ui.Krem);
            }
        }
        if (!moze) return;
        // a finger on the cloth: the cue ball goes toward it
        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && !trzymaSila && ui.kam && !ui.dol.Contains(e.mousePosition) && !ui.gora.Contains(e.mousePosition))
        {
            var ray = ui.kam.ScreenPointToRay(new Vector3(e.mousePosition.x, Screen.height - e.mousePosition.y, 0f));
            var pl = new Plane(Vector3.up, Biala(t).position);
            if (pl.Raycast(ray, out float dist))
            {
                var d = ray.GetPoint(dist) - Biala(t).position; d.y = 0f;
                if (d.sqrMagnitude > r * r) kierunek = d.normalized;
            }
        }
        // the guide: to the first ball or cushion; from a ball the line it takes
        if (e.type == EventType.Repaint)
        {
            var c0 = Biala(t).position; var gold = new Color(1f, 1f, 1f, .85f);
            float daleko = 2f * (polX + polZ);
            Vector3 c1 = c0 + kierunek * daleko; Rigidbody trafiona = null;
            if (Physics.SphereCast(c0, r * .98f, kierunek, out var hit, daleko, ~0, QueryTriggerInteraction.Ignore))
            {
                c1 = c0 + kierunek * hit.distance;
                if (hit.rigidbody && hit.rigidbody != Biala(t)) trafiona = hit.rigidbody;
            }
            if (ui.Rzut(c0, out var A) && ui.Rzut(c1, out var B))
            {
                BraxChampionsStanowisko.Ui.Linia(A, B, gold, ui.s * .004f);
                if (trafiona)
                {
                    var bc = trafiona.position; var od = bc - c1; od.y = 0f;
                    if (od.sqrMagnitude > 1e-6f && ui.Rzut(bc, out var C) && ui.Rzut(bc + od.normalized * .35f, out var D)) BraxChampionsStanowisko.Ui.Linia(C, D, BraxChampionsStanowisko.Ui.Zloto, ui.s * .005f);
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (!gotowe || stol < 0 || !ruch) return;
        var c = stoly[stol]; float maxV = 0f;
        for (int i = 0; i < Bil; i++)
        {
            int k = stol * Bil + i; var b = bile[k]; if (!b || wbita[k] || b.isKinematic) continue;
            var pos = b.position; var d = pos - c;
            // in a pocket: the centre over the hole, or past the cushion nose line (only possible inside a pocket's jaws)
            bool wpadla = Mathf.Abs(d.x) > polX + r * .25f || Mathf.Abs(d.z) > polZ + r * .25f;
            if (!wpadla)
                foreach (var l in luzy) { var e = d - l; e.y = 0f; if (e.magnitude < promienLuzy) { wpadla = true; break; } }
            if (wpadla && Mathf.Abs(d.x) < polX + r * 6f && Mathf.Abs(d.z) < polZ + r * 6f) { Wpadla(k, i); continue; }
            // jumped the rail (off the table): back on the foot spot, as the rules say
            if (pos.y < c.y - r * 2.5f || wpadla) { ZaStol(k, i); continue; }
            // never let a ball skid forever: a ball on the cloth gives up its last centimetres per second
            var v = Predkosc(b); v.y = 0f; float s = v.magnitude; maxV = Mathf.Max(maxV, s);
            if (s < .02f && pos.y < c.y + r * 1.3f) { Predkosc(b, Vector3.zero); b.angularVelocity = Vector3.zero; }
        }
        if (maxV < .02f) { spokojOd += Time.fixedDeltaTime; if (spokojOd > .5f) Spokoj(); }
        else spokojOd = 0f;
    }

    void Wpadla(int k, int i)
    {
        var b = bile[k];
        Predkosc(b, Vector3.zero); b.angularVelocity = Vector3.zero; b.isKinematic = true;
        Widac(k, false); wbita[k] = true;
        if (i == 0) BraxChampionsGry.Pokaz("BIAŁA W ŁUZIE", 1.6f);
        else
        {
            wbite++;
            BraxChampionsGry.Pokaz(wbite >= 15 ? "WSZYSTKIE 15 · " + uderzenia + " UDERZEŃ" : "WBITA " + i + " · " + wbite + "/15", wbite >= 15 ? 4f : 1.4f);
        }
    }

    void ZaStol(int k, int i)
    {
        var b = bile[k];
        Predkosc(b, Vector3.zero); b.angularVelocity = Vector3.zero;
        var at = Wolne(start[i == 0 ? k : stol * Bil + 1]);
        b.position = at; b.transform.position = at;
        BraxChampionsGry.Pokaz(i == 0 ? "BIAŁA ZA STOŁEM" : "BILA " + i + " ZA STOŁEM · NA PUNKT", 1.4f);
    }
    // a free spot on the table's long axis, from p toward the head
    Vector3 Wolne(Vector3 at)
    {
        for (int tries = 0; tries < 30; tries++)
        {
            bool wolne = true;
            for (int i = 0; i < Bil; i++) { int k = stol * Bil + i; if (!wbita[k] && bile[k] && (bile[k].position - at).sqrMagnitude < 4.4f * r * r) { wolne = false; break; } }
            if (wolne) break;
            at.z -= 2.2f * r;
        }
        return at;
    }

    void Spokoj()
    {
        ruch = false;
        for (int i = 0; i < Bil; i++)
        {
            int k = stol * Bil + i; var b = bile[k]; if (!b || wbita[k]) continue;
            if (!b.isKinematic) { Predkosc(b, Vector3.zero); b.angularVelocity = Vector3.zero; b.Sleep(); }
        }
        // the cue ball back on the head spot (moved along the long axis until it is free)
        int cueK = stol * Bil;
        if (wbita[cueK])
        {
            var at = Wolne(start[cueK]);
            var cb = bile[cueK]; cb.position = at; cb.transform.position = at; Widac(cueK, true); wbita[cueK] = false;
            cb.isKinematic = false; Predkosc(cb, Vector3.zero);
            BraxChampionsGry.Pokaz("BIAŁA NA PUNKCIE", 1.2f);
        }
    }

    // ---- Unity 6 renamed velocity (linearVelocity)
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

static class BraxChampionsRigidbodyCompat
{
    // Unity 6: angularDrag -> angularDamping
    public static void angularDampingCompat(this Rigidbody b, float d)
    {
#if UNITY_6000_0_OR_NEWER
        b.angularDamping = d;
#else
        b.angularDrag = d;
#endif
    }
    // Unity 6: drag -> linearDamping
    public static void linearDampingCompat(this Rigidbody b, float d)
    {
#if UNITY_6000_0_OR_NEWER
        b.linearDamping = d;
#else
        b.drag = d;
#endif
    }
}
