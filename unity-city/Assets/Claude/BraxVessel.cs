using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pojazd wodny gracza (skuter, motorówka, jacht) — decyzja właściciela 31.10 i 2.11 („ma być możliwość pływania skuterem
/// i jachtem”): przy pomoście AKCJA „WSIĄDŹ”, jazda joystickiem (y = gaz / wsteczny, x = ster), AKCJA „WYSIĄDŹ” przy pomoście
/// lub brzegu (np. pomost wyspy). Prosta fizyka łodzi: przyspieszenie i opór, poślizg boczny w zakręcie, przechył (skuter
/// i motorówka do środka zakrętu, jacht lekko na zewnątrz), dziób unosi się przy rozpędzaniu i wchodzeniu w ślizg, kołysanie
/// na tych samych falach co shader wody (HarbourWater: SwellPhase). Fala dziobowa i ślad za rufą to siatki z pianą
/// (shader Braxhorn/NabrzezePiana) — siła zależy od prędkości, bez cząsteczek. Ląd, pomosty i boje kąpieliska zatrzymują
/// łódź (promienie przed dziobem i przy burtach). Stawiany z kodu w BraxNabrzezeMarina.
/// </summary>
public sealed class BraxVessel : MonoBehaviour
{
    public static readonly List<BraxVessel> All = new List<BraxVessel>();

    public string label = "SKUTER";
    public float waterY = -1.7f;
    public float draft = .22f;            // dodatkowe zanurzenie korzenia (model ustawiony już na swoim zanurzeniu)
    public float maxSpeed = 11f;          // m/s
    public float accel = 5f;              // m/s²
    public float turnRate = 70f;          // stopni/s przy pełnym sterze i prędkości przelotowej
    public float length = 3f, width = 1.2f;
    public Vector3 seat = new Vector3(0, .9f, -.2f);   // miejsce gracza (lokalnie, względem środka pojazdu)
    public bool sit = true;               // skuter/motorówka: siedzi (BraxPose.Ride); jacht: stoi za sterem
    public float sitLower = .95f;         // awaryjny offset gdy rig nie ma Hips
    public bool walkableDeck;             // jacht: etap wejścia na pokład przed sterowaniem
    public Vector3 deckEntry;            // lokalny punkt wejścia na pokład, nie woda
    public Vector3 helmCameraLocal;      // kamera wewnętrzna za kołem sterowym
    public bool IsOnDeck => rider && walkableDeck && onDeck;
    public bool AtHelm => rider && (!walkableDeck || !onDeck);
    public bool HasPilotCamera => rider && walkableDeck && !onDeck;
    public Vector3 moor;                  // gdzie pojazd stoi zacumowany
    public float moorYaw;
    public Vector3 wejscie;               // miejsce wejścia (lokalnie): przy burcie od pomostu albo za rufą; zero = środek
    public Renderer[] fale = new Renderer[0];   // fala dziobowa i ślad za rufą (siatki z pianą)

    public bool Occupied { get { return rider; } }
    public float Speed { get { return speed; } }
    BraxGymPlayer rider;
    bool onDeck, returnedToDeck;
    UnityEngine.SceneManagement.Scene riderScene;
    float speed, yawVel, baseYaw, side, pitchS, rollS, heave;
    Vector3 home; bool homed;
    MaterialPropertyBlock mpb;
    static readonly int MocId = Shader.PropertyToID("_Moc"), BiegId = Shader.PropertyToID("_Bieg");

    void OnEnable() { All.Add(this); if (!homed) { homed = true; home = transform.position; baseYaw = transform.eulerAngles.y; } }
    void OnDisable() { All.Remove(this); }

    /// <summary>Wysokość fali jak w HarbourWater.shader (SwellPhase + amplitudy).</summary>
    public static float Wave(float x, float z, float t)
    {
        float qx = x - 84f, qz = z - 60f;
        float p0 = (qx * .84f + qz * .5426f) * .84f - t * .65f;
        float p1 = (qx * -.4191f + qz * .9079f) * 1.46f - t * .92f;
        return Mathf.Sin(p0) * .10f + Mathf.Sin(p1) * .06f;
    }

    public Vector3 BoardingPoint { get { return wejscie == Vector3.zero ? transform.position : transform.TransformPoint(wejscie); } }

    public static BraxVessel Nearest(Vector3 p, float max)
    {
        BraxVessel best = null; float bd = max + 1.3f; // zasięg większy tylko dla jachtu z trapem
        foreach (var v in All)
        {
            if (!v || v.rider) continue;
            var d = v.BoardingPoint - p; d.y = 0;
            // Jacht: długi, wysoki kadłub i trap z pomostu. Punkt akcji
            // musi być osiągalny z normalnego końca trapu, nie tylko z wody.
            float zasieg = v.walkableDeck ? Mathf.Max(max, 4.5f) : max;
            if (d.magnitude < zasieg && d.magnitude < bd) { bd = d.magnitude; best = v; }
        }
        return best;
    }

    static Transform ZnajdzBiodra(BraxGymPlayer p)
    {
        if (!p || !p.visual) return null;
        foreach (var t in p.visual.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name; int i = n.LastIndexOf(':');
            if (i >= 0) n = n.Substring(i + 1);
            if (n == "Hips") return t;
        }
        return null;
    }

    void UstawZaSterem(BraxGymPlayer player)
    {
        if (!player) return;
        onDeck = false;
        // BraxSeat używa bioder mierzonego rigu. Ten sam standard
        // stosujemy na łodzi, zamiast odejmować stałe 95 cm od źle
        // trafionego miejsca i sadzać Braxa w powietrzu / pod fotelem.
        var hipsBone = ZnajdzBiodra(player);
        float hip = hipsBone ? Mathf.Clamp(
            hipsBone.position.y - player.transform.position.y, .55f, 1.60f) : sitLower;
        // Kość Hips po ustawieniu parent na obracającym się pojeździe
        // niekoniecznie jest w osi świata. Ustawiamy lokalny punkt oparcia
        // tak, żeby biodra były ~8 cm ponad realną powierzchnią fotela.
        player.transform.localPosition = seat +
            Vector3.up * (.08f * Mathf.Max(1f, transform.lossyScale.y) - hip);
        player.transform.localRotation = Quaternion.identity;
        if (player.clips && player.clips["Idle"] != null)
        {
            player.clips["Idle"].speed = 0f;
            player.clips.Play("Idle");
        }
        if (sit)
            BraxPose.Get(player).Set(label == "SKUTER" ?
                BraxPose.Mode.Ride : BraxPose.Mode.Ster, transform);
        Debug.Log("BRAXHORN_LODZ_FOTEL " + label + " hip=" +
            hip.ToString("F2") + "m, siedzenie_local=" + seat.ToString("F2") +
            ", root_local=" + player.transform.localPosition.ToString("F2"));
    }

    public void Board(BraxGymPlayer player)
    {
        if (rider || !player) return;
        rider = player;
        riderScene = player.gameObject.scene;
        player.vessel = this;
        if (player.body) player.body.enabled = false;
        player.transform.SetParent(transform, true);
        speed = 0f; yawVel = 0f; side = 0f;
        returnedToDeck = false;
        if (walkableDeck)
        {
            onDeck = true;
            player.transform.localPosition = deckEntry;
            player.transform.localRotation = Quaternion.identity;
            BraxPose.Get(player).Set(BraxPose.Mode.None, null);
            if (player.clips && player.clips["Idle"] != null)
            {
                player.clips["Idle"].speed = 1f;
                player.clips.Play("Idle");
            }
            Debug.Log("BRAXHORN_JACHT_POKLAD: wejście z pomostu, teraz możesz przejść za ster");
        }
        else UstawZaSterem(player);
        player.SnapKamera();
        Debug.Log("BRAXHORN_LODZ wsiadł: " + label + " " + name);
    }

    // JACHT: po wejściu na pokład joystick porusza postacią WYŁĄCZNIE
    // w obrysie bezpiecznego, otwartego pokładu. Nie jedzie łódź.
    public void WalkDeck(Vector2 move, float dt)
    {
        if (!IsOnDeck || dt <= 0f) return;
        var local = rider.transform.localPosition;
        var delta = new Vector3(move.x, 0f, move.y);
        if (delta.sqrMagnitude > 1f) delta.Normalize();
        local += delta * (2.15f * dt);
        float bw = Mathf.Max(.75f, width * .26f);
        local.x = Mathf.Clamp(local.x, -bw, bw);
        local.z = Mathf.Clamp(local.z, -length * .45f, length * .24f);
        local.y = deckEntry.y;
        rider.transform.localPosition = local;
        if (delta.sqrMagnitude > .025f)
        {
            rider.transform.localRotation = Quaternion.Slerp(
                rider.transform.localRotation, Quaternion.LookRotation(delta),
                Mathf.Clamp01(dt * 10f));
        }
        if (rider.clips)
        {
            string clip = delta.sqrMagnitude > .025f ? "Walk" : "Idle";
            if (rider.clips[clip] != null && !rider.clips.IsPlaying(clip))
                rider.clips.CrossFade(clip, .2f);
        }
    }

    // Nie uzależniaj sterowania od historii wejścia — wcześniejsza flaga
    // returnedToDeck blokowała POWRÓT do steru po pierwszym wyjściu z kokpitu.
    bool BliskoSteru()
    {
        if (!rider) return false;
        Vector3 a = rider.transform.localPosition, b = seat;
        float d = new Vector2(a.x - b.x, a.z - b.z).magnitude;
        return d < Mathf.Max(1.25f, width * .32f);
    }
    bool BliskoTrapu()
    {
        if (!rider) return false;
        Vector3 a = rider.transform.localPosition;
        return new Vector2(a.x - deckEntry.x, a.z - deckEntry.z).magnitude <
               Mathf.Max(1.25f, width * .32f);
    }

    public string Interakcja()
    {
        if (!rider) return "";
        if (!walkableDeck) return "ZEJDŹ · " + label;
        if (!onDeck) return "WYJDŹ NA POKŁAD";
        if (BliskoSteru()) return "STERUJ JACHTEM";
        if (BliskoTrapu()) return "ZEJDŹ NA POMOST";
        return "PRZEJDŹ ZA STER";
    }

    public bool Interact()
    {
        if (!rider) return false;
        if (!walkableDeck) return Dismount();
        if (!onDeck)
        {
            BraxPose.Get(rider).Set(BraxPose.Mode.None, null);
            if (rider.clips && rider.clips["Idle"] != null)
                rider.clips["Idle"].speed = 1f;
            onDeck = true; returnedToDeck = true;
            // Po oddaniu steru nie przenoś gracza na rufę; zostaje obok kokpitu,
            // dzięki czemu może później ponownie przejąć ster.
            rider.transform.localPosition = new Vector3(seat.x,
                deckEntry.y, Mathf.Clamp(seat.z - .75f, -length * .43f, length * .24f));
            rider.transform.localRotation = Quaternion.identity;
            rider.SnapKamera();
            return true;
        }
        if (BliskoTrapu() && !BliskoSteru())
            return Dismount(); // przy trapie: zejście, nie teleport do kokpitu
        // Przy sterze albo na pozostałej części otwartego pokładu: gracz
        // może przejąć ster przyciskiem; przycisk nie może być martwy.
        UstawZaSterem(rider);
        rider.SnapKamera();
        return true;
    }

    /// <summary>Zejście: na najbliższy twardy grunt nad wodą w promieniu 4,5 m (pomost, piasek). Zwraca false, gdy za daleko od brzegu.</summary>
    public bool Dismount()
    {
        if (!rider) return false;
        Vector3 best = Vector3.zero; bool found = false; float bd = 99f;
        // A 2.11: szukanie wzdłuż całego kadłuba (środek, dziób, rufa) — długi jacht dobity dziobem albo burtą do pomostu
        var fw = transform.forward; fw.y = 0f; fw.Normalize();
        foreach (float wz in new[] { 0f, .38f, -.38f, .2f, -.2f })
        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.PI * 2f / 16f;
            for (float r = 1.2f; r <= Mathf.Max(4.5f, width * .5f + 3f); r += .6f)
            {
                var o = transform.position + fw * (length * wz) + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                RaycastHit hit;
                if (!Physics.Raycast(o + Vector3.up * 6f, Vector3.down, out hit, 12f, ~((1 << 8) | (1 << 29)), QueryTriggerInteraction.Ignore)) continue;
                if (hit.point.y < waterY + .15f) continue;                 // dno pod wodą — nie
                if (hit.collider.transform.IsChildOf(transform)) continue;  // własny kadłub — nie
                if (hit.normal.y < .7f) continue;                          // ściana, skała — nie
                if (Physics.CheckCapsule(hit.point + Vector3.up * .5f, hit.point + Vector3.up * 1.9f, .3f, ~((1 << 8) | (1 << 29)), QueryTriggerInteraction.Ignore)) continue;   // miejsce zajęte (krawędź, słupek)
                float score = r + Mathf.Abs(wz) * length * .5f;
                if (score < bd) { bd = score; best = hit.point; found = true; }
            }
        }
        if (!found) return false;
        var p = rider;
        BraxPose.Get(p).Set(BraxPose.Mode.None, null);
        if (p.clips && p.clips["Idle"] != null) p.clips["Idle"].speed = 1f;
        p.transform.SetParent(null, true);
        if (riderScene.IsValid() && riderScene.isLoaded && p.gameObject.scene != riderScene)
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(p.gameObject, riderScene);
        p.transform.position = best + Vector3.up * .08f;
        p.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        p.body.enabled = true;
        p.vessel = null;
        rider = null; onDeck = false; returnedToDeck = false; speed = 0; side = 0;
        home = transform.position; baseYaw = transform.eulerAngles.y;   // pojazd zostaje tam, gdzie go zostawiono
        Debug.Log("BRAXHORN_LODZ wysiadł: " + label + " przy " + best.ToString("F1"));
        return true;
    }

    bool Lad(Vector3 p)
    {
        RaycastHit hit;
        if (!Physics.Raycast(p + Vector3.up * 4f, Vector3.down, out hit, 20f, ~((1 << 8) | (1 << 29)), QueryTriggerInteraction.Ignore)) return false;
        return !hit.collider.transform.IsChildOf(transform) && hit.point.y > waterY - .8f;
    }

    /// <summary>Punkt w obrysie innej łodzi (kapsuła wzdłuż kadłuba) — łodzie nie mają kolizji, żeby gracz wsiadał bez zacięć.</summary>
    bool Lodz(Vector3 p)
    {
        foreach (var o in All)
        {
            if (!o || o == this) continue;
            var d = p - o.transform.position; d.y = 0;
            if (d.sqrMagnitude > (o.length * o.length)) continue;
            var f = o.transform.forward; f.y = 0; f.Normalize();
            float along = Mathf.Clamp(Vector3.Dot(d, f), -o.length * .5f + o.width * .5f, o.length * .5f - o.width * .5f);
            if ((d - f * along).magnitude < o.width * .5f + .25f) return true;
        }
        return false;
    }

    /// <summary>Jazda: move.y = gaz (ujemny = wstecz, wolniej), move.x = ster. Wywołuje gracz w Update.</summary>
    public void Drive(Vector2 move, float dt)
    {
        float target = move.y > 0 ? move.y * maxSpeed : move.y * maxSpeed * .3f;
        float a = Mathf.Abs(target) > Mathf.Abs(speed) ? accel : accel * 1.4f;      // zdejmowanie gazu: opór wody hamuje szybciej
        float before = speed;
        speed = Mathf.MoveTowards(speed, target, dt * a);
        float acc = (speed - before) / Mathf.Max(dt, 1e-4f);
        // ster działa tylko przy ruchu (strumień wody na sterze / dyszy), przy prędkości przelotowej najmocniej
        float steerEff = Mathf.Clamp01(Mathf.Abs(speed) / (maxSpeed * .25f));
        float steer = move.x * turnRate * steerEff * (speed < 0 ? -1f : 1f);
        // AKWEN (pkt 15): w pasie miękkim silny prąd — łódź zwalnia i jest łagodnie obracana do środka akwenu, komunikat
        float prad = BraxAkwen.Prad(transform.position, out var doSrodka);
        if (prad > 0f)
        {
            BraxAkwen.Komunikat(prad);
            float kat = Mathf.DeltaAngle(transform.eulerAngles.y, Quaternion.LookRotation(doSrodka).eulerAngles.y);
            steer += Mathf.Clamp(kat, -60f, 60f) * Mathf.Clamp01(prad) * 1.2f;
            float vMax = maxSpeed * Mathf.Lerp(1f, .25f, Mathf.Clamp01(prad));
            if (speed > vMax) speed = Mathf.MoveTowards(speed, vMax, dt * accel * 2f);
        }
        yawVel = Mathf.Lerp(yawVel, steer, 1f - Mathf.Exp(-dt * (sit ? 4f : 1.6f)));    // jacht: bezwładny
        float yaw = transform.eulerAngles.y + yawVel * dt;
        var fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward; var right = Quaternion.Euler(0, yaw, 0) * Vector3.right;
        // poślizg boczny: w zakręcie łódź „wynosi” na zewnątrz, opór boczny wygasza go
        side = Mathf.Lerp(side, -yawVel * Mathf.Deg2Rad * speed * .35f, 1f - Mathf.Exp(-dt * 2.5f));
        var next = transform.position + (fwd * speed + right * side) * dt;
        // ląd / pomost / boje przed dziobem i przy burtach → zatrzymanie
        var probe = next + fwd * (length * .5f + .6f) * Mathf.Sign(speed == 0 ? 1 : speed);
        if (BraxAkwen.Prad(next) >= 1f && BraxAkwen.Prad(next) > prad) { speed = 0; side = 0; next = transform.position; }   // twarda granica akwenu
        // pętla wybrzeża (pkt 15b′): w połowie pasa mgły przeniesienie na drugi koniec akwenu — kurs i prędkość bez zmian
        bool przeniesiono = false;
        if (BraxAkwen.Petla(next, out var celPetli))
        {
            var q = transform.rotation; var pol = new Vector3(width * .5f + .3f, 1.5f, length * .5f + .3f);
            for (int k = 0; k < 8 && Physics.CheckBox(celPetli + Vector3.up * 1.6f, pol, q, ~((1 << 8) | (1 << 29)), QueryTriggerInteraction.Ignore); k++) celPetli.x += 25f;
            Debug.Log("BRAXHORN_AKWEN_PETLA " + label + " z " + next.ToString("F0") + " na " + celPetli.ToString("F0"));
            next = celPetli; przeniesiono = true; BraxAkwen.Ciemno = Time.unscaledTime + .4f;
        }
        if (Lad(probe) || Lodz(probe))
        {
            if (Mathf.Abs(speed) > 3f) Debug.Log("BRAXHORN_LODZ uderzenie w brzeg/pomost: " + label + " v=" + speed.ToString("F1"));
            speed = 0; side = 0; next = transform.position;
        }
        float t = Time.time;
        float h = Wave(next.x, next.z, t);
        float hf = Wave(next.x + fwd.x * length * .4f, next.z + fwd.z * length * .4f, t);
        float hr = Wave(next.x + right.x * width * .5f, next.z + right.z * width * .5f, t);
        float v01 = Mathf.Clamp01(Mathf.Abs(speed) / maxSpeed);
        // ślizg: dziób w górę przy rozpędzaniu i w „garbie” fali (ok. 40 % prędkości), na pełnej prędkości kadłub płasko
        float hump = Mathf.Exp(-Mathf.Pow((v01 - .38f) / .18f, 2f));
        float wantPitch = Mathf.Atan2(hf - h, length * .4f) * Mathf.Rad2Deg * (sit ? 1f : .5f)
                          + (sit ? 7f : 2.5f) * hump + Mathf.Clamp(acc, -4f, 4f) * (sit ? .9f : .3f);
        float wantRoll = Mathf.Atan2(hr - h, width * .5f) * Mathf.Rad2Deg * .6f
                         + yawVel * v01 * (sit ? -.22f : .05f);                     // skuter/motorówka do środka, jacht lekko na zewnątrz
        if (prad > 0f && BraxAkwen.Rodzaj == 1)
        {   // sztorm: wysokie fale — kołysanie i dziób w górę/dół rosną z prądem
            float sz = Mathf.Clamp01(prad * 1.4f);
            wantRoll += Mathf.Sin(Time.time * 1.9f) * 16f * sz; wantPitch += Mathf.Sin(Time.time * 1.3f + 1f) * 8f * sz;
        }
        pitchS = Mathf.Lerp(pitchS, wantPitch, 1f - Mathf.Exp(-dt * 3f));
        rollS = Mathf.Lerp(rollS, Mathf.Clamp(wantRoll, -24f, 24f), 1f - Mathf.Exp(-dt * 3f));
        heave = Mathf.Lerp(heave, sit ? v01 * .12f : 0f, 1f - Mathf.Exp(-dt * 2f));   // w ślizgu kadłub wynurza się
        next.y = waterY + h - draft + heave;
        transform.position = next;
        transform.rotation = Quaternion.Euler(-pitchS, yaw, rollS);
        if (przeniesiono && rider) rider.SnapKamera();
        Fale(v01, speed > 0);
    }

    void Fale(float v01, bool naprzod)
    {
        if (fale == null || fale.Length == 0) return;
        bool on = v01 > .06f && naprzod;
        if (mpb == null) mpb = new MaterialPropertyBlock();
        mpb.SetFloat(MocId, Mathf.Clamp01(v01 * 1.4f));
        mpb.SetFloat(BiegId, 1f + v01 * 6f);
        foreach (var r in fale)
        {
            if (!r) continue;
            if (r.enabled != on) r.enabled = on;
            if (on) r.SetPropertyBlock(mpb);
        }
    }

    void Zwolnij()
    {
        var p = rider; rider = null; onDeck = false; returnedToDeck = false; speed = 0; side = 0;
        home = transform.position; baseYaw = transform.eulerAngles.y;
        if (!p) return;
        BraxPose.Get(p).Set(BraxPose.Mode.None, null);
        if (p.clips && p.clips["Idle"] != null) p.clips["Idle"].speed = 1f;
        if (p.transform.parent == transform) p.transform.SetParent(null, true);
        if (riderScene.IsValid() && riderScene.isLoaded && p.gameObject.scene != riderScene)
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(p.gameObject, riderScene);
        if (p.body) p.body.enabled = true;
        if (p.vessel == this) p.vessel = null;
        Debug.Log("BRAXHORN_LODZ zwolniona bez zejścia (reset/podróż): " + label);
    }

    /// <summary>Zacumowany pojazd lekko kołysze się na fali.</summary>
    // WŁAŚCICIEL 3.11 11:00: „motorówki, skutery — wszystko ma mieć odgłos”. Silnik syntezowany w grze (bez nagrań i licencji):
    // skuter — wysoki dwusuw, motorówka — V8 niżej, jacht — diesel z dudnieniem; ton i głośność rosną z prędkością.
    // Gra, gdy ktoś płynie; łódź zacumowana — cicho. Pętla 2 s (całe okresy tonu + przenikanie końców).
    AudioSource silnikA;
    static readonly System.Collections.Generic.Dictionary<string, AudioClip> klipySilnika = new System.Collections.Generic.Dictionary<string, AudioClip>();
    static AudioClip KlipSilnika(string typ)
    {
        if (klipySilnika.TryGetValue(typ, out var c) && c) return c;
        const int SR = 22050; const float DL = 2f; int n = (int)(SR * DL);
        float f0 = typ == "SKUTER" ? 95f : typ == "MOTORÓWKA" ? 62f : 38f;   // f0 × 2 s = całe okresy (pętla bez trzasku)
        float ostrosc = typ == "SKUTER" ? 10f : typ == "MOTORÓWKA" ? 6f : 4f, szum = typ == "SKUTER" ? .45f : .3f;
        var x = new float[n]; var rnd = new System.Random(typ.GetHashCode()); float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR, w = 2f * Mathf.PI * f0 * t;
            float ton = Mathf.Sin(w) * .45f + Mathf.Sin(2f * w) * .25f + Mathf.Sin(3f * w) * .12f + Mathf.Sin(.5f * w) * (typ == "JACHT" ? .3f : .08f);
            float zaplon = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(w)), ostrosc) * .55f;
            lp += ((float)(rnd.NextDouble() * 2.0 - 1.0) - lp) * .09f;
            x[i] = Mathf.Clamp((ton * .55f + zaplon + lp * szum) * .55f, -1f, 1f);
        }
        int fade = SR / 20; for (int i = 0; i < fade; i++) { float k = i / (float)fade; x[n - fade + i] = x[n - fade + i] * (1f - k) + x[i] * k; }
        c = AudioClip.Create("Silnik łodzi: " + typ, n, 1, SR, false); c.SetData(x, 0); klipySilnika[typ] = c; return c;
    }
    void DzwiekSilnika()
    {
        bool plynie = rider || Mathf.Abs(speed) > .3f;
        if (!plynie) { if (silnikA && silnikA.isPlaying) silnikA.Pause(); return; }
        if (!silnikA)
        {
            silnikA = gameObject.AddComponent<AudioSource>(); silnikA.clip = KlipSilnika(label); silnikA.loop = true; silnikA.spatialBlend = 1f;
            silnikA.rolloffMode = AudioRolloffMode.Logarithmic; silnikA.minDistance = 4f; silnikA.maxDistance = 120f; silnikA.dopplerLevel = .3f; silnikA.priority = 90;
        }
        float v01 = Mathf.Clamp01(Mathf.Abs(speed) / Mathf.Max(.1f, maxSpeed));
        silnikA.pitch = Mathf.Lerp(.75f, label == "SKUTER" ? 1.9f : 1.5f, v01);
        silnikA.volume = Mathf.Lerp(.35f, .95f, v01);
        if (!silnikA.isPlaying) silnikA.Play();
    }

    void Update()
    {
        // 2.11 recenzja: reset albo szybka podróż w trakcie rejsu odpinają gracza bez zejścia — zwolnij łódź i pozę
        if (rider && (rider.vessel != this || (rider.transform.position - transform.position).sqrMagnitude > 1600f)) Zwolnij();
        DzwiekSilnika();
        if (rider) return;
        if (speed != 0) speed = 0;
        Fale(0, false);
        var p = home; p.y = waterY + Wave(home.x, home.z, Time.time) - draft;
        transform.position = p;
        transform.rotation = Quaternion.Euler(Mathf.Sin(Time.time * .9f + home.x) * (sit ? 1.2f : .5f), baseYaw, Mathf.Sin(Time.time * 1.3f + home.z) * (sit ? 1.6f : .7f));
    }
}
