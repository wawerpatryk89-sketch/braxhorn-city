using UnityEngine;

// Choreografia dwóch istniejących modeli. Nie korzysta z AI ani sterowania gracza.
public sealed class BraxArenaPokaz : MonoBehaviour
{
    public Transform zawodnikA, zawodnikB;
    public Animation animacjaA, animacjaB;
    public float promienBezpieczny = 3.6f;
    public float dystans = 1.9f;
    public float zasieg = 65f;
    public Vector3 srodek;
    float zegar;
    int stanA = -1, stanB = -1;
    Transform kamera;
    Transform glowaA, glowaB, lewaA, prawaA, lewaB, prawaB;
    float reakcjaA, reakcjaB, kontaktA, kontaktB;
    static readonly string[] Klipy = { "Garda", "Krok", "Kombinacja", "Unik", "Trafienie" };

    void Awake()
    {
        if (!zawodnikA || !zawodnikB || !animacjaA || !animacjaB)
        { Debug.LogError("BRAXHORN_ARENA_POKAZ: brak zawodników/animacji", this); enabled = false; return; }
        foreach (string nazwa in Klipy)
            if (!animacjaA.GetClip(nazwa) || !animacjaB.GetClip(nazwa))
            { Debug.LogError("BRAXHORN_ARENA_POKAZ: brak klipu " + nazwa, this); enabled = false; return; }
        glowaA = Kosc(zawodnikA, "Head"); glowaB = Kosc(zawodnikB, "Head");
        lewaA = Kosc(zawodnikA, "LeftHand"); prawaA = Kosc(zawodnikA, "RightHand");
        lewaB = Kosc(zawodnikB, "LeftHand"); prawaB = Kosc(zawodnikB, "RightHand");
        if (!glowaA || !glowaB || !lewaA || !prawaA || !lewaB || !prawaB)
        { Debug.LogError("BRAXHORN_ARENA_POKAZ: brak kości kontaktu", this); enabled = false; return; }
        Ustaw(0f);
    }

    static Transform Kosc(Transform model, string name)
    {
        foreach (var t in model.GetComponentsInChildren<Transform>(true))
            if (t.name.EndsWith(name, System.StringComparison.Ordinal)) return t;
        return null;
    }

    void Update()
    {
        if (!kamera && Camera.main) kamera = Camera.main.transform;
        bool blisko = kamera && (kamera.position - srodek).sqrMagnitude < zasieg * zasieg;
        animacjaA.enabled = animacjaB.enabled = blisko;
        if (!blisko) return;
        reakcjaA -= Time.deltaTime; reakcjaB -= Time.deltaTime;
        kontaktA -= Time.deltaTime; kontaktB -= Time.deltaTime;
        zegar = Mathf.Repeat(zegar + Time.deltaTime, 24f);
        Ustaw(zegar);
    }

    void Ustaw(float czas)
    {
        // Druga połowa rundy: role zamienione. Powolne przesunięcie pary w obrębie klatki.
        float faza = Mathf.Repeat(czas, 12f);
        bool zamiana = czas >= 12f;
        float kat = Mathf.Sin(czas * Mathf.PI / 12f) * .32f;
        Vector3 os = new Vector3(Mathf.Sin(kat), 0f, Mathf.Cos(kat));
        Vector3 przes = Vector3.right * (Mathf.Sin(czas * Mathf.PI / 6f) * .5f);
        float odstep = dystans + .4f * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2f, 3f, faza)))
            + .4f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(8f, 9f, faza));
        Vector3 pa = przes - os * odstep * .5f, pb = przes + os * odstep * .5f;
        pa = Vector3.ClampMagnitude(pa, promienBezpieczny); pb = Vector3.ClampMagnitude(pb, promienBezpieczny);
        zawodnikA.position = srodek + pa; zawodnikB.position = srodek + pb;
        zawodnikA.rotation = Quaternion.LookRotation(pb - pa);
        zawodnikB.rotation = Quaternion.LookRotation(pa - pb);
        int atak = 0, obrona = 0;
        if (faza < 2.5f || faza > 9f) atak = obrona = 1;
        else if (faza < 4.4f) { atak = 2; obrona = 0; }
        else if (faza < 5.5f) { atak = 0; obrona = 3; }
        else if (faza < 7.4f) { atak = 0; obrona = 2; }
        else if (faza < 8.5f) { atak = 3; obrona = 0; }
        Zagraj(animacjaA, reakcjaA > 0 ? 4 : zamiana ? obrona : atak, ref stanA);
        Zagraj(animacjaB, reakcjaB > 0 ? 4 : zamiana ? atak : obrona, ref stanB);
    }

    void LateUpdate()
    {
        if (!animacjaA || !animacjaA.enabled || !glowaA || !glowaB) return;
        // Reakcja wynika z bliskości rękawicy i głowy po próbkowaniu animacji,
        // nie ze sztywnego zegara trafień. Bez obrażeń, AI i reakcji gracza.
        if (stanA == 2 && stanB != 2 && stanB != 3 && kontaktB <= 0 && Kontakt(lewaA, prawaA, glowaB))
        { reakcjaB = animacjaB.GetClip("Trafienie").length; kontaktB = .45f; stanB = -1; Zagraj(animacjaB, 4, ref stanB); }
        if (stanB == 2 && stanA != 2 && stanA != 3 && kontaktA <= 0 && Kontakt(lewaB, prawaB, glowaA))
        { reakcjaA = animacjaA.GetClip("Trafienie").length; kontaktA = .45f; stanA = -1; Zagraj(animacjaA, 4, ref stanA); }
    }
    static bool Kontakt(Transform left, Transform right, Transform head) =>
        (left.position - head.position).sqrMagnitude < .20f || (right.position - head.position).sqrMagnitude < .20f;

    static void Zagraj(Animation anim, int stan, ref int poprzedni)
    {
        if (stan == poprzedni) return;
        poprzedni = stan;
        var clip = anim[Klipy[stan]];
        clip.time = 0f;
        if (stan == 2) clip.speed = clip.length / 1.9f;
        anim.CrossFade(Klipy[stan], .16f);
    }

    void OnDisable()
    {
        if (animacjaA) animacjaA.Stop();
        if (animacjaB) animacjaB.Stop();
        stanA = stanB = -1;
    }
}
