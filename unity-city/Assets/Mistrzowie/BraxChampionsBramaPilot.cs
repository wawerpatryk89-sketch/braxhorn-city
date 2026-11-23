using System.Collections.Generic;
using UnityEngine;

// MIASTO MISTRZÓW - the villas' garage doors open for a car by themselves (owner 23.11 22:21: "garaże mają się otwierać, tak?
// Tak samo. Z pilota nawet w samochodzie, jak jadę"): a remote in the car - the up-and-over door (BraxChampionsSzafa) opens
// when a car with a driver comes within 14 m (nominal, x the region's scale) of the point before it or near the point
// inside, and closes 4 s after no driven car is near, never while a car stands in its doorway. It closes only what it opened:
// on foot AKCJA works as before.
public sealed class BraxChampionsBramaPilot : MonoBehaviour
{
    public BraxChampionsSzafa brama;
    public Transform przed, wGarazu;                 // the AKCJA points before the door (outside) and in the garage
    public float zasieg = 14f;                       // nominal metres (x lossyScale)
    static readonly List<BraxChampionsCar> auta = new List<BraxChampionsCar>();
    static float odswiezone = -10f;
    float ostatnio = -100f, nastepny;
    bool pilotem;

    static void Odswiez()
    {
        if (Time.time - odswiezone < 5f && auta.Count > 0) return;
        odswiezone = Time.time; auta.Clear();
        auta.AddRange(Object.FindObjectsByType<BraxChampionsCar>(FindObjectsSortMode.None));
    }
    static float Plasko(Vector3 d) { d.y = 0f; return d.magnitude; }

    void Update()
    {
        if (!brama || !przed || Time.time < nastepny) return;
        nastepny = Time.time + .25f;
        Odswiez();
        float s = Mathf.Max(.01f, transform.lossyScale.y);
        Vector3 a = przed.position, b = wGarazu ? wGarazu.position : a, drzwi = brama.zawias ? brama.zawias.position : a;
        var n = a - drzwi; n.y = 0f; n = n.sqrMagnitude > 1e-6f ? n.normalized : Vector3.forward;   // out of the garage
        var bok = new Vector3(n.z, 0f, -n.x);
        bool jedzie = false, wDrzwiach = false;
        foreach (var car in auta)
        {
            if (!car || car.helikopter) continue;
            var p = car.transform.position;
            if (car.Driver && (Plasko(p - a) < zasieg * s || Plasko(p - b) < 6f * s)) jedzie = true;
            var d = p - drzwi; d.y = 0f;
            if (Mathf.Abs(Vector3.Dot(d, n)) < 2.8f * s && Mathf.Abs(Vector3.Dot(d, bok)) < 2.2f * s) wDrzwiach = true;   // its bumper under the leaf
        }
        if (jedzie) { ostatnio = Time.time; if (!brama.Otwarta) { brama.Ustaw(true); pilotem = true; } }
        else if (pilotem && brama.Otwarta && Time.time - ostatnio > 4f && !wDrzwiach) { brama.Ustaw(false); pilotem = false; }
    }
}
