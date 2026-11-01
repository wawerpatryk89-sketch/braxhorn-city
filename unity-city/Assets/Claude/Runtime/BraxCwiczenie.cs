using UnityEngine;

// BRAXHORN — ćwiczenia przy sprzęcie (właściciel 1.11, 18:18: „ruchy do postaci pod to wszystko”). Stanowisko w strefie siłowej:
// ławka (wyciskanie leżąc), klatka (przysiad ze sztangą na barkach), stojak z hantlami (uginanie ramion). Brax ma tylko klipy
// Idle/Walk/Run/Punch/Kick/Jump, więc ruch jest z kodu: po animacji (LateUpdate) kości rąk, nóg i kręgosłupa są celowane
// w kierunki zależne od fazy powtórzenia (jak BraxPose — kości szukane po końcówce nazwy, Mixamo), nogi w przysiadzie
// liczone prostym IK dwóch kości (stopy zostają na podłodze). Sztanga / hantle w dłoniach z prymitywów.
//  • AKCJA przy stanowisku (rejestr BraxInterakcje): „ĆWICZ · …” — start. Brax stoi / leży w miejscu (unieruchomiony tym samym
//    mechanizmem co siedzisko: BraxSeat ukryty, nieoferowany jako „USIĄDŹ”), w trakcie przycisk pokazuje „WSTAŃ” = koniec
//    (także wychylenie joysticka). Licznik powtórzeń na ekranie (pod przyszłe misje: `Powtorzenia`).
public sealed class BraxCwiczenie : MonoBehaviour, IBraxInterakcja
{
    public enum Rodzaj { Wyciskanie, Przysiad, Uginanie }
    public Rodzaj rodzaj;
    public float wysokosc = .46f;        // wierzch ławki (wyciskanie)
    public GameObject ukryj;             // sztanga leżąca na stojaku — chowana na czas ćwiczenia
    public Material chrom, guma;
    public int Powtorzenia { get; private set; }

    static readonly string[] Nazwy = { "WYCISKANIE", "PRZYSIAD", "UGINANIE" };
    static readonly float[] Okres = { 2.4f, 2.8f, 2.0f };
    BraxSeat miejsce; BraxGymPlayer gracz; bool trwa; float t0;
    Transform hips, spine, chest, head, lUp, lLeg, lFoot, rUp, rLeg, rFoot, lArm, lFore, lHand, rArm, rFore, rHand;
    GameObject sprzet, hL, hR; GUIStyle styl;

    void Awake()
    {
        var g = new GameObject("miejsce ćwiczenia"); g.transform.SetParent(transform, false);
        miejsce = g.AddComponent<BraxSeat>(); miejsce.enabled = false;   // nie trafia do listy siedzisk (OnDisable)
        miejsce.label = Nazwy[(int)rodzaj]; miejsce.lie = rodzaj == Rodzaj.Wyciskanie; miejsce.surfaceY = transform.position.y + wysokosc;
    }
    void OnEnable() { BraxInterakcje.Dodaj(this); }
    void OnDisable() { BraxInterakcje.Usun(this); Koniec(); }

    BraxGymPlayer Gracz() { if (!gracz) gracz = FindAnyObjectByType<BraxGymPlayer>(); return gracz; }

    public string Kontekst(Vector3 p)
    {
        if (trwa) return "";
        var d = p - transform.position; d.y = 0f; if (d.magnitude > 1.5f) return "";
        var g = Gracz(); if (!g || g.seat || g.vessel) return "";
        return "ĆWICZ · " + Nazwy[(int)rodzaj];
    }

    public bool Wykonaj(Vector3 p)
    {
        var g = Gracz(); if (!g || g.seat || g.vessel || trwa) return false;
        miejsce.Use(g);
        BraxPose.Get(g).Set(BraxPose.Mode.None, null);
        var f = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (rodzaj == Rodzaj.Wyciskanie)
        {
            g.transform.rotation = Quaternion.LookRotation(Vector3.up, -f);                       // na plecach, głowa w stronę −forward
            g.transform.position = transform.position + Vector3.up * (wysokosc + .14f) + f * 1.15f;
        }
        else { g.transform.rotation = Quaternion.LookRotation(f); g.transform.position = transform.position + Vector3.up * .02f; }
        Szukaj(g); trwa = true; t0 = Time.time; Powtorzenia = 0;
        Sprzet();
        if (ukryj) ukryj.SetActive(false);
        return true;
    }

    void Koniec()
    {
        trwa = false;
        if (sprzet) Destroy(sprzet); if (hL) Destroy(hL); if (hR) Destroy(hR);
        if (ukryj) ukryj.SetActive(true);
    }

    Transform Znajdz(Transform root, string key)
    {
        Transform zap = null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name; int i = n.LastIndexOf(':'); if (i >= 0) n = n.Substring(i + 1);
            if (n == key) return t;
            if (zap == null && n.EndsWith(key)) zap = t;
        }
        return zap;
    }
    void Szukaj(BraxGymPlayer g)
    {
        var v = g.visual ? g.visual : g.transform;
        hips = Znajdz(v, "Hips"); spine = Znajdz(v, "Spine"); chest = Znajdz(v, "Spine2") ?? Znajdz(v, "Spine1"); head = Znajdz(v, "Head");
        lUp = Znajdz(v, "LeftUpLeg"); lLeg = Znajdz(v, "LeftLeg"); lFoot = Znajdz(v, "LeftFoot");
        rUp = Znajdz(v, "RightUpLeg"); rLeg = Znajdz(v, "RightLeg"); rFoot = Znajdz(v, "RightFoot");
        lArm = Znajdz(v, "LeftArm"); lFore = Znajdz(v, "LeftForeArm"); lHand = Znajdz(v, "LeftHand");
        rArm = Znajdz(v, "RightArm"); rFore = Znajdz(v, "RightForeArm"); rHand = Znajdz(v, "RightHand");
    }

    // sztanga (gryf 2.0 m, tuleje, talerze) albo dwa hantle — z prymitywów, bez kolizji, bez cieni
    GameObject Walec(Transform parent, Vector3 lokalnie, float r, float dl, Material m)
    {
        var o = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(o.GetComponent<Collider>());
        o.transform.SetParent(parent, false); o.transform.localPosition = lokalnie; o.transform.localScale = new Vector3(2f * r, dl * .5f, 2f * r);
        var mr = o.GetComponent<MeshRenderer>(); if (m) mr.sharedMaterial = m; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return o;
    }
    GameObject Hantel(float masa)
    {
        var h = new GameObject("hantel"); float r = .05f + masa * .0022f;
        Walec(h.transform, Vector3.zero, .016f, .3f, chrom);
        foreach (float s in new[] { -1f, 1f }) Walec(h.transform, Vector3.up * (s * .12f), r, .07f, guma);
        return h;
    }
    void Sprzet()
    {
        if (rodzaj == Rodzaj.Uginanie) { hL = Hantel(15f); hR = Hantel(15f); return; }
        sprzet = new GameObject("sztanga (ćwiczenie)");
        Walec(sprzet.transform, Vector3.zero, .015f, 2.0f, chrom);
        foreach (float s in new[] { -1f, 1f })
        {
            Walec(sprzet.transform, Vector3.up * (s * .83f), .025f, .4f, chrom);
            Walec(sprzet.transform, Vector3.up * (s * .7f), .225f, .05f, guma);
            Walec(sprzet.transform, Vector3.up * (s * .76f), .2f, .04f, guma);
        }
    }

    static void Aim(Transform bone, Transform child, Vector3 want)
    {
        if (!bone || !child || want.sqrMagnitude < 1e-6f) return;
        var cur = child.position - bone.position; if (cur.sqrMagnitude < 1e-8f) return;
        bone.rotation = Quaternion.FromToRotation(cur.normalized, want.normalized) * bone.rotation;
    }
    // IK dwóch kości: biodro → kolano → stopa w punkcie `cel`, kolano w stronę `hint`
    static void Noga(Transform up, Transform leg, Transform foot, Vector3 cel, Vector3 hint)
    {
        if (!up || !leg || !foot) return;
        float l1 = Vector3.Distance(up.position, leg.position), l2 = Vector3.Distance(leg.position, foot.position);
        var a = up.position; var d = cel - a; float dl = Mathf.Clamp(d.magnitude, .01f, l1 + l2 - .001f); var dn = d.normalized;
        float x = (l1 * l1 - l2 * l2 + dl * dl) / (2f * dl), h = Mathf.Sqrt(Mathf.Max(0f, l1 * l1 - x * x));
        var b = Vector3.ProjectOnPlane(hint, dn).normalized; if (b.sqrMagnitude < .01f) b = Vector3.forward;
        var kolano = a + dn * x + b * h;
        Aim(up, leg, kolano - a); Aim(leg, foot, cel - leg.position);
    }

    void LateUpdate()
    {
        if (!trwa) return;
        var g = gracz;
        if (!g || g.seat != miejsce) { Koniec(); return; }
        float okres = Okres[(int)rodzaj], t = (Time.time - t0) / okres;
        Powtorzenia = Mathf.FloorToInt(t);
        float p = .5f - .5f * Mathf.Cos(t * Mathf.PI * 2f);    // 0 → 1 → 0 w każdym powtórzeniu
        var U = Vector3.up;
        if (rodzaj == Rodzaj.Wyciskanie)
        {
            var f = Vector3.ProjectOnPlane(transform.forward, U).normalized; var R = Vector3.Cross(U, f);   // prawa strona leżącego
            Aim(lUp, lLeg, f * .9f - R * .18f - U * .12f); Aim(rUp, rLeg, f * .9f + R * .18f - U * .12f);
            Aim(lLeg, lFoot, -U * .95f + f * .12f); Aim(rLeg, rFoot, -U * .95f + f * .12f);
            // p = 1: ręce wyprostowane (sztanga nad klatką), p = 0: sztanga przy klatce, łokcie w bok
            Aim(lArm, lFore, Vector3.Lerp(-R * .9f + U * .05f + f * .15f, U * .95f - R * .22f, p)); Aim(rArm, rFore, Vector3.Lerp(R * .9f + U * .05f + f * .15f, U * .95f + R * .22f, p));
            Aim(lFore, lHand, U * .95f - R * .1f); Aim(rFore, rHand, U * .95f + R * .1f);
        }
        else if (rodzaj == Rodzaj.Przysiad)
        {
            var F = g.transform.forward; var R = g.transform.right;
            Vector3 sL = lFoot ? lFoot.position : Vector3.zero, sR = rFoot ? rFoot.position : Vector3.zero;   // stopy z animacji (na podłodze)
            if (hips) hips.position -= U * (.45f * p) + F * (.12f * p);
            Aim(spine, chest ? chest : head, U * .93f + F * (.15f + .35f * p));
            Noga(lUp, lLeg, lFoot, sL, F - R * .35f); Noga(rUp, rLeg, rFoot, sR, F + R * .35f);
            Aim(lArm, lFore, -R * .75f - U * .25f - F * .35f); Aim(rArm, rFore, R * .75f - U * .25f - F * .35f);
            Aim(lFore, lHand, U * .9f - F * .2f + R * .1f); Aim(rFore, rHand, U * .9f - F * .2f - R * .1f);
        }
        else
        {
            var F = g.transform.forward; var R = g.transform.right;
            Aim(lArm, lFore, -U * .97f - R * .12f + F * .08f); Aim(rArm, rFore, -U * .97f + R * .12f + F * .08f);
            var przed = Vector3.Lerp(-U * .95f + F * .25f, U * .62f + F * .72f, p);
            Aim(lFore, lHand, przed); Aim(rFore, rHand, przed);
        }
        // sprzęt w dłoniach
        if (sprzet && lHand && rHand)
        {
            Vector3 mid = (lHand.position + rHand.position) * .5f, os = rHand.position - lHand.position;
            if (rodzaj == Rodzaj.Przysiad && head) { mid = head.position - U * .3f - g.transform.forward * .12f; os = g.transform.right; }
            if (os.sqrMagnitude < 1e-4f) os = g.transform.right;
            sprzet.transform.position = mid; sprzet.transform.rotation = Quaternion.FromToRotation(Vector3.up, os.normalized);
        }
        if (hL && lHand) { hL.transform.position = lHand.position; hL.transform.rotation = Quaternion.FromToRotation(Vector3.up, g.transform.right); }
        if (hR && rHand) { hR.transform.position = rHand.position; hR.transform.rotation = Quaternion.FromToRotation(Vector3.up, g.transform.right); }
    }

    void OnGUI()
    {
        if (!trwa) return;
        float s = Mathf.Min(Screen.width, Screen.height);
        if (styl == null) { styl = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(s * .05f) }; styl.normal.textColor = Color.white; }
        var r = new Rect(0, s * .1f, Screen.width, s * .08f);
        var old = GUI.color; GUI.color = new Color(0, 0, 0, .5f); GUI.DrawTexture(new Rect(Screen.width * .3f, r.y, Screen.width * .4f, r.height), Texture2D.whiteTexture); GUI.color = old;
        GUI.Label(r, Nazwy[(int)rodzaj] + " · " + Powtorzenia + " POWT.", styl);
    }
}
