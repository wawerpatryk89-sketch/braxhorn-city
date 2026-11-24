using System;
using UnityEngine;

// A QUAD BIKE OF THE BRAXHORN QUAD TRACK (owner 24.11.2026 11:31-11:45: "tu są kłady ... tor do kładów", "to są całe koła, to
// chyba trzeba będzie zrobić, żeby się kręciły, kierownica, żeby się kręciła tak samo", "minimum 4 kłady ... każdy inny kolor").
// The owner's two Tripo quads, cut into a body, four wheels and the handlebar (unity-tools/mistrzowie/klady), built by
// BraxChampionsKlady at the track's rental booth.
//
// AKCJA beside a parked quad = WSIĄDŹ: the rider sits astride (BraxPose.Ride, the jet ski's pose), hands on the bars, and rides
// with the quad; the walking controls rest (as at the casino's games) and the quad's own touch controls take the fingers -
// the left thumb steers (drag left / right), GAZ and HAMULEC under the right thumb, ZSIĄDŹ at the top; on a desktop W/S/A/D or
// the arrows, E to get off, R to set the quad on its wheels.
// The ride is a real rigid body on four ray-cast wheels (the way the racing games do it): a spring and a damper under each
// wheel (the sag and the travel of the real machine), each wheel's grip limited by the load on it, the rear (or all four)
// driven, the front steered; in the air it flies on gravity x K (the world is K times the real one, so a jump keeps the
// real machine's flight), the throttle lifts the nose and the brake drops it. The wheels roll at the ground's speed, the
// front ones turn with the bars, the hubs rise and fall with the suspension.
// The race's rivals (BraxChampionsKladRywal) ride the same machines through the same physics: their inputs come from the AI
// (Wejscie) instead of the fingers; a rival's quad is never offered to the player.
[DisallowMultipleComponent]
[DefaultExecutionOrder(5100)]                                   // the camera after the player's own and the casino seat's
public sealed class BraxChampionsKlad : MonoBehaviour, IBraxInterakcja
{
    // ---------------------------------------------------------------- set by the builder (BraxChampionsKlady)
    public string nazwa = "KŁAD";                // the AKCJA label: "KŁAD · CZERWONY"
    public float K = 1f;                         // world metres per real metre
    public Transform[] piasty = new Transform[4];  // the hubs FL, FR, RL, RR (the suspension moves them; the front ones steer)
    public Transform[] kola = new Transform[4];    // the spinning wheels (children of the hubs)
    public Vector3[] spoczynek = new Vector3[4];   // the hubs' rest positions, root-local (the model's own stance)
    public Transform kierownica;                 // the handlebar's pivot on the steering axis
    public Vector3 osKierownicy = Vector3.up;    // the steering axis in the pivot's parent space
    public Quaternion kierownicaSpoczynek = Quaternion.identity;
    public Transform siodlo;                     // where the rider's hips sit (root-local point on the seat)
    public BoxCollider korpus;
    public float promien = .30f;                 // the tyre's radius, real metres
    public float masa = 330f;                    // machine + rider, kg
    public float vMaxKmh = 80f;                  // real top speed
    public float przyspieszenie = 3.6f;          // real launch acceleration, m/s2
    public bool naprzod4x4;                      // all four wheels driven (the utility quad)
    public float skok = .22f;                    // suspension travel, real metres (the sag is 40 % of it)
    public float maxSkret = 32f;                 // front wheels' lock at walking pace, degrees (at top speed 30 % of it)
    public float przyczepnosc = 1.05f;           // tyre friction on packed dirt (arcade, the way the racing games tune it)
    public AudioClip silnikWolne, silnikJazda;   // real recordings (CC0 BigSoundBank #711 / #713, Audio/Klad)
    public BraxChampionsTorCzas tor;             // the lap timer of the track (optional)
    public bool tylkoRywal;                      // a race rival's machine (its rider sits on it), never the player's

    // ---------------------------------------------------------------- state
    public static BraxChampionsKlad Jedzie { get; private set; }      // the quad the player rides (one at a time)
    public bool Zajety => gracz;
    public bool Rywal { get; private set; }                          // ridden by the AI now
    public bool NaZiemi { get; private set; }                        // a wheel on the ground at the last physics step
    public bool ZGraczem => gracz;
    public float PredkoscKmh => rb ? Vector3.Dot(Vel, transform.forward) * 3.6f / Mathf.Max(.01f, K) : 0f;

    Rigidbody rb;
    // Unity 6 renamed velocity (linearVelocity), as BraxChampionsBilard
    Vector3 Vel
    {
        get
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }
        set
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = value;
#else
            rb.velocity = value;
#endif
        }
    }
    BraxGymPlayer gracz; BraxSeat miejsce; BraxTouchControls tc; bool symulacja;
    Transform rodzicGracza; Vector3 przedPoz; Quaternion przedRot;
    float gaz, hamulec, skretWej, skret, obrot, odWsiadania, doGory, stoi;
    readonly bool[] naZiemi = new bool[4];
    readonly float[] kompresja = new float[4];
    readonly Vector3[] punktKontaktu = new Vector3[4];
    readonly Vector3[] normalnaKontaktu = new Vector3[4];
    readonly RaycastHit[] trafienia = new RaycastHit[12];
    AudioSource wolne, jazda;
    Camera kam; Vector3 kamPoz, kamKier; bool kamOk;
    const int WarstwaGracza = 8;

    // ---------------------------------------------------------------- freestyle (owner 24.11 12:57: "rampy po bokach")
    // In the air (a jump of the track or a kicker of the freestyle zone) OBRÓT turns the quad a flat 360 and SALTO a backflip
    // (the FMX quad riders' trick); a clean landing (on its wheels, under 44 degrees of tilt) scores, a trick still turning
    // when the wheels or the body meet the ground is a crash and the jump scores nothing.
    enum Trik { Brak, Obrot, Salto }
    public const int PunktyObrot = 150, PunktySalto = 400;
    public const float CzasObrotu = .9f, CzasSalta = 1.05f, Kombo = 1.5f, PunktyZaSekunde = 40f;
    const string KluczRekordu = "BRAX_KLAD_REKORD_SKOKU";
    Trik trik; float trikKat, trikW, lot, vKol; int trikKroki, trikiSkoku, punktySkoku; Vector3 trikOs;
    bool chceObrot, chceSalto, rozbity; string nazwyTrikow = "";
    string napis = ""; float napisDo; Color napisKolor = Color.white;
    public int Punkty { get; private set; }                          // this ride's points
    static int rekordSkoku = -1;                                      // the best single jump, kept on the phone
    public static int RekordSkoku { get { if (rekordSkoku < 0) { try { rekordSkoku = PlayerPrefs.GetInt(KluczRekordu, 0); } catch { rekordSkoku = 0; } } return rekordSkoku; } }
    bool MozeTrik => rb && !rb.isKinematic && lot > .15f && trik == Trik.Brak && !rozbity;

    // ---------------------------------------------------------------- life
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.mass = masa; rb.isKinematic = true; rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.angularDampingCompat(.6f); rb.linearDampingCompat(.02f);
        rb.centerOfMass = new Vector3(0f, .42f * K, -.05f * K);    // low: the engine and the rider's hips
        rb.maxAngularVelocity = 9f;
        Dzwiek();
    }
    void OnEnable() { BraxInterakcje.Dodaj(this); }
    void OnDisable()
    {
        BraxInterakcje.Usun(this);
        if (gracz) Zsiadz(true);
    }

    // ---------------------------------------------------------------- AKCJA
    public string Kontekst(Vector3 p)
    {
        if (gracz || Jedzie || tylkoRywal) return "";
        var d = p - transform.position; d.y = 0f;
        if (d.sqrMagnitude > (2.6f * K) * (2.6f * K) || Mathf.Abs(p.y - transform.position.y) > 2.2f * K) return "";
        return "WSIĄDŹ · " + nazwa;
    }
    public bool Wykonaj(Vector3 p)
    {
        if (string.IsNullOrEmpty(Kontekst(p))) return false;
        var g = BraxChampionsGry.Gracz;
        if (!g) return false;
        Wsiadz(g);
        return true;
    }

    void Wsiadz(BraxGymPlayer g)
    {
        gracz = g; Jedzie = this; odWsiadania = Time.time;
        // the seat that tells the player's controller he sits (the walking stops, AKCJA would stand him up): never offered
        // to anyone (its object is not active, so it is not in BraxSeat.All)
        if (!miejsce)
        {
            var o = new GameObject("KŁAD | miejsce kierowcy"); o.SetActive(false);
            o.transform.SetParent(siodlo ? siodlo : transform, false);
            miejsce = o.AddComponent<BraxSeat>(); miejsce.label = nazwa;
        }
        przedPoz = g.transform.position; przedRot = g.transform.rotation; rodzicGracza = g.transform.parent;
        // the hips over the saddle: measured standing, as BraxSeat does (hips ~53 % of the height, ~5.5 % over the seat)
        float biodra = .95f;                               // Brax's standing hips over his feet (BraxSeat.hipsAboveRoot)
        var kosc = Biodra(g);
        if (kosc) biodra = Mathf.Clamp(kosc.position.y - g.transform.position.y, .5f, 1.8f);
        float wzrost = biodra / .53f, nad = .055f * wzrost;
        g.MoveInput("0,0");
        g.seat = miejsce;
        if (g.body) g.body.enabled = false;
        var s = siodlo ? siodlo : transform;
        g.transform.SetParent(s, true);
        g.transform.SetPositionAndRotation(s.position + transform.up * (nad - biodra) - transform.forward * .04f * K, transform.rotation);
        BraxPose.Get(g).Set(BraxPose.Mode.Ride, transform);
        if (g.clips && g.clips["Idle"] != null) { g.clips["Idle"].time = 0f; g.clips["Idle"].speed = 0f; }
        // the walking controls rest; the quad's buttons take the fingers
        tc = FindAnyObjectByType<BraxTouchControls>();
        if (tc && tc.enabled) tc.enabled = false; else tc = null;
        symulacja = Input.simulateMouseWithTouches; Input.simulateMouseWithTouches = false;
        kam = g.view ? g.view : Camera.main; kamOk = false;
        rb.isKinematic = false; Vel = Vector3.zero; rb.angularVelocity = Vector3.zero;
        if (wolne && silnikWolne) { wolne.Play(); }
        if (jazda && silnikJazda) { jazda.Play(); }
        if (tor) tor.Zacznij(this);
        Debug.Log("BRAX_KLAD wsiada " + nazwa + " hips=" + biodra.ToString("F2"));
    }

    static Transform Biodra(BraxGymPlayer p)
    {
        var v = p.visual ? p.visual : p.transform;
        foreach (var t in v.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name; int i = n.LastIndexOf(':'); if (i >= 0) n = n.Substring(i + 1);
            if (n == "Hips") return t;
        }
        return null;
    }

    /// <summary>Off the quad, at its left side on the ground (force: wherever it is, the quad being switched off).</summary>
    public bool Zsiadz(bool force = false)
    {
        if (!gracz) return false;
        if (!force && Mathf.Abs(PredkoscKmh) > 8f) return false;
        var g = gracz; gracz = null; if (Jedzie == this) Jedzie = null;
        if (tor) tor.Zakoncz(this);
        BraxPose.Get(g).Set(BraxPose.Mode.None, null);
        if (g.clips && g.clips["Idle"] != null) g.clips["Idle"].speed = 1f;
        g.transform.SetParent(rodzicGracza, true);
        var bok = transform.position - Flat(transform.right) * 1.25f * K;
        var p = bok + Vector3.up * 2.5f * K;
        Vector3 cel = force && !isActiveAndEnabled ? przedPoz : bok;
        if (Physics.Raycast(p, Vector3.down, out var hit, 6f * K, ~(1 << WarstwaGracza), QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(transform))
            cel = hit.point + Vector3.up * .05f;
        g.transform.SetPositionAndRotation(cel, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
        g.seat = null;
        if (g.body) { g.body.enabled = false; g.body.enabled = true; }
        if (tc) { tc.enabled = true; } tc = null;
        Input.simulateMouseWithTouches = symulacja;
        if (!rb.isKinematic) { Vel = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        rb.isKinematic = true;
        if (wolne) wolne.Stop(); if (jazda) jazda.Stop();
        gaz = hamulec = skretWej = 0f;
        trik = Trik.Brak; lot = 0f; Zeruj(); Punkty = 0;
        Debug.Log("BRAX_KLAD zsiada " + nazwa);
        return true;
    }

    /// <summary>R / POSTAW KŁAD: back on its wheels where it lies, facing the way it went.</summary>
    public void Postaw()
    {
        var f = Flat(transform.forward); if (f.sqrMagnitude < .01f) f = Flat(transform.up);
        if (!rb.isKinematic) { Vel = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        rb.position = transform.position + Vector3.up * .8f * K;
        rb.rotation = Quaternion.LookRotation(f, Vector3.up);
        doGory = stoi = 0f;
        trik = Trik.Brak; lot = 0f; Zeruj();
        Debug.Log("BRAX_KLAD postawiony " + nazwa);
    }

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.zero; }

    // ---------------------------------------------------------------- input (touch, mouse, keys)
    int palecSkretu = -99, palecGazu = -99, palecHamulca = -99; float skretX0;
    Rect rGaz, rHamulec, rZsiadz, rPostaw, rSkret, rObrot, rSalto;
    float jednostka => Mathf.Max(1f, Screen.height / 720f);

    void Uklad()
    {
        float u = jednostka, w = Screen.width, h = Screen.height;
        rGaz = new Rect(w - 210f * u, h - 250f * u, 180f * u, 220f * u);
        rHamulec = new Rect(w - 400f * u, h - 170f * u, 160f * u, 140f * u);
        rZsiadz = new Rect(20f * u, 20f * u, 190f * u, 70f * u);
        rPostaw = new Rect(w * .5f - 130f * u, 110f * u, 260f * u, 70f * u);
        rSkret = new Rect(0f, h * .35f, w * .5f, h * .65f);
        // the tricks over the right thumb's pedals, shown in the air only
        rSalto = new Rect(w - 210f * u, h - 420f * u, 180f * u, 150f * u);
        rObrot = new Rect(w - 400f * u, h - 340f * u, 160f * u, 150f * u);
    }
    static Vector2 Gui(Vector2 ekran) => new Vector2(ekran.x, Screen.height - ekran.y);

    void Sterowanie()
    {
        Uklad();
        float g = 0f, b = 0f; float s = 0f; bool trzymaSkret = false;
        bool zsiadz = false, postaw = false;
        int n = Input.touchCount;
        for (int i = -1; i < n; i++)
        {
            int id; Vector2 p; TouchPhase faza;
            if (i < 0)
            {
                if (n > 0 || !(Input.GetMouseButton(0) || Input.GetMouseButtonUp(0))) continue;   // the mouse = one finger (editor)
                id = -1; p = Gui(Input.mousePosition);
                faza = Input.GetMouseButtonDown(0) ? TouchPhase.Began : Input.GetMouseButtonUp(0) ? TouchPhase.Ended : TouchPhase.Moved;
            }
            else { var t = Input.GetTouch(i); id = t.fingerId; p = Gui(t.position); faza = t.phase; }
            bool koniec = faza == TouchPhase.Ended || faza == TouchPhase.Canceled;
            if (faza == TouchPhase.Began)
            {
                if (rZsiadz.Contains(p)) { zsiadz = true; continue; }
                if (rPostaw.Contains(p) && Przewrocony) { postaw = true; continue; }
                if (MozeTrik && rSalto.Contains(p)) { chceSalto = true; continue; }
                if (MozeTrik && rObrot.Contains(p)) { chceObrot = true; continue; }
                if (rGaz.Contains(p)) palecGazu = id;
                else if (rHamulec.Contains(p)) palecHamulca = id;
                else if (rSkret.Contains(p)) { palecSkretu = id; skretX0 = p.x; }
            }
            if (id == palecGazu) { if (koniec) palecGazu = -99; else g = 1f; }
            if (id == palecHamulca) { if (koniec) palecHamulca = -99; else b = 1f; }
            if (id == palecSkretu)
            {
                if (koniec) palecSkretu = -99;
                else { s = Mathf.Clamp((p.x - skretX0) / (Screen.width * .11f), -1f, 1f); trzymaSkret = true; }
            }
        }
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) g = 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) b = 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) { s = -1f; trzymaSkret = true; }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) { s = 1f; trzymaSkret = true; }
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Escape)) zsiadz = true;
        if (Input.GetKeyDown(KeyCode.R)) postaw = true;
        if (MozeTrik && Input.GetKeyDown(KeyCode.Q)) chceObrot = true;
        if (MozeTrik && Input.GetKeyDown(KeyCode.Space)) chceSalto = true;
        gaz = g; hamulec = b; skretWej = trzymaSkret ? s : 0f;
        if (zsiadz && Time.time - odWsiadania > .5f) Zsiadz();
        if (postaw) Postaw();
    }

    bool Przewrocony => transform.up.y < .35f && trik == Trik.Brak;

    // ---------------------------------------------------------------- the ride
    void Update()
    {
        if (!gracz) { if (Rywal) { Kierownica(); Silnik(); } return; }
        // the player was taken off some other way (the game reset him): the quad lets go of him
        if (!gracz.transform.IsChildOf(transform) || gracz.seat != miejsce) { var g = gracz; gracz = null; if (Jedzie == this) Jedzie = null; rb.isKinematic = true; if (tc) tc.enabled = true; tc = null; return; }
        if (gracz.clips && gracz.clips["Idle"] != null) gracz.clips["Idle"].speed = 0f;
        Sterowanie();
        Kierownica();
        // over / stuck
        doGory = Przewrocony ? doGory + Time.deltaTime : 0f;
        if (doGory > 2.5f) Postaw();
        if (transform.position.y < -60f * K) { Postaw(); rb.position = tor ? tor.PunktStartu : transform.position + Vector3.up * 80f * K; }
        Silnik();
    }

    // the bars follow the thumb (full lock in ~.3 s, back in ~.2 s); the lock closes with the speed
    public float Skret(float v01) => maxSkret * Mathf.Lerp(1f, .3f, Mathf.Clamp01(v01));
    void Kierownica()
    {
        float lockDeg = Skret(Mathf.Abs(PredkoscKmh) / Mathf.Max(1f, vMaxKmh));
        skret = Mathf.MoveTowards(skret, skretWej * lockDeg, (skretWej == 0f ? 160f : 110f) * Time.deltaTime);
    }

    // ---------------------------------------------------------------- the race (BraxChampionsWyscig, BraxChampionsKladRywal)
    /// <summary>The AI rides it (true: a dynamic body, the engine on, heard in 3D) or it is parked (false).</summary>
    public void Sam(bool jedzie)
    {
        if (gracz) return;
        Rywal = jedzie; gaz = hamulec = skretWej = 0f;
        if (wolne) { wolne.spatialBlend = 1f; if (jedzie && silnikWolne) wolne.Play(); else wolne.Stop(); }
        if (jazda) { jazda.spatialBlend = 1f; if (jedzie && silnikJazda) jazda.Play(); else jazda.Stop(); }
        if (!jedzie && !rb.isKinematic) { Vel = Vector3.zero; rb.angularVelocity = Vector3.zero; }   // (a kinematic body takes no velocity)
        rb.isKinematic = !jedzie;
    }
    /// <summary>The AI's hands: throttle, brake (and reverse), the bars (-1 .. 1 of the lock).</summary>
    public void Wejscie(float g, float b, float s) { if (gracz) return; gaz = Mathf.Clamp01(g); hamulec = Mathf.Clamp01(b); skretWej = Mathf.Clamp(s, -1f, 1f); }
    /// <summary>Held still (the start gate's countdown) or let go.</summary>
    public void Trzymaj(bool stoj)
    {
        if (stoj) { if (!rb.isKinematic) { Vel = Vector3.zero; rb.angularVelocity = Vector3.zero; } rb.isKinematic = true; gaz = hamulec = 0f; }
        else if (gracz || Rywal) rb.isKinematic = false;
    }
    /// <summary>Put at p facing r (a start place, a rival back on the line), moving at v (world).</summary>
    public void Ustaw(Vector3 p, Quaternion r, Vector3 v)
    {
        transform.SetPositionAndRotation(p, r); rb.position = p; rb.rotation = r;
        if (!rb.isKinematic) { Vel = v; rb.angularVelocity = Vector3.zero; }
        for (int i = 0; i < 4; i++) kompresja[i] = 0f;
        doGory = stoi = lot = 0f; trik = Trik.Brak; kamOk = false; vKol = Vector3.Dot(v, r * Vector3.forward);
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        if (rb.isKinematic) { Kola(dt, 0f); return; }
        float g = 9.81f * K;
        rb.AddForce(Vector3.down * (g + Physics.gravity.y), ForceMode.Acceleration);   // the world is K x the real one
        float vMax = vMaxKmh / 3.6f * K;
        var up = transform.up; var fwd = transform.forward;
        float vNaprzod = Vector3.Dot(Vel, fwd);
        float kSpr = masa * g * .25f / (.4f * skok * K);                     // 40 % sag at rest
        float cTl = 2f * .42f * Mathf.Sqrt(kSpr * masa * .25f);              // 0.42 of critical
        float zasieg = skok * K + promien * K;
        int ileNaZiemi = 0;
        var maska = ~(1 << WarstwaGracza);
        for (int i = 0; i < 4; i++)
        {
            var gora = transform.TransformPoint(spoczynek[i] + Vector3.up * (.6f * skok * K));
            naZiemi[i] = false;
            int n = Physics.RaycastNonAlloc(gora, -up, trafienia, zasieg, maska, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue; int bi = -1;
            for (int k = 0; k < n; k++)
            {
                var h = trafienia[k];
                if (h.collider.attachedRigidbody == rb || h.collider.transform.IsChildOf(transform)) continue;
                if (h.distance < best) { best = h.distance; bi = k; }
            }
            float stara = kompresja[i];
            if (bi < 0) { kompresja[i] = 0f; continue; }
            var hit = trafienia[bi];
            naZiemi[i] = true; ileNaZiemi++;
            kompresja[i] = zasieg - hit.distance;
            punktKontaktu[i] = hit.point; normalnaKontaktu[i] = hit.normal;
            float c = kompresja[i], vc = (c - stara) / dt;
            float sila = kSpr * c + cTl * vc;
            if (c > .92f * skok * K) sila += kSpr * 6f * (c - .92f * skok * K);    // the bump stop
            sila = Mathf.Max(0f, sila);
            rb.AddForceAtPosition(up * sila, gora);
            // the tyre: grip across, drive and brake along, each limited by its load (the friction circle, arcade)
            var n0 = hit.normal;
            Quaternion st = i < 2 ? Quaternion.AngleAxis(skret, up) : Quaternion.identity;
            var wf = Vector3.ProjectOnPlane(st * fwd, n0).normalized; var wr = Vector3.Cross(n0, wf);
            var vp = rb.GetPointVelocity(hit.point);
            float vLat = Vector3.Dot(vp, wr), vLon = Vector3.Dot(vp, wf);
            float lim = przyczepnosc * sila;
            float boczna = Mathf.Clamp(-vLat * masa * .25f / dt * .55f, -lim, lim);
            float wzdluz = 0f;
            bool napedowe = naprzod4x4 || i >= 2;
            if (gaz > 0f && napedowe)
            {
                float a = przyspieszenie * K * (1f - Mathf.Clamp01(vNaprzod / vMax) * Mathf.Clamp01(vNaprzod / vMax));
                wzdluz += gaz * a * masa / (naprzod4x4 ? 4f : 2f);
            }
            if (hamulec > 0f)
            {
                if (vNaprzod > .8f * K) wzdluz -= hamulec * 7.5f * K * masa * .25f;                               // the brakes
                else if (napedowe && vNaprzod > -14f / 3.6f * K) wzdluz -= hamulec * przyspieszenie * .6f * K * masa / (naprzod4x4 ? 4f : 2f);   // reverse, 14 km/h
            }
            if (gaz == 0f && hamulec == 0f) wzdluz -= Mathf.Clamp(vLon * masa * .25f * .35f, -lim * .3f, lim * .3f);   // engine braking, rolling
            float razem = Mathf.Sqrt(boczna * boczna + wzdluz * wzdluz);
            if (razem > lim && razem > 0f) { boczna *= lim / razem; wzdluz *= lim / razem; }
            rb.AddForceAtPosition(wr * boczna + wf * wzdluz, hit.point);
        }
        // the yaw of a steered machine never faster than its tyres hold (as the A1 car: ~1.6 g at the arcade grip)
        if (ileNaZiemi >= 2)
        {
            // keep it on its wheels in the berms: a roll past 28 degrees is pushed back (the rider's weight, the anti-roll bar)
            float roll = Vector3.SignedAngle(Vector3.ProjectOnPlane(up, fwd), Vector3.ProjectOnPlane(Vector3.up, fwd), fwd);
            if (Mathf.Abs(roll) > 28f) rb.AddTorque(fwd * Mathf.Sign(roll) * (Mathf.Abs(roll) - 28f) * masa * .12f * K, ForceMode.Force);
        }
        else if (trik == Trik.Brak)
        {
            // in the air: the nose follows the flight (the rider's body, the way he lands a jump), the throttle lifts it and
            // the brake drops it on top (the wheels' gyro), the thumb yaws a little; the roll settles
            var right = transform.right;
            var tor3 = Vector3.ProjectOnPlane(Vel, right);
            if (lot > .25f && tor3.sqrMagnitude > 9f * K * K && Vector3.Dot(tor3, fwd) > 0f)
            {
                float blad = Mathf.Clamp(Vector3.SignedAngle(fwd, tor3, right), -60f, 60f) * Mathf.Deg2Rad;
                rb.AddTorque(right * (blad * 5f - Vector3.Dot(rb.angularVelocity, right) * 3f), ForceMode.Acceleration);
            }
            rb.AddTorque(right * (hamulec - gaz) * 2.4f, ForceMode.Acceleration);
            rb.AddTorque(up * skretWej * .9f, ForceMode.Acceleration);
            float roll = Vector3.SignedAngle(Vector3.ProjectOnPlane(up, fwd), Vector3.ProjectOnPlane(Vector3.up, fwd), fwd);
            rb.AddTorque(fwd * roll * .02f, ForceMode.Acceleration);
        }
        NaZiemi = ileNaZiemi > 0;
        Freestyle(dt, ileNaZiemi);
        // stuck on its belly (a mound between the wheels): it slides on, as a real quad rocks off
        stoi = ileNaZiemi == 0 && trik == Trik.Brak && Vel.sqrMagnitude < .3f * K * K && gaz > 0f ? stoi + dt : 0f;
        if (stoi > 2.5f) Postaw();
        // the wheels keep the speed they left the ground with (a flip turns the quad, not its tyres)
        vKol = ileNaZiemi > 0 ? vNaprzod : vKol * (1f - .3f * dt);
        Kola(dt, vKol);
    }

    // ---------------------------------------------------------------- freestyle: the time in the air, the tricks, the landing
    void Freestyle(float dt, int ileNaZiemi)
    {
        if (!gracz) { lot = ileNaZiemi == 0 ? lot + dt : 0f; trik = Trik.Brak; return; }    // a rival: no tricks, no score
        if (ileNaZiemi == 0)
        {
            lot += dt;
            if (trik == Trik.Brak && (chceSalto || chceObrot)) TrikStart(chceSalto ? Trik.Salto : Trik.Obrot);
        }
        else
        {
            if (trik != Trik.Brak) Wywrotka();                 // a wheel down mid-trick
            else if (!rozbity && lot >= .45f) Ladowanie();
            lot = 0f; rozbity = false;
        }
        chceObrot = chceSalto = false;
        if (trik == Trik.Brak) return;
        // the angle the last physics step turned (read back: the damping and the step are the engine's own)
        if (trikKroki++ > 0) trikKat += Mathf.Abs(Vector3.Dot(rb.angularVelocity, trikOs)) * Mathf.Rad2Deg * dt;
        if (trikKat >= 360f - trikW * dt * .5f)
        {
            rb.angularVelocity = Vector3.zero;                 // out of it as it went in
            trikiSkoku++; punktySkoku += trik == Trik.Salto ? PunktySalto : PunktyObrot;
            nazwyTrikow += (nazwyTrikow.Length > 0 ? " + " : "") + (trik == Trik.Salto ? "SALTO" : "OBRÓT 360");
            Debug.Log("BRAX_KLAD_TRIK " + trik + " air=" + lot.ToString("F2"));
            trik = Trik.Brak;
            return;
        }
        float w = Mathf.Min(trikW, (360f - trikKat) / dt);
        rb.angularVelocity = trikOs * (w * Mathf.Deg2Rad / (1f - .6f * dt));   // the angular damping (.6) made good
    }

    void TrikStart(Trik t)
    {
        trik = t; trikKat = 0f; trikKroki = 0;
        // the backflip: the nose up and over, about the quad's own axle line; the 360: about the vertical, the way the thumb leans
        trikOs = t == Trik.Salto ? -transform.right : Vector3.up * (skretWej < 0f ? -1f : 1f);
        trikW = 360f / (t == Trik.Salto ? CzasSalta : CzasObrotu);
    }

    void Ladowanie()
    {
        if (transform.up.y <= .72f)
        {
            if (trikiSkoku > 0 || lot >= .8f) Napis("WYWROTKA!", new Color(1f, .35f, .3f), 2.2f);
            Zeruj(); return;
        }
        int pkt = trikiSkoku >= 2 ? Mathf.RoundToInt(punktySkoku * Kombo) : punktySkoku;
        if (lot >= .8f) pkt += Mathf.RoundToInt(lot * PunktyZaSekunde);
        if (pkt > 0)
        {
            Punkty += pkt;
            bool rekord = pkt > RekordSkoku;
            if (rekord) { rekordSkoku = pkt; try { PlayerPrefs.SetInt(KluczRekordu, pkt); PlayerPrefs.Save(); } catch { } }
            string co = trikiSkoku > 0 ? (trikiSkoku >= 2 ? "KOMBO! " : "") + nazwyTrikow + "!" : "SKOK " + lot.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " s";
            Napis(co + "  +" + pkt + (rekord ? "\nREKORD SKOKU!" : ""), new Color(.93f, .76f, .36f), 2.6f);
            Debug.Log("BRAX_KLAD_SKOK pkt=" + pkt + " triki=" + trikiSkoku + " air=" + lot.ToString("F2") + " razem=" + Punkty);
        }
        Zeruj();
    }

    void Wywrotka()
    {
        trik = Trik.Brak; rozbity = true;                       // nothing more scores until it is down
        Napis("WYWROTKA!", new Color(1f, .35f, .3f), 2.2f);
        Debug.Log("BRAX_KLAD_SKOK wywrotka air=" + lot.ToString("F2"));
        Zeruj();
    }
    void Zeruj() { trikiSkoku = punktySkoku = 0; nazwyTrikow = ""; }
    void Napis(string t, Color c, float s) { napis = t; napisKolor = c; napisDo = Time.time + s; }

    // the body meets something while a trick still turns: the crash
    void OnCollisionEnter(Collision c) { if (gracz && trik != Trik.Brak) Wywrotka(); }

    // the wheels: roll at the ground's speed, the front ones turn with the bars, the hubs ride the suspension
    void Kola(float dt, float vNaprzod)
    {
        float R = Mathf.Max(.05f, promien * K);
        obrot += vNaprzod / R * Mathf.Rad2Deg * dt;
        if (obrot > 3600f || obrot < -3600f) obrot %= 360f;
        for (int i = 0; i < 4; i++)
        {
            var piasta = piasty[i]; if (!piasta) continue;
            float wysuniecie = rb && !rb.isKinematic ? (naZiemi[i] ? kompresja[i] - .4f * skok * K : -.6f * skok * K) : 0f;
            piasta.localPosition = spoczynek[i] + Vector3.up * Mathf.Clamp(wysuniecie, -.6f * skok * K, .6f * skok * K);
            if (i < 2) piasta.localRotation = Quaternion.Euler(0f, skret, 0f);
            if (kola[i]) kola[i].localRotation = Quaternion.Euler(obrot, 0f, 0f);
        }
        if (kierownica) kierownica.localRotation = Quaternion.AngleAxis(skret * .9f, osKierownicy) * kierownicaSpoczynek;
    }

    // ---------------------------------------------------------------- the camera: behind and over, the way the ride goes
    void LateUpdate()
    {
        if (!gracz || !kam) return;
        float dt = Mathf.Min(Time.unscaledDeltaTime, .1f);
        var v = Vel; var kier = Flat(transform.forward);
        var fv = Flat(v); if (v.sqrMagnitude > 4f * K * K && Vector3.Dot(fv, kier) > 0f) kier = Flat(Vector3.Slerp(kier, fv, .5f));
        // in the air the camera follows the flight, not the quad (a backflip or a 360 turns under a still camera)
        if (lot > .1f || trik != Trik.Brak) kier = fv.sqrMagnitude > .5f && new Vector2(v.x, v.z).sqrMagnitude > 4f * K * K ? fv : kamKier;
        if (kier.sqrMagnitude < .5f) kier = kamKier.sqrMagnitude > .5f ? kamKier : Vector3.forward;
        kamKier = kamOk && kamKier.sqrMagnitude > .5f ? Flat(Vector3.Slerp(kamKier, kier, 1f - Mathf.Exp(-dt * 6f))) : kier;
        kier = kamKier;
        float szybko = Mathf.Clamp01(Mathf.Abs(PredkoscKmh) / Mathf.Max(1f, vMaxKmh));
        var cel = transform.position + Vector3.up * 1.25f * K;
        var chce = cel - kier * (4.6f + 1.6f * szybko) * K + Vector3.up * (1.55f + .3f * szybko) * K;
        // no camera inside a mound or a fence
        var d = chce - cel; float dl = d.magnitude;
        if (dl > .01f && Physics.SphereCast(cel, .25f * K, d / dl, out var hit, dl, ~(1 << WarstwaGracza), QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(transform))
            chce = cel + d / dl * Mathf.Max(.8f * K, hit.distance - .1f * K);
        if (!kamOk) { kamPoz = chce; kamOk = true; }
        kamPoz = Vector3.Lerp(kamPoz, chce, 1f - Mathf.Exp(-dt * 7f));
        kam.transform.SetPositionAndRotation(kamPoz, Quaternion.LookRotation(cel + kier * 2.2f * K - kamPoz, Vector3.up));
    }

    // ---------------------------------------------------------------- the engine: the idle and the ride loop, real recordings
    void Dzwiek()
    {
        if (silnikWolne) { wolne = gameObject.AddComponent<AudioSource>(); wolne.clip = silnikWolne; wolne.loop = true; wolne.playOnAwake = false; wolne.spatialBlend = .7f; wolne.minDistance = 3f * K; wolne.maxDistance = 60f * K; wolne.volume = .55f; }
        if (silnikJazda) { jazda = gameObject.AddComponent<AudioSource>(); jazda.clip = silnikJazda; jazda.loop = true; jazda.playOnAwake = false; jazda.spatialBlend = .7f; jazda.minDistance = 3f * K; jazda.maxDistance = 90f * K; jazda.volume = 0f; }
    }
    float obroty;
    void Silnik()
    {
        if (!wolne && !jazda) return;
        float v = Mathf.Clamp01(Mathf.Abs(PredkoscKmh) / Mathf.Max(1f, vMaxKmh));
        // a CVT quad: the revs follow the throttle first, then the speed
        obroty = Mathf.MoveTowards(obroty, Mathf.Max(gaz * .55f, v), Time.deltaTime * (gaz > 0f ? 1.6f : .9f));
        if (wolne) { wolne.volume = .5f * (1f - Mathf.Clamp01(obroty * 2.2f)) + .08f; wolne.pitch = 1f + obroty * .25f; }
        if (jazda) { jazda.volume = Mathf.Clamp01(obroty * 2.2f) * .75f; jazda.pitch = .78f + obroty * .62f; }
    }

    // ---------------------------------------------------------------- the quad's own buttons
    static GUIStyle styl, stylMaly; static Texture2D biel;
    void OnGUI()
    {
        if (!gracz) return;
        GUI.depth = -25;
        Uklad();
        if (!biel) { biel = new Texture2D(1, 1); biel.SetPixel(0, 0, Color.white); biel.Apply(); }
        float u = jednostka;
        if (styl == null) { styl = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold }; stylMaly = new GUIStyle(styl); }
        styl.fontSize = Mathf.RoundToInt(26 * u); stylMaly.fontSize = Mathf.RoundToInt(19 * u);
        var zloto = new Color(.93f, .76f, .36f); var szklo = new Color(0f, 0f, 0f, .45f);
        Pole(rGaz, palecGazu != -99 ? new Color(.25f, .6f, .25f, .75f) : szklo); Tekst(rGaz, "GAZ", styl, Color.white);
        Pole(rHamulec, palecHamulca != -99 ? new Color(.7f, .2f, .2f, .75f) : szklo); Tekst(rHamulec, "HAMULEC\nWSTECZ", stylMaly, Color.white);
        bool moze = Mathf.Abs(PredkoscKmh) <= 8f;
        Pole(rZsiadz, szklo); Tekst(rZsiadz, moze ? "ZSIĄDŹ" : "ZWOLNIJ", styl, moze ? zloto : new Color(.8f, .8f, .8f));
        if (Przewrocony) { Pole(rPostaw, new Color(.75f, .45f, .05f, .85f)); Tekst(rPostaw, "POSTAW KŁAD", styl, Color.white); }
        // the tricks, in the air only
        if (MozeTrik)
        {
            Pole(rSalto, new Color(.85f, .35f, .05f, .85f)); Tekst(rSalto, "SALTO\n+" + PunktySalto, styl, Color.white);
            Pole(rObrot, new Color(.15f, .4f, .8f, .85f)); Tekst(rObrot, "OBRÓT\n+" + PunktyObrot, stylMaly, Color.white);
        }
        // the steering thumb: where it went down and how far it is pushed
        if (palecSkretu != -99)
        {
            float y = rSkret.y + rSkret.height * .7f, w = Screen.width * .22f;
            Pole(new Rect(skretX0 - w * .5f, y - 6f * u, w, 12f * u), new Color(1f, 1f, 1f, .35f));
            float x = skretX0 + skretWej * Screen.width * .11f;
            Pole(new Rect(x - 28f * u, y - 28f * u, 56f * u, 56f * u), new Color(zloto.r, zloto.g, zloto.b, .85f));
        }
        else Tekst(new Rect(20f * u, Screen.height - 90f * u, Screen.width * .45f, 60f * u), "◀  PRZESUŃ KCIUK  ▶", stylMaly, new Color(1f, 1f, 1f, .7f));
        // the speed and the lap
        var info = new Rect(Screen.width - 330f * u, 20f * u, 310f * u, tor ? 190f * u : 110f * u);
        Pole(info, szklo);
        Tekst(new Rect(info.x, info.y, info.width, 70f * u), Mathf.RoundToInt(Mathf.Abs(PredkoscKmh)) + " km/h", styl, Color.white);
        float yi = info.y + 62f * u;
        if (tor)
        {
            Tekst(new Rect(info.x, yi, info.width, 40f * u), "OKRĄŻENIE " + BraxChampionsTorCzas.Czas(tor.Biezace), stylMaly, zloto);
            Tekst(new Rect(info.x, yi + 38f * u, info.width, 40f * u), "REKORD " + (tor.Najlepsze > 0f ? BraxChampionsTorCzas.Czas(tor.Najlepsze) : "—"), stylMaly, Color.white);
            yi += 76f * u;
        }
        Tekst(new Rect(info.x, yi, info.width, 40f * u), "PUNKTY " + Punkty + " · SKOK MAX " + RekordSkoku, stylMaly, Color.white);
        if (tor && Time.time < tor.KomunikatDo)
        {
            var r = new Rect(Screen.width * .5f - 260f * u, Screen.height * .28f, 520f * u, 80f * u);
            Pole(r, new Color(0f, 0f, 0f, .6f)); Tekst(r, tor.Komunikat, styl, zloto);
        }
        // the jump's score or the crash
        if (Time.time < napisDo && napis.Length > 0)
        {
            var r = new Rect(Screen.width * .5f - 300f * u, Screen.height * .40f, 600f * u, 110f * u);
            Pole(r, new Color(0f, 0f, 0f, .6f)); Tekst(r, napis, styl, napisKolor);
        }
    }
    static void Pole(Rect r, Color c) { var o = GUI.color; GUI.color = c; GUI.DrawTexture(r, biel); GUI.color = o; }
    static void Tekst(Rect r, string t, GUIStyle s, Color c) { var o = GUI.color; GUI.color = c; GUI.Label(r, t, s); GUI.color = o; }
}
