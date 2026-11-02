using System.Collections.Generic;
using UnityEngine;

// AUTO W RUCHU (build 14; właściciel 1.11 „żeby się ruch toczył”, zdjęcia z buildu 12: auta tyłem, obrót w miejscu, koła
// stoją). Jazdę liczy BraxRuchAut / BraxRuchAutSym (jedna symulacja dla wszystkich aut); tu tylko wygląd i dźwięk:
//  • nadwozie ustawione po modelu rowerowym (tylna oś na torze, przód po łuku), lekkie przechyły w zakrętach i przy hamowaniu;
//  • koła kręcą się zgodnie z prędkością (promień koła zmierzony z geometrii), przednie skręcają;
//  • blisko kamery (~30 m): światła STOP przy hamowaniu i postoju, kierunkowskaz przed skrętem (miga 1,5 Hz), spaliny
//    (kilka cząsteczek, mocniej przy ruszaniu), klakson, gdy gracz stoi na jezdni;
//  • dźwięk silnika (BraxClaudeOsiedleDzwieki → silnik), ton z prędkości; daleko — cisza.
// Fotele: dzieci „BRAXHORN fotel kierowcy” / „BRAXHORN fotel pasażera” w nadwoziu (miejsce dla ludzi — inny etap).
public sealed class BraxAuto : MonoBehaviour
{
    public int trasa;
    public float sStart, vmax = 8.5f;
    public float dl = 4.6f, szer = 1.9f, rozstaw = 2.7f, osTyl = -1.3f, promienKola = .33f;
    public Transform nadwozie;
    public Transform[] kola = new Transform[0];    // obracające się części kół (LP, PP, LT, PT)
    public Transform[] skret = new Transform[0];   // osie skrętu przednich kół
    public Renderer[] stop = new Renderer[0], migL = new Renderer[0], migP = new Renderer[0];
    public ParticleSystem dym;
    public AudioClip silnik, klakson;
    public static readonly List<BraxAuto> Wszystkie = new List<BraxAuto>();

    AudioSource dzwiek;
    float katKol, skretAkt, przechyl, pochyl, kierPoprz;
    bool stopWl, migWl, dymWl;

    void OnEnable() { Wszystkie.Add(this); }
    void OnDisable() { Wszystkie.Remove(this); }

    void Start()
    {
        if (silnik)
        {
            // dźwięk 3D: słychać z ok. 40 m, głośniej z bliska; ton rośnie z prędkością (właściciel 1.11: „auta niech mają dźwięk”)
            dzwiek = gameObject.AddComponent<AudioSource>(); dzwiek.clip = silnik; dzwiek.loop = true; dzwiek.spatialBlend = 1f;
            dzwiek.rolloffMode = AudioRolloffMode.Logarithmic; dzwiek.minDistance = 3.5f; dzwiek.maxDistance = 45f; dzwiek.dopplerLevel = .4f;
            dzwiek.volume = .55f; dzwiek.priority = 160; dzwiek.pitch = .8f; dzwiek.time = Random.value * silnik.length * .9f;
        }
        Swiatla(stop, false); Swiatla(migL, false); Swiatla(migP, false);
        if (dym) { dym.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); dymWl = false; }
    }

    static void Swiatla(Renderer[] r, bool wl) { if (r == null) return; foreach (var x in r) if (x) x.enabled = wl; }

    // Wywoływane przez BraxRuchAut co klatkę.
    public void Ustaw(BraxRuchAutSym.Auto st, float dt, float odl, bool skok)
    {
        var rot = Quaternion.LookRotation(st.kier, Vector3.up);
        float zmianaKier = 0f;
        if (dt > 1e-4f && !skok) zmianaKier = Mathf.DeltaAngle(kierPoprz, rot.eulerAngles.y) / dt;   // [°/s], + = w prawo
        kierPoprz = rot.eulerAngles.y;
        transform.SetPositionAndRotation(st.poz, rot);
        bool blisko = odl < 60f, bardzo = odl < 32f;
        if (blisko && dt > 0f)
        {
            // koła: obrót wokół osi poprzecznej (dodatni = toczenie do przodu), przednie skręcają
            katKol = Mathf.Repeat(katKol + st.v * dt / Mathf.Max(.12f, promienKola) * Mathf.Rad2Deg, 360f);
            var q = Quaternion.Euler(katKol, 0f, 0f);
            foreach (var k in kola) if (k) k.localRotation = q;
            skretAkt = Mathf.Lerp(skretAkt, st.skret, 1f - Mathf.Exp(-dt * 10f));
            var qs = Quaternion.Euler(0f, skretAkt, 0f);
            foreach (var s in skret) if (s) s.localRotation = qs;
            // przechyły: w zakręcie nadwozie pochyla się na zewnątrz łuku, przy hamowaniu „nurkuje”
            if (nadwozie)
            {
                float boczne = st.v * zmianaKier * Mathf.Deg2Rad;                    // przyspieszenie boczne [m/s²]
                przechyl = Mathf.Lerp(przechyl, Mathf.Clamp(boczne * .55f, -2.2f, 2.2f), 1f - Mathf.Exp(-dt * 4f));
                pochyl = Mathf.Lerp(pochyl, Mathf.Clamp(-st.a * .32f, -1.2f, 1.6f), 1f - Mathf.Exp(-dt * 5f));
                nadwozie.localRotation = Quaternion.Euler(pochyl, 0f, przechyl);
            }
        }
        // światła STOP i kierunkowskazy tylko blisko
        bool s1 = bardzo && st.hamuje;
        if (s1 != stopWl) { stopWl = s1; Swiatla(stop, s1); }
        bool m1 = bardzo && st.migacz != 0 && Mathf.Repeat(Time.time * 1.5f + GetInstanceID() * .137f, 1f) < .5f;
        if (m1 != migWl || (m1 && st.migacz != 0))
        {
            migWl = m1;
            Swiatla(migL, m1 && st.migacz < 0); Swiatla(migP, m1 && st.migacz > 0);
        }
        // spaliny: blisko, mocniej przy ruszaniu
        if (dym)
        {
            if (bardzo)
            {
                if (!dymWl) { dym.Play(); dymWl = true; }
                var em = dym.emission;
                em.rateOverTime = 1.6f + st.v * .25f + (st.a > .4f && st.v < 5f ? 9f * st.a : 0f);
            }
            else if (dymWl) { dym.Stop(true, ParticleSystemStopBehavior.StopEmitting); dymWl = false; }
        }
        // silnik
        if (dzwiek)
        {
            if (odl < 55f) { if (!dzwiek.isPlaying) dzwiek.Play(); }
            else if (dzwiek.isPlaying) dzwiek.Pause();
            if (dzwiek.isPlaying && dt > 0f)
                dzwiek.pitch = Mathf.Lerp(dzwiek.pitch, .78f + Mathf.Clamp01(st.v / Mathf.Max(1f, vmax)) * .75f + (st.a > .5f ? .08f : 0f), 1f - Mathf.Exp(-dt * 3f));
        }
        // klakson (gracz na jezdni)
        if (st.klakson)
        {
            st.klakson = false;
            if (klakson && odl < 45f)
            {
                if (!dzwiek) { dzwiek = gameObject.AddComponent<AudioSource>(); dzwiek.spatialBlend = 1f; dzwiek.minDistance = 4f; dzwiek.maxDistance = 50f; dzwiek.rolloffMode = AudioRolloffMode.Logarithmic; }
                dzwiek.PlayOneShot(klakson, .9f);
            }
        }
    }
}
