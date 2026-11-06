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
    public float promienWlaczeniaRonda=10.6f;

    BraxRuchAutSym sym;
    Camera kam; Transform gracz; BraxGymPlayer graczKomponent; float szukaj;
    public BraxRuchAutSym Symulacja => sym;
    readonly Collider[] przeszkody = new Collider[64];

    bool PrzeszkodaSceny(Vector3 p, Vector3 forward, float halfWidth, float halfLength)
    {
        // Above road/kerbs, below car roof. Do not exclude parked cars or people.
        int count=Physics.OverlapBoxNonAlloc(p+Vector3.up*.95f,new Vector3(halfWidth,.60f,halfLength),
            przeszkody,Quaternion.LookRotation(forward),~(1<<29),QueryTriggerInteraction.Ignore);
        if(count==przeszkody.Length)return true;
        for(int i=0;i<count;i++)
        {
            var c=przeszkody[i];if(!c)continue;
            var ai=c.GetComponentInParent<BraxAuto>();
            // Moving AI cars are handled by the deterministic OBB simulation.
            if(ai && System.Array.IndexOf(auta,ai)>=0)continue;
            if(c.GetComponentInParent<BraxAutoGracza>() || c.GetComponentInParent<BraxAutobus>())continue;
            if(c.bounds.max.y<=p.y+.32f)continue;
            return true;
        }
        return false;
    }

    void Start()
    {
        if (trasy == null || trasy.Length == 0 || auta == null || auta.Length == 0) { enabled = false; return; }
        BraxRuchAutSym.RondoWlaczony=promienWlaczeniaRonda;
        float szer = 1.6f, dl = 3.5f;
        foreach (var a in auta) if (a) { szer = Mathf.Max(szer, a.szer); dl = Mathf.Max(dl, a.dl); }
        sym = new BraxRuchAutSym(punkty, trasy, zdarzenia, wezly, ziarno, szer, dl, vZakret);
        foreach (var a in auta) if (a) sym.Dodaj(a.trasaNr, a.sStart, a.dl, a.szer, a.rozstaw, a.osTyl, a.vmax); else sym.Dodaj(0, 0f, 4.5f, 1.9f, 2.7f, -1.3f, 8f);
        sym.widoczne = Widoczne;
        sym.przeszkodaSceny = PrzeszkodaSceny;
        // Do not initialize traffic until real obstacles have been registered.
        // OnEnable runs before Start for scene objects, including the player car and buses.
        OdswiezPrzeszkody();
        sym.UstawStart();
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

    void OdswiezPrzeszkody()
    {
        sym.zewnetrzne.Clear();
        foreach (var car in BraxAutoGracza.Wszystkie)
            if (car && car.obrys) sym.zewnetrzne.Add(new BraxRuchAutSym.ObrysZewnetrzny {
                srodek=car.transform.TransformPoint(car.obrys.center), przod=car.transform.forward,
                polSzer=car.obrys.size.x*Mathf.Abs(car.obrys.transform.lossyScale.x)*.5f, polDl=car.obrys.size.z*Mathf.Abs(car.obrys.transform.lossyScale.z)*.5f });
        foreach (var bus in BraxAutobus.Wszystkie)   // runda 6 (E23): autobus jako przeszkoda dla aut AI
            if (bus && bus.obrys) sym.zewnetrzne.Add(new BraxRuchAutSym.ObrysZewnetrzny {
                srodek=bus.transform.TransformPoint(bus.obrys.center), przod=bus.transform.forward,
                polSzer=bus.obrys.size.x*Mathf.Abs(bus.obrys.transform.lossyScale.x)*.5f, polDl=bus.obrys.size.z*Mathf.Abs(bus.obrys.transform.lossyScale.z)*.5f });
    }

    void Update()
    {
        if (sym == null) return;
        if (!kam) kam = Camera.main;
        if ((!gracz || !graczKomponent) && Time.time > szukaj)
        {
            var p = FindAnyObjectByType<BraxGymPlayer>();
            if (p) { gracz = p.transform; graczKomponent = p; }
            szukaj = Time.time + 2f;
        }
        // Gdy Patryk prowadzi samochód, przeszkodą dla AI jest obrys jego pojazdu;
        // nie traktuj dodatkowo pasażera jako pieszego wymagającego klaksonu.
        sym.graczJest = gracz && graczKomponent && !graczKomponent.autoGracza && !graczKomponent.seat;
        if (gracz) sym.gracz = gracz.position;
        OdswiezPrzeszkody();
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
