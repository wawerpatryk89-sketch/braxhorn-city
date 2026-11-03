using System.Collections.Generic;
using UnityEngine;

// AUTOBUS OSIEDLA (runda 6, E23). Jedzie po ścieżce (linia prawego pasa z tras ruchu, BraxRuchAut), zatrzymuje się przy
// przystankach drzwiami przy wiacie, otwiera drzwi (skrzydła odsuwane), wypuszcza pasażera (pojawia się przy drzwiach
// i odchodzi 1.5 m w stronę wiaty), zabiera czekających (znikają przy drzwiach po kolei), zamyka drzwi i rusza.
// Ruch: przyspieszenie / hamowanie, w zakrętach do vZakret, hamuje za autami AI (BraxRuchAut) i przed graczem; auta AI
// widzą autobus jako przeszkodę (BraxRuchAut dodaje Wszystkie do obrysów zewnętrznych). Ścieżka otwarta (linia B):
// na końcu autobus czeka, aż zniknie z kadru, i wjeżdża od początku (jak auta B na ślepej ulicy).
public sealed class BraxAutobus : MonoBehaviour
{
    public static readonly List<BraxAutobus> Wszystkie = new List<BraxAutobus>();
    [System.Serializable] public class Przystanek
    {
        public string nazwa; public float s; public float postoj = 9f;
        public Transform[] czekajacy = new Transform[0];   // znikają przy drzwiach (wsiadają)
        public Transform[] wysiadajacy = new Transform[0]; // nieaktywni do przyjazdu; wysiadają i zostają przy wiacie
        public Vector3 wiata;
    }
    public Vector3[] sciezka = new Vector3[0];
    public bool petla = true;
    public Przystanek[] przystanki = new Przystanek[0];
    public Transform[] skrzydla = new Transform[0];   // drzwi: skrzydła przesuwane wzdłuż boku (lokalne +z / −z naprzemiennie)
    public Transform[] kola = new Transform[0];
    public Transform drzwiPunkt;                       // punkt przy przednich drzwiach (na chodniku)
    public float dl = 10.3f, szer = 3.1f, vmax = 10.8f, vZakret = 5.4f, przyspieszenie = 1.2f, hamowanie = 2.4f, rKola = .65f;
    public BoxCollider obrys;
    public float s, v;

    float[] odc; float L; int nast; float tPostoj = -1f; bool drzwiOtw; float drzwiStan; Vector3[] skrzydlaZamk; BraxRuchAut ruch; BraxGymPlayer gracz; float szukaj;
    Renderer[] rend; AudioSource dzwiek; static AudioClip syk;
    // dźwięk drzwi pneumatycznych (syk 0.7 s: szum filtrowany z opadającą obwiednią + klik zamka) — synteza, bez nagrań
    static AudioClip Syk()
    {
        if (syk) return syk;
        const int sr = 22050; int n = (int)(.7f * sr); var x = new float[n]; var r = new System.Random(31); float lp = 0f, hp = 0f, prev = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)sr, w = (float)(r.NextDouble() * 2 - 1); lp += .35f * (w - lp); hp = .92f * (hp + lp - prev); prev = lp;
            x[i] = hp * Mathf.Clamp01(t / .03f) * Mathf.Exp(-t * 4.5f) * .8f + (t > .62f && t < .64f ? (float)(r.NextDouble() * 2 - 1) * .6f : 0f);
        }
        syk = AudioClip.Create("autobus: drzwi (syk)", n, 1, sr, false); syk.SetData(x, 0); return syk;
    }
    void Graj() { if (dzwiek) dzwiek.PlayOneShot(Syk(), .8f); }

    void OnEnable() { if (!Wszystkie.Contains(this)) Wszystkie.Add(this); }
    void OnDisable() { Wszystkie.Remove(this); }

    void Start()
    {
        if (sciezka == null || sciezka.Length < 2) { enabled = false; return; }
        odc = new float[sciezka.Length]; L = 0f;
        for (int i = 1; i < sciezka.Length; i++) { L += Vector3.Distance(sciezka[i - 1], sciezka[i]); odc[i] = L; }
        skrzydlaZamk = new Vector3[skrzydla.Length]; for (int i = 0; i < skrzydla.Length; i++) if (skrzydla[i]) skrzydlaZamk[i] = skrzydla[i].localPosition;
        rend = GetComponentsInChildren<Renderer>();
        dzwiek = gameObject.AddComponent<AudioSource>(); dzwiek.spatialBlend = 1f; dzwiek.rolloffMode = AudioRolloffMode.Linear; dzwiek.minDistance = 3f; dzwiek.maxDistance = 30f; dzwiek.dopplerLevel = 0f; dzwiek.playOnAwake = false;
        foreach (var p in przystanki) foreach (var w in p.wysiadajacy) if (w) w.gameObject.SetActive(false);
        nast = NastepnyPrzystanek(s);
        Ustaw();
    }
    int NastepnyPrzystanek(float od)
    {
        int best = -1; float bd = float.MaxValue;
        for (int i = 0; i < przystanki.Length; i++) { float d = przystanki[i].s - od; if (d < -.5f) d += petla ? L : 1e6f; if (d < bd) { bd = d; best = i; } }
        return best;
    }
    Vector3 P(float x, out Vector3 kier)
    {
        if (petla) { x %= L; if (x < 0) x += L; } else x = Mathf.Clamp(x, 0f, L);
        int lo = 1, hi = sciezka.Length - 1;
        while (lo < hi) { int m = (lo + hi) / 2; if (odc[m] < x) lo = m + 1; else hi = m; }
        float d = Mathf.Max(1e-4f, odc[lo] - odc[lo - 1]); kier = (sciezka[lo] - sciezka[lo - 1]) / d;
        return Vector3.Lerp(sciezka[lo - 1], sciezka[lo], Mathf.Clamp01((x - odc[lo - 1]) / d));
    }
    // ustawienie nadwozia: oś przednia i tylna na ścieżce (bez „ślizgu” tyłu w zakrętach)
    void Poza(float dystans, out Vector3 poz, out Vector3 kier)
    {
        var przod = P(dystans + dl * .25f, out _); var tyl = P(dystans - dl * .25f, out _);
        var f = przod - tyl; f.y = 0f; if (f.sqrMagnitude < 1e-6f) P(dystans, out f);
        poz = (przod + tyl) * .5f; kier = f.sqrMagnitude > 1e-6f ? f.normalized : transform.forward;
    }
    void Ustaw() { Poza(s, out var p, out var f); transform.SetPositionAndRotation(p, Quaternion.LookRotation(f, Vector3.up)); }
    bool Widoczny() { if (rend != null) foreach (var r in rend) if (r && r.isVisible) return true; return false; }

    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, .1f); if (dt <= 0f || odc == null) return;
        if (!ruch) ruch = FindAnyObjectByType<BraxRuchAut>();
        if (!gracz && Time.time > szukaj) { gracz = FindAnyObjectByType<BraxGymPlayer>(); szukaj = Time.time + 2f; }
        Drzwi(dt);
        if (tPostoj >= 0f) { Postoj(dt); return; }
        // koniec ścieżki otwartej: czeka poza kadrem i wjeżdża od nowa
        if (!petla && s >= L - .05f) { v = 0f; if (!Widoczny()) { s = 0f; nast = NastepnyPrzystanek(0f); Ustaw(); } return; }
        float cel = vmax;
        // zakręt przed nami (kąt między kierunkiem teraz i za 12 m)
        P(s, out var k0); P(s + 12f, out var k1); if (Vector3.Angle(k0, k1) > 25f) cel = Mathf.Min(cel, vZakret);
        // przystanek: hamowanie do zera w punkcie postoju
        if (nast >= 0)
        {
            float d = przystanki[nast].s - s; if (petla && d < -1f) d += L;
            if (d >= 0f) cel = Mathf.Min(cel, Mathf.Sqrt(2f * hamowanie * .8f * Mathf.Max(0f, d)));
            if (d < .3f && v < .6f) { v = 0f; tPostoj = 0f; drzwiOtw = true; Graj(); return; }
        }
        // Pełne obrysy pojazdów na trasie w zasięgu drogi hamowania oraz aktywne linie zatrzymania.
        bool przeszkoda = Przed(out float odl);
        if (przeszkoda) cel = Mathf.Min(cel, Mathf.Sqrt(2f * hamowanie * Mathf.Max(0f, odl - 1.4f)));
        v = Mathf.MoveTowards(v, Mathf.Max(0f, cel), (cel > v ? przyspieszenie : hamowanie * (cel < v - 3f ? 1.4f : 1f)) * dt);
        float krok = v * dt;
        if (przeszkoda && krok > Mathf.Max(0f, odl - .25f)) { krok = Mathf.Max(0f, odl - .25f); v = krok / dt; }
        s += krok; if (!petla) s = Mathf.Min(s, L);
        Ustaw();
        foreach (var k in kola) if (k) k.Rotate(transform.right, v * dt / Mathf.Max(.1f, rKola) * Mathf.Rad2Deg, Space.World);
    }
    bool Przed(out float odl)
    {
        // Mierzymy obrys całego nadwozia po zakrzywionej trasie, nie punkt środka auta przed autobusem.
        float zasieg = Mathf.Max(12f, v * v / (2f * Mathf.Max(.1f, hamowanie)) + 4f);
        for (float d = 0f; d <= zasieg; d += .5f)
        {
            Poza(s + d, out var c, out var f);
            bool Trafia(Vector3 p, Vector3 kier, float hw, float hl) => Mathf.Abs(c.y - p.y) < 3f
                && BraxRuchAutSym.Prost(c, f, szer * .5f + .1f, dl * .5f + .1f, p, kier, hw, hl);
            bool kol = false;
            if (ruch && ruch.Symulacja != null)
                foreach (var a in ruch.Symulacja.auta) if (Trafia(a.poz, a.kier, a.szer * .5f, a.dl * .5f)) { kol = true; break; }
            if (!kol) foreach (var a in BraxAutoGracza.Wszystkie)
                if (a && a.obrys && Trafia(a.transform.TransformPoint(a.obrys.center), a.transform.forward,
                    a.obrys.size.x * Mathf.Abs(a.transform.lossyScale.x) * .5f, a.obrys.size.z * Mathf.Abs(a.transform.lossyScale.z) * .5f)) { kol = true; break; }
            if (!kol) foreach (var a in Wszystkie)
                if (a && a != this && Trafia(a.transform.position, a.transform.forward, a.szer * .5f, a.dl * .5f)) { kol = true; break; }
            if (!kol && gracz && !gracz.autoGracza) kol = Trafia(gracz.transform.position, Vector3.forward, .45f, .45f);
            var przod = P(s + d + dl * .5f, out var kierPrzodu);
            if (kol || BraxStrefyZatrzymania.CzyStop(przod, kierPrzodu, false))
            { odl = Mathf.Max(0f, d - .5f); return true; }
        }
        odl = float.PositiveInfinity; return false;
    }
    void Postoj(float dt)
    {
        var p = przystanki[nast]; tPostoj += dt;
        // wysiadanie: po 1 s pierwszy wysiadający pojawia się przy drzwiach i odchodzi 1.5 m w stronę wiaty
        for (int i = 0; i < p.wysiadajacy.Length; i++)
        {
            var w = p.wysiadajacy[i]; if (!w) continue; float t0 = 1f + i * 1.2f;
            if (tPostoj >= t0 && !w.gameObject.activeSelf && drzwiPunkt) { w.position = drzwiPunkt.position; w.gameObject.SetActive(true); }
            if (w.gameObject.activeSelf && tPostoj < t0 + 1.6f && drzwiPunkt)
            {
                var cel = Vector3.Lerp(drzwiPunkt.position, p.wiata, .5f); cel.y = w.position.y;
                w.position = Vector3.MoveTowards(w.position, cel, dt * 1.1f);
                var kier = cel - w.position; kier.y = 0; if (kier.sqrMagnitude > 1e-4f) w.rotation = Quaternion.LookRotation(kier);
            }
        }
        // wsiadanie: czekający znikają przy drzwiach po kolei (od 3 s)
        for (int i = 0; i < p.czekajacy.Length; i++)
        {
            var c = p.czekajacy[i]; if (!c || !c.gameObject.activeSelf) continue;
            float t0 = 3f + i * 1.3f;
            if (tPostoj >= t0 - 1.2f && drzwiPunkt) { var cel = drzwiPunkt.position; cel.y = c.position.y; c.position = Vector3.MoveTowards(c.position, cel, dt * 1.3f); var kier = cel - c.position; kier.y = 0; if (kier.sqrMagnitude > 1e-4f) c.rotation = Quaternion.LookRotation(kier); }
            if (tPostoj >= t0) c.gameObject.SetActive(false);
        }
        if (tPostoj >= p.postoj - 1.2f && drzwiOtw) { drzwiOtw = false; Graj(); }
        if (tPostoj >= p.postoj)
        {
            // zamiana ról: wysiadający czekają na następny autobus, czekający (w autobusie) wysiądą na kolejnym okrążeniu
            var t = p.czekajacy; p.czekajacy = p.wysiadajacy; p.wysiadajacy = t;
            tPostoj = -1f; s += .4f; nast = NastepnyPrzystanek(s);
        }
    }
    void Drzwi(float dt)
    {
        drzwiStan = Mathf.MoveTowards(drzwiStan, drzwiOtw ? 1f : 0f, dt / .9f);
        for (int i = 0; i < skrzydla.Length; i++)
            if (skrzydla[i])
            {   // przesunięcie w układzie autobusu (na zewnątrz +x, wzdłuż ±z) przeliczone na układ rodzica skrzydła (model może mieć inne osie i skalę)
                var w = transform.TransformVector(new Vector3(.06f * Mathf.Clamp01(drzwiStan * 3f), 0f, (i % 2 == 0 ? 1f : -1f) * .55f * drzwiStan));
                var par = skrzydla[i].parent; skrzydla[i].localPosition = skrzydlaZamk[i] + (par ? par.InverseTransformVector(w) : w);
            }
    }
}
