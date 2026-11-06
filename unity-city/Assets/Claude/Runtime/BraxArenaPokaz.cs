using UnityEngine;

// BRAXHORN ARENA na wyspie: RAKK/PITBULL kontra ZYRO/DOBERMAN.
// Pokaz choreograficzny z REALNYCH klipów FBX, nie AI walki z graczem.
// Samo odegranie klipu ataku nie oznacza trafienia: kontakt sprawdzany
// jest względem kości dłoni/stóp i głowy/tułowia po próbkowaniu animacji.
// Całą parę trzymamy wewnątrz klatki; wysokość stóp pozostaje na macie.
public sealed class BraxArenaPokaz : MonoBehaviour
{
    public Transform zawodnikA, zawodnikB;
    public Animation animacjaA, animacjaB;
    public float promienBezpieczny = 3.6f;
    public float dystans = 1.9f;
    public float zasieg = 65f;
    public Vector3 srodek;

    // Liczniki są wynikiem rzeczywistego testu animacji w runtime, nie
    // obietnicą 100% trafień podczas statycznego przeglądu kodu.
    public int trafieniaPitbulla, trafieniaDobermana;
    public int pudlaPitbulla, pudlaDobermana;

    const int Garda = 0, Krok = 1, Cios = 2, Unik = 3, GlowaHit = 4;
    const int Kopniecie = 5, Blok = 6, KorpusHit = 7, Wstanie = 8, Zaczepka = 9;
    static readonly string[] Klipy =
    {
        "Garda", "Krok", "Kombinacja", "Unik", "Trafienie",
        "Kopniecie", "Blok", "TrafienieKorpus", "Wstanie", "Zaczepka"
    };

    Transform kamera;
    Transform glowaA, glowaB, korpusA, korpusB;
    Transform lewaA, prawaA, lewaB, prawaB, stopaLA, stopaPA, stopaLB, stopaPB;
    float zegar, reakcjaA, reakcjaB, kontaktA, kontaktB, odbicieA, odbicieB;
    int reakcjaTypA = GlowaHit, reakcjaTypB = GlowaHit;
    int stanA = -1, stanB = -1;
    int poprzedniAtakA = -1, poprzedniAtakB = -1;
    bool danyAtakTrafionyA, danyAtakTrafionyB;

    void Awake()
    {
        if (!zawodnikA || !zawodnikB || !animacjaA || !animacjaB)
        {
            Debug.LogError("BRAXHORN_ARENA_POKAZ brak obu zawodników lub animacji", this);
            enabled = false; return;
        }
        foreach (string nazwa in Klipy)
            if (!animacjaA.GetClip(nazwa) || !animacjaB.GetClip(nazwa))
            {
                Debug.LogError("BRAXHORN_ARENA_POKAZ brakuje klipu " + nazwa, this);
                enabled = false; return;
            }
        glowaA = Kosc(zawodnikA, "Head"); glowaB = Kosc(zawodnikB, "Head");
        korpusA = Kosc(zawodnikA, "Spine2"); korpusB = Kosc(zawodnikB, "Spine2");
        lewaA = Kosc(zawodnikA, "LeftHand"); prawaA = Kosc(zawodnikA, "RightHand");
        lewaB = Kosc(zawodnikB, "LeftHand"); prawaB = Kosc(zawodnikB, "RightHand");
        stopaLA = Kosc(zawodnikA, "LeftFoot"); stopaPA = Kosc(zawodnikA, "RightFoot");
        stopaLB = Kosc(zawodnikB, "LeftFoot"); stopaPB = Kosc(zawodnikB, "RightFoot");
        if (!glowaA || !glowaB || !korpusA || !korpusB ||
            !lewaA || !prawaA || !lewaB || !prawaB ||
            !stopaLA || !stopaPA || !stopaLB || !stopaPB)
        {
            Debug.LogError("BRAXHORN_ARENA_POKAZ brak kości kontaktu, nie symuluję fałszywych trafień", this);
            enabled = false; return;
        }
        Ustaw(0f);
        Debug.Log("BRAXHORN_ARENA_POKAZ start PITBULL/RAKK kontra DOBERMAN/ZYRO, źródło=FBX, trafienia=kości");
    }

    static Transform Kosc(Transform model, string nazwa)
    {
        foreach (var t in model.GetComponentsInChildren<Transform>(true))
            if (t.name.EndsWith(":" + nazwa, System.StringComparison.Ordinal) ||
                t.name == nazwa) return t;
        return null;
    }

    void Update()
    {
        if (!kamera && Camera.main) kamera = Camera.main.transform;
        bool blisko = kamera && (kamera.position - srodek).sqrMagnitude < zasieg * zasieg;
        if (animacjaA) animacjaA.enabled = blisko;
        if (animacjaB) animacjaB.enabled = blisko;
        if (!blisko) return;

        float dt = Time.deltaTime;
        reakcjaA = Mathf.Max(0f, reakcjaA - dt);
        reakcjaB = Mathf.Max(0f, reakcjaB - dt);
        kontaktA = Mathf.Max(0f, kontaktA - dt);
        kontaktB = Mathf.Max(0f, kontaktB - dt);
        odbicieA = Mathf.MoveTowards(odbicieA, 0f, dt * .30f);
        odbicieB = Mathf.MoveTowards(odbicieB, 0f, dt * .30f);
        zegar = Mathf.Repeat(zegar + dt, 24f);
        Ustaw(zegar);
    }

    void Ustaw(float czas)
    {
        // Dwa półokresy 12 sekund: atakujący i broniący ZAMIENIAJĄ ROLE.
        // Podejścia są płynne, bez teleportu i bez wyskakiwania poza matę.
        float f = Mathf.Repeat(czas, 12f);
        bool zmiana = czas >= 12f;
        float podejscie = Mathf.Max(
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.4f, 2.45f, f)) *
                (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3.8f, 4.7f, f))),
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4.6f, 5.15f, f)) *
                (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(6.45f, 7.1f, f))));
        float odstep = Mathf.Lerp(dystans + .25f, Mathf.Min(dystans, 1.45f), podejscie);
        float kat = Mathf.Sin(czas * Mathf.PI / 12f) * .26f;
        Vector3 os = new Vector3(Mathf.Sin(kat), 0f, Mathf.Cos(kat));
        Vector3 boczny = Vector3.right * (Mathf.Sin(czas * Mathf.PI / 6f) * .34f);
        Vector3 pa = boczny - os * (odstep * .5f + odbicieA);
        Vector3 pb = boczny + os * (odstep * .5f + odbicieB);
        pa = Vector3.ClampMagnitude(pa, promienBezpieczny);
        pb = Vector3.ClampMagnitude(pb, promienBezpieczny);
        zawodnikA.position = srodek + pa;
        zawodnikB.position = srodek + pb;
        Vector3 kierunek = pb - pa;
        if (kierunek.sqrMagnitude > .01f)
        {
            zawodnikA.rotation = Quaternion.LookRotation(kierunek);
            zawodnikB.rotation = Quaternion.LookRotation(-kierunek);
        }

        // Te fazy sterują tylko WYBOREM klipu. Trafienia nigdy nie są
        // wyzwalane zegarem: LateUpdate weryfikuje fizyczny kontakt.
        int atak = Garda, obrona = Garda;
        if (f < 1.15f) atak = obrona = Garda;
        else if (f < 2.45f) atak = obrona = Krok;
        else if (f < 4.05f) { atak = Cios; obrona = Garda; }
        else if (f < 4.75f) { atak = Garda; obrona = Unik; }
        else if (f < 6.5f) { atak = Kopniecie; obrona = Garda; }
        else if (f < 7.15f) atak = obrona = Garda;
        else if (f < 8.85f) { atak = Garda; obrona = Cios; }
        else if (f < 9.65f) { atak = Unik; obrona = Blok; }
        else if (f < 10.45f) { atak = Zaczepka; obrona = Garda; }
        else atak = obrona = Krok;

        int a = zmiana ? obrona : atak, b = zmiana ? atak : obrona;
        Zagraj(animacjaA, reakcjaA > 0f ? reakcjaTypA : a, ref stanA);
        Zagraj(animacjaB, reakcjaB > 0f ? reakcjaTypB : b, ref stanB);
        if (stanA != Cios && stanA != Kopniecie && poprzedniAtakA != -1)
        {
            if (!danyAtakTrafionyA) pudlaPitbulla++;
            poprzedniAtakA = -1;
        }
        if (stanB != Cios && stanB != Kopniecie && poprzedniAtakB != -1)
        {
            if (!danyAtakTrafionyB) pudlaDobermana++;
            poprzedniAtakB = -1;
        }
        if ((stanA == Cios || stanA == Kopniecie) && stanA != poprzedniAtakA)
        { poprzedniAtakA = stanA; danyAtakTrafionyA = false; }
        if ((stanB == Cios || stanB == Kopniecie) && stanB != poprzedniAtakB)
        { poprzedniAtakB = stanB; danyAtakTrafionyB = false; }
    }

    void LateUpdate()
    {
        if (!animacjaA || !animacjaA.enabled || !animacjaB || !animacjaB.enabled) return;
        // 0.30–0.42 m od kości trafienia; zero losowych trafień z zegara.
        // Głowa: cios dłonią, korpus: dłonie albo stopy podczas kopnięcia.
        if ((stanA == Cios || stanA == Kopniecie) && kontaktB <= 0f &&
            stanB != Unik && stanB != Blok && reakcjaB <= 0f)
        {
            int traf = Trafienie(animacjaA, stanA, lewaA, prawaA, stopaLA, stopaPA, glowaB, korpusB);
            if (traf >= 0)
            {
                Reakcja(false, traf);
                danyAtakTrafionyA = true;
                trafieniaPitbulla++;
                Debug.Log("BRAXHORN_ARENA_TRAFIENIE PITBULL -> DOBERMAN " + (traf == GlowaHit ? "GLOWA" : "KORPUS"));
            }
        }
        if ((stanB == Cios || stanB == Kopniecie) && kontaktA <= 0f &&
            stanA != Unik && stanA != Blok && reakcjaA <= 0f)
        {
            int traf = Trafienie(animacjaB, stanB, lewaB, prawaB, stopaLB, stopaPB, glowaA, korpusA);
            if (traf >= 0)
            {
                Reakcja(true, traf);
                danyAtakTrafionyB = true;
                trafieniaDobermana++;
                Debug.Log("BRAXHORN_ARENA_TRAFIENIE DOBERMAN -> PITBULL " + (traf == GlowaHit ? "GLOWA" : "KORPUS"));
            }
        }
    }

    static int Trafienie(Animation anim, int stan, Transform lewa, Transform prawa,
        Transform stopaL, Transform stopaP, Transform glowa, Transform korpus)
    {
        var clip = anim[Klipy[stan]];
        if (clip == null || clip.length <= .01f) return -1;
        float faza = Mathf.Clamp01(clip.time / clip.length);
        // Pomijamy wyprowadzenie i koniec uderzenia.
        if (faza < .17f || faza > .93f) return -1;
        if (stan == Cios)
        {
            if (Blisko(lewa, glowa, .32f) || Blisko(prawa, glowa, .32f)) return GlowaHit;
            if (Blisko(lewa, korpus, .39f) || Blisko(prawa, korpus, .39f)) return KorpusHit;
        }
        if (stan == Kopniecie &&
            (Blisko(stopaL, korpus, .42f) || Blisko(stopaP, korpus, .42f)))
            return KorpusHit;
        return -1;
    }

    static bool Blisko(Transform reka, Transform cel, float odleglosc)
        => reka && cel && (reka.position - cel.position).sqrMagnitude <= odleglosc * odleglosc;

    void Reakcja(bool trafionyA, int typ)
    {
        var anim = trafionyA ? animacjaA : animacjaB;
        float trwanie = Mathf.Clamp(anim.GetClip(Klipy[typ]).length, .45f, 1.4f);
        if (trafionyA)
        {
            reakcjaA = trwanie; reakcjaTypA = typ; kontaktA = .50f;
            odbicieA = Mathf.Min(.25f, odbicieA + .09f);
            stanA = -1;
            Zagraj(animacjaA, typ, ref stanA);
        }
        else
        {
            reakcjaB = trwanie; reakcjaTypB = typ; kontaktB = .50f;
            odbicieB = Mathf.Min(.25f, odbicieB + .09f);
            stanB = -1;
            Zagraj(animacjaB, typ, ref stanB);
        }
    }

    static void Zagraj(Animation anim, int stan, ref int poprzedni)
    {
        if (stan == poprzedni) return;
        poprzedni = stan;
        var klip = anim[Klipy[stan]];
        if (klip == null) return;
        klip.time = 0f;
        // Nie przyspieszamy źródłowego szkieletu przez modyfikację kości.
        // Ataki mieszczą się w długości swojej fazy choreografii.
        klip.speed = stan == Cios ? klip.length / 1.60f :
                     stan == Kopniecie ? klip.length / 1.75f : 1f;
        anim.CrossFade(Klipy[stan], stan == GlowaHit || stan == KorpusHit ? .08f : .17f);
    }

    void OnDisable()
    {
        if (animacjaA) animacjaA.Stop();
        if (animacjaB) animacjaB.Stop();
        stanA = stanB = -1;
    }
}
