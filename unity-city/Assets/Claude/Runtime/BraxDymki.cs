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
    public static float Powiedz(Transform cel, string tekst, float ton = 1f) => Powiedz(cel, tekst, ton, BraxGlosPostaci.Rodzaj.Rozmowa);
    public static float Powiedz(Transform cel, string tekst, float ton, BraxGlosPostaci.Rodzaj rodzaj)
    {
        if (!cel || string.IsNullOrEmpty(tekst)) return 0f;
        var clip = Klip(tekst, ton); if (!clip) return 0f;
        var camera = Camera.main;
        if (camera && (camera.transform.position - cel.position).sqrMagnitude > (BraxGlosPostaci.MaxOdl + 2f) * (BraxGlosPostaci.MaxOdl + 2f)) return clip.length;   // build 45: zasięg głosu 24 m × K
        var voice = cel.GetComponent<BraxGlosPostaci>();
        if (!voice) voice = cel.gameObject.AddComponent<BraxGlosPostaci>();
        voice.Mow(clip, rodzaj);
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

    // ---------------- E4b (build 45): więcej kwestii, wg sytuacji i roli (właściciel 20:40: „więcej tekstów … nie każdy zagaduje”) ----------------
    // Zwracają się „Patryk” / „sąsiad” — bez wymyślonych imion. Nagrania: unity-tools/claude/osiedle_glosy.py (sytuacja = nazwa tablicy).
    public static readonly string[] Mijanie =
    {
        "Dzień dobry.", "Cześć.", "Siema.", "Dobry.", "Witam.", "Hej, sąsiad!", "Cześć, Patryk!", "No hej.", "Dzień dobry, sąsiedzie.",
        "Siemanko!", "Cześć, co słychać?", "O, Patryk! Siema!", "Uszanowanie.", "Dzień dobry panu.", "Elo, sąsiad.",
        "Hej, jak leci?", "Cześć, cześć.", "Dzień dobry, jak zdrowie?", "Siema, wszystko gra?", "Witam sąsiada!",
    };
    public static readonly string[] MijanieRano =
    {
        "Dzień dobry! Wcześnie dziś.", "Dobry, kawa już była?", "Rano najlepiej się biega.", "Dzień dobry, do pracy?",
        "O, ranny ptaszek.", "Dzień dobry! Ładny poranek.", "Dzień dobry, chłodno dziś rano.",
    };
    public static readonly string[] MijanieWieczor =
    {
        "Dobry wieczór.", "Wieczorkiem na spacer?", "Dobry wieczór, sąsiedzie.", "Ciepły wieczór, co?", "Dobranoc, sąsiad.",
        "Dobry wieczór! Z treningu?", "Już ciemno się robi.",
    };
    public static readonly string[] WiesciWiecej =
    {
        "Mówią, że w ARENIE BRAXHORN będzie walka wieczoru. Bilety schodzą jak ciepłe bułki.",
        "Na sali bokserskiej przyszedł nowy worek. Podobno twardy jak beton.",
        "W kawiarni mają nowe ciasto. Ludzie stoją w kolejce.",
        "Komisariat szuka świadków wczorajszej stłuczki na rondzie.",
        "Sąsiad z naprzeciwka kupił nowe auto. Cała ulica się gapi.",
        "W markecie jest promocja na owoce. Warto zajrzeć.",
        "Fontanna w parku wieczorem ładnie wygląda.",
        "Podobno na Wiśniowej będą sadzić nowe drzewa.",
        "Na boisku w parku w sobotę jest turniej. Zapisy u trenera.",
        "W klubie w piątek gra nowy DJ. Ma być tłum.",
        "Szpital szuka krwiodawców. Można się zgłosić w rejestracji.",
        "Na korcie tenisowym wieczorem jest wolne. Można pograć.",
        "Kasyno zmieniło godziny otwarcia. Teraz do rana.",
        "W siłowni BRAXHORN jest nowy trener od zapasów.",
        "Ktoś zostawił rower przy parku. Stoi tam od tygodnia.",
        "Mówią, że w willach na południu wprowadza się nowa rodzina.",
        "Ulica przy parku jest w remoncie. Lepiej objechać.",
        "W mieście za tunelem jest teraz festyn. Głośno do późna.",
        "Shake Bar ma nowy smak. Czekoladowy z orzechami.",
        "Na Bydgoskiej ktoś znowu zaparkował na chodniku.",
        "Wieczorem w ARENIE trenują zawodnicy przed galą. Można popatrzeć.",
        "Butik ma się otworzyć lada dzień. Witryny już gotowe.",
        "W parku centralnym ktoś karmi kaczki przy fontannie.",
    };
    public static readonly string[] Sasiedzi =
    {
        "O, sąsiad! Jak tam ogródek?", "Trawa znowu do koszenia.", "Sąsiedzie, pożyczysz drabinę?", "Kwiaty w tym roku pięknie zakwitły.",
        "Grill w sobotę? Zapraszam.", "Ktoś parkuje pod moją bramą. Nie wiesz kto?", "Paczka przyszła do ciebie, wzięłam ją do siebie.",
        "Uważaj na schodach, mokro.", "Dzieciaki znowu grały w piłkę pod oknami.", "Słyszałeś tę burzę w nocy?",
        "Płot trzeba by pomalować przed zimą.", "Jak się mieszka? Wszystko w porządku?", "Mój pies znowu uciekł do ogrodu obok. Przepraszam.",
        "Wieczorem było głośno na ulicy. Też słyszałeś?", "Sąsiedzie, masz może klucz do altanki?",
    };
    public static readonly string[] Sklep =
    {
        "Dzień dobry, coś podać?", "Świeże pieczywo dopiero przyszło.", "Reklamówka potrzebna?", "Gotówka czy karta?",
        "Dziękuję, zapraszam ponownie.", "Mamy dziś promocję na wodę.", "Paragon do torby?", "Proszę bardzo, reszta się zgadza.",
        "Kawa na wynos czy na miejscu?", "Ostatnie dwa rogaliki, zostawić?", "Coś jeszcze?", "Może jakiś deser do kawy?",
        "Karta zbliżeniowo, proszę przyłożyć.", "Proszę, smacznego!", "Zaraz podam, chwileczkę.",
    };
    public static readonly string[] Parking =
    {
        "Ładne auto, sąsiad!", "Uważaj, tu się ciasno parkuje.", "Ktoś mi zastawił wyjazd.", "Silnik ci ładnie chodzi.",
        "Na parkingu przy parku zawsze jest miejsce.", "Nie zostawiaj auta na zebrze, policja pilnuje.", "Wieczorem na rondzie jest korek.",
        "Umyłeś auto? Błyszczy się jak nowe.", "Uważaj na słupki przy wjeździe.", "Daleko się wybierasz?", "Te sportowe auta to jednak coś.",
        "Przy arenie zawsze ktoś stoi na dwóch miejscach.",
    };
    public static readonly string[] Arena =
    {
        "Idziesz na trening?", "Ile dziś na klatę?", "Widziałem twoją walkę. Mocny prawy!", "Gala w ARENIE już niedługo. Szykujesz się?",
        "Na worku trzeba pracować codziennie.", "Trener mówił, że masz dobrą gardę.", "Rozciągnij się, bo jutro będzie bolało.",
        "W klatce trzeba mieć głowę na karku.", "Brax trenuje dziś do późna.", "Pół godziny skakanki i jestem padnięty.", "Kiedy sparing?",
        "Zapisałem się na kickboxing. Trzymaj kciuki.", "Po treningu shake z Shake Baru to podstawa.", "Ring na sali bokserskiej świeżo naciągnięty.",
        "Dziś dzień nóg. Nie wiem, jak wrócę do domu.", "Widziałeś nowe rękawice w siłowni?", "Pamiętaj o gardzie, Patryk.",
        "Na gali będzie pełna hala.", "Trening rano, robota, i znowu trening.", "Ochrona w ARENIE już ćwiczy wpuszczanie widzów.",
    };
    public static readonly string[] StarszySasiad =
    {
        "Za moich czasów to tu było pole.", "Młody człowieku, nie biegaj tak szybko.", "Ach, te samochody. Kiedyś było ciszej.",
        "Pogoda na zmianę, kolano mnie boli.", "Kłaniam się, sąsiedzie.", "Proszę uważać na siebie.", "Pamiętam, jak tu sadzili te wiśnie.",
        "Dzień dobry. Ładnie się tu zrobiło.",
    };
    public static readonly string[] Ochroniarz =
    {
        "Wszystko w porządku?", "Proszę przechodzić.", "Bez awantur, proszę.", "Wejście tylko dla gości.", "Spokojnie, pilnuję.",
        "Dobry wieczór. Zapraszam do środka.",
    };
    public static readonly string[] PozegnaniaWiecej =
    {
        "Na razie!", "Do jutra!", "Udanego treningu!", "Uważaj na siebie!", "Pozdrów rodzinkę!", "Do zobaczenia na gali!",
        "Cześć, lecę!", "Spokojnego wieczoru!", "Wpadaj, kiedy chcesz!",
    };
    public static readonly string[] OdpowiedziPatryka =
    {
        "Jasne, sąsiedzie.", "Dzięki, wpadnę.", "Dobra, pamiętam.", "Zobaczymy.", "No, trzeba będzie.", "Spoko, ogarnę to.", "Dzień dobry!", "Cześć, cześć.",
    };
    // POGADAJ: krótkie rozmowy (mieszkaniec, Patryk, mieszkaniec)
    public static readonly string[][] Wymiany =
    {
        new[] { "Hej, Patryk! Długo cię nie było.", "Byłem w mieście, sporo roboty.", "To dobrze, że wróciłeś." },
        new[] { "Trenujesz jeszcze w BRAXHORN?", "Codziennie, rano i wieczorem.", "Szacun. Ja ledwo raz w tygodniu." },
        new[] { "Idziesz na galę do ARENY?", "Jasne, nie przegapię.", "To widzimy się na trybunach." },
        new[] { "Słyszałeś, że przy rondzie mają postawić nowe latarnie?", "Najwyższy czas, wieczorem ciemno.", "No właśnie." },
        new[] { "Sąsiedzie, pomożesz mi wnieść szafę?", "Pewnie, tylko się przebiorę.", "Dzięki, jesteś złoty." },
        new[] { "Jak tam auto, jeździ?", "Jak marzenie.", "Pokażesz mi kiedyś?" },
        new[] { "W Shake Barze był dziś tłum.", "Bananowy znowu się skończył?", "Jak zawsze o tej porze." },
        new[] { "Ten nowy w siłowni to podobno były zawodnik.", "Chętnie z nim posparuję.", "Tylko uważaj na nos." },
        new[] { "Dzieciaki pytały, czy pokażesz im parę ciosów.", "Czemu nie, w sobotę w parku.", "Przekażę im, będą zachwyceni." },
        new[] { "Kasyno znowu otwarte do rana.", "Ja tam wolę się wyspać.", "Mądry człowiek." },
        new[] { "Widziałeś, jak w parku naprawili fontannę?", "Widziałem, w końcu działa.", "Wieczorem jest podświetlona." },
        new[] { "Patryk, masz chwilę?", "Jasne, co tam?", "Nic, tak tylko pytam, co u ciebie." },
        new[] { "Gdzie się kupuje bilety na galę?", "Najlepiej w ARENIE, przy wejściu.", "Dzięki, jutro pójdę." },
        new[] { "Ale dziś upał, co?", "No, trening był ciężki.", "To pij dużo wody." },
        new[] { "Słyszałem, że chcesz walczyć na gali.", "Zobaczymy, co trener powie.", "Trzymam kciuki." },
    };
}
