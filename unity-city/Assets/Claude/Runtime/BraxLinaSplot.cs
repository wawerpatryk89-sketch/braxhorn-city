using UnityEngine;

// BRAXHORN — bok ringu z LINAMI SKRĘCANYMI ZE SPLOTEK, które pracują (MIASTO R1, sala kickboxingu na faweli).
// Właściciel 10.11 00:28: „liny mają pracować, mają być aktywne, jak coś się o nie obija”; 07:13: „na około prawdziwe linki”.
// Geometria: BraxLinaSplotGeom (3 splotki śrubowo, paski z taśmy spinające liny na 1/3 i 2/3). Fizyka jak w BraxLina z sali osiedla
// (tamtej klasy nie zmieniamy — inna sala, inny wygląd lin):
//  • Brax napierający na linę od środka ringu: lina ugina się w punkcie kontaktu (dwa proste odcinki od słupków do miejsca
//    nacisku) dokładnie tyle, ile ciało weszło za linię liny; kolizja boku (pudło za liną) zatrzymuje gracza.
//  • Po puszczeniu lina drga z tłumieniem (ok. 3 Hz), każda trochę inaczej. Cios / kop w linę też ją buja.
//  • Koszt: nic, gdy lina stoi, a Brax jest dalej niż 2.5 m od boku (siatka prosta, bez przeliczania).
[RequireComponent(typeof(MeshFilter))]
public sealed class BraxLinaSplot : MonoBehaviour
{
    public Vector3 a, b;                 // końce boku w świecie (oś liny na wysokości 0 — wysokości lin w `wysokosci`)
    public float[] wysokosci = { 1.3f, 1.62f, 1.94f, 2.26f };
    public float promien = .028f;        // średnica 5.6 cm (stara gruba lina)
    public float skok = .2f;             // długość jednego skrętu splotek
    public Vector3 naZewnatrz = Vector3.forward;   // poziomy kierunek „za ring”
    public float maksUgiecie = .2f;
    public Color barwa = Color.white, pasek = new Color(.28f, .2f, .14f);

    float[] u, pr;                       // ugięcie i prędkość każdej liny (m, m/s)
    float t0 = .5f;                      // miejsce ostatniego nacisku (0..1 wzdłuż boku)
    Vector3[] baza, teraz; float[] vt, vl;
    Mesh siatka; bool pracuje, prosta = true;
    static Transform gracz; static CharacterController cialo; static float szukaj;

    void Awake() { Zbuduj(); }
    void OnEnable() { BraxGymPlayer.OnCios += Cios; }
    void OnDisable() { BraxGymPlayer.OnCios -= Cios; }

    // Siatka w lokalnym układzie obiektu (obiekt stoi w (0,0,0) bez obrotu — końce w świecie = lokalne).
    public void Zbuduj()
    {
        int n = wysokosci.Length; u = new float[n]; pr = new float[n];
        var g = new BraxLinaSplotGeom.Wynik();
        for (int i = 0; i < n; i++)
            BraxLinaSplotGeom.Lina(g, new[] { a + Vector3.up * wysokosci[i], b + Vector3.up * wysokosci[i] }, promien, skok, barwa, i * 1.9f, i);
        BraxLinaSplotGeom.Spinki(g, a, b, wysokosci, promien, pasek);
        baza = g.v.ToArray(); teraz = g.v.ToArray(); vt = g.t.ToArray(); vl = g.l.ToArray();
        siatka = new Mesh { name = "Liny ringu ze splotek (bok)" };
        if (g.v.Count > 65000) siatka.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        siatka.MarkDynamic();
        siatka.SetVertices(g.v); siatka.SetNormals(g.n); siatka.SetUVs(0, g.uv); siatka.SetColors(g.c); siatka.SetTriangles(g.tr, 0);
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
            if (wej > 0f && q > 0f && t > .02f && t < .98f)   // tylko od środka ringu
            {
                kontakt = true; t0 = Mathf.Clamp(t, .08f, .92f);
                cel = new float[u.Length];
                for (int i = 0; i < u.Length; i++)
                {
                    float y = transform.position.y + wysokosci[i];
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
            if (kontakt && cel[i] > u[i]) { acc = (cel[i] - u[i]) * 2500f - pr[i] * 60f; }
            else acc = -k * u[i] - c * pr[i];
            pr[i] += acc * dt; u[i] += pr[i] * dt;
            u[i] = Mathf.Clamp(u[i], -maksUgiecie * .6f, maksUgiecie);
            ruch += Mathf.Abs(u[i]) + Mathf.Abs(pr[i]) * .05f;
        }
        if (!kontakt && ruch < 1e-4f)
        {
            for (int i = 0; i < u.Length; i++) { u[i] = 0f; pr[i] = 0f; }
            float qq = 99f; pracuje = gracz != null && Wzgledem(gracz.position, out _, out qq) && qq < 1.2f;
            if (!prosta) { siatka.SetVertices(baza); prosta = true; }
            return;
        }
        prosta = false;
        int n = u.Length;
        for (int j = 0; j < baza.Length; j++)
        {
            float ts = vt[j], ksz = ts < t0 ? ts / t0 : (1f - ts) / (1f - t0);
            ksz = Mathf.Clamp01(ksz); ksz = ksz * (1.25f - .25f * ksz);
            float lv = vl[j]; int i0 = Mathf.Clamp(Mathf.FloorToInt(lv), 0, n - 1), i1 = Mathf.Min(i0 + 1, n - 1);
            float ug = Mathf.Lerp(u[i0], u[i1], lv - i0);
            teraz[j] = baza[j] + naZewnatrz * (ug * ksz);
        }
        siatka.SetVertices(teraz);
    }
}
