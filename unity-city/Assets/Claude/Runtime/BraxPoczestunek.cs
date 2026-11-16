using UnityEngine;

public sealed class BraxPoczestunek : MonoBehaviour, IBraxInterakcja
{
    public BraxGymPlayer gracz;
    static readonly System.Collections.Generic.List<BraxPoczestunek> miejsca = new System.Collections.Generic.List<BraxPoczestunek>();
    void OnEnable() { miejsca.Add(this); BraxInterakcje.Dodaj(this); }
    void OnDisable() { miejsca.Remove(this); BraxInterakcje.Usun(this); }

    public static bool Blisko(Vector3 osoba)
    {
        foreach (var miejsce in miejsca)
            if ((miejsce.transform.position-osoba).sqrMagnitude < 9f*BraxGlosPostaci.K*BraxGlosPostaci.K) return true;
        return false;
    }

    public string Kontekst(Vector3 p) =>
        gracz && gracz.MozeZjesc && Vector3.Distance(transform.position,p) < 2.3f*BraxGlosPostaci.K
            ? "ZJEDZ JABŁKO" : "";

    public bool Wykonaj(Vector3 p) => Kontekst(p).Length > 0 && gracz.Zjedz();
}
