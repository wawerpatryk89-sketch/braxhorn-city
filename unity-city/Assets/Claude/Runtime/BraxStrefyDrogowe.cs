using System.Collections.Generic;
using UnityEngine;

// STREFY ZATRZYMANIA NA DROGACH BEZ ŚWIATEŁ (build 14, wykonawca „drogi”) — rejestrowane przy starcie sceny
// w BraxStrefyZatrzymania:
//   • rondo: linia „ustąp pierwszeństwa” na każdym wlocie — aktywna, gdy po pierścieniu (przed tym wlotem, pod prąd ruchu)
//     jedzie auto (kolizja z komponentem BraxAuto w punkcie kontrolnym);
//   • przejścia dla pieszych bez sygnalizacji: strefy dla aut przed pasami (oba kierunki) — aktywne, gdy na pasach albo
//     tuż przy krawężniku stoi pieszy (BraxPieszy) lub gracz.
// Sprawdzenia fizyki najwyżej co 0,2 s na strefę (taniej na telefonie). Komponent bez geometrii — same dane.
public sealed class BraxStrefyDrogowe : MonoBehaviour
{
    public Vector3[] rondoSrodek = new Vector3[0], rondoKierunek = new Vector3[0], rondoKontrola = new Vector3[0];
    public float rondoPromien = 8f, kontrolaPromien = 3.6f;
    public Vector3[] zebraSrodek = new Vector3[0], zebraPolowa = new Vector3[0];     // prostokąt pasy + 0.8 m chodnika (osie świata)
    public Vector3[] stopSrodek = new Vector3[0], stopKierunek = new Vector3[0];
    public int[] stopZebra = new int[0];
    public float stopPromien = 9f;

    readonly List<int> strefy = new List<int>();
    static readonly Collider[] bufor = new Collider[24];
    float[] czasZ, czasR; bool[] zajZ, zajR;

    void OnEnable()
    {
        czasZ = new float[zebraSrodek.Length]; zajZ = new bool[zebraSrodek.Length];
        czasR = new float[rondoSrodek.Length]; zajR = new bool[rondoSrodek.Length];
        for (int i = 0; i < czasZ.Length; i++) czasZ[i] = -1f;
        for (int i = 0; i < czasR.Length; i++) czasR[i] = -1f;
        for (int i = 0; i < rondoSrodek.Length && i < rondoKierunek.Length && i < rondoKontrola.Length; i++)
        {
            int k = i;
            strefy.Add(BraxStrefyZatrzymania.Dodaj(new BraxStrefyZatrzymania.Strefa
            { srodek = rondoSrodek[i], kierunek = rondoKierunek[i], promien = rondoPromien, dlaPieszych = false, aktywna = () => RondoZajete(k) }));
        }
        for (int i = 0; i < stopSrodek.Length && i < stopKierunek.Length && i < stopZebra.Length; i++)
        {
            int z = stopZebra[i]; if (z < 0 || z >= zebraSrodek.Length || z >= zebraPolowa.Length) continue;
            strefy.Add(BraxStrefyZatrzymania.Dodaj(new BraxStrefyZatrzymania.Strefa
            { srodek = stopSrodek[i], kierunek = stopKierunek[i], promien = stopPromien, dlaPieszych = false, aktywna = () => ZebraZajeta(z) }));
        }
    }

    void OnDisable()
    {
        foreach (var id in strefy) BraxStrefyZatrzymania.Usun(id);
        strefy.Clear();
    }

    bool ZebraZajeta(int i)
    {
        if (Time.time - czasZ[i] < .2f && czasZ[i] >= 0f) return zajZ[i];
        czasZ[i] = Time.time;
        int n = Physics.OverlapBoxNonAlloc(zebraSrodek[i], zebraPolowa[i], bufor, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
        bool z = false;
        for (int q = 0; q < n && !z; q++)
        {
            var c = bufor[q]; if (!c) continue;
            if (c.GetComponentInParent<BraxGymPlayer>()) z = true;
            else if (c.GetComponent("BraxPieszy") || (c.attachedRigidbody && c.attachedRigidbody.GetComponent("BraxPieszy"))) z = true;
        }
        zajZ[i] = z; return z;
    }

    bool RondoZajete(int i)
    {
        if (Time.time - czasR[i] < .2f && czasR[i] >= 0f) return zajR[i];
        czasR[i] = Time.time;
        int n = Physics.OverlapSphereNonAlloc(rondoKontrola[i], kontrolaPromien, bufor, ~0, QueryTriggerInteraction.Collide);
        bool z = false;
        for (int q = 0; q < n && !z; q++)
        {
            var c = bufor[q]; if (!c) continue;
            if (c.GetComponent("BraxAuto") || (c.attachedRigidbody && c.attachedRigidbody.GetComponent("BraxAuto"))) z = true;
        }
        zajR[i] = z; return z;
    }
}
