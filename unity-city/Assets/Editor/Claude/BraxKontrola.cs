using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

// KONTROLE JAKOŚCI I NAPRAWY GLOBALNE PRZY BUILDZIE (build 14, właściciel 2.11: „nie chcę już nagrywać — kontrole mają
// to łapać same”; audyt: SKALA GLOBALNA, SKALA WZGLĘDEM BOHATERA, KAŻDA RZECZ SPRAWDZONA — UMIESZCZENIE I SENS, ROBOT KOLIZJI,
// MIGOTANIE, KAŻDE POMIESZCZENIE KOMPLETNE). Kod ogólny — działa na dowolnej scenie (osiedle teraz, miasto później):
//   1) spis: każdy mebel/przedmiot rozpoznany po nazwie obiektu / siatki / materiału (PL i EN) + przedmioty ogólne
//      (modele z LODGroup, pojedyncze siatki zwarte do 4.5 m) — bez budynków, aut, ludzi i siatek scalonych przez generator;
//   2) sondy: na czas kontroli każda widoczna siatka (bez trawy, kwiatów, liści) dostaje kolizję siatki na warstwie 31 —
//      dzięki temu promienie „widzą” to, co widać na ekranie, a nie tylko kolizje. Usuwane w finally przed zapisem sceny;
//   3) prostowanie → skala względem bohatera → umieszczenie (podłoga, ściany, części, szyldy, daszki, lady) → kolizje →
//      migotanie → listy pomieszczeń → robot kolizji. Każdy krok w try/catch: błąd = „DO POPRAWKI”, build idzie dalej.
// Wyniki w logu CI (BRAXHORN_SKALA_RAPORT, BRAXHORN_UMIESZCZENIE, BRAXHORN_KOLIZJE, BRAXHORN_ROBOT, BRAXHORN_MIGOTANIE,
// BRAXHORN_POMIESZCZENIA, BRAXHORN_KONTROLA) i w pliku city-review/kontrola-<obszar>.json.
public static class BraxKontrola
{
    public const int WarstwaSondy = 31;                  // tymczasowe kolizje siatek (tylko na czas kontroli)
    public const int WarstwaKamery = 29;                 // przeszkody tylko dla kamery (BraxClaudeSufity)
    public const int MaskaSond = 1 << WarstwaSondy;
    // kolizje „prawdziwe” (to, na czym stoi i o co obija się gracz): bez gracza (8), bez Ignore Raycast (2), bez kamery, bez sond
    public const int MaskaStala = ~((1 << 8) | (1 << 2) | (1 << WarstwaKamery) | (1 << WarstwaSondy));
    public static float K => BraxClaudeOsiedle.K;
    public static float H => BraxClaudeOsiedle.WzrostBohatera;

    static Stopwatch zegar;
    public static float Sekundy => zegar == null ? 0f : (float)zegar.Elapsed.TotalSeconds;
    public static string Obszar = "";
    static readonly List<string> raport = new List<string>();
    public static void Zapisz(string linia) { if (raport.Count < 4000) raport.Add(linia); }

    [Serializable] sealed class Plik { public string obszar; public float wzrostBohatera, k, sekundy; public List<string> linie = new List<string>(); }

    // ---------------- wejście ----------------
    public static void Wszystko(Scene scena, string obszar)
    {
        zegar = Stopwatch.StartNew(); Obszar = obszar; raport.Clear(); Wyczysc();
        try { UstawStrefe(scena); } catch (Exception e) { JestStrefa = false; Debug.LogWarning("DO POPRAWKI: kontrola — strefa chroniona: " + e.Message); }
        try { BraxClaudeOsiedle.ZmierzBohatera(); } catch (Exception e) { Debug.LogWarning("DO POPRAWKI: kontrola — pomiar bohatera: " + e.Message); }
        Physics.SyncTransforms();                        // kolizje generatora ustawione po dodaniu — granice aktualne przed spisem
        Spis spis = null;
        Krok("spis", () => spis = Spis.Zbuduj(scena));
        if (spis == null) return;
        try
        {
            Krok("sondy", () => Sondy.Zbuduj(scena));
            Krok("prostowanie", () => BraxKontrolaUmieszczenia.Prostuj(spis));
            Krok("skala", () => BraxKontrolaSkali.Wykonaj(spis));
            Krok("umieszczenie", () => BraxKontrolaUmieszczenia.Wykonaj(scena, spis));
            Krok("kolizje", () => BraxKontrolaKolizji.Wykonaj(spis));
            Krok("migotanie", () => BraxKontrolaMigotania.Wykonaj(scena, spis));
            Krok("materiały (podłogi-lustra)", () => BraxKontrolaMaterialow.Wykonaj(scena));
            Krok("pomieszczenia", () => BraxKontrolaPomieszczen.Wykonaj(scena));
            Krok("robot", () => BraxRobotKolizji.Wykonaj(scena, spis));
        }
        finally
        {
            Krok("sprzątanie sond", Sondy.Usun);
        }
        Krok("raport", () =>
        {
            Directory.CreateDirectory("city-review");
            var p = new Plik { obszar = obszar, wzrostBohatera = H, k = K, sekundy = Sekundy, linie = new List<string>(raport) };
            File.WriteAllText("city-review/kontrola-" + obszar + ".json", JsonUtility.ToJson(p, true));
        });
        Wyczysc();
        Debug.Log("BRAXHORN_KONTROLA " + obszar + ": czas " + Sekundy.ToString("F1") + " s, jednostek " + spis.jednostki.Count +
                  " (meble/przedmioty rozpoznane " + spis.jednostki.Count(j => j.typ != null) + "), wzrost bohatera " + H.ToString("F2") + " m, k " + K.ToString("F3"));
    }

    // pamięć podręczna siatek i nazw — zwalniana po kontroli (siatki trzymane do odczytu zajmują setki MB)
    static void Wyczysc() { siatki.Clear(); scalone.Clear(); roslMat.Clear(); chronCache.Clear(); typCache.Clear(); normCache.Clear(); }

    static void Krok(string co, Action a)
    {
        var t0 = Sekundy;
        try { a(); }
        catch (Exception e) { Debug.LogWarning("DO POPRAWKI: kontrola " + co + " (" + Obszar + "): " + e); Zapisz("BŁĄD kroku " + co + ": " + e.Message); }
        Zapisz("krok " + co + ": " + (Sekundy - t0).ToString("F1") + " s");
    }

    // ---------------- nazwy ----------------
    static readonly Dictionary<string, string> normCache = new Dictionary<string, string>();
    // małe litery, bez polskich znaków, separatory → spacja, granice litera/cyfra i camelCase rozdzielone; spacja z obu stron
    public static string Norm(string s)
    {
        if (string.IsNullOrEmpty(s)) return " ";
        if (normCache.TryGetValue(s, out var have)) return have;
        var sb = new StringBuilder(s.Length + 8); sb.Append(' ');
        char prevRaw = ' '; int prevKind = 0;           // 0 separator, 1 litera, 2 cyfra
        foreach (char raw in s)
        {
            char c = char.ToLowerInvariant(raw);
            switch (c)
            {
                case 'ą': c = 'a'; break; case 'ć': c = 'c'; break; case 'ę': c = 'e'; break; case 'ł': c = 'l'; break;
                case 'ń': c = 'n'; break; case 'ó': c = 'o'; break; case 'ś': c = 's'; break; case 'ź': c = 'z'; break; case 'ż': c = 'z'; break;
            }
            int kind = c >= 'a' && c <= 'z' ? 1 : c >= '0' && c <= '9' ? 2 : 0;
            if (kind == 0) { if (prevKind != 0) sb.Append(' '); prevKind = 0; prevRaw = raw; continue; }
            bool camel = kind == 1 && prevKind == 1 && char.IsUpper(raw) && char.IsLower(prevRaw);
            if ((prevKind != 0 && prevKind != kind) || camel) sb.Append(' ');
            sb.Append(c); prevKind = kind; prevRaw = raw;
        }
        sb.Append(' ');
        var r = Regex.Replace(sb.ToString(), " +", " ");
        if (normCache.Count < 200000) normCache[s] = r;
        return r;
    }

    // ---------------- typy mebli i przedmiotów ----------------
    public enum Pomiar { Brak, Wysokosc, Dlugosc, Siedzisko, Blat }
    public sealed class Typ
    {
        public string nazwa; public Regex wzor, wyjatek;
        public Pomiar p1, p2; public float lo1, hi1, lo2, hi2;
        public bool raport, pochylony, wisi, siedzi, stol, lada, kosz;
        public float maxL = 4f, maxH = 3f;                 // największy sensowny rozmiar jednostki (m, wymiar rzeczywisty)
        public Typ M(Pomiar p, float lo, float hi) { if (p1 == Pomiar.Brak) { p1 = p; lo1 = lo; hi1 = hi; } else { p2 = p; lo2 = lo; hi2 = hi; } return this; }
        public Typ G(float l, float h) { maxL = l; maxH = h; return this; }
        public Typ R() { raport = true; return this; }        // tylko raport (bez skalowania / przesuwania)
        public Typ P() { pochylony = true; return this; }     // celowo pochylony — bez prostowania
        public Typ S() { siedzi = true; return this; }
        public Typ St() { stol = true; return this; }
        public Typ L() { lada = true; return this; }
        public Typ Ko() { kosz = true; return this; }
        public Typ Wi() { wisi = true; return this; }
        public bool Pasuje(string n) => wzor.IsMatch(n) && (wyjatek == null || !wyjatek.IsMatch(n));
    }
    static Typ T(string nazwa, string wzor, string wyjatek = null) => new Typ { nazwa = nazwa, wzor = new Regex(wzor), wyjatek = wyjatek == null ? null : new Regex(wyjatek) };
    const Pomiar W = Pomiar.Wysokosc, D = Pomiar.Dlugosc, Si = Pomiar.Siedzisko, Bl = Pomiar.Blat;

    // Kolejność ma znaczenie: bardziej szczegółowe przed ogólnymi (fotel przed krzesłem, stolik barowy przed stołem…).
    // Wymiary rzeczywiste (m) dla człowieka 1.80 m; kontrola mnoży je przez k = H / 1.80.
    public static readonly List<Typ> Typy = new List<Typ>
    {
        // zestaw (stół + krzesła jednym modelem, np. outdoor_table_chair_set_01): mierzony stół z zestawu, skalowany cały zestaw
        T("zestaw", @"\b(table\w*|stol\w*|chair\w*|krzesl\w*|bench\w*|lawk\w*)\b.*\b(set|zestaw\w*|komplet\w*)\b|\b(set|zestaw\w*|komplet\w*)\b.*\b(table\w*|stol\w*|chair\w*|krzesl\w*|bench\w*|lawk\w*)\b").M(W, .72f, .78f).M(Bl, .72f, .78f).St().G(6f, 3.2f),
        T("bieżnia", @"\b(bieznia\w*|biezni\w*|treadmill\w*)\b").M(W, 1.35f, 1.60f).G(3f, 2.2f),
        T("rower stacjonarny", @"\b(rower\w* (stacjonarn|treningow|spinning)\w*|rowerek|exercise bike|spin\w* bike|stationary bike|orbitrek\w*|elliptical|wioslarz\w*|rowing machine|ergometr\w*)\b").M(W, 1.05f, 1.45f).G(2.6f, 2f),
        T("sprzęt siłowni", @"\b(power rack|squat rack|klatk\w* (do )?przysiad\w*|bramownic\w*|smith|wyciag\w*|atlas|atlasy|crossover|lawk\w* (do )?(wyciskan|cwiczen|skosn|treningow)\w*|weight bench|bench press|flat bench|incline bench|hantl\w*|hantel|dumbbell\w*|kettlebell\w*|sztang\w*|barbell\w*|worek|worki|worka|punching bag|heavy bag|gruszk\w*|stojak\w* (na )?(hantl|talerz|sztang)\w*|plyo|sprzet\w* silown\w*|gym equipment)\b").R().G(7f, 3.5f),
        T("hoker", @"\b(hoker\w*|bar ?stool\w*|barstool\w*|stol(ek|ki|ka|kow) barow\w*|krzesl\w* barow\w*|bar chair\w*)\b").M(Si, .72f, .80f).M(W, .72f, 1.10f).S().G(1.2f, 1.6f),
        T("leżak", @"\b(lezak\w*|lounger\w*|sun ?bed\w*|sunlounger\w*|deck ?chair\w*|chaise\w*)\b").M(D, 1.90f, 2.00f).P().S().G(3f, 1.6f),
        T("pufa", @"\b(puf|pufa|pufy|pufk\w*|ottoman\w*|podnozek\w*|footstool\w*|pouf\w*)\b").M(W, .38f, .46f).S().G(1.6f, .9f),
        T("fotel", @"\b(fotel\w*|armchair\w*|arm chair\w*|lounge chair\w*|club chair\w*|recliner\w*|wingback\w*)\b").M(Si, .40f, .46f).M(W, .75f, 1.05f).S().G(1.8f, 1.8f),
        T("sofa", @"\b(sofa\w*|couch\w*|kanap\w*|settee\w*|loveseat\w*)\b").M(D, 2.00f, 2.40f).M(Si, .40f, .47f).S().G(4.5f, 1.8f),
        T("krzesło", @"\b(krzesl\w*|krzesel\w*|krzeselk\w*|chair|chairs|seat|seats|siedzisk\w*)\b", @"\b(sedzi\w*|umpire|lifeguard|ratownik\w*|trybun\w*|car|auto|fotel\w* kierow\w*|fotel\w* pasaz\w*)\b").M(Si, .45f, .45f).M(W, .85f, .95f).S().G(1.4f, 1.8f),
        T("stolik barowy", @"\b(bar ?table\w*|stolik\w* barow\w*|stol\w* barow\w*|high ?table\w*|cocktail table\w*|stolik\w* koktajl\w*)\b").M(Bl, 1.00f, 1.10f).M(W, 1.00f, 1.10f).St().G(2.5f, 1.8f),
        T("stolik kawowy", @"\b(coffee ?table\w*|stolik\w* kawow\w*|lawa|lawy|lawie|stolik\w* (boczn|pomocnicz)\w*|side ?table\w*|end table\w*)\b").M(Bl, .38f, .55f).M(W, .38f, .55f).St().G(2.4f, 1.2f),
        T("szafka nocna", @"\b(szafk\w* nocn\w*|nightstand\w*|night stand\w*|bedside\w*)\b", @"\blamp\w*\b").M(W, .50f, .65f).G(1.2f, 1.2f),
        T("stół do gry", @"\b(ruletk\w*|roulette\w*|blackjack\w*|poker\w*|stol\w* do gry|gaming table\w*|craps|bilard\w*|billiard\w*|pool table\w*|snooker\w*)\b").M(Bl, .74f, .90f).M(W, .74f, .95f).R().St().G(5f, 2f),
        T("lada", @"\b(lada|lady|ladzie|lade|lada\w*|counter|counters|kontuar\w*|recepcj\w*|reception|checkout|front desk|bar counter|kasa|kasy)\b", @"\b(countertop\w*|blat\w*)\b").M(W, 1.05f, 1.10f).R().L().G(16f, 2.6f),
        T("biurko", @"\b(biurk\w*|desk|desks)\b", @"\b(lamp\w*|lampk\w*|organizer\w*|pad|pads|notepad\w*)\b").M(Bl, .72f, .78f).M(W, .72f, .78f).St().G(3.5f, 1.8f),
        T("stół", @"\b(stol|stoly|stolu|stole|stolem|stolik|stoliki|stolika|stolikow|table|tables|diningtable\w*|picnic ?table\w*)\b", @"\b(tablet\w*|vegetable\w*|timetable\w*|turntable\w*|tablic\w*|tabel\w*|stolark\w*|tennis|pingpong|ping pong)\b").M(Bl, .72f, .78f).M(W, .72f, .78f).St().G(4.5f, 3.2f),
        T("łóżko", @"\b(lozk\w*|lozek|bed|beds|bedframe\w*)\b", @"\b(flower\w*|kwiat\w*|rabat\w*|garden|raised|sun ?bed\w*|sea ?bed\w*|river ?bed\w*|bedside\w*|prycz\w*)\b").M(D, 1.95f, 2.15f).S().G(3.2f, 2f),
        T("ławka", @"\b(lawk\w*|lawek|lawce|bench|benches|seating)\b", @"\b(workbench\w*|warsztat\w*|wyciskan\w*|bench press)\b").M(Si, .45f, .45f).M(D, 1.50f, 2.00f).S().G(6f, 1.6f),
        T("taboret", @"\b(taboret\w*|stolek|stolki|stolka|stool|stools)\b").M(W, .42f, .50f).S().G(1f, 1f),
        T("kontener na śmieci", @"\b(kontener\w*|wheelie bin\w*|dumpster\w*|pojemnik\w* (na|do) (smiec|segregac|odpad)\w*|container\w*)\b").M(W, 1.05f, 1.40f).G(3f, 2.2f),
        T("kosz na śmieci", @"\b(kosz|kosze|koszy|smietnik\w*|trash\w*|bin|bins|garbage\w*|litter\w*|waste\w*|dustbin\w*|wastebasket\w*|rubbish\w*)\b", @"\b(koszyk\w*|basket\w*|boisk\w*|obrecz\w*|hoop\w*|kosza|pilk\w*|tenis\w*)\b").M(W, .90f, 1.00f).Ko().G(1.5f, 1.8f),
        T("drzwi", @"\b(drzwi|door|doors|doorway)\b", @"\b(garaz\w*|garage|roller\w*|shutter\w*|rolet\w*|bram\w*|gate|indoor|outdoor|szafk\w*|szafa|szafy|cabinet\w*|wardrobe\w*|fridge|lodowk\w*|kolizj\w*)\b").M(W, 2.00f, 2.20f).R().G(4f, 3.5f),
        T("latarnia", @"\b(latarni\w*|latarn\w*|lamp ?post\w*|street ?lamp\w*|streetlight\w*|street light\w*|lamppost\w*|light ?pole\w*|park ?lamp\w*|slup\w* oswietl\w*)\b", @"\b(lantern\w*|klosz\w*|chandelier\w*|zyrandol\w*)\b").M(W, 3.50f, 6.00f).G(4f, 12f),
        T("hydrant", @"\b(hydrant\w*)\b").M(W, .65f, .90f).G(1f, 1.4f),
        T("rower", @"\b(rower|rowery|roweru|rowerow|bicycle\w*|bike|bikes)\b", @"\b(stacjonarn\w*|exercise|spin\w*|stationary|motor\w*|treningow\w*)\b").M(D, 1.65f, 1.85f).P().G(2.6f, 1.6f),
        T("parasol", @"\b(parasol\w*|umbrella\w*)\b").M(W, 2.20f, 2.60f).P().G(4.5f, 3.6f),
        T("słupek", @"\b(slupek|slupki|slupkow|bollard\w*|pacholek|pacholki|stanchion\w*)\b", @"\b(ganku|ringu|plotu|ogrodzen\w*|bramk\w*|szlaban\w*)\b").M(W, .80f, 1.00f).G(1f, 1.4f),
    };

    // całe poddrzewa pomijane (ludzie, auta, gracz)
    static readonly Regex Ludzie = new Regex(@"\b(czlowiek|ludzie|osoba|pieszy|piesi|przechod\w*|npc|person|people|human|pedestrian|rocketbox|player|gracz|hero|bohater|brax original|patryk|kierowca|driver|krupier\w*|kasjer\w*|barman\w*|ochroniarz\w*|policjant\w*|lekarz\w*|sprzedaw\w*|trener\w*|zawodni\w*|kibic\w*|manekin\w*|mannequin\w*)\b");
    static readonly Regex Pojazdy = new Regex(@"\b(auto|auta|aut|samochod\w*|car|cars|vehicle\w*|pojazd\w*|truck\w*|ciezarow\w*|bus|autobus\w*|van|taxi|taksowk\w*|radiowoz\w*|karetk\w*|ambulance|motocykl\w*|motorbike\w*|motorcycle\w*|skuter\w*|scooter\w*|jacht\w*|yacht\w*|lodz|lodzie|lodka|boat\w*|ponton\w*|smieciark\w*)\b");
    // przedmioty ogólne — nie z tych (budowle, nawierzchnie, rośliny, rzeczy miękkie / świetlne)
    public static readonly Regex NieMebel = new Regex(@"\b(budynek|budynki|building\w*|house\w*|dom|domy|willa|villa|kamienic\w*|blok|garaz\w*|garage|elewac\w*|facade|sciana|sciany|wall|walls|dach\w*|roof\w*|podlog\w*|floor\w*|sufit\w*|ceiling\w*|strop\w*|schod\w*|stairs|stair|stopn\w*|step|steps|ramp\w*|plot|ploty|fence\w*|ogrodzen\w*|brama|gate|tunel\w*|teren\w*|terrain|ground|droga|road\w*|ulica|street|chodnik\w*|sidewalk\w*|jezdni\w*|asfalt\w*|parking\w*|trawa|traw\w*|grass\w*|trawnik\w*|kwiat\w*|flower\w*|roz[ay]|platk\w*|petal\w*|drzew\w*|tree\w*|krzew\w*|bush\w*|shrub\w*|lisc\w*|leaf|leaves|foliage|koron\w*|crown\w*|igliw\w*|kep\w*|tuft\w*|dywan\w*|rug|rugs|carpet\w*|wykladzin\w*|mata|maty|zaslon\w*|curtain\w*|firan\w*|laser\w*|beam\w*|promien\w*|swiatl\w*|light|lights|woda|water|mgla|fog|cien|shadow\w*|decal\w*|naklejk\w*|mural\w*|hologram\w*|niebo|sky|chmur\w*|cloud\w*|sonda|kolizj\w*|collider\w*)\b");
    static readonly Regex Chronione = new Regex(@"\b(kopia oryginalu|original gym|braxhorn original gym)\b");
    static readonly Dictionary<Transform, bool> chronCache = new Dictionary<Transform, bool>();
    // zatwierdzona kopia siłowni (approvedGymUnchanged) — kontrole tylko raportują, nic w niej nie ruszają
    public static bool WChronionym(Transform t)
    {
        if (!t) return false;
        if (chronCache.TryGetValue(t, out var c)) return c;
        c = Chronione.IsMatch(Norm(t.name)) || WChronionym(t.parent) || WStrefie(t);
        chronCache[t] = c; return c;
    }
    // MIASTO (2.11): zatwierdzona siłownia to korzeń „braxhorn-original-gym” + osobne obiekty obok (sofa z biblioteki, worki,
    // wykończenie ringu, mural…) — wszystko, czego renderer leży w bryle siłowni, jest nietykalne (tylko raport).
    public static bool JestStrefa; public static Bounds StrefaChroniona;
    static bool WStrefie(Transform t)
    {
        if (!JestStrefa) return false;
        var r = t.GetComponent<Renderer>();
        return r && StrefaChroniona.Contains(r.bounds.center);
    }
    static void UstawStrefe(Scene scena)
    {
        JestStrefa = false;
        foreach (var root in scena.GetRootGameObjects())
        {
            if (root.name != "braxhorn-original-gym") continue;
            bool any = false; Bounds b = default;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true)) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            if (!any) continue;
            b.Expand(1f); StrefaChroniona = b; JestStrefa = true;
            Debug.Log("BRAXHORN_KONTROLA strefa chroniona (zatwierdzona siłownia): " + F(b.min) + " … " + F(b.max));
        }
    }
    public static readonly Regex Roslina = new Regex(@"\b(lisc\w*|leaf|leaves|foliage|koron\w*|crown\w*|igliw\w*|needle\w*|grass\w*|traw\w*|kep\w*|tuft\w*|flower\w*|kwiat\w*|roz[ay]|platk\w*|petal\w*|bloom\w*|blooms|krzew\w*|bush\w*|shrub\w*|hedge\w*|zywoplot\w*|plant|plants|rosl\w*)\b");

    static readonly Dictionary<string, Typ> typCache = new Dictionary<string, Typ>();
    static readonly Typ Zaden = new Typ { nazwa = "—" };
    public static Typ Rozpoznaj(string tekst)
    {
        if (typCache.TryGetValue(tekst, out var t)) return t == Zaden ? null : t;
        Typ wynik = null;
        foreach (var ty in Typy) if (ty.Pasuje(tekst)) { wynik = ty; break; }
        if (typCache.Count < 100000) typCache[tekst] = wynik ?? Zaden;
        return wynik;
    }

    // nazwy oryginalne siatek z manifestów (EstateMesh_N → „Chair”, „Bed” z Codexa) — rozpoznanie także w imporcie glTF
    static Dictionary<string, string> oryginalne;
    public static string Oryginalna(string nazwa)
    {
        if (oryginalne == null)
        {
            oryginalne = new Dictionary<string, string>();
            try
            {
                if (Directory.Exists("Assets/Source/City"))
                    foreach (var f in Directory.GetFiles("Assets/Source/City", "*-manifest.json"))
                    {
                        var m = JsonUtility.FromJson<BraxCityBuild.Manifest>(File.ReadAllText(f));
                        if (m?.meshes != null) foreach (var me in m.meshes) if (!string.IsNullOrEmpty(me.name) && !string.IsNullOrEmpty(me.originalName)) oryginalne[me.name] = me.originalName;
                    }
            }
            catch (Exception e) { Debug.LogWarning("DO POPRAWKI: kontrola — manifesty nazw: " + e.Message); }
        }
        return oryginalne.TryGetValue(nazwa, out var o) ? o : "";
    }

    // ---------------- siatki i renderery ----------------
    static readonly Dictionary<Mesh, (Vector3[] v, int[] t)> siatki = new Dictionary<Mesh, (Vector3[], int[])>();
    public static bool Dane(Mesh m, out Vector3[] v, out int[] t)
    {
        v = null; t = null;
        if (!m) return false;
        if (siatki.TryGetValue(m, out var d)) { v = d.v; t = d.t; return v != null; }
        try { if (m.vertexCount > 0 && m.vertexCount < 400000) { v = m.vertices; t = m.triangles; } } catch { v = null; t = null; }
        if (v != null && (t == null || t.Length < 3)) v = null;
        siatki[m] = (v, t);
        return v != null;
    }
    static readonly Dictionary<Mesh, bool> scalone = new Dictionary<Mesh, bool>();
    // siatka scalona przez generator (wiele rzeczy naraz) — nie jest jednym przedmiotem
    public static bool Scalona(Mesh m)
    {
        if (!m) return false;
        if (scalone.TryGetValue(m, out var s)) return s;
        string n = m.name ?? "";
        s = n.StartsWith("Osiedle ") || n.StartsWith("Kępy ") || n.StartsWith("Kepy ") || n.StartsWith("scal") || n.StartsWith("Combined");
        if (!s) { var p = AssetDatabase.GetAssetPath(m); s = !string.IsNullOrEmpty(p) && (p.Contains("/scal_") || p.Contains("/mesh_") || p.Contains("/kepy_")); }
        scalone[m] = s; return s;
    }
    static readonly Dictionary<Material, bool> roslMat = new Dictionary<Material, bool>();
    // trawa, kwiaty, liście, kępy: materiał z przycinaniem alfy albo shader roślin
    public static bool Roslina2(Renderer r)
    {
        foreach (var m in r.sharedMaterials)
        {
            if (!m) continue;
            if (!roslMat.TryGetValue(m, out var y))
            {
                string sh = m.shader ? m.shader.name : "";
                y = (m.renderQueue >= 2450 && m.renderQueue < 2900) || sh.Contains("Liscie") || sh.Contains("Kep") || sh.Contains("Trawa") || sh.Contains("Leaf") || sh.Contains("Foliage") || sh.Contains("Grass") || sh.Contains("Tree") || Roslina.IsMatch(Norm(m.name));
                roslMat[m] = y;
            }
            if (y) return true;
        }
        return Roslina.IsMatch(Norm(r.name));
    }
    public static bool Widoczny(Renderer r) => r && r.enabled && r.gameObject.activeInHierarchy && r.gameObject.layer != WarstwaSondy && r.gameObject.layer != 8;

    // Prostokąt o najmniejszym polu w poziomie (obrót wokół pionu) — długość/szerokość mebla niezależnie od obrotu.
    public static void Prostokat(List<Vector3> pkt, out float kat, out Vector2 srodek, out float dl, out float sz)
    {
        kat = 0; srodek = Vector2.zero; dl = 0; sz = 0;
        if (pkt.Count == 0) return;
        float best = float.MaxValue, bk = 0;
        for (int pass = 0; pass < 2; pass++)
        {
            float a0 = pass == 0 ? 0 : bk - 3f, a1 = pass == 0 ? 87f : bk + 3f, st = pass == 0 ? 3f : .5f;
            for (float a = a0; a <= a1 + .001f; a += st)
            {
                float c = Mathf.Cos(a * Mathf.Deg2Rad), s = Mathf.Sin(a * Mathf.Deg2Rad);
                float u0 = float.MaxValue, u1 = float.MinValue, v0 = float.MaxValue, v1 = float.MinValue;
                foreach (var p in pkt) { float u = p.x * c + p.z * s, v = -p.x * s + p.z * c; if (u < u0) u0 = u; if (u > u1) u1 = u; if (v < v0) v0 = v; if (v > v1) v1 = v; }
                float pole = (u1 - u0) * (v1 - v0);
                if (pole < best - 1e-6f) { best = pole; bk = a; }
            }
        }
        {
            float c = Mathf.Cos(bk * Mathf.Deg2Rad), s = Mathf.Sin(bk * Mathf.Deg2Rad);
            float u0 = float.MaxValue, u1 = float.MinValue, v0 = float.MaxValue, v1 = float.MinValue;
            foreach (var p in pkt) { float u = p.x * c + p.z * s, v = -p.x * s + p.z * c; if (u < u0) u0 = u; if (u > u1) u1 = u; if (v < v0) v0 = v; if (v > v1) v1 = v; }
            float um = (u0 + u1) * .5f, vm = (v0 + v1) * .5f;
            srodek = new Vector2(um * c - vm * s, um * s + vm * c);
            // kąt jako obrót Unity wokół osi Y: oś u = (cos, sin) w (x, z) → yaw = −kąt
            kat = -bk; dl = u1 - u0; sz = v1 - v0;
            if (sz > dl) { var t = dl; dl = sz; sz = t; kat += 90f; }   // dl = dłuższy bok, wzdłuż lokalnego x obrotu Euler(0, kat, 0)
        }
    }
    public static void Narozniki(Renderer r, List<Vector3> dst)
    {
        Bounds lb; Matrix4x4 m;
        var mf = r.GetComponent<MeshFilter>();
        if (mf && mf.sharedMesh) { lb = mf.sharedMesh.bounds; m = r.transform.localToWorldMatrix; }
        else { var b = r.bounds; dst.Add(b.min); dst.Add(b.max); dst.Add(new Vector3(b.min.x, b.min.y, b.max.z)); dst.Add(new Vector3(b.max.x, b.min.y, b.min.z)); return; }
        for (int i = 0; i < 8; i++)
            dst.Add(m.MultiplyPoint3x4(new Vector3((i & 1) == 0 ? lb.min.x : lb.max.x, (i & 2) == 0 ? lb.min.y : lb.max.y, (i & 4) == 0 ? lb.min.z : lb.max.z)));
    }
    public static string F(Vector3 p) => "(" + p.x.ToString("F1") + ", " + p.y.ToString("F2") + ", " + p.z.ToString("F1") + ")";
    public static string Sciezka(Transform t)
    {
        var s = t.name; var p = t.parent; int n = 0;
        while (p && n < 2) { s = p.name + "/" + s; p = p.parent; n++; }
        return s;
    }
    // ---------------- spis jednostek ----------------
    public sealed class Jednostka
    {
        public Transform t; public Typ typ; public string tekst, opis;
        public List<Renderer> rs = new List<Renderer>();
        public Bounds b;
        public HashSet<Collider> kol = new HashSet<Collider>();          // własne + dołączone
        public List<Collider> dolaczone = new List<Collider>();          // osobne kolizje z generatora (np. „kolizja” w grupie kolizje)
        public List<Transform> pasazerowie;                               // przedmioty leżące na meblu (wazon na stole)
        public bool chroniony, wewnatrz, zmieniony;
        public float kat, dl, sz; public Vector2 srodekXZ;
        public Vector3 Podstawa => new Vector3(b.center.x, b.min.y, b.center.z);
        public void Odswiez()
        {
            bool any = false; Bounds nb = default;
            foreach (var r in rs) if (r) { if (!any) { nb = r.bounds; any = true; } else nb.Encapsulate(r.bounds); }
            if (any) b = nb;
            var pts = new List<Vector3>(rs.Count * 8); foreach (var r in rs) if (r) Narozniki(r, pts);
            Prostokat(pts, out kat, out srodekXZ, out dl, out sz);
        }
        // ruch sztywny: mebel + jego osobne kolizje + rzeczy na nim
        public void Przesun(Vector3 d)
        {
            t.position += d;
            foreach (var c in dolaczone) if (c) c.transform.position += d;
            if (pasazerowie != null) foreach (var p in pasazerowie) if (p) p.position += d;
            Siedziska(s => s.surfaceY += d.y);
            zmieniony = true;
        }
        public void Skaluj(float f, Vector3 piwot)
        {
            t.localScale *= f;
            Odswiez(); var d = piwot - Podstawa; t.position += d;          // podstawa zostaje w miejscu
            foreach (var c in dolaczone)
            {
                if (!c) continue;
                c.transform.position = piwot + (c.transform.position - piwot) * f;
                if (c is BoxCollider bc) { bc.size *= f; bc.center *= f; }
                else if (c is CapsuleCollider cc) { cc.radius *= f; cc.height *= f; cc.center *= f; }
                else if (c is SphereCollider sc) { sc.radius *= f; sc.center *= f; }
            }
            if (pasazerowie != null) foreach (var p in pasazerowie) if (p) p.position = piwot + (p.position - piwot) * f;
            Siedziska(s => s.surfaceY = piwot.y + (s.surfaceY - piwot.y) * f);
            var lod = t.GetComponent<LODGroup>(); if (lod) lod.RecalculateBounds();
            Odswiez(); zmieniony = true;
        }
        public void Obroc(Quaternion q, Vector3 piwot)
        {
            t.position = piwot + q * (t.position - piwot); t.rotation = q * t.rotation;
            foreach (var c in dolaczone) if (c) { c.transform.position = piwot + q * (c.transform.position - piwot); c.transform.rotation = q * c.transform.rotation; }
            if (pasazerowie != null) foreach (var p in pasazerowie) if (p) { p.position = piwot + q * (p.position - piwot); p.rotation = q * p.rotation; }
            Odswiez(); zmieniony = true;
        }
        void Siedziska(Action<BraxSeat> a)
        {
            foreach (var s in t.GetComponentsInChildren<BraxSeat>(true)) a(s);
            if (pasazerowie != null) foreach (var p in pasazerowie) if (p) { var s = p.GetComponent<BraxSeat>(); if (s && !s.transform.IsChildOf(t)) a(s); }
        }
    }

    public sealed class Spis
    {
        public readonly List<Jednostka> jednostki = new List<Jednostka>();
        public readonly Dictionary<Transform, Jednostka> poTransform = new Dictionary<Transform, Jednostka>();
        public readonly Dictionary<Collider, Jednostka> poKolizji = new Dictionary<Collider, Jednostka>();
        public int pominieteLudzie, pominieteAuta, scaloneSiatki;
        public readonly HashSet<Transform> drzwiSkrzydla = new HashSet<Transform>();

        public static Spis Zbuduj(Scene scena)
        {
            var s = new Spis();
            foreach (var d in Object.FindObjectsByType<BraxCityDoor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (d.leaves != null) foreach (var l in d.leaves) if (l) s.drzwiSkrzydla.Add(l);
            foreach (var root in scena.GetRootGameObjects()) s.Odwiedz(root.transform, false);
            // osobne kolizje generatora (GameObject = Transform + kolizja) dopasowane do mebla: środek w obrysie, pokrycie ≥ 50 %
            var siatka = new Dictionary<long, List<Collider>>();
            foreach (var root in scena.GetRootGameObjects())
                foreach (var c in root.GetComponentsInChildren<Collider>(false))
                {
                    if (!c.enabled || c.isTrigger || c.gameObject.layer == WarstwaSondy || c.gameObject.layer == WarstwaKamery) continue;
                    if (c.GetComponents<Component>().Length != 2 || c.transform.childCount > 0) continue;
                    if (c is MeshCollider || c is TerrainCollider) continue;
                    var cb = c.bounds; if (cb.size.x > 20f || cb.size.z > 20f) continue;
                    long k = Klucz(cb.center, 2f); if (!siatka.TryGetValue(k, out var l)) siatka[k] = l = new List<Collider>(); l.Add(c);
                }
            var wziete = new HashSet<Collider>();
            foreach (var j in s.jednostki)
            {
                foreach (var c in j.t.GetComponentsInChildren<Collider>(true)) if (c.gameObject.layer != WarstwaSondy) { j.kol.Add(c); s.poKolizji[c] = j; }
                int cx = Mathf.FloorToInt(j.b.center.x / 2f), cz = Mathf.FloorToInt(j.b.center.z / 2f);
                var e = j.b; e.Expand(new Vector3(.1f + j.b.size.x * .15f, .1f + j.b.size.y * .15f, .1f + j.b.size.z * .15f));
                float pole = Mathf.Max(.01f, j.b.size.x * j.b.size.z);
                int zasieg = Mathf.CeilToInt(Mathf.Max(j.b.size.x, j.b.size.z) / 4f) + 1;
                for (int x = cx - zasieg; x <= cx + zasieg; x++) for (int z = cz - zasieg; z <= cz + zasieg; z++)
                    if (siatka.TryGetValue(((long)x << 32) ^ (uint)z, out var l))
                        foreach (var c in l)
                        {
                            if (wziete.Contains(c)) continue;
                            var cb = c.bounds;
                            if (!j.b.Contains(new Vector3(cb.center.x, Mathf.Clamp(cb.center.y, j.b.min.y, j.b.max.y), cb.center.z))) continue;
                            if (!e.Contains(cb.min) || !e.Contains(cb.max)) continue;
                            float ix = Mathf.Min(cb.max.x, j.b.max.x) - Mathf.Max(cb.min.x, j.b.min.x), iz = Mathf.Min(cb.max.z, j.b.max.z) - Mathf.Max(cb.min.z, j.b.min.z);
                            if (ix <= 0 || iz <= 0 || ix * iz < pole * .5f) continue;
                            wziete.Add(c); j.dolaczone.Add(c); j.kol.Add(c); s.poKolizji[c] = j;
                        }
            }
            // rzeczy na meblach (wazon na stole, poduszka na sofie, punkt siedzenia) — jadą razem z meblem
            var ind = new Dictionary<long, List<Jednostka>>();
            foreach (var j in s.jednostki) { long k = Klucz(j.b.center, 4f); if (!ind.TryGetValue(k, out var l)) ind[k] = l = new List<Jednostka>(); l.Add(j); }
            var siedziska = Object.FindObjectsByType<BraxSeat>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var j in s.jednostki)
            {
                if (j.typ == null) continue;
                float poleJ = j.b.size.x * j.b.size.z;
                int cx = Mathf.FloorToInt(j.b.center.x / 4f), cz = Mathf.FloorToInt(j.b.center.z / 4f), zs = Mathf.CeilToInt(Mathf.Max(j.b.size.x, j.b.size.z) / 8f) + 1;
                for (int x = cx - zs; x <= cx + zs; x++) for (int z = cz - zs; z <= cz + zs; z++)
                    if (ind.TryGetValue(((long)x << 32) ^ (uint)z, out var l))
                        foreach (var o in l)
                        {
                            if (o == j || o.t.IsChildOf(j.t) || j.t.IsChildOf(o.t)) continue;
                            var p = o.Podstawa;
                            if (p.x < j.b.min.x || p.x > j.b.max.x || p.z < j.b.min.z || p.z > j.b.max.z) continue;
                            if (p.y < j.b.min.y + .08f || p.y > j.b.max.y + .03f) continue;
                            if (o.b.size.x * o.b.size.z > poleJ * .6f) continue;
                            (j.pasazerowie ??= new List<Transform>()).Add(o.t);
                        }
                foreach (var st in siedziska)
                {
                    if (!st || st.transform.IsChildOf(j.t)) continue;
                    var p = st.transform.position;
                    if (p.x >= j.b.min.x && p.x <= j.b.max.x && p.z >= j.b.min.z && p.z <= j.b.max.z && p.y >= j.b.min.y - .05f && p.y <= j.b.max.y + .2f)
                        (j.pasazerowie ??= new List<Transform>()).Add(st.transform);
                }
            }
            Debug.Log("BRAXHORN_KONTROLA spis " + Obszar + ": jednostek " + s.jednostki.Count + ", rozpoznanych mebli/przedmiotów " + s.jednostki.Count(j => j.typ != null) +
                      " (" + string.Join(", ", s.jednostki.Where(j => j.typ != null).GroupBy(j => j.typ.nazwa).OrderByDescending(g => g.Count()).Select(g => g.Key + " " + g.Count())) + ")" +
                      ", pominięte poddrzewa: ludzie " + s.pominieteLudzie + ", auta " + s.pominieteAuta + ", siatki scalone " + s.scaloneSiatki +
                      ", z kolizją dołączoną " + s.jednostki.Count(j => j.dolaczone.Count > 0));
            return s;
        }

        static bool Kontener(Transform t)
        {
            string n = t.name;
            return n.Contains(" / ") || n.StartsWith("CLAUDE ") || n.StartsWith("BRAXHORN ") || n.StartsWith("Original ") || n == "kolizje" || n.StartsWith("Osiedle ") || n.StartsWith("Park ") || n.StartsWith("Ruch") || t.parent == null && t.childCount > 20;
        }

        void Odwiedz(Transform t, bool chroniony)
        {
            var go = t.gameObject;
            if (!go.activeInHierarchy || go.layer == 8 || go.layer == WarstwaSondy) return;
            if (t.GetComponent<BraxGymPlayer>() || t.GetComponent<Camera>() || t.GetComponent<Rigidbody>() || t.GetComponent<BraxAuto>() || t.GetComponent<BraxPieszy>() || t.GetComponent<Animator>() || t.GetComponent<Animation>() || t.GetComponent<BraxCityDoor>() || drzwiSkrzydla.Contains(t)) return;
            if (t.GetComponent<SkinnedMeshRenderer>()) { pominieteLudzie++; return; }
            string nn = Norm(t.name);
            if (Ludzie.IsMatch(nn)) { pominieteLudzie++; return; }
            if (Pojazdy.IsMatch(nn)) { pominieteAuta++; return; }
            if (!chroniony && Chronione.IsMatch(nn)) chroniony = true;
            if (Kontener(t)) { for (int i = 0; i < t.childCount; i++) Odwiedz(t.GetChild(i), chroniony); return; }

            var mf = t.GetComponent<MeshFilter>(); var mesh = mf ? mf.sharedMesh : null;
            if (mesh && Scalona(mesh)) { scaloneSiatki++; for (int i = 0; i < t.childCount; i++) Odwiedz(t.GetChild(i), chroniony); return; }
            string tekst = nn + Norm(mesh ? mesh.name : "") + Norm(Oryginalna(t.name));
            var typ = Rozpoznaj(tekst);
            var mr = t.GetComponent<MeshRenderer>();
            if (typ == null && mr && t.childCount == 0) { var m0 = mr.sharedMaterial; if (m0) { typ = Rozpoznaj(Norm(m0.name)); if (typ != null) tekst += Norm(m0.name); } }

            var rs = new List<Renderer>();
            bool skinned = false, scalonaPod = false;
            foreach (var r in t.GetComponentsInChildren<Renderer>(false))
            {
                if (r is SkinnedMeshRenderer) { skinned = true; break; }
                if (!(r is MeshRenderer) || !Widoczny(r)) continue;
                var f = r.GetComponent<MeshFilter>(); if (!f || !f.sharedMesh) continue;
                if (Scalona(f.sharedMesh)) { scalonaPod = true; continue; }
                rs.Add(r);
            }
            if (skinned) { pominieteLudzie++; return; }
            if (rs.Count == 0) { for (int i = 0; i < t.childCount; i++) Odwiedz(t.GetChild(i), chroniony); return; }
            Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            float kk = Mathf.Max(1f, K) * 1.6f;
            if (typ != null && !scalonaPod && Mathf.Max(b.size.x, b.size.z) <= typ.maxL * kk && b.size.y <= typ.maxH * kk && b.size.y > .02f)
            { Dodaj(t, typ, tekst, rs, b, chroniony); return; }
            // przedmiot ogólny: model z LODGroup, pojedyncza siatka bez dzieci albo węzeł glTF z kilkoma siatkami-liśćmi
            bool lod = t.GetComponent<LODGroup>(), lisc = mr && t.childCount == 0, wezel = !mr && t.childCount > 0 && t.childCount <= 8 && rs.Count == t.childCount;
            if (wezel) for (int i = 0; i < t.childCount; i++) if (t.GetChild(i).childCount > 0) wezel = false;
            if (!scalonaPod && (lod || lisc || wezel) && Mathf.Max(b.size.x, b.size.z) <= 4.5f && b.size.y <= 4.5f && Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)) > .05f
                && !NieMebel.IsMatch(tekst) && !rs.All(Roslina2))
            { Dodaj(t, null, tekst, rs, b, chroniony); return; }
            for (int i = 0; i < t.childCount; i++) Odwiedz(t.GetChild(i), chroniony);
        }

        void Dodaj(Transform t, Typ typ, string tekst, List<Renderer> rs, Bounds b, bool chroniony)
        {
            if (JestStrefa && StrefaChroniona.Contains(b.center)) chroniony = true;
            var j = new Jednostka { t = t, typ = typ, tekst = tekst, rs = rs, b = b, chroniony = chroniony, opis = Sciezka(t) };
            j.Odswiez();
            jednostki.Add(j); poTransform[t] = j;
        }
    }
    public static long Klucz(Vector3 p, float komorka) => ((long)Mathf.FloorToInt(p.x / komorka) << 32) ^ (uint)Mathf.FloorToInt(p.z / komorka);

    // ---------------- sondy: widoczne siatki jako kolizje na czas kontroli ----------------
    public static class Sondy
    {
        static readonly List<GameObject> obiekty = new List<GameObject>();
        public static readonly Dictionary<Collider, Renderer> renderer = new Dictionary<Collider, Renderer>();
        public static readonly Dictionary<Renderer, Collider> sonda = new Dictionary<Renderer, Collider>();
        public static int Liczba => obiekty.Count;

        public static void Zbuduj(Scene scena)
        {
            Usun();
            int pominieteRosliny = 0, duze = 0; long tri = 0;
            foreach (var root in scena.GetRootGameObjects())
                foreach (var r in root.GetComponentsInChildren<MeshRenderer>(false))
                {
                    if (!Widoczny(r) || r.gameObject.layer == WarstwaKamery) continue;
                    var mf = r.GetComponent<MeshFilter>(); var m = mf ? mf.sharedMesh : null;
                    if (!m || m.vertexCount < 3) continue;
                    bool troj = true; for (int s = 0; s < m.subMeshCount; s++) if (m.GetTopology(s) != MeshTopology.Triangles) troj = false;
                    if (!troj || r.GetComponentInParent<BraxGymPlayer>()) continue;
                    if (Roslina2(r)) { pominieteRosliny++; continue; }
                    long n = 0; for (int s = 0; s < m.subMeshCount; s++) n += m.GetIndexCount(s) / 3;
                    if (n > 700000) { duze++; continue; }
                    var b = r.bounds; if (b.size.sqrMagnitude < 1e-6f || Mathf.Abs(b.center.y) > 400f) continue;
                    var g = new GameObject("BRAXHORN sonda kontroli") { layer = WarstwaSondy, hideFlags = HideFlags.DontSave };
                    g.transform.SetParent(r.transform, false);
                    try
                    {
                        var mc = g.AddComponent<MeshCollider>(); mc.sharedMesh = m;
                        obiekty.Add(g); renderer[mc] = r; sonda[r] = mc; tri += n;
                    }
                    catch { Object.DestroyImmediate(g); }
                }
            Physics.SyncTransforms();
            Debug.Log("BRAXHORN_KONTROLA sondy " + Obszar + ": " + obiekty.Count + " siatek (" + (tri / 1000) + " tys. trójkątów), pominięte rośliny " + pominieteRosliny + ", za duże " + duze + ", czas " + Sekundy.ToString("F1") + " s");
        }
        public static void Usun()
        {
            foreach (var g in obiekty) if (g) Object.DestroyImmediate(g);
            obiekty.Clear(); renderer.Clear(); sonda.Clear();
            Physics.SyncTransforms();
        }
        public static Renderer Czyj(Collider c) => c != null && renderer.TryGetValue(c, out var r) ? r : null;
    }

    // ---------------- promienie pomocnicze ----------------
    static readonly RaycastHit[] bufor = new RaycastHit[64];
    // pierwsze trafienie promienia, pomijając kolizje i sondy danej jednostki
    public static bool Promien(Vector3 z, Vector3 kier, float dl, int maska, Jednostka pomin, out RaycastHit hit)
    {
        hit = default;
        int n = Physics.RaycastNonAlloc(z, kier, bufor, dl, maska, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue; bool ok = false;
        for (int i = 0; i < n; i++)
        {
            var h = bufor[i];
            if (pomin != null)
            {
                if (pomin.kol.Contains(h.collider)) continue;
                var r = Sondy.Czyj(h.collider); if (r && (pomin.rs.Contains(r) || r.transform.IsChildOf(pomin.t))) continue;
                if (h.collider.transform.IsChildOf(pomin.t)) continue;
            }
            if (h.distance < best) { best = h.distance; hit = h; ok = true; }
        }
        return ok;
    }
}
