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
    bool animowane, wstaje;
    float fazaOd;
    Transform biodra;
    public bool GraKlip => animowane;
    public bool ZajetaAnimacja { get; set; }
    void OnDisable() { All.Remove(this); if (rider) Leave(); }

    public static BraxSeat Nearest(Vector3 p, float max)
    {
        BraxSeat best = null; float bd = max;
        foreach (var s in All) { if (!s || s.rider) continue; var d = s.transform.position - p; d.y = 0; if (d.magnitude < bd) { bd = d.magnitude; best = s; } }
        return best;
    }

    public void Use(BraxGymPlayer p, bool animuj = true)
    {
        if (rider || !p) return;
        rider = p; before = p.transform.position; beforeRot = p.transform.rotation;
        p.seat = this; p.body.enabled = false;
        var fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        animowane = !lie && animuj && p.playFullActionClips && p.clips && p.clips.GetClip("SitDown") &&
            p.clips.GetClip("SeatedIdle") && p.clips.GetClip("StandUp");
        if (animowane)
        {
            biodra = null;
            foreach (var t in p.visual.GetComponentsInChildren<Transform>())
                if (t.name.EndsWith("Hips",System.StringComparison.Ordinal)) { biodra=t; break; }
            animowane = biodra;
        }
        if (animowane)
        {
            fazaOd=Time.time; wstaje=false;
            p.transform.rotation=Quaternion.LookRotation(fwd);
            p.clips["SitDown"].wrapMode=WrapMode.ClampForever; p.clips["SitDown"].speed=1f;
            p.clips.CrossFade("SitDown", .16f);
        }
        else if (!lie)
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

    public void Wstan()
    {
        if (!animowane) { Leave(); return; }
        if (wstaje) return;
        wstaje=true; fazaOd=Time.time;
        rider.clips["StandUp"].wrapMode=WrapMode.ClampForever;
        rider.clips["StandUp"].speed=1f; rider.clips.CrossFade("StandUp",.18f);
    }

    void LateUpdate()
    {
        if (!animowane || !rider || !biodra) return;
        string klip = wstaje ? "StandUp" : "SitDown";
        float t = Mathf.Clamp01((Time.time-fazaOd)/Mathf.Max(.01f,rider.clips[klip].length));
        var punkt = transform.position; punkt.y=surfaceY+.10f;
        var siedzi = rider.transform.position + punkt-biodra.position;
        float waga = wstaje ? 1f-t : t;
        rider.transform.position=Vector3.Lerp(before,siedzi,Mathf.SmoothStep(0,1,waga));
        if (t<1f) return;
        if (wstaje) { Leave(); return; }
        if (!rider.clips.IsPlaying("SeatedIdle"))
        {
            rider.clips["SeatedIdle"].wrapMode=WrapMode.Loop;
            rider.clips.CrossFade("SeatedIdle",.18f);
        }
    }

    public void Leave()
    {
        if (!rider) return;
        var p = rider; rider = null;
        if (animowane) p.clips.CrossFade("Idle", .16f);
        animowane=wstaje=ZajetaAnimacja=false;
        BraxPose.Get(p).Set(BraxPose.Mode.None, null);
        p.transform.rotation = Quaternion.Euler(0, beforeRot.eulerAngles.y, 0);
        p.transform.position = before;
        p.body.enabled = true; p.seat = null;
    }
}
