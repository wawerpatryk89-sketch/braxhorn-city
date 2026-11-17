using UnityEngine;

// MIASTO MISTRZÓW - KASYNO WAWER, the owner's hidden office (16.11 23:41 "wejście będzie nikomu nieznane ... przez szafę"):
// a wardrobe on the office block's hall wall is its door - a bookcase door: AKCJA "SZAFA" swings the whole wardrobe on its
// hinge into the office, AKCJA again closes it, from the hall or from inside. Its collider turns with it. The label says
// only "SZAFA": nobody sees a door. Without a hinge it is a plain AKCJA spot (the open safe: napis, komunikat).
public sealed class BraxChampionsSzafa : MonoBehaviour, IBraxInterakcja
{
    public Transform zawias;                         // the hinge (the wardrobe its child), turns about its up axis
    public float katOtwarcia = -90f;                 // degrees
    public Transform[] punkty = new Transform[0];    // where AKCJA works
    public float zasieg = 1.4f;                      // metres (x the transform's scale)
    public string napis = "SZAFA", komunikat = "TAJNE PRZEJŚCIE · BIURO PATRYKA";
    bool otwarta, pierwszy = true; float kat, cel; Quaternion spoczynek;

    void Awake() { if (zawias) spoczynek = zawias.localRotation; }
    void OnEnable() { BraxInterakcje.Dodaj(this); }
    void OnDisable() { BraxInterakcje.Usun(this); }

    bool Blisko(Vector3 g)
    {
        float s = Mathf.Max(.01f, transform.lossyScale.y);
        foreach (var p in punkty)
        {
            if (!p) continue; var d = g - p.position;
            if (Mathf.Abs(d.y) > 1.6f * s) continue; d.y = 0f;
            if (d.magnitude < zasieg * s) return true;
        }
        return false;
    }
    public string Kontekst(Vector3 g) => Blisko(g) ? (zawias && otwarta ? napis + " · ZAMKNIJ" : napis) : "";
    public bool Wykonaj(Vector3 g)
    {
        if (!Blisko(g)) return false;
        if (!zawias) { BraxChampionsGry.Pokaz(komunikat, 3f); return true; }
        otwarta = !otwarta; cel = otwarta ? katOtwarcia : 0f;
        if (otwarta && pierwszy) { pierwszy = false; BraxChampionsGry.Pokaz(komunikat, 2.8f); }
        return true;
    }
    public bool Otwarta => otwarta;
    void Update()
    {
        if (!zawias || Mathf.Approximately(kat, cel)) return;
        kat = Mathf.MoveTowards(kat, cel, 75f * Time.deltaTime);
        zawias.localRotation = spoczynek * Quaternion.AngleAxis(kat, Vector3.up);
    }
}
