using System.Collections.Generic;
using UnityEngine;

// Osobny plik (1.11, 22:20): Unity zapisuje skrypt w scenie tylko, gdy nazwa pliku = nazwa klasy; inaczej w APK „brakujący skrypt” → awaria przy wczytywaniu sceny (osiedle 13, miasto od 9).
/// <summary>Siedzisko (sofa, ławka, hoker) albo łóżko: punkt i kierunek siedzenia. Stawiane z kodu (BraxRegionSeats).</summary>
public sealed class BraxSeat : MonoBehaviour
{
    public static readonly List<BraxSeat> All = new List<BraxSeat>();
    public string label = "SOFA";
    public bool lie;
    public float hipsAboveRoot = .95f;    // wysokość bioder nad stopami w pozie stojącej (model Braxa)
    public float surfaceY;                // wysokość siedziska / materaca (świat)
    BraxGymPlayer rider; Vector3 before; Quaternion beforeRot;
    public bool Occupied { get { return rider; } }
    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    public static BraxSeat Nearest(Vector3 p, float max)
    {
        BraxSeat best = null; float bd = max;
        foreach (var s in All) { if (!s || s.rider) continue; var d = s.transform.position - p; d.y = 0; if (d.magnitude < bd) { bd = d.magnitude; best = s; } }
        return best;
    }

    public void Use(BraxGymPlayer p)
    {
        if (rider || !p) return;
        rider = p; before = p.transform.position; beforeRot = p.transform.rotation;
        p.seat = this; p.body.enabled = false;
        var fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (!lie)
        {
            p.transform.position = new Vector3(transform.position.x, surfaceY - hipsAboveRoot, transform.position.z) - fwd * .1f;
            p.transform.rotation = Quaternion.LookRotation(fwd);
            BraxPose.Get(p).Set(BraxPose.Mode.Sit, transform);
        }
        else
        {
            // na plecach: stopy w stronę forward łóżka, głowa w tył; oś postaci pozioma
            p.transform.position = new Vector3(transform.position.x, surfaceY + .12f, transform.position.z) + fwd * .9f;   // stopy przy końcu „forward”, głowa z tyłu
            p.transform.rotation = Quaternion.LookRotation(Vector3.up, -fwd);
            BraxPose.Get(p).Set(BraxPose.Mode.Lie, transform);
        }
    }

    public void Leave()
    {
        if (!rider) return;
        var p = rider; rider = null;
        BraxPose.Get(p).Set(BraxPose.Mode.None, null);
        p.transform.rotation = Quaternion.Euler(0, beforeRot.eulerAngles.y, 0);
        p.transform.position = before;
        p.body.enabled = true; p.seat = null;
    }
}
