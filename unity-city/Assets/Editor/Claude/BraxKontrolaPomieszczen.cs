using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

// KAŻDE POMIESZCZENIE KOMPLETNE — LISTY OBOWIĄZKOWE (właściciel 1.11, 23:43: „lustro, zawsze musi być lustro; jak prysznic,
// to słuchawka; wszystko jak powinno być”). Tylko raport (bez dorabiania mebli). Pomieszczenie rozpoznane po nazwie grupy
// (łazienka / WC / szatnia / kuchnia / sypialnia / salon / recepcja / bar) albo — w grupach z wieloma pokojami (wille) —
// po materiałach („marmur łazienki”, „fronty kuchni”, „dywan sypialni”…). Wyposażenie szukane w nazwach obiektów, siatek
// i materiałów całej grupy (PL i EN). Uwaga: w grupie z kilkoma pokojami rzecz z jednego pokoju „liczy się” też w drugim —
// raport pokazuje braki pewne, nie wszystkie możliwe.
public static class BraxKontrolaPomieszczen
{
    sealed class Pokoj { public string nazwa; public Regex rozp; public (string co, Regex w)[] lista; }
    static Regex R(string s) => new Regex(s);
    static readonly Regex Lustro = R(@"\b(lustr\w*|mirror\w*)\b"), Umywalka = R(@"\b(umywalk\w*|umywaln\w*|sink\w*|basin\w*|washbasin\w*)\b"),
        Bateria = R(@"\b(bateri\w*|kran|kranu|krany|faucet\w*|tap|taps|mixer\w*|wylewk\w*)\b"), Sedes = R(@"\b(sedes\w*|toalet\w*|toilet\w*|wc|muszl\w*|ustep\w*)\b"),
        Splucz = R(@"\b(splucz\w*|flush\w*|cistern\w*|stelaz\w*)\b"), Papier = R(@"\b(papier\w* toalet\w*|toilet paper|tissue\w*|uchwyt\w* (na )?papier\w*|papier\w*)\b"),
        Prysznic = R(@"\b(prysznic\w*|shower\w*|kabin\w* prysznic\w*|brodzik\w*)\b"), Sluchawka = R(@"\b(sluchawk\w*|hand ?shower\w*|deszczownic\w*|rain ?shower\w*|shower ?head\w*|glowic\w*)\b"),
        Odplyw = R(@"\b(odplyw\w*|drain\w*|syfon\w*|kratk\w* odplyw\w*)\b"), Lampa = R(@"\b(lamp\w*|kinkiet\w*|oswietl\w*|swiatl\w*|light\w*|plafon\w*|zyrandol\w*|chandelier\w*|zarowk\w*|zarow\w*)\b"),
        Szafka = R(@"\b(szafk\w*|cabinet\w*|vanity|szafa|szafy|komod\w*|drawer\w*)\b"), Recznik = R(@"\b(recznik\w*|towel\w*)\b"),
        Kosz = R(@"\b(kosz|kosze|koszy|bin|bins|trash\w*|smietnik\w*|waste\w*)\b"), Dywanik = R(@"\b(dywan\w*|mata|maty|bath ?mat\w*|rug|rugs|carpet\w*)\b"),
        Wentyl = R(@"\b(wentyl\w*|vent|vents|ventilation|wywiew\w*|kratk\w* wentyl\w*|anemostat\w*)\b"),
        Zlew = R(@"\b(zlew\w*|zlewozmyw\w*|sink\w*)\b"), Plyta = R(@"\b(plyt\w* (grzew|indukc|gazow|kuchen|ceramiczn)\w*|kuchenk\w*|cooktop\w*|hob|hobs|stove\w*|palnik\w*|indukc\w*)\b"),
        Piekarnik = R(@"\b(piekarnik\w*|oven\w*)\b"), Okap = R(@"\b(okap\w*|hood\w*|extractor\w*)\b"), Lodowka = R(@"\b(lodowk\w*|fridge\w*|refrigerator\w*|chlodziark\w*|chlodni\w*)\b"),
        Blat = R(@"\b(blat\w*|countertop\w*|worktop\w*)\b"), Czajnik = R(@"\b(czajnik\w*|kettle)\b"), Ekspres = R(@"\b(ekspres\w*|coffee ?machine\w*|espresso\w*|kawiark\w*)\b"),
        Lozko = R(@"\b(lozk\w*|lozek|bed|beds|materac\w*|mattress\w*)\b"), Posciel = R(@"\b(posciel\w*|koldr\w*|duvet\w*|bedding\w*|narzut\w*|kapa|blanket\w*|pled\w*|okrycie\w*)\b"),
        Poduszki = R(@"\b(poduszk\w*|pillow\w*|jasiek\w*|wezglow\w*)\b"), Nocna = R(@"\b(nocn\w*|nightstand\w*|bedside\w*)\b"),
        Szafa = R(@"\b(szafa|szafy|wardrobe\w*|closet\w*|garderob\w*)\b"), Zaslony = R(@"\b(zaslon\w*|rolet\w*|curtain\w*|blind\w*|firan\w*|zaluzj\w*)\b"),
        Sofa = R(@"\b(sofa\w*|kanap\w*|couch\w*|naroznik\w*)\b"), Stolik = R(@"\b(stolik\w*|coffee ?table\w*|lawa|stol|stolu|table\w*)\b"),
        Tv = R(@"\b(tv|telewizor\w*|television\w*|ekran\w*|screen\w*)\b"), Pilot = R(@"\b(pilot\w*|remote\w*)\b"),
        Rtv = R(@"\b(rtv|tv ?stand\w*|tv ?cabinet\w*|szafk\w*|komod\w*|console\w*|cabinet\w*|wiez\w* grajac\w*)\b"),
        Rosliny = R(@"\b(rosl\w*|plant\w*|kwiat\w*|donic\w*|bukiet\w*|monster\w*|fikus\w*|vase\w*|wazon\w*)\b"),
        Regal = R(@"\b(regal\w*|shel\w*|bookcase\w*|ksiaz\w*|obraz\w*|picture\w*|frame\w*|ramk\w*|plakat\w*|trofe\w*|puchar\w*)\b"),
        Lada = R(@"\b(lada\w*|lady|counter\w*|recepcj\w*|desk\w*|kontuar\w*|bar)\b"), Krzeslo = R(@"\b(krzesl\w*|chair\w*|fotel\w*|hoker\w*|stool\w*|stolek\w*|siedzisk\w*|seat\w*)\b"),
        Komputer = R(@"\b(komputer\w*|computer\w*|monitor\w*|laptop\w*|pc|terminal\w*)\b"), Telefon = R(@"\b(telefon\w*|phone\w*)\b"),
        Oznaczenie = R(@"\b(szyld\w*|sign\w*|napis\w*|logo\w*|tablic\w*)\b"), Butelki = R(@"\b(butel\w*|bottle\w*|polk\w*|shel\w*|regal\w*)\b"),
        Kasa = R(@"\b(kasa|kasy|register\w*|terminal\w*|kasow\w*)\b"), Szklanki = R(@"\b(szklan\w*|glass|glasses|kieliszk\w*|kufl\w*|kubk\w*|cup|cups|mug\w*)\b"),
        Nalewak = R(@"\b(ekspres\w*|nalewak\w*|beer ?tap\w*|coffee ?machine\w*|blender\w*|shaker\w*|dystrybutor\w*)\b"),
        Szafki = R(@"\b(szafk\w*|locker\w*|szafa|szafy)\b"), Lawka = R(@"\b(lawk\w*|bench\w*)\b");

    static readonly Pokoj[] Pokoje =
    {
        new Pokoj { nazwa = "łazienka", rozp = R(@"\b(lazienk\w*|bathroom\w*)\b"), lista = new[] { ("lustro", Lustro), ("lampa nad lustrem", Lampa), ("umywalka", Umywalka), ("bateria (kran)", Bateria), ("odpływ", Odplyw), ("szafka", Szafka), ("sedes", Sedes), ("spłuczka", Splucz), ("uchwyt na papier", Papier), ("prysznic / brodzik", Prysznic), ("słuchawka / deszczownica", Sluchawka), ("ręcznik", Recznik), ("dywanik", Dywanik), ("kosz", Kosz), ("wentylacja", Wentyl) } },
        new Pokoj { nazwa = "WC", rozp = R(@"\b(wc|toalet\w*|toilet\w*|restroom\w*|ubikacj\w*)\b"), lista = new[] { ("lustro", Lustro), ("umywalka", Umywalka), ("bateria (kran)", Bateria), ("sedes", Sedes), ("spłuczka", Splucz), ("uchwyt na papier", Papier), ("kosz", Kosz), ("lampa", Lampa) } },
        new Pokoj { nazwa = "szatnia", rozp = R(@"\b(szatni\w*|locker ?room\w*|przebieraln\w*)\b"), lista = new[] { ("szafki", Szafki), ("ławka", Lawka), ("prysznic", Prysznic), ("bateria", Bateria), ("słuchawka prysznica", Sluchawka), ("lustro", Lustro), ("lampa", Lampa) } },
        new Pokoj { nazwa = "kuchnia", rozp = R(@"\b(kuchni\w*|kuchnia|kitchen\w*|aneks\w* kuchenn\w*)\b"), lista = new[] { ("zlew", Zlew), ("bateria (kran)", Bateria), ("płyta grzewcza", Plyta), ("piekarnik", Piekarnik), ("okap", Okap), ("lodówka", Lodowka), ("szafki", Szafka), ("blat", Blat), ("czajnik", Czajnik), ("ekspres", Ekspres), ("kosz", Kosz), ("oświetlenie", Lampa) } },
        new Pokoj { nazwa = "sypialnia", rozp = R(@"\b(sypialn\w*|bedroom\w*)\b"), lista = new[] { ("łóżko z materacem", Lozko), ("pościel", Posciel), ("poduszki", Poduszki), ("szafki nocne", Nocna), ("lampki", Lampa), ("szafa", Szafa), ("zasłony / rolety", Zaslony), ("dywan", Dywanik) } },
        new Pokoj { nazwa = "salon", rozp = R(@"\b(salon\w*|living ?room\w*|pokoj\w* dzienn\w*)\b"), lista = new[] { ("sofa", Sofa), ("stolik", Stolik), ("TV", Tv), ("pilot", Pilot), ("szafka RTV", Rtv), ("dywan", Dywanik), ("lampy", Lampa), ("rośliny", Rosliny), ("regał / obrazy", Regal) } },
        new Pokoj { nazwa = "recepcja", rozp = R(@"\b(recepcj\w*|reception\w*)\b"), lista = new[] { ("lada", Lada), ("krzesło", Krzeslo), ("komputer", Komputer), ("telefon", Telefon), ("kosz", Kosz), ("oświetlenie", Lampa), ("oznaczenie / szyld", Oznaczenie) } },
        new Pokoj { nazwa = "bar", rozp = R(@"\b(bar|bary|baru|pub|kawiarn\w*|cafe|koktajl\w*)\b"), lista = new[] { ("lada", Lada), ("hokery / krzesła", Krzeslo), ("półki z butelkami", Butelki), ("kasa", Kasa), ("szklanki", Szklanki), ("ekspres / nalewak", Nalewak), ("lodówka", Lodowka), ("oświetlenie", Lampa), ("stoliki", Stolik) } },
    };

    public static void Wykonaj(Scene scena)
    {
        var znalezione = new List<(Pokoj p, Transform t, string skad)>();
        // 1) po nazwie: najwyższy obiekt w gałęzi o nazwie pokoju (z widocznymi siatkami pod sobą)
        foreach (var root in scena.GetRootGameObjects()) Szukaj(root.transform, znalezione, new HashSet<Pokoj>());
        // 2) po materiałach w grupach z wieloma pokojami (np. „Osiedle B / willa 3”: marmur łazienki, fronty kuchni, dywan sypialni)
        foreach (var root in scena.GetRootGameObjects())
            foreach (Transform g in root.GetComponentsInChildren<Transform>(false))
            {
                if (!g.name.Contains(" / ") && g.parent != root.transform) continue;
                var mats = new HashSet<string>();
                foreach (var r in g.GetComponentsInChildren<MeshRenderer>(false)) foreach (var m in r.sharedMaterials) if (m) mats.Add(BraxKontrola.Norm(m.name));
                foreach (var p in Pokoje)
                {
                    if (p.nazwa == "bar" || p.nazwa == "recepcja") continue;              // te tylko po nazwie grupy
                    if (!mats.Any(m => p.rozp.IsMatch(m))) continue;
                    if (znalezione.Any(z => z.p == p && (z.t == g || z.t.IsChildOf(g) || g.IsChildOf(z.t)))) continue;
                    znalezione.Add((p, g, "z materiałów"));
                }
            }
        int braki = 0, pokoje = 0;
        var linie = new List<string>();
        foreach (var (p, t, skad) in znalezione)
        {
            var sb = new StringBuilder(4096);
            foreach (var x in t.GetComponentsInChildren<Transform>(false))
            {
                sb.Append(BraxKontrola.Norm(x.name));
                var mf = x.GetComponent<MeshFilter>(); if (mf && mf.sharedMesh) sb.Append(BraxKontrola.Norm(mf.sharedMesh.name));
                var r = x.GetComponent<Renderer>(); if (r) foreach (var m in r.sharedMaterials) if (m) sb.Append(BraxKontrola.Norm(m.name));
                if (sb.Length > 400000) break;
            }
            string tekst = sb.ToString();
            var brak = p.lista.Where(e => !e.w.IsMatch(tekst)).Select(e => e.co).ToList();
            pokoje++; braki += brak.Count;
            bool any = false; Bounds b = default;
            foreach (var r in t.GetComponentsInChildren<MeshRenderer>(false)) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            string l = p.nazwa + " w „" + BraxKontrola.Sciezka(t) + "” (" + skad + (any ? ", " + BraxKontrola.F(b.center) : "") + "): " + (brak.Count == 0 ? "komplet" : "brak: " + string.Join(", ", brak));
            linie.Add(l); BraxKontrola.Zapisz("pomieszczenia: " + l);
        }
        // An empty room inventory cannot be accepted as complete for the
        // estate: it indicates a missing/empty generated interior or failed
        // room recognition. Keep the issue advisory to signed APK delivery.
        if (BraxKontrola.Obszar == "osiedle" && pokoje == 0)
        {
            const string brakPokoi = "pomieszczenia: ZGŁOSZONE audyt nie rozpoznał żadnego pokoju osiedla";
            BraxKontrola.Zapisz(brakPokoi);
            Debug.LogWarning("BRAXHORN_POMIESZCZENIA DO POPRAWKI: " + brakPokoi);
        }
        var o = new StringBuilder("BRAXHORN_POMIESZCZENIA " + BraxKontrola.Obszar + ": braki=" + braki + " (pomieszczeń " + pokoje + ")");
        foreach (var l in linie.OrderByDescending(x => x.Length).Take(40)) o.Append("\n  ").Append(l);
        Debug.Log(o.ToString());
    }

    static void Szukaj(Transform t, List<(Pokoj, Transform, string)> wynik, HashSet<Pokoj> nad)
    {
        if (!t.gameObject.activeInHierarchy || t.gameObject.layer == BraxKontrola.WarstwaSondy) return;
        string n = BraxKontrola.Norm(t.name);
        HashSet<Pokoj> tu = null;
        if (t.childCount >= 2)                                                       // grupa (nie sam sedes „WC” ani pojedyncza siatka)
            foreach (var p in Pokoje)
            {
                if (nad.Contains(p) || !p.rozp.IsMatch(n)) continue;
                if (p.nazwa == "salon" && Regex.IsMatch(n, @"\b(lounge|chair\w*)\b")) continue;
                if (t.GetComponentsInChildren<MeshRenderer>(false).Length < 3) continue;
                wynik.Add((p, t, "po nazwie"));
                (tu ??= new HashSet<Pokoj>(nad)).Add(p);
            }
        var dalej = tu ?? nad;
        for (int i = 0; i < t.childCount; i++) Szukaj(t.GetChild(i), wynik, dalej);
    }
}
