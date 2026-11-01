using UnityEngine;

// BRAXHORN — liny ringu, które reagują (właściciel 1.11: „w ringu mają być białe, grube, grubsze liny… i mają reagować,
// jak się o nie obijasz”). Jeden komponent = jeden bok ringu (od słupka do słupka) z kilkoma linami jedna nad drugą
// i linkami łączącymi. Siatka liczona w kodzie (gładki walec, 18 odcinków na długość) i przeliczana tylko, gdy lina pracuje.
//  • Brax napierający na linę (kapsuła gracza): lina ugina się w punkcie kontaktu (jak prawdziwa lina — dwa proste odcinki
//    od słupków do miejsca nacisku), dokładnie tyle, ile ciało weszło za linię liny. Kolizja boku (pudło w edytorze, za liną
//    o maks. ugięcie) zatrzymuje gracza — z ringu się nie wypada.
//  • Po puszczeniu lina drga z tłumieniem (sprężyna ok. 3 Hz), każda trochę inaczej. CIOS / KOP w linę też ją buja.
//  • Koszt: nic, gdy lina stoi, a Brax jest dalej niż 2.5 m od boku.
[RequireComponent(typeof(MeshFilter))]
public sealed class BraxLina : MonoBehaviour
{
    public Vector3 a, b;                 // końce boku w świecie (oś liny na wysokości 0 — wysokości lin w `wysokosci`)
    public float[] wysokosci = { 1.3f, 1.62f, 1.94f, 2.26f };
    public float promien = .024f;        // średnica 4.8 cm
    public Vector3 naZewnatrz = Vector3.forward;   // poziomy kierunek „za ring”
    public float maksUgiecie = .2f;
    public int odcinki = 18, boki = 10;

    float[] u, pr;                       // ugięcie i prędkość każdej liny (m, m/s)
    float t0 = .5f;                      // miejsce ostatniego nacisku (0..1 wzdłuż boku)
    Vector3[] baza, teraz; float[] vt, vl; // pozycje bazowe, parametr wzdłuż (0..1) i „poziom” (indeks liny, ułamkowy dla linek)
    Mesh siatka; bool pracuje, prosta = true;
    static Transform gracz; static CharacterController cialo; static float szukaj;

    void Awake() { Zbuduj(); }
    void OnEnable() { BraxGymPlayer.OnCios += Cios; }
    void OnDisable() { BraxGymPlayer.OnCios -= Cios; }

    // Siatka w lokalnym układzie obiektu (obiekt stoi w (0,0,0) bez obrotu — końce w świecie = lokalne).
    public void Zbuduj()
    {
        int n = wysokosci.Length; u = new float[n]; pr = new float[n];
        var v = new System.Collections.Generic.List<Vector3>(); var nr = new System.Collections.Generic.List<Vector3>();
        var uv = new System.Collections.Generic.List<Vector2>(); var tr = new System.Collections.Generic.List<int>();
        var t = new System.Collections.Generic.List<float>(); var l = new System.Collections.Generic.List<float>();
        Vector3 os = b - a; float dl = os.magnitude; os /= Mathf.Max(1e-4f, dl);
        Vector3 bok = Vector3.Cross(Vector3.up, os).normalized, gor = Vector3.up;
        for (int i = 0; i < n; i++)
        {
            int k0 = v.Count;
            for (int s = 0; s <= odcinki; s++)
            {
                float ts = (float)s / odcinki; var c = a + os * (dl * ts) + Vector3.up * wysokosci[i];
                for (int q = 0; q <= boki; q++)
                {
                    float ang = q * Mathf.PI * 2f / boki; var d = bok * Mathf.Cos(ang) + gor * Mathf.Sin(ang);
                    v.Add(c + d * promien); nr.Add(d); uv.Add(new Vector2(dl * ts / .3f, (float)q / boki)); t.Add(ts); l.Add(i);
                }
            }
            for (int s = 0; s < odcinki; s++)
                for (int q = 0; q < boki; q++)
                {
                    int p0 = k0 + s * (boki + 1) + q, p1 = p0 + boki + 1;
                    tr.AddRange(new[] { p0, p0 + 1, p1, p0 + 1, p1 + 1, p1 });
                }
        }
        // linki łączące (płaskie taśmy 3 × 0.6 cm) na 1/3 i 2/3 długości, od najniższej do najwyższej liny
        if (n > 1)
            foreach (float ts in new[] { 1f / 3f, 2f / 3f })
            {
                var c = a + os * (dl * ts);
                for (int side = -1; side <= 1; side += 2)
                {
                    int k0 = v.Count; var off = bok * (promien + .004f) * side;
                    foreach (var (y, lv) in new[] { (wysokosci[0] - .03f, 0f), (wysokosci[n - 1] + .03f, (float)(n - 1)) })
                        foreach (float w in new[] { -.015f, .015f })
                        { v.Add(c + off + os * w + Vector3.up * y); nr.Add(bok * side); uv.Add(new Vector2(w, y)); t.Add(ts); l.Add(Mathf.Clamp(lv, 0, n - 1)); }
                    // 0: dół-lewo, 1: dół-prawo, 2: góra-lewo, 3: góra-prawo
                    if (side > 0) tr.AddRange(new[] { k0, k0 + 2, k0 + 1, k0 + 1, k0 + 2, k0 + 3 });
                    else tr.AddRange(new[] { k0, k0 + 1, k0 + 2, k0 + 1, k0 + 3, k0 + 2 });
                }
            }
        baza = v.ToArray(); teraz = v.ToArray(); vt = t.ToArray(); vl = l.ToArray();
        siatka = new Mesh { name = "Liny ringu (bok)" };
        siatka.MarkDynamic();
        siatka.SetVertices(v); siatka.SetNormals(nr); siatka.SetUVs(0, uv); siatka.SetTriangles(tr, 0);
        siatka.RecalculateBounds();
        var bb = siatka.bounds; bb.Encapsulate(bb.center + naZewnatrz * (maksUgiecie + .1f)); bb.Encapsulate(bb.center - naZewnatrz * (maksUgiecie + .1f)); siatka.bounds = bb;
        GetComponent<MeshFilter>().sharedMesh = siatka;
    }

    static void ZnajdzGracza()
    {
        if (gracz || Time.unscaledTime < szukaj) return;
        szukaj = Time.unscaledTime + 1f;
        var p = FindAnyObjectByType<BraxGymPlayer>();
        if (p) { gracz = p.transform; cialo = p.GetComponent<CharacterController>(); }
    }

    // Położenie gracza względem boku: t (0..1 wzdłuż), q = odległość od linii liny w stronę ringu (dodatnia w środku).
    bool Wzgledem(Vector3 p, out float t, out float q)
    {
        Vector3 os = b - a; os.y = 0; float dl2 = os.sqrMagnitude; var d = p - a; d.y = 0;
        t = dl2 > 1e-6f ? Vector3.Dot(d, os) / dl2 : .5f; q = -Vector3.Dot(d, naZewnatrz);
        return t > -.05f && t < 1.05f;
    }

    void Cios(Vector3 kto, Vector3 kierunek, bool kop)
    {
        if (!isActiveAndEnabled || u == null) return;
        if (!Wzgledem(kto, out float t, out float q) || q < 0f || q > 1.3f) return;
        kierunek.y = 0f; if (Vector3.Dot(kierunek.normalized, naZewnatrz) < .4f) return;
        t0 = Mathf.Clamp(t, .08f, .92f);
        for (int i = 0; i < pr.Length; i++) pr[i] += (kop ? (i < 2 ? 1.1f : .5f) : (i >= 1 && i <= 2 ? .9f : .45f));
        pracuje = true;
    }

    void Update()
    {
        if (u == null) return;
        ZnajdzGracza();
        float dt = Mathf.Min(Time.deltaTime, .033f);
        bool kontakt = false; float[] cel = null; float t = .5f, q = 99f;
        bool obok = gracz != null && Wzgledem(gracz.position, out t, out q);
        if (obok && q > -.5f && q < 2.5f)
        {
            pracuje = true;
            float r = cialo ? cialo.radius : .35f, y0 = gracz.position.y, y1 = y0 + (cialo ? cialo.height : 2.25f);
            float wej = r + promien - q;                   // ile ciało weszło za linię liny
            if (wej > 0f && q > 0f && t > .02f && t < .98f)   // tylko od środka ringu (z zewnątrz Brax stoi niżej, na podłodze hali)
            {
                kontakt = true; t0 = Mathf.Clamp(t, .08f, .92f);
                cel = new float[u.Length];
                for (int i = 0; i < u.Length; i++)
                {
                    float y = transform.position.y + wysokosci[i];
                    // nacisk tylko tam, gdzie ciało jest na wysokości liny (pełny w pasie, mniejszy przy kostkach i głowie)
                    float h = Mathf.InverseLerp(y0, y1, y); float w = h < .15f || h > .95f ? 0f : Mathf.Clamp01(Mathf.Min(h - .15f, .95f - h) * 6f);
                    cel[i] = Mathf.Min(maksUgiecie, wej * w);
                }
            }
        }
        if (!pracuje) return;
        float ruch = 0f;
        for (int i = 0; i < u.Length; i++)
        {
            float k = 340f * (1f + .08f * i), c = 4.2f;      // ok. 3 Hz, każda lina trochę inaczej
            float acc;
            if (kontakt && cel[i] > u[i]) { acc = (cel[i] - u[i]) * 2500f - pr[i] * 60f; }   // ciało pcha linę: lina „przykleja się” do ciała
            else acc = -k * u[i] - c * pr[i];
            pr[i] += acc * dt; u[i] += pr[i] * dt;
            u[i] = Mathf.Clamp(u[i], -maksUgiecie * .6f, maksUgiecie);
            ruch += Mathf.Abs(u[i]) + Mathf.Abs(pr[i]) * .05f;
        }
        if (!kontakt && ruch < 1e-4f)
        {
            for (int i = 0; i < u.Length; i++) { u[i] = 0f; pr[i] = 0f; }
            float qq = 99f; pracuje = gracz != null && Wzgledem(gracz.position, out _, out qq) && qq < 1.2f;
            if (!prosta) { siatka.SetVertices(baza); prosta = true; }   // lina stoi — siatka prosta, bez przeliczania co klatkę
            return;
        }
        prosta = false;
        // przeliczenie wierzchołków: kształt liny naciśniętej w t0 (dwa proste odcinki od słupków), łagodnie zaokrąglony
        int n = u.Length;
        for (int j = 0; j < baza.Length; j++)
        {
            float ts = vt[j], ksz = ts < t0 ? ts / t0 : (1f - ts) / (1f - t0);
            ksz = Mathf.Clamp01(ksz); ksz = ksz * (1.25f - .25f * ksz);   // lekko zaokrąglony szczyt
            float lv = vl[j]; int i0 = Mathf.Clamp(Mathf.FloorToInt(lv), 0, n - 1), i1 = Mathf.Min(i0 + 1, n - 1);
            float ug = Mathf.Lerp(u[i0], u[i1], lv - i0);
            teraz[j] = baza[j] + naZewnatrz * (ug * ksz);
        }
        siatka.SetVertices(teraz);
    }
}
