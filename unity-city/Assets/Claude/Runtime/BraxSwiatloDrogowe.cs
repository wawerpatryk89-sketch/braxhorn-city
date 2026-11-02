using System.Collections.Generic;
using UnityEngine;

// SYGNALIZACJA ŚWIETLNA NA SKRZYŻOWANIU OSIEDLA (build 14, wykonawca „drogi”). Fazy jak w mieście — jedna logika dla całej
// gry (BraxTrafficCycle): czerwone+żółte → zielone → żółte → wszystkie czerwone → piesi zielone → piesi zielone migające →
// wszystkie czerwone. Skrzyżowanie ma dwie osie ruchu: A (wloty wzdłuż x) i B (wloty wzdłuż z) — zielone dostają na zmianę,
// co cykl (parzysty = A, nieparzysty = B); w fazie pieszych wszystkie wloty mają czerwone, piesi przechodzą wszystkimi
// przejściami. Szkła: jeden renderer na funkcję (np. wszystkie czerwone osi A) — przełączany materiał (świeci / zgaszony).
// Strefy zatrzymania (BraxStrefyZatrzymania) rejestrowane przy włączeniu: auta przed linią zatrzymania, gdy ich oś nie ma
// zielonego; piesi na krawędzi przejścia, gdy ludzik nie jest zielony.
public sealed class BraxSwiatloDrogowe : MonoBehaviour
{
    public Renderer czerwoneA, zolteA, zieloneA, czerwoneB, zolteB, zieloneB, pieszyCzerwony, pieszyZielony;
    public Material czerwonyWl, czerwonyWyl, zoltyWl, zoltyWyl, zielonyWl, zielonyWyl;
    public Material ludzikCzerwonyWl, ludzikCzerwonyWyl, ludzikZielonyWl, ludzikZielonyWyl;
    public Vector3[] stopSrodek = new Vector3[0], stopKierunek = new Vector3[0];
    public int[] stopOs = new int[0];
    public Vector3[] pieszySrodek = new Vector3[0], pieszyKierunek = new Vector3[0];
    public float stopPromien = 10f, pieszyPromien = 1.8f, przesuniecie;

    readonly BraxTrafficCycle cykl = new BraxTrafficCycle();
    readonly List<int> strefy = new List<int>();
    int stan = -1;

    public BraxTrafficCycle Cykl { get { return cykl; } }
    public int AktywnaOs { get { return cykl.CompletedCycles % 2; } }
    public bool Zielone(int os) { return AktywnaOs == os && cykl.VehiclesMayProceed; }

    void OnEnable()
    {
        cykl.Reset();
        if (przesuniecie > 0f) cykl.Advance(przesuniecie);
        for (int i = 0; i < stopSrodek.Length && i < stopKierunek.Length && i < stopOs.Length; i++)
        {
            int os = stopOs[i];
            strefy.Add(BraxStrefyZatrzymania.Dodaj(new BraxStrefyZatrzymania.Strefa
            { srodek = stopSrodek[i], kierunek = stopKierunek[i], promien = stopPromien, dlaPieszych = false, aktywna = () => !Zielone(os) }));
        }
        for (int i = 0; i < pieszySrodek.Length && i < pieszyKierunek.Length; i++)
            strefy.Add(BraxStrefyZatrzymania.Dodaj(new BraxStrefyZatrzymania.Strefa
            { srodek = pieszySrodek[i], kierunek = pieszyKierunek[i], promien = pieszyPromien, dlaPieszych = true, aktywna = () => !cykl.PedestriansMayProceed }));
        stan = -1; Pokaz();
    }

    void OnDisable()
    {
        foreach (var id in strefy) BraxStrefyZatrzymania.Usun(id);
        strefy.Clear();
    }

    void Update()
    {
        cykl.Advance(Mathf.Min(Time.deltaTime, .25f));
        Pokaz();
    }

    void Pokaz()
    {
        bool miga = cykl.Phase == BraxTrafficPhase.PedestrianClearance;
        bool mignij = miga && Mathf.Repeat(Time.time, 1f) < .5f;
        int s = (int)cykl.Phase * 8 + AktywnaOs * 2 + (mignij ? 1 : 0);
        if (s == stan) return;
        stan = s;
        for (int os = 0; os < 2; os++)
        {
            bool akt = AktywnaOs == os;
            bool czerw = !akt || cykl.VehicleRed, zolte = akt && cykl.VehicleAmber, ziel = akt && cykl.VehiclesMayProceed;
            Ustaw(os == 0 ? czerwoneA : czerwoneB, czerw, czerwonyWl, czerwonyWyl);
            Ustaw(os == 0 ? zolteA : zolteB, zolte, zoltyWl, zoltyWyl);
            Ustaw(os == 0 ? zieloneA : zieloneB, ziel, zielonyWl, zielonyWyl);
        }
        bool pZiel = cykl.PedestriansMayProceed || mignij;
        bool pCzerw = !cykl.PedestriansMayProceed && !miga;
        Ustaw(pieszyZielony, pZiel, ludzikZielonyWl, ludzikZielonyWyl);
        Ustaw(pieszyCzerwony, pCzerw, ludzikCzerwonyWl, ludzikCzerwonyWyl);
    }

    static void Ustaw(Renderer r, bool wl, Material on, Material off)
    {
        if (!r) return;
        var m = wl ? on : off;
        if (m && r.sharedMaterial != m) r.sharedMaterial = m;
    }
}
