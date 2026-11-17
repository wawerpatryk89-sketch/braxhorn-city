using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// REKLAMA MIASTA MISTRZÓW (właściciel 16.11, 23:48–23:49: „ale zrób taką reklamę”,
// „jakbyś zrobił to w tle z jakimiś gestami, to jesteś kot”, „pod koniec podchodzi
// Mateusz z Kamilem pijani”).
//
// Patryk stoi przodem do kamery i mówi SWOIM nagraniem, zmieniając gesty. Pod koniec
// z boku wchodzą Mateusz i Kamil, zataczając się (DrunkWalk), zatrzymują się obok
// i przerywają mu swoimi kwestiami. Ich głos idzie z generatora, bo właściciel
// powiedział wprost: „ten drugi tekst zrób inny, nie mój głos”.
//
// Kolejność kwestii Patryka = kolejność plików patryk_NN_m. Dopóki nie nagra tekstu
// reklamy, lecą jego dotychczasowe frazy jako podkład — scena jest gotowa, wystarczy
// podmienić pliki.
[DefaultExecutionOrder(20000)]
public sealed class BraxReklama : MonoBehaviour
{
    public Animation patryk;
    public Animation mateusz;
    public Animation kamil;
    public Camera kamera;
    public Transform srodek;
    public float wzrost = 1.8f;

    // KADR LICZONY, NIE ZGADNIĘTY. Telefon trzymany pionowo ma wąskie pole widzenia
    // w poziomie: przy 55 stopniach w pionie i proporcji 9:19,5 połowa kadru w poziomie
    // to niecałe 15 stopni, czyli w odległości 4,3 m mieści się tylko ±1,1 m. Dlatego
    // na zdjęciu właściciela (17.11, 13:40) drugi aktor wisiał na krawędzi ekranu.
    // Tu odległość wychodzi z tego, co naprawdę ma być w kadrze.
    void Kadruj(Vector3[] punkty, float zapas)
    {
        if (!kamera || punkty.Length == 0 || !srodek) return;
        Vector3 przod = srodek.forward, prawo = srodek.right;
        Vector3 c = Vector3.zero;
        foreach (var p in punkty) c += p;
        c /= punkty.Length;

        float polSzer = .55f;            // pół szerokości barków skrajnego aktora
        foreach (var p in punkty) polSzer = Mathf.Max(polSzer, Mathf.Abs(Vector3.Dot(p - c, prawo)) + .55f);
        float polWys = wzrost * .62f;    // sylwetka plus zapas nad głową i pod stopami

        float tanPion = Mathf.Tan(kamera.fieldOfView * .5f * Mathf.Deg2Rad);
        float tanPoziom = tanPion * Mathf.Max(.2f, kamera.aspect);
        float d = Mathf.Max(polSzer / tanPoziom, polWys / tanPion) * zapas;

        Vector3 cel = c + Vector3.up * (wzrost * .56f);
        // Postać patrzy wzdłuż swojego „przodu”, więc kamera stoi PO TEJ stronie.
        // (Na zdjęciu właściciela była po drugiej i widać było same plecy.)
        kamera.transform.position = cel + przod * d;
        kamera.transform.rotation = Quaternion.LookRotation(cel - kamera.transform.position, Vector3.up);
    }

    IEnumerator PrzejedzKadr(Vector3[] punkty, float zapas, float czas)
    {
        Vector3 skadP = kamera.transform.position;
        Quaternion skadR = kamera.transform.rotation;
        Kadruj(punkty, zapas);
        Vector3 dokadP = kamera.transform.position;
        Quaternion dokadR = kamera.transform.rotation;
        for (float t = 0; t < 1f; t += Time.deltaTime / czas)
        {
            float s = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));
            kamera.transform.position = Vector3.Lerp(skadP, dokadP, s);
            kamera.transform.rotation = Quaternion.Slerp(skadR, dokadR, s);
            yield return null;
        }
        kamera.transform.position = dokadP;
        kamera.transform.rotation = dokadR;
    }

    // Gest na kwestię. Jak postać nie ma ujęcia, zostaje przy Idle — nie przerywa sceny.
    static readonly string[] Gesty =
    {
        "Wave", "Idle", "Point", "Idle", "Taunt", "Idle", "Point",
        "Victory", "Idle", "Point", "Idle", "Idle", "Idle", "Bow",
    };

    string etap = "";
    string mowi = "";

    void Start() => StartCoroutine(Scena());

    static List<AudioClip> Kwestie(string przedrostek)
    {
        var lista = new List<AudioClip>();
        foreach (var k in Resources.LoadAll<AudioClip>("OsiedleGlosy"))
            if (k && k.name.StartsWith(przedrostek, System.StringComparison.Ordinal))
                lista.Add(k);
        lista.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return lista;
    }

    static AudioSource Zrodlo(Component c)
    {
        var z = c.GetComponent<AudioSource>();
        if (!z) z = c.gameObject.AddComponent<AudioSource>();
        z.spatialBlend = 0f;
        z.playOnAwake = false;
        return z;
    }

    void Graj(Animation a, string klip)
    {
        if (!a || !a.GetClip(klip)) return;
        a[klip].wrapMode = klip == "Idle" || klip == "DrunkWalk" || klip == "DrunkIdle"
            ? WrapMode.Loop : WrapMode.ClampForever;
        a.CrossFade(klip, .25f);
    }

    IEnumerator Powiedz(Animation kto, AudioClip klip, string podpis)
    {
        if (!kto || !klip) yield break;
        var z = Zrodlo(kto);
        z.clip = klip;
        z.Play();
        mowi = podpis;
        var mim = kto.GetComponentInChildren<BraxMimikaOsiedla>();
        if (mim) mim.Wypowiedz(z);
        float koniec = Time.time + klip.length + .18f;
        while (Time.time < koniec) yield return null;
        mowi = "";
    }

    IEnumerator Scena()
    {
        yield return null;
        var patrykKwestie = Kwestie("patryk_rekl_");
        if (patrykKwestie.Count == 0) patrykKwestie = Kwestie("patryk_");
        var duet = new List<(Animation kto, AudioClip klip, string podpis)>();
        foreach (var k in Kwestie("mateusz_rekl_")) duet.Add((mateusz, k, "Mateusz"));
        foreach (var k in Kwestie("kamil_rekl_")) duet.Add((kamil, k, "Kamil"));
        duet.Sort((a, b) => string.CompareOrdinal(
            a.klip.name.Substring(a.klip.name.IndexOf("_rekl_", System.StringComparison.Ordinal)),
            b.klip.name.Substring(b.klip.name.IndexOf("_rekl_", System.StringComparison.Ordinal))));

        Debug.Log($"BRAX_REKLAMA kwestie Patryka={patrykKwestie.Count} duet={duet.Count}");

        // Ci dwaj czekają poza kadrem, z boku i z tyłu.
        Vector3 startM = Vector3.zero, startK = Vector3.zero, celM = Vector3.zero, celK = Vector3.zero;
        if (patryk)
        {
            var p = patryk.transform.parent ? patryk.transform.parent : patryk.transform;
            Vector3 bok = p.right, przod = p.forward;
            startM = p.position + bok * 7.5f + przod * 1.2f;
            startK = p.position + bok * 9.2f + przod * .2f;
            // Stają BLIŻEJ niż poprzednio i lekko z tyłu — przy trzech osobach w pionowym
            // kadrze każdy metr w bok kosztuje odsunięcie kamery, a wtedy twarze maleją.
            celM = p.position + bok * .95f - przod * .35f;
            celK = p.position + bok * 1.75f - przod * .6f;
            foreach (var (a, poz) in new[] { (mateusz, startM), (kamil, startK) })
                if (a)
                {
                    var r = a.transform.parent ? a.transform.parent : a.transform;
                    r.position = poz;
                    r.rotation = Quaternion.LookRotation(-bok, Vector3.up);
                    Graj(a, "Idle");
                }
        }

        // 1. Patryk mówi i gestykuluje — kadr tylko na niego, żeby gesty były duże.
        var sam = patryk.transform.parent ? patryk.transform.parent : patryk.transform;
        Kadruj(new[] { sam.position }, 1.12f);
        etap = "REKLAMA";
        for (int i = 0; i < patrykKwestie.Count; i++)
        {
            Graj(patryk, i < Gesty.Length ? Gesty[i] : "Idle");
            yield return Powiedz(patryk, patrykKwestie[i], "Patryk");
        }

        // 2. Wchodzą pijani — w trakcie ostatnich słów Patryka.
        etap = "…i wtedy wchodzą ci dwaj";
        Graj(mateusz, "DrunkWalk");
        Graj(kamil, "DrunkWalk");
        // Kamera odjeżdża w tym samym czasie, w którym oni wchodzą — inaczej albo weszliby
        // poza kadr, albo trzeba by od początku trzymać szeroki plan i twarze byłyby małe.
        StartCoroutine(PrzejedzKadr(new[] { sam.position, celM, celK }, 1.1f, 3.0f));
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 3.4f;
            float s = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));
            if (mateusz)
            {
                var r = mateusz.transform.parent ? mateusz.transform.parent : mateusz.transform;
                r.position = Vector3.Lerp(startM, celM, s);
            }
            if (kamil)
            {
                var r = kamil.transform.parent ? kamil.transform.parent : kamil.transform;
                r.position = Vector3.Lerp(startK, celK, Mathf.Clamp01(s * 1.12f));
            }
            yield return null;
        }
        foreach (var a in new[] { mateusz, kamil })
            if (a)
            {
                Graj(a, "DrunkIdle");
                var r = a.transform.parent ? a.transform.parent : a.transform;
                if (kamera) r.rotation = Quaternion.LookRotation(
                    Vector3.ProjectOnPlane(kamera.transform.position - r.position, Vector3.up), Vector3.up);
            }
        Graj(patryk, "Idle");

        // 3. Przerywają mu.
        etap = "PIJANI PRZERYWAJĄ";
        foreach (var (kto, klip, podpis) in duet)
            yield return Powiedz(kto, klip, podpis);

        etap = "KONIEC — dotknij, żeby od nowa";
        while (!Input.anyKeyDown && Input.touchCount == 0) yield return null;
        StartCoroutine(Scena());
    }

    void OnGUI()
    {
        float s = Screen.height / 1280f;
        var styl = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(30 * s),
            alignment = TextAnchor.UpperLeft,
        };
        styl.normal.textColor = Color.white;
        GUI.Label(new Rect(18 * s, 14 * s, Screen.width - 36 * s, 40 * s), etap, styl);
        if (mowi != "")
        {
            var m = new GUIStyle(styl) { fontSize = Mathf.RoundToInt(34 * s), alignment = TextAnchor.LowerCenter };
            m.normal.textColor = new Color(1f, .86f, .35f);
            GUI.Label(new Rect(0, Screen.height - 86 * s, Screen.width, 50 * s), mowi, m);
        }
    }
}
