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
    // Ackermann: the inside front wheel turns further than the outside wheel.
    public static float KatKola(float angle, float wheelbase, float side)
    {
        if (Mathf.Abs(angle)<.01f) return 0;
        float sign=Mathf.Sign(angle), radius=wheelbase/Mathf.Tan(Mathf.Abs(angle)*Mathf.Deg2Rad);
        return sign*Mathf.Atan2(wheelbase,Mathf.Max(.2f,radius-sign*side))*Mathf.Rad2Deg;
    }
    public int trasaNr;                            // indeks trasy w BraxRuchAut
    public float sStart, vmax = 8.5f;
    public Vector3[] trasa = new Vector3[0];       // punkty trasy co ~3 m (tylko do odczytu, np. MENU → UTKNĄŁEM szuka najbliższej drogi)
    public float dl = 4.6f, szer = 1.9f, rozstaw = 2.7f, osTyl = -1.3f, promienKola = .33f;
    public Transform nadwozie;
    public Transform[] kola = new Transform[0];    // obracające się części kół (LP, PP, LT, PT)
    public Transform[] skret = new Transform[0];   // osie skrętu przednich kół
    public Renderer[] stop = new Renderer[0], migL = new Renderer[0], migP = new Renderer[0];
    public Transform[] dym = new Transform[0];     // kłęby spalin: płytki zwrócone do kamery (moduł cząsteczek jest w projekcie wyłączony — build 14 padł na ParticleSystem)
    public float dymSkala = 1f;                    // skala świata (k) z generatora
    public AudioClip silnik, klakson;
    public static readonly List<BraxAuto> Wszystkie = new List<BraxAuto>();

    AudioSource dzwiek;
    float katKol, skretAkt, przechyl, pochyl, kierPoprz;
    bool stopWl, migWl, dymWl;
    Renderer[] dymR; float[] dymFaza; MaterialPropertyBlock dymMpb; static Camera kam;

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
        dymR = new Renderer[dym.Length]; dymFaza = new float[dym.Length]; dymMpb = new MaterialPropertyBlock();
        for (int i = 0; i < dym.Length; i++) { dymR[i] = dym[i] ? dym[i].GetComponent<Renderer>() : null; if (dymR[i]) dymR[i].enabled = false; dymFaza[i] = (i + Random.value * .5f) / Mathf.Max(1, dym.Length); }
        dymWl = false;
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
            foreach (var s in skret) if (s) s.localRotation = Quaternion.Euler(0f,KatKola(skretAkt,rozstaw,transform.InverseTransformPoint(s.position).x),0f);
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
        if (dym.Length > 0 && dymR != null)
        {
            if (bardzo && dt > 0f)
            {
                if (!dymWl) { dymWl = true; foreach (var r in dymR) if (r) r.enabled = true; }
                if (!kam) kam = Camera.main;
                float sila = Mathf.Clamp01(.3f + st.v * .03f + (st.a > .4f && st.v < 5f ? .55f * st.a : 0f));   // mocniej przy ruszaniu
                float zasieg = .35f + Mathf.Min(st.v, 6f) * .3f;                                              // w ruchu smuga dłuższa
                for (int i = 0; i < dym.Length; i++)
                {
                    var tr = dym[i]; if (!tr) continue;
                    float t = dymFaza[i] = Mathf.Repeat(dymFaza[i] + dt / 1.4f, 1f);
                    tr.localPosition = new Vector3(Mathf.Sin((i + 1) * 2.3f + t * 3f) * .05f * t, .16f * t, zasieg * t) * dymSkala;
                    float sk = Mathf.Lerp(.1f, .5f, t) * dymSkala; tr.localScale = new Vector3(sk, sk, sk);
                    if (kam) tr.rotation = kam.transform.rotation;
                    float al = (1f - t) * (1f - t) * sila * .45f;
                    dymMpb.SetColor("_TintColor", new Color(.5f, .5f, .5f, al)); dymMpb.SetColor("_Color", new Color(.85f, .85f, .85f, al));
                    if (dymR[i]) dymR[i].SetPropertyBlock(dymMpb);
                }
            }
            else if (dymWl) { dymWl = false; foreach (var r in dymR) if (r) r.enabled = false; }
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
