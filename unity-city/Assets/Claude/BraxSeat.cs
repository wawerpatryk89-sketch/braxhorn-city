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
    // Tylko siedzisko T1: transport gracza razem z wagonem i bezpieczne wysiadanie na przystanku.
    public BraxTramwajT1 tramOwner;
    BraxGymPlayer rider; Vector3 before; Quaternion beforeRot;
    Vector3 tramLocalPlayer; Quaternion tramLocalRotation;
    public bool Occupied { get { return rider; } }
    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    public static BraxSeat Nearest(Vector3 p, float max)
    {
        BraxSeat best = null; float bd = max;
        foreach (var s in All)
        {
            if (!s || s.rider || s.tramOwner) continue;
            var d = s.transform.position - p;
            // R1 / NOVA: sofa na piętrze nie może być dostępna przez sufit z poziomu ulicy.
            // Wysokość siedziska względem stóp gracza może wynosić ~1 m (hoker), lecz nie kilka metrów.
            if (Mathf.Abs(d.y) > 1.4f) continue;
            d.y = 0f;
            var dist = d.magnitude;
            if (dist < bd) { bd = dist; best = s; }
        }
        return best;
    }

    // SIEDZENIE BEZ BUJANIA (zgłoszenie z47, 2.11 — „Brax siedzi na krzesełku i się buja”). Przyczyna: w trakcie siedzenia
    // gracz grał pętlę klipu „Idle” (postawa bokserska z ugięciem kolan i kołysaniem bioder), a poza z kodu (BraxPose) celuje
    // tylko kości kończyn i kręgosłupa — biodra (korzeń szkieletu) dalej chodziły w rytm klipu, więc całe ciało się bujało.
    // Do tego wysokość bioder była stała (0.95 m — model sprzed skali Braxa 2.33 m), więc postać wisiała nad siedziskiem.
    // Teraz: przy siadaniu klip „Idle” jest ZATRZYMANY na pierwszej klatce (prędkość 0 pilnowana co klatkę, dopóki gracz
    // siedzi — BraxGymPlayer przy wejściu w siedzenie przełącza na „Idle” jeden raz), a wysokość bioder jest MIERZONA na
    // szkielecie (kość Hips nad stopami), staw biodrowy ląduje 5.5 % wzrostu nad siedziskiem (jak u człowieka). Dla każdego
    // siedziska w grze (ławki, krzesła, sofy, hokery) — jedna klasa.
    float hipsZmierzone = -1f;
    Transform kosc;

    static Transform Biodra(BraxGymPlayer p)
    {
        if (!p || !p.visual) return null;
        foreach (var t in p.visual.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name; int i = n.LastIndexOf(':'); if (i >= 0) n = n.Substring(i + 1);
            if (n == "Hips") return t;
        }
        return null;
    }

    void Zamroz(BraxGymPlayer p, bool zeroCzasu)
    {
        if (!p || !p.clips) return;
        var st = p.clips["Idle"]; if (st == null) return;
        if (zeroCzasu) st.time = 0f;
        st.speed = 0f;
    }

    // Bez Update na każdej ławce (recenzja 2.11: setki ławek i krzeseł × Update co klatkę na telefonie) — pilnowanie
    // zatrzymanej pozy działa tylko, gdy ktoś siedzi (korutyna kończy się sama po Leave). BraxGymPlayer w siedzeniu ustawia
    // „Idle” z prędkością 1 — tu zostaje 0 (bez bujania).
    System.Collections.IEnumerator Trzymaj()
    {
        while (rider) { Zamroz(rider, false); yield return null; }
    }

    public void Use(BraxGymPlayer p)
    {
        if (rider || !p || (tramOwner && !tramOwner.MoznaWysiasc)) return;
        rider = p; before = p.transform.position; beforeRot = p.transform.rotation;
        p.seat = this; p.body.enabled = false;
        var fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (!lie)
        {
            kosc = Biodra(p);
            if (kosc) hipsZmierzone = Mathf.Clamp(kosc.position.y - p.transform.position.y, .5f, 1.6f);
            float hips = hipsZmierzone > 0f ? hipsZmierzone : hipsAboveRoot;
            float wzrost = hips / .53f;                     // biodra stojąc ≈ 53 % wzrostu
            float nadSiedziskiem = .055f * wzrost;          // staw biodrowy siedzącego ≈ 5.5 % wzrostu nad siedziskiem
            p.transform.position = new Vector3(transform.position.x, surfaceY + nadSiedziskiem - hips, transform.position.z) - fwd * .1f;
            p.transform.rotation = Quaternion.LookRotation(fwd);
            BraxPose.Get(p).Set(BraxPose.Mode.Sit, transform);
            Zamroz(p, true);
            Debug.Log("BRAXHORN_SIEDZENIE " + label + " biodra=" + hips.ToString("F2") + " m (zmierzone=" + (hipsZmierzone > 0f) + ") siedzisko y=" + surfaceY.ToString("F2") + " klip Idle zatrzymany (bez bujania)");
        }
        else
        {
            // na plecach: stopy w stronę forward łóżka, głowa w tył; oś postaci pozioma
            p.transform.position = new Vector3(transform.position.x, surfaceY + .12f, transform.position.z) + fwd * .9f;   // stopy przy końcu „forward”, głowa z tyłu
            p.transform.rotation = Quaternion.LookRotation(Vector3.up, -fwd);
            BraxPose.Get(p).Set(BraxPose.Mode.Lie, transform);
        }
        if (tramOwner)
        {
            tramLocalPlayer = tramOwner.transform.InverseTransformPoint(p.transform.position);
            tramLocalRotation = Quaternion.Inverse(tramOwner.transform.rotation) * p.transform.rotation;
        }
        StartCoroutine(Trzymaj());
    }

    // Jedno wywolanie na klatke na TRAMWAJ, a nie Update na setkach siedzisk.
    // Nie zmieniamy parent gracza (sceny additive nie moga go przypadkowo zniszczyc).
    public void SyncTram()
    {
        if (!tramOwner || !rider) return;
        rider.transform.SetPositionAndRotation(tramOwner.transform.TransformPoint(tramLocalPlayer),
                                                tramOwner.transform.rotation * tramLocalRotation);
    }

    public void Leave() => Odpusc(false);
    public void ForceLeave() => Odpusc(true);

    void Odpusc(bool wymus)
    {
        if (!rider) return;
        if (tramOwner && !wymus && !tramOwner.MoznaWysiasc) return; // podczas jazdy nie wyrzucaj postaci na tory
        var p = rider; rider = null;
        BraxPose.Get(p).Set(BraxPose.Mode.None, null);
        if (p.clips && p.clips["Idle"] != null) p.clips["Idle"].speed = 1f;
        p.transform.rotation = Quaternion.Euler(0, beforeRot.eulerAngles.y, 0);
        p.transform.position = tramOwner ? tramOwner.WyjscieSwiat : before;
        p.body.enabled = true; p.seat = null;
    }
}
