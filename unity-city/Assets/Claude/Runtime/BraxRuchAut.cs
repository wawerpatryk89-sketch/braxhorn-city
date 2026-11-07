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
    // Domyślnie wyłączone — kompatybilność z osiedlem i innymi rejonami.
    public int autobusIndex = -1, autobusTrasa = -1;
    public float autobusStopS = -1f, autobusStopCzas = 8f;
    float autobusKoniecPostoju, autobusMaxV; bool autobusObsłużony, autobusStoi;

    BraxRuchAutSym sym;
    Camera kam; Transform gracz; float szukaj;
    public BraxRuchAutSym Symulacja => sym;

    void Start()
    {
        if (trasy == null || trasy.Length == 0 || auta == null || auta.Length == 0) { enabled = false; return; }
        float szer = 1.6f, dl = 3.5f;
        foreach (var a in auta) if (a) { szer = Mathf.Max(szer, a.szer); dl = Mathf.Max(dl, a.dl); }
        sym = new BraxRuchAutSym(punkty, trasy, zdarzenia, wezly, ziarno, szer, dl);
        foreach (var a in auta) if (a) sym.Dodaj(a.trasaNr, a.sStart, a.dl, a.szer, a.rozstaw, a.osTyl, a.vmax); else sym.Dodaj(0, 0f, 4.5f, 1.9f, 2.7f, -1.3f, 8f);
        sym.UstawStart();
        if (autobusIndex >= 0 && autobusIndex < sym.auta.Count)
            autobusMaxV = sym.auta[autobusIndex].vmax;
        sym.widoczne = Widoczne;
        for (int i = 0; i < auta.Length; i++) if (auta[i]) auta[i].Ustaw(sym.auta[i], 0f, 999f, true);
    }

    void AutobusPostoj()
    {
        if (sym == null || autobusIndex < 0 || autobusIndex >= sym.auta.Count ||
            autobusTrasa < 0 || autobusTrasa >= trasy.Length || autobusStopS < 0f) return;
        var a = sym.auta[autobusIndex];
        float length = trasy[autobusTrasa].dl;
        if (length <= 1f) return;
        float ahead = Mathf.Repeat(autobusStopS - a.s, length);
        if (autobusStoi)
        {
            a.vmax = 0f;
            if (Time.time >= autobusKoniecPostoju)
            {
                autobusStoi = false; autobusObsłużony = true;
                a.vmax = autobusMaxV;
                Debug.Log("BRAXHORN_BUS odjazd z ALEI CENTRALNEJ");
            }
            return;
        }
        if (autobusObsłużony)
        {
            a.vmax = autobusMaxV;
            if (ahead > 25f && ahead < length - 25f) autobusObsłużony = false;
            return;
        }
        if (ahead <= 28f)
        {
            // Hamowanie w czasie; nie teleportuj modelu ani nie zamykaj oczu
            // symulacji na inne pojazdy podczas wjazdu do zatoki.
            float allowed = Mathf.Sqrt(Mathf.Max(0f, 2f * 1.8f * (ahead - .30f)));
            a.vmax = Mathf.Min(autobusMaxV, allowed);
            if (ahead <= .60f && a.v < .35f)
            {
                autobusStoi = true; a.vmax = 0f;
                autobusKoniecPostoju = Time.time + Mathf.Max(4f, autobusStopCzas);
                Debug.Log("BRAXHORN_BUS POSTÓJ ALEJA CENTRALNA " +
                    autobusStopCzas.ToString("F0") + "s");
            }
        }
        else a.vmax = autobusMaxV;
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
        float dt = Mathf.Min(Time.deltaTime, .1f);
        AutobusPostoj();
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
