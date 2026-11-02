using UnityEngine;

// RUCH AUT NA OSIEDLU (build 14) — jeden komponent na scenę: trasy (linie prawego pasa z łukami i rondem), zdarzenia
// (linie zatrzymania, rondo, kierunkowskazy, końce tras), skrzyżowania i lista aut. Symulacja w BraxRuchAutSym (ta sama co
// w teście CI), tu: kamera (auta przestawiane tylko poza jej widokiem), gracz (auto hamuje i trąbi), ustawienie aut
// i ich wyglądu (koła, światła, kierunkowskazy, spaliny, dźwięk) — dodatki tylko blisko kamery.
public sealed class BraxRuchAut : MonoBehaviour
{
    public Vector3[] punkty = new Vector3[0];
    public BraxTrasaAut[] trasy = new BraxTrasaAut[0];
    public BraxZdarzenieAut[] zdarzenia = new BraxZdarzenieAut[0];
    public BraxWezelAut[] wezly = new BraxWezelAut[0];
    public BraxAuto[] auta = new BraxAuto[0];
    public int ziarno = 2026;
    public float vZakret;   // E3d/e: prędkość w zakręcie (ustawia generator osiedla: 15 km/h × K)

    BraxRuchAutSym sym;
    Camera kam; Transform gracz; float szukaj;
    public BraxRuchAutSym Symulacja => sym;

    void Start()
    {
        if (trasy == null || trasy.Length == 0 || auta == null || auta.Length == 0) { enabled = false; return; }
        float szer = 1.6f, dl = 3.5f;
        foreach (var a in auta) if (a) { szer = Mathf.Max(szer, a.szer); dl = Mathf.Max(dl, a.dl); }
        sym = new BraxRuchAutSym(punkty, trasy, zdarzenia, wezly, ziarno, szer, dl, vZakret);
        foreach (var a in auta) if (a) sym.Dodaj(a.trasaNr, a.sStart, a.dl, a.szer, a.rozstaw, a.osTyl, a.vmax); else sym.Dodaj(0, 0f, 4.5f, 1.9f, 2.7f, -1.3f, 8f);
        sym.UstawStart();
        sym.widoczne = Widoczne;
        for (int i = 0; i < auta.Length; i++) if (auta[i]) auta[i].Ustaw(sym.auta[i], 0f, 999f, true);
    }

    bool Widoczne(Vector3 p)
    {
        if (!kam) return false;
        var d = p - kam.transform.position;
        if (d.sqrMagnitude < 30f * 30f) return true;
        if (d.sqrMagnitude > 170f * 170f) return false;
        var v = kam.WorldToViewportPoint(p);
        return v.z > 0f && v.x > -.15f && v.x < 1.15f && v.y > -.2f && v.y < 1.2f;
    }

    void Update()
    {
        if (sym == null) return;
        if (!kam) kam = Camera.main;
        if (!gracz && Time.time > szukaj) { var p = FindAnyObjectByType<BraxGymPlayer>(); if (p) gracz = p.transform; szukaj = Time.time + 2f; }
        sym.graczJest = gracz; if (gracz) sym.gracz = gracz.position;
        sym.zewnetrzne.Clear();
        foreach (var car in BraxAutoGracza.Wszystkie)
            if (car && car.obrys) sym.zewnetrzne.Add(new BraxRuchAutSym.ObrysZewnetrzny {
                srodek=car.transform.TransformPoint(car.obrys.center), przod=car.transform.forward,
                polSzer=car.obrys.size.x*.5f, polDl=car.obrys.size.z*.5f });
        float dt = Mathf.Min(Time.deltaTime, .1f);
        sym.Krok(dt);
        Vector3 kp = kam ? kam.transform.position : new Vector3(1e6f, 0f, 1e6f);
        for (int i = 0; i < auta.Length; i++)
        {
            var a = auta[i]; if (!a) continue;
            var st = sym.auta[i];
            a.Ustaw(st, dt, (st.poz - kp).magnitude, st.przestawiony);
        }
    }
}
