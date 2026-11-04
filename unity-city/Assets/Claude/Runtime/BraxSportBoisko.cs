using UnityEngine;

// BRAXHORN — boisko z piłką (1.11, „cała rozrywka na osiedlu ma być użytkowa”). Jedno boisko = jeden komponent,
// stawiany przez generator osiedla (BraxClaudeOsiedleSport) z danymi: środek, oś, wymiary, kosze / bramki / siatka / wyrzutnia.
// Wszystko inne (piłki z teksturą malowaną w kodzie, kolizje obręczy, tablic, siatek i bramek, podłoga dla piłek) powstaje
// przy starcie. Sterowanie: AKCJA (BraxInterakcje) i CIOS / KOP (BraxGymPlayer.OnCios — chwila trafienia, piłka leci
// dokładnie wtedy, gdy ręka / noga jej dotyka).
//   KOSZ:      AKCJA = podnieś / puść piłkę; z piłką w ruchu kozioł; CIOS = rzut do najbliższego kosza przed Braxem
//              (wspomaganie celowania, pudło tym częstsze, im dalej); 2 pkt, zza 6.75 m 3 pkt.
//   PIŁKA:     Brax pcha piłkę ciałem; KOP = strzał (mocniej w biegu); CIOS = podanie po ziemi; piłka w bramce = GOL!
//   SIATKÓWKA: AKCJA przy leżącej piłce = podrzut do zagrywki; CIOS (piłka do 2.2 m) = odbicie łukiem nad siatką;
//              po drugiej stronie piłka wraca (ściana-partner); piłka na ziemi po stronie gracza = koniec akcji.
//   TENIS:     na korcie rakieta w dłoni; wyrzutnia co 3 s (AKCJA przy wyrzutni = wł./wył.); CIOS (piłka do 1.8 m) = return.
// Wydajność: gracz dalej niż 40 m — skrypt nic nie liczy, piłki śpią. Piłki na warstwie gracza (8): kamera ich nie omija,
// kapsuła gracza ich nie pcha (IgnoreCollision) — pchanie ciałem liczy skrypt.
public sealed class BraxSportBoisko : MonoBehaviour, IBraxInterakcja
{
    public enum Gra { Kosz, Nozna, Siatkowka, Tenis }
    public Gra gra;
    public float jednostkiNaMetr = 1f;
    public Vector3 srodek;                       // środek pola gry; y = nawierzchnia
    public Vector3 osX = Vector3.right;          // długa oś (siatkówka, tenis: prostopadle do siatki)
    public Vector2 pol = new Vector2(10, 5);     // półwymiary pola gry (wzdłuż osX, w poprzek)
    public Vector2 strefa = new Vector2(12, 7);  // półwymiary strefy „na boisku” (panel wyniku, rakieta, CIOS)
    public Vector3[] cele = new Vector3[0];       // kosz: środki obręczy; piłka: środki linii bramkowych; tenis: wyrzutnia
    public Vector3[] tablice = new Vector3[0];    // kosz: środki tablic
    public float siatkaDol = 1.43f, siatkaGora = 2.43f, siatkaPol = 3.5f;   // siatkówka (wysokości bezwzględne)
    public float bramkaSzer = 3f, bramkaWys = 1.5f, bramkaGl = 1f;

    public static readonly System.Collections.Generic.List<BraxSportBoisko> Wszystkie = new System.Collections.Generic.List<BraxSportBoisko>();
    public bool Na { get; private set; }        // gracz w strefie tego boiska (boiska mogą się nakładać: bramki na boisku do kosza)
    public float Ostatnio { get; private set; }  // ostatnie zagranie piłką tego boiska (panel pokazuje ostatnio używaną grę)
    const int Warstwa = 8;
    Vector3 osZ; float podloga, r;
    Rigidbody[] pilki = new Rigidbody[0]; SphereCollider[] kule = new SphereCollider[0];
    float[] poprzVy = new float[0], lezy = new float[0]; bool[] zwrocona = new bool[0], wGrze = new bool[0];
    BraxGymPlayer gracz; Transform gt;
    bool aktywne, gotowe; float sprawdz;
    Vector3 graczPoprz, graczPoprzF; float graczV; Vector3 graczVel; bool graczF;
    Transform kolizje;
    // kosz
    bool trzyma, liczony, rzut3; float faza, kozBlend;
    // piłka nożna
    float golDo;
    // siatkówka
    bool akcja; int strona = 1, ciag;
    // tenis
    bool wyrzutnia = true; float nastepny;

    public string Nazwa => gra == Gra.Kosz ? "KOSZ" : gra == Gra.Nozna ? "PIŁKA" : gra == Gra.Siatkowka ? "SIATKÓWKA" : "TENIS";
    public string Panel
    {
        get
        {
            switch (gra)
            {
                case Gra.Kosz: return "KOSZ · " + BraxSport.KoszPunkty + " PKT · " + BraxSport.KoszTrafione + "/" + BraxSport.KoszProby;
                case Gra.Nozna: return "PIŁKA · GOLE " + BraxSport.Gole;
                case Gra.Siatkowka: return akcja ? "SIATKÓWKA · CIĄG " + ciag : "SIATKÓWKA · NAJLEPSZY CIĄG " + BraxSport.SiatkaNajlepszy;
                default: return "TENIS · RETURNY " + BraxSport.Returny + (wyrzutnia ? "" : " · WYRZUTNIA WYŁ.");
            }
        }
    }

    void OnEnable() { BraxInterakcje.Dodaj(this); BraxGymPlayer.OnCios += Cios; if (!Wszystkie.Contains(this)) Wszystkie.Add(this); }
    void OnDisable()
    {
        BraxInterakcje.Usun(this); BraxGymPlayer.OnCios -= Cios; Wszystkie.Remove(this); Na = false;
        if (BraxSport.Aktywne == this) { BraxSport.Aktywne = null; BraxSport.Rakieta = false; }
    }

    // ---------------- przygotowanie ----------------
    void Start()
    {
        osX.y = 0f; if (osX.sqrMagnitude < 1e-4f) osX = Vector3.right; osX.Normalize();
        osZ = Vector3.Cross(osX, Vector3.up);
        kolizje = new GameObject("kolizje gry").transform; kolizje.SetParent(transform, false);
        // nawierzchnia dla piłek: jeśli pod boiskiem nie ma kolizji na wysokości nawierzchni — cienka płyta
        podloga = srodek.y;
        var probe = srodek + osX * pol.x * .5f + osZ * pol.y * .3f;
        bool jest = false;
        if (Physics.Raycast(new Vector3(probe.x, podloga + 3f, probe.z), Vector3.down, out var hit, 6f, ~((1 << 8) | (1 << 29)), QueryTriggerInteraction.Ignore))
            if (hit.point.y > podloga - .03f && hit.point.y < podloga + .25f) { jest = true; podloga = Mathf.Max(podloga, hit.point.y); }
        // tylko pole gry + 0.6 m (przy willi obok jest jacuzzi i basen — płyta nie może nad nie wchodzić)
        if (!jest) Kol(new Vector3(srodek.x, podloga - .1f, srodek.z), new Vector3((pol.x + .6f) * 2f, .2f, (pol.y + .6f) * 2f));
        switch (gra)
        {
            case Gra.Kosz:
                r = .12f * jednostkiNaMetr;
                foreach (var c in cele) for (int k = 0; k < 16; k++) { float a = k * Mathf.PI / 8f; KolKula(c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (.23f * jednostkiNaMetr), .02f * jednostkiNaMetr); }   // obręcz Ø 0.46 z 16 kul
                foreach (var b in tablice) Kol(b, new Vector3(.05f, 1.05f, 1.8f) * jednostkiNaMetr);
                for (int i = 0; i < cele.Length && i < tablice.Length; i++)   // mocowanie obręczy do tablicy
                {
                    var c = cele[i]; var b = tablice[i]; var mid = new Vector3((c.x + b.x) * .5f, c.y - .03f, (c.z + b.z) * .5f);
                    var dd = b - c; dd.y = 0f; float len = Mathf.Max(.05f, dd.magnitude - .23f * jednostkiNaMetr);
                    var mm = c + dd.normalized * (.23f * jednostkiNaMetr + len * .5f); mm.y = c.y - .03f;
                    KolObr(mm, new Vector3(.12f, .04f, len), Quaternion.LookRotation(dd.normalized));
                }
                Pilki(1, r, .6f, .82f, 0, "piłka do koszykówki");
                break;
            case Gra.Nozna:
                r = .11f * jednostkiNaMetr;
                foreach (var g in cele)
                {
                    var n = g - srodek; n.y = 0f; n.Normalize(); var s = Vector3.Cross(Vector3.up, n); var q = Quaternion.LookRotation(n);
                    float w = bramkaSzer, h = bramkaWys, d = bramkaGl;
                    foreach (int sg in new[] { -1, 1 })
                    {
                        KolObr(g + s * sg * w * .5f + n * .05f + Vector3.up * h * .5f, new Vector3(.1f, h, .1f), q);          // słupek
                        KolObr(g + s * sg * w * .5f + n * d * .5f + Vector3.up * h * .5f, new Vector3(.04f, h, d), q);        // siatka boczna
                    }
                    KolObr(g + n * .05f + Vector3.up * h, new Vector3(w, .1f, .1f), q);                                      // poprzeczka
                    KolObr(g + n * d + Vector3.up * h * .5f, new Vector3(w, h, .05f), q);                                    // siatka tylna
                    KolObr(g + n * d * .5f + Vector3.up * h, new Vector3(w, .04f, d), q);                                    // siatka górna
                }
                Pilki(1, r, .43f, .62f, 1, "piłka nożna");
                break;
            case Gra.Siatkowka:
                r = .105f * jednostkiNaMetr;
                Kol(new Vector3(srodek.x, (siatkaDol + siatkaGora) * .5f, srodek.z), new Vector3(.04f, siatkaGora - siatkaDol, siatkaPol * 2f));
                Pilki(1, r, .27f, .6f, 2, "piłka do siatkówki");
                break;
            default:
                r = .033f * jednostkiNaMetr;
                Pilki(3, r, .057f, .74f, 3, "piłka tenisowa");
                for (int i = 0; i < pilki.Length; i++) Schowaj(i);
                break;
        }
        if (gra != Gra.Tenis) for (int i = 0; i < pilki.Length; i++) Wroc(i, Srodek(i));
        gotowe = true;
    }

    Vector3 Srodek(int i) => new Vector3(srodek.x, podloga + r + .02f, srodek.z) + osZ * (i * .4f) + (gra == Gra.Siatkowka ? osX * pol.x * .5f : gra == Gra.Nozna ? osZ * 2.5f : Vector3.zero);

    void Kol(Vector3 c, Vector3 size) => KolObr(c, size, Quaternion.LookRotation(osZ));   // lokalne x = osX
    void KolObr(Vector3 c, Vector3 size, Quaternion q)
    {
        var g = new GameObject("kolizja"); g.layer = Warstwa; g.transform.SetParent(kolizje, false); g.transform.SetPositionAndRotation(c, q);
        g.AddComponent<BoxCollider>().size = size;
    }
    void KolKula(Vector3 c, float rr)
    {
        var g = new GameObject("kolizja obręczy"); g.layer = Warstwa; g.transform.SetParent(kolizje, false); g.transform.position = c;
        g.AddComponent<SphereCollider>().radius = rr;
    }

    void Pilki(int n, float rr, float masa, float odbicie, int tex, string nazwa)
    {
        pilki = new Rigidbody[n]; kule = new SphereCollider[n]; poprzVy = new float[n]; lezy = new float[n]; zwrocona = new bool[n]; wGrze = new bool[n];
        var mat = new PhysicsMaterial(nazwa) { bounciness = odbicie, dynamicFriction = .45f, staticFriction = .55f, bounceCombine = PhysicsMaterialCombine.Maximum, frictionCombine = PhysicsMaterialCombine.Average };
        Material wyglad = null;
        for (int i = 0; i < n; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = nazwa; go.layer = Warstwa;
            go.transform.SetParent(transform, true); go.transform.localScale = Vector3.one * rr * 2f;
            var mr = go.GetComponent<MeshRenderer>();
            if (!wyglad) wyglad = BraxSport.Mat(mr, Color.white, BraxSport.Tekstura(tex), tex == 3 ? .05f : .3f);
            mr.sharedMaterial = wyglad;
            var col = go.GetComponent<SphereCollider>(); col.sharedMaterial = mat;
            var rb = go.AddComponent<Rigidbody>(); rb.mass = masa; rb.linearDamping = 0f; rb.angularDamping = 1f;
            rb.interpolation = RigidbodyInterpolation.Interpolate; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            pilki[i] = rb; kule[i] = col;
            go.AddComponent<BraxOdbicie>().rodzaj = nazwa.ToLowerInvariant().Contains("tenis") ? 1 : 0;   // dźwięk odbicia (1.11)
        }
    }

    void Ignoruj()
    {
        if (!gracz || !gracz.body) return;
        foreach (var k in kule) if (k && k.enabled) Physics.IgnoreCollision(k, gracz.body, true);
    }

    void Wroc(int i, Vector3 gdzie)
    {
        var b = pilki[i]; if (!b) return;
        b.isKinematic = false; kule[i].enabled = true; b.GetComponent<Renderer>().enabled = true;
        b.position = gdzie; b.transform.position = gdzie; b.linearVelocity = Vector3.zero; b.angularVelocity = Vector3.zero;
        poprzVy[i] = 0f; lezy[i] = 0f; zwrocona[i] = false; Ignoruj();
    }

    // tenis: piłka wraca do wyrzutni (ukryta)
    void Schowaj(int i)
    {
        var b = pilki[i]; if (!b) return;
        if (!b.isKinematic) { b.linearVelocity = Vector3.zero; b.angularVelocity = Vector3.zero; }
        b.isKinematic = true; kule[i].enabled = false; b.GetComponent<Renderer>().enabled = false;
        var at = (cele.Length > 0 ? cele[0] : srodek) + Vector3.up * .5f; b.transform.position = at; b.position = at;
        wGrze[i] = false; zwrocona[i] = false; lezy[i] = 0f;
    }

    Vector2 Lok(Vector3 w) { var d = w - srodek; return new Vector2(Vector3.Dot(d, osX), Vector3.Dot(d, osZ)); }
    Vector3 Swiat(float lx, float lz, float y) { var p = srodek + osX * lx + osZ * lz; p.y = y; return p; }
    static Vector3 Balist(Vector3 p0, Vector3 cel, float T) => (cel - p0) / T - .5f * Physics.gravity * T;
    // wysokość toru nad siatką (lokalne x = 0); T rośnie, aż piłka przejdzie z zapasem
    Vector3 NadSiatka(Vector3 p0, Vector3 cel, float T, float minY)
    {
        Vector3 v = Balist(p0, cel, T);
        for (int k = 0; k < 12; k++)
        {
            v = Balist(p0, cel, T);
            float a = Lok(p0).x, b = Lok(cel).x; if (Mathf.Sign(a) == Mathf.Sign(b) || Mathf.Abs(a - b) < .01f) break;
            float t = a / (a - b) * T, y = p0.y + v.y * t + .5f * Physics.gravity.y * t * t;
            if (y > minY) break; T += .12f;
        }
        return v;
    }
    Vector3 Plasko(Vector3 v) { v.y = 0f; return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.zero; }

    // ---------------- pętla ----------------
    void Update()
    {
        if (!gotowe) return;
        if (Time.unscaledTime >= sprawdz)
        {
            sprawdz = Time.unscaledTime + .5f;
            gracz = BraxSport.Gracz; gt = gracz ? gracz.transform : null;
            // Nie włączaj sterowania grą podczas jazdy, na łodzi ani na siedzisku.
            // Po powrocie pieszo boisko samo ponownie aktywuje piłki i animacje.
            bool dostepny = gracz && !gracz.autoGracza && !gracz.vessel && !gracz.seat;
            bool a = false;
            if (dostepny && gt) { var d = gt.position - srodek; d.y = 0f; a = d.magnitude < 40f + Mathf.Max(strefa.x, strefa.y); }
            if (a && !aktywne) { foreach (var b in pilki) if (b && !b.isKinematic) b.WakeUp(); BraxRuchySport.Get(gracz); BraxSport.UpewnijHud(); graczPoprz = gt.position; }
            if (!a && aktywne) Uspij();
            aktywne = a;
            if (a) Ignoruj();
        }
        if (!aktywne || !gt) return;
        var p = gt.position; var dv = p - graczPoprz; dv.y = 0f;
        graczV = Mathf.Lerp(graczV, dv.magnitude / Mathf.Max(Time.deltaTime, 1e-4f), .25f); graczPoprz = p;
        var l = Lok(p);
        bool na = Mathf.Abs(l.x) < strefa.x && Mathf.Abs(l.y) < strefa.y && Mathf.Abs(p.y - podloga) < 3f;
        Na = na;
        if (na) { var ak = BraxSport.Aktywne; if (!ak || ak == this || !ak.Na || Ostatnio > ak.Ostatnio) BraxSport.Aktywne = this; }
        else if (BraxSport.Aktywne == this) BraxSport.Aktywne = null;
        if (gra == Gra.Kosz) TrzymanaPilka(na);
        if (gra == Gra.Tenis)
        {
            BraxSport.Rakieta = na;
            if (wyrzutnia && na && l.x > 0f && Mathf.Abs(l.y) < pol.y + 2f && Time.time > nastepny) { nastepny = Time.time + 3f; Wyrzut(); }
        }
    }

    void Uspij()
    {
        if (trzyma) Pusc();
        foreach (var b in pilki) if (b && !b.isKinematic) b.Sleep();
        Na = false; if (BraxSport.Aktywne == this) BraxSport.Aktywne = null;
        if (gra == Gra.Tenis) BraxSport.Rakieta = false;
        graczF = false; graczVel = Vector3.zero;
        akcja = false; ciag = 0;
    }

    void FixedUpdate()
    {
        if (!gotowe || !aktywne || !gt) return;
        // prędkość gracza w kroku fizyki (pchanie piłki ciałem)
        var gp = gt.position;
        graczVel = graczF ? (gp - graczPoprzF) / Mathf.Max(1e-4f, Time.fixedDeltaTime) : Vector3.zero; graczVel.y = 0f; graczPoprzF = gp; graczF = true;
        for (int i = 0; i < pilki.Length; i++)
        {
            var b = pilki[i]; if (!b || b.isKinematic) continue;
            var pos = b.position; var v = b.linearVelocity;
            bool przyZiemi = pos.y < podloga + r + .05f;
            b.linearDamping = przyZiemi ? (gra == Gra.Nozna ? .45f : .6f) : 0f;   // w locie bez oporu (tor zgodny z celowaniem), po ziemi toczy się i staje
            var l = Lok(pos);
            if (l.magnitude > (gra == Gra.Tenis ? 30f : 25f) || pos.y < podloga - 4f)
            { if (gra == Gra.Tenis) Schowaj(i); else { Wroc(i, Srodek(i)); akcja = false; ciag = 0; } continue; }
            switch (gra)
            {
                case Gra.Kosz:
                    if (!liczony && poprzVy[i] < 0f)
                        foreach (var c in cele)
                        {
                            float yPrev = pos.y - v.y * Time.fixedDeltaTime;
                            var h = pos - c; h.y = 0f;
                            if (yPrev > c.y && pos.y <= c.y && h.magnitude < .2f)
                            {
                                int pts = rzut3 ? 3 : 2; liczony = true;
                                BraxSport.KoszPunkty += pts; BraxSport.KoszTrafione++;
                                BraxSport.Pokaz("KOSZ! +" + pts, 1.6f); BraxSport.Punkt("KOSZ", pts);
                            }
                        }
                    break;
                case Gra.Nozna:
                    if (golDo > 0f) { if (Time.time > golDo) { golDo = 0f; Wroc(i, Srodek(i)); } break; }
                    foreach (var g in cele)
                    {
                        var n = Plasko(g - srodek); var s = Vector3.Cross(Vector3.up, n); var d = pos - g;
                        float ln = Vector3.Dot(d, n), ls = Vector3.Dot(d, s);
                        if (ln > r + .05f && ln < bramkaGl && Mathf.Abs(ls) < bramkaSzer * .5f && pos.y < g.y + bramkaWys) { golDo = Time.time + 1.8f; BraxSport.Gol(); break; }
                    }
                    Pchaj(i);
                    break;
                case Gra.Siatkowka:
                    if (akcja)
                    {
                        // druga strona: piłka opada — wraca do gracza (ściana-partner)
                        if (Mathf.Sign(l.x) == -strona && Mathf.Abs(l.x) > .3f && v.y < 0f && pos.y < podloga + 1.3f && Mathf.Abs(l.x) < pol.x + 3f && Mathf.Abs(l.y) < pol.y + 3f)
                        {
                            var cel = gp + Vector3.up * 1.6f + new Vector3(Random.Range(-.6f, .6f), 0f, Random.Range(-.6f, .6f));
                            var lc = Lok(cel); lc.x = strona * Mathf.Clamp(Mathf.Abs(lc.x), 1.2f, pol.x); lc.y = Mathf.Clamp(lc.y, -pol.y, pol.y);
                            cel = Swiat(lc.x, lc.y, cel.y);
                            b.linearVelocity = NadSiatka(pos, cel, 1.5f, siatkaGora + .45f);
                        }
                        else if (przyZiemi && poprzVy[i] < -.4f) { akcja = false; BraxSport.KoniecAkcji(ciag); ciag = 0; BraxSport.SiatkaCiag = 0; }
                    }
                    break;
                case Gra.Tenis:
                    if (zwrocona[i] && przyZiemi && poprzVy[i] < -.3f)
                    {
                        zwrocona[i] = false;
                        if (l.x < 0f && l.x > -pol.x - .05f && Mathf.Abs(l.y) < pol.y + .05f) BraxSport.Ret(); else BraxSport.Pokaz("AUT", .9f);
                    }
                    if (v.sqrMagnitude < .05f && pos.y < podloga + .25f) { if (lezy[i] == 0f) lezy[i] = Time.time; else if (Time.time - lezy[i] > 3f) Schowaj(i); }
                    else lezy[i] = 0f;
                    break;
            }
            poprzVy[i] = b.linearVelocity.y;
        }
    }

    // Kapsuła gracza nie pcha brył — piłka dostaje prędkość gracza w stronę od niego (jak BraxPilka na siłowni).
    void Pchaj(int i)
    {
        var b = pilki[i]; var d = b.position - gt.position; d.y = 0f;
        float rr = (gracz.body ? gracz.body.radius : .35f) + r + .05f;
        if (d.magnitude > rr || b.position.y > gt.position.y + 1.2f || d.sqrMagnitude < 1e-6f) return;
        var n = d.normalized; float naPilke = Vector3.Dot(graczVel, n);
        if (naPilke < .05f) return;
        var vh = b.linearVelocity; vh.y = 0f; float cel = Mathf.Min(naPilke * 1.25f + .35f, 7f);
        if (Vector3.Dot(vh, n) < cel) { b.WakeUp(); b.AddForce(n * (cel - Vector3.Dot(vh, n)), ForceMode.VelocityChange); Ostatnio = Time.time; }
    }

    // ---------------- koszykówka: trzymanie, kozioł, rzut ----------------
    void TrzymanaPilka(bool naBoisku)
    {
        if (!trzyma) { if (BraxSport.PilkaWRekach == (pilki.Length > 0 && pilki[0] ? pilki[0].transform : null)) { BraxSport.PilkaWRekach = null; BraxSport.Kozluje = false; } return; }
        var l = Lok(gt.position);
        if (Mathf.Abs(l.x) > strefa.x + 6f || Mathf.Abs(l.y) > strefa.y + 6f || gracz.seat || gracz.vessel) { Pusc(); return; }
        var b = pilki[0]; Vector3 F = Plasko(gt.forward), R = Vector3.Cross(Vector3.up, F), p = gt.position;
        float biodra = p.y + .98f * jednostkiNaMetr;
        kozBlend = Mathf.MoveTowards(kozBlend, graczV > .35f && BraxSport.RzutFaza < .05f ? 1f : 0f, Time.deltaTime * 4f);
        faza += Mathf.Max(graczV, 1.2f * kozBlend) * Time.deltaTime / 1.35f;           // jedno odbicie na ok. 1.35 m (dwa kroki)
        float h = podloga + r + (biodra - podloga - r) * Mathf.Abs(Mathf.Cos(faza * Mathf.PI));
        var trzym = p + F * (.42f * jednostkiNaMetr) + Vector3.up * (1.02f * jednostkiNaMetr);
        var koz = p + F * (.42f * jednostkiNaMetr) + R * (.3f * jednostkiNaMetr); koz.y = h;
        var cel = Vector3.Lerp(trzym, koz, kozBlend);
        cel = Vector3.Lerp(cel, p + F * (.25f * jednostkiNaMetr) + Vector3.up * (2.2f * jednostkiNaMetr), BraxSport.RzutFaza);   // przed rzutem nad głowę
        b.transform.position = cel;
        b.transform.Rotate(R, graczV * Time.deltaTime * 330f, Space.World);
        BraxSport.PilkaWRekach = b.transform; BraxSport.Kozluje = kozBlend > .5f;
    }

    void Podnies()
    {
        var b = pilki[0]; trzyma = true; liczony = true; Ostatnio = Time.time;
        b.linearVelocity = Vector3.zero; b.angularVelocity = Vector3.zero;
        b.isKinematic = true; b.interpolation = RigidbodyInterpolation.None; kule[0].enabled = false;
        BraxSport.PilkaWRekach = b.transform; kozBlend = 0f;
    }

    void Pusc(Vector3? predkosc = null)
    {
        var b = pilki[0]; trzyma = false;
        b.isKinematic = false; b.interpolation = RigidbodyInterpolation.Interpolate; kule[0].enabled = true; Ignoruj();
        b.position = b.transform.position;
        b.linearVelocity = predkosc ?? (gt ? Plasko(gt.forward) * graczV * .8f : Vector3.zero);
        BraxSport.PilkaWRekach = null; BraxSport.Kozluje = false;
    }

    void Rzut()
    {
        var b = pilki[0]; Vector3 F = Plasko(gt.forward), R = Vector3.Cross(Vector3.up, F);
        var p0 = b.transform.position;
        int best = -1; float bd = 1e9f;
        for (int i = 0; i < cele.Length; i++)
        {
            var to = cele[i] - gt.position; to.y = 0f; float d = to.magnitude;
            if (d > .3f && Vector3.Dot(F, to / d) < .25f) continue;
            if (d < bd) { bd = d; best = i; }
        }
        Vector3 v;
        BraxSport.KoszProby++; liczony = false; rzut3 = false; Ostatnio = Time.time;
        if (best < 0) v = F * 5.5f + Vector3.up * 4.5f;   // żadnego kosza przed Braxem — rzut przed siebie
        else
        {
            var c = cele[best]; var hz = c - gt.position; hz.y = 0f; float d = hz.magnitude;
            rzut3 = d > 6.75f;
            float szansa = Mathf.Clamp(.9f - .065f * d, .2f, .88f);
            var cel = c + Vector3.up * .03f;
            if (Random.value < szansa) cel += new Vector3(Random.Range(-.015f, .015f), 0f, Random.Range(-.015f, .015f));
            else { float a = Random.value * Mathf.PI * 2f; cel += new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Random.Range(.2f, .36f); }   // pudło: w obręcz / tablicę
            float T = Mathf.Clamp(.8f + .09f * d, .85f, 1.6f);   // wysoki łuk (kąt wejścia ok. 50°)
            v = Balist(p0, cel, T);
        }
        Pusc(v);
        b.angularVelocity = -R * 9f;   // rotacja wsteczna
    }

    // ---------------- tenis: wyrzutnia ----------------
    void Wyrzut()
    {
        int i = -1;
        for (int k = 0; k < pilki.Length; k++) if (!wGrze[k]) { i = k; break; }
        if (i < 0) return;   // wszystkie w grze — następna za 3 s
        var p0 = (cele.Length > 0 ? cele[0] : srodek - osX * (pol.x + 1.5f)) + Vector3.up * .82f + osX * .52f;   // wylot rury wyrzutni
        var lp = Lok(gt.position);
        float lx = Mathf.Clamp(lp.x - 2.6f, 1.5f, pol.x - .4f), lz = Mathf.Clamp(lp.y + Random.Range(-1f, 1f), -pol.y + .3f, pol.y - .3f);
        var cel = Swiat(lx, lz, podloga + r);
        var b = pilki[i];
        b.isKinematic = false; kule[i].enabled = true; b.GetComponent<Renderer>().enabled = true;
        b.position = p0; b.transform.position = p0; b.angularVelocity = Vector3.zero;
        b.linearVelocity = NadSiatka(p0, cel, 1.1f, .914f + .3f);
        wGrze[i] = true; zwrocona[i] = false; lezy[i] = 0f; poprzVy[i] = 0f; Ignoruj();
    }

    // ---------------- CIOS / KOP (chwila kontaktu) ----------------
    void Cios(Vector3 p, Vector3 przod, bool kop)
    {
        if (!gotowe || !aktywne || !gt || !Na) return;
        Vector3 F = Plasko(przod); if (F == Vector3.zero) return;
        var R = Vector3.Cross(Vector3.up, F);
        switch (gra)
        {
            case Gra.Kosz:
                if (trzyma && !kop) Rzut();
                break;
            case Gra.Nozna:
                {
                    if (!kop && BraxSport.PilkaWRekach) return;   // z piłką do kosza w rękach CIOS = rzut, nie podanie
                    var b = pilki[0]; var d = b.position - p; d.y = 0f; float dist = d.magnitude;
                    if (dist > 1.5f || b.position.y > p.y + .9f || (dist > .2f && Vector3.Dot(F, d / dist) < .3f)) return;
                    float bieg = Mathf.Clamp01(graczV / 5f);
                    b.WakeUp();
                    b.linearVelocity = kop ? F * (9f + 8f * bieg) + Vector3.up * (2.4f + 2f * bieg) : F * 6.5f + Vector3.up * .4f;
                    b.angularVelocity = R * (kop ? 25f : 15f); Ostatnio = Time.time;
                }
                break;
            case Gra.Siatkowka:
                {
                    if (kop) return;
                    var b = pilki[0]; var pos = b.position;
                    if ((pos - (p + Vector3.up * 1.25f)).magnitude > 2.2f || pos.y < podloga + r + .12f) return;
                    var lp = Lok(p); strona = lp.x >= 0f ? 1 : -1;
                    float lx = -strona * Random.Range(2f, pol.x - .7f), lz = Mathf.Clamp(Lok(pos).y * .5f + Random.Range(-1.5f, 1.5f), -pol.y + .6f, pol.y - .6f);
                    b.WakeUp();
                    b.linearVelocity = NadSiatka(pos, Swiat(lx, lz, podloga + r), 1.3f, siatkaGora + .35f);
                    b.angularVelocity = R * 6f;
                    akcja = true; ciag++; BraxSport.SiatkaCiag = ciag; BraxSport.Punkt("SIATKÓWKA", 1); Ostatnio = Time.time;
                }
                break;
            case Gra.Tenis:
                {
                    if (kop) return;
                    int best = -1; float bd = 1.8f; var reka = p + Vector3.up * 1.0f + F * .3f;
                    for (int i = 0; i < pilki.Length; i++) { if (!wGrze[i]) continue; float d = (pilki[i].position - reka).magnitude; if (d < bd) { bd = d; best = i; } }
                    if (best < 0) return;
                    var b = pilki[best]; var pos = b.position; var lp = Lok(p);
                    float sgn = lp.x >= 0f ? 1f : -1f;   // return na drugą stronę siatki
                    float lx = -sgn * Random.Range(5.5f, pol.x - 1.2f);
                    float lz = Mathf.Clamp(lp.y * .3f + Vector3.Dot(F, osZ) * 4f + Random.Range(-1f, 1f), -pol.y + .7f, pol.y - .7f);   // lekkie celowanie kierunkiem Braxa
                    b.linearVelocity = NadSiatka(pos, Swiat(lx, lz, podloga + r), .95f, .914f + .3f);
                    b.angularVelocity = R * 30f;
                    zwrocona[best] = true; lezy[best] = 0f; Ostatnio = Time.time;
                }
                break;
        }
    }

    // Ruch postaci do zagrania (pytanie BraxRuchySport w chwili startu ciosu)
    public BraxRuch Zamiar(Vector3 p, Vector3 przod)
    {
        if (!gotowe || pilki.Length == 0) return BraxRuch.Brak;
        var F = Plasko(przod); var R = Vector3.Cross(Vector3.up, F);
        switch (gra)
        {
            case Gra.Kosz: return trzyma ? BraxRuch.Rzut : BraxRuch.Brak;
            case Gra.Nozna:
                {
                    if (BraxSport.PilkaWRekach) return BraxRuch.Brak;
                    var d = pilki[0].position - p; d.y = 0f;
                    return d.magnitude < 1.7f && Vector3.Dot(F, d.normalized) > .2f ? BraxRuch.Podanie : BraxRuch.Brak;
                }
            case Gra.Siatkowka:
                {
                    var pos = pilki[0].position;
                    if ((pos - (p + Vector3.up * 1.25f)).magnitude > 3.2f) return BraxRuch.Brak;
                    return pos.y - p.y > 1.55f ? BraxRuch.OdbicieGorne : BraxRuch.OdbicieDolne;
                }
            default:
                {
                    int best = -1; float bd = 4f;
                    for (int i = 0; i < pilki.Length; i++) { if (!wGrze[i]) continue; float d = (pilki[i].position - p).magnitude; if (d < bd) { bd = d; best = i; } }
                    if (best < 0) return BraxRuch.Forhend;
                    return Vector3.Dot(pilki[best].position - p, R) >= 0f ? BraxRuch.Forhend : BraxRuch.Bekhend;
                }
        }
    }

    // ---------------- AKCJA ----------------
    public string Kontekst(Vector3 g)
    {
        if (!gotowe || !aktywne || pilki.Length == 0) return "";
        switch (gra)
        {
            case Gra.Kosz:
                {
                    if (trzyma) return "PUŚĆ PIŁKĘ";
                    var d = pilki[0].position - g; float y = d.y; d.y = 0f;
                    return d.magnitude < 1.7f && y < 1.6f && pilki[0].linearVelocity.sqrMagnitude < 25f ? "PODNIEŚ PIŁKĘ" : "";
                }
            case Gra.Siatkowka:
                {
                    if (akcja) return "";
                    var b = pilki[0]; var d = b.position - g; d.y = 0f;
                    return d.magnitude < 2f && b.position.y < podloga + .5f && b.linearVelocity.sqrMagnitude < 1f ? "ZAGRYWKA" : "";
                }
            case Gra.Tenis:
                {
                    if (cele.Length == 0) return "";
                    var d = cele[0] - g; d.y = 0f;
                    return d.magnitude < 2.6f ? (wyrzutnia ? "WYRZUTNIA: WYŁĄCZ" : "WYRZUTNIA: WŁĄCZ") : "";
                }
        }
        return "";
    }

    public bool Wykonaj(Vector3 g)
    {
        switch (gra)
        {
            case Gra.Kosz: if (trzyma) Pusc(); else Podnies(); return true;
            case Gra.Siatkowka:
                {
                    // podrzut do zagrywki: piłka nad głowę — CIOS ją odbija
                    var b = pilki[0]; var F = Plasko(gt.forward);
                    var at = gt.position + F * .4f + Vector3.up * 1.5f;
                    b.WakeUp(); b.position = at; b.transform.position = at;
                    b.linearVelocity = Vector3.up * 3.6f + F * .15f; b.angularVelocity = Vector3.zero;
                    ciag = 0; akcja = false; strona = Lok(gt.position).x >= 0f ? 1 : -1;
                    return true;
                }
            case Gra.Tenis:
                wyrzutnia = !wyrzutnia; nastepny = Time.time + 1.5f;
                BraxSport.Pokaz(wyrzutnia ? "WYRZUTNIA WŁĄCZONA" : "WYRZUTNIA WYŁĄCZONA", 1.2f);
                return true;
        }
        return false;
    }
}
