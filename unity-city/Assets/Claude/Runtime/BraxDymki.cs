using System;
using System.Collections.Generic;
using UnityEngine;

// Existing dialogue API retained; dialogue is spoken Polish audio, with no speech bubbles.
public sealed class BraxDymki : MonoBehaviour
{
    [Serializable] sealed class Kwestia { public string tekst, id; }
    [Serializable] sealed class Indeks { public Kwestia[] kwestie; }
    static Dictionary<string, string> indeks;
    static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    static readonly HashSet<string> brak = new HashSet<string>();

    static AudioClip Klip(string tekst, float ton)
    {
        if (indeks == null)
        {
            indeks = new Dictionary<string, string>();
            var json = Resources.Load<TextAsset>("OsiedleGlosy/indeks");
            if (!json) { Debug.LogError("BRAXHORN_GLOS brak indeksu polskich dialogów"); return null; }
            foreach (var q in JsonUtility.FromJson<Indeks>(json.text).kwestie) indeks[q.tekst] = q.id;
        }
        if (!indeks.TryGetValue(tekst, out var id))
        {
            if (brak.Add(tekst)) Debug.LogError("BRAXHORN_GLOS brak nagrania: " + tekst);
            return null;
        }
        string key = id + (ton > 1.15f ? "_f" : "_m");
        if (!clips.TryGetValue(key, out var clip)) clips[key] = clip = Resources.Load<AudioClip>("OsiedleGlosy/" + key);
        return clip;
    }
    public static float Czas(string tekst, float ton = 1f)
    {
        var clip = Klip(tekst, ton); return clip ? clip.length : 0f;
    }
    public static float Powiedz(Transform cel, string tekst, float ton = 1f)
    {
        if (!cel || string.IsNullOrEmpty(tekst)) return 0f;
        var clip = Klip(tekst, ton); if (!clip) return 0f;
        var camera = Camera.main;
        if (camera && (camera.transform.position - cel.position).sqrMagnitude > (BraxGlosPostaci.MaxOdl + 2f) * (BraxGlosPostaci.MaxOdl + 2f)) return clip.length;   // build 45: zasięg głosu 24 m × K
        var voice = cel.GetComponent<BraxGlosPostaci>();
        if (!voice) voice = cel.gameObject.AddComponent<BraxGlosPostaci>();
        voice.Mow(clip);
        return clip.length;
    }
    public static void Pokaz(Transform cel, string tekst, float czas, float wys = 2.25f) { Powiedz(cel, tekst); }

    static readonly System.Random los = new System.Random();
    public static string Los(string[] z) => z[los.Next(z.Length)];

    public static readonly string[] Powitania = { "Siema!", "O, cześć!", "Elo! Co tam?", "Dzień dobry!", "Ale forma, szacun!", "Trening dziś był?", "Hej!", "Cześć, mistrzu!" };
    public static readonly string[] Wiesci =
    {
        "Słyszałeś? Za kasynem jest siłownia BRAXHORN. Podobno sprzęt jak w zawodowym klubie.",
        "W Parku Wiśniowym chłopaki grają w kosza. Wpadnij, może trafisz za trzy.",
        "W hali walk szykują galę. Ludzie już pytają o bilety.",
        "Na Poznańskiej zrobili rondo. W końcu nie stoi się w korku pod tunelem.",
        "Fontanna w parku znowu działa. Dzieciaki mają frajdę.",
        "W Shake Barze robią najlepsze szejki na osiedlu. Spróbuj bananowego.",
        "Tunelem dojdziesz do miasta. Tam dopiero się dzieje.",
        "Pogoda dziś jak marzenie. Aż chce się pobiegać.",
        "Mój syn chce trenować kick-boxing. Przyjmujecie dzieciaki?",
        "W kasynie wczoraj ktoś rozbił bank. Cała dzielnica o tym gada.",
        "Na korcie ktoś zostawił wyrzutnię piłek. Można poodbijać.",
        "Mapę osiedla masz w menu. Ja bym się tu bez niej zgubił.",
    };
    public static readonly string[] OdpowiedziBraxa = { "Wpadnę, zobaczę.", "Dzięki, dobrze wiedzieć.", "No to trzeba sprawdzić.", "Brzmi nieźle.", "Jasne, zapraszam na trening.", "Dzięki, trzymaj się." };
    public static readonly string[] Pozegnania = { "Powodzenia!", "Trzymaj się!", "To do zobaczenia!", "Nara!", "Miłego dnia!" };
    public static readonly string[][] MiedzySoba =
    {
        new[] { "Widziałeś nowe auta na parkingu?", "Te Porsche? Pewnie ktoś z kasyna.", "No ba." },
        new[] { "Idziesz jutro na siłownię?", "Rano, przed pracą.", "To się widzimy." },
        new[] { "Ale dziś słońce, co?", "Wreszcie. Cały tydzień lało." },
        new[] { "Byłeś w nowym Shake Barze?", "Byłem. Truskawkowy daje radę." },
        new[] { "Gdzie tu jest komisariat?", "Na Wiśniowej, za skrzyżowaniem.", "Dzięki!" },
        new[] { "Słyszałeś o gali w hali walk?", "Słyszałem. Brax ma podobno wystąpić.", "To trzeba iść!" },
        new[] { "Kupujesz tę willę na sprzedaż?", "Chciałbym. Ale ceny kosmos." },
        new[] { "Fajnie, że zrobili to rondo.", "No, wcześniej był tu chaos." },
        new[] { "Grasz dziś w kosza?", "Jak skończę robotę. Wpadaj o szóstej." },
    };
}
