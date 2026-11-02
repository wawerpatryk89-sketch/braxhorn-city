using UnityEngine;

// BRAXHORN CITY — PROSTOKĄT GRYWALNY MIASTA w JEDNYM miejscu (wykonawca „horyzont”, 2.11).
// Wszystko, co jest za granicą gry, liczy się OD TEGO prostokąta: teren dookoła (pola, wzgórza, dolina, las, droga),
// granica z kolizją (balustrada nad skarpą, ogrodzenie lasu, bariery drogowe), panorama 360° i niebo (środek walca
// panoramy = środek prostokąta), przedłużenie wiaduktu. Miasto się powiększy (galeria, dzielnica) — wtedy zmienia się
// TYLKO te cztery liczby (X0, X1, Z0, Z1) i ewentualnie linia brzegu; reszta przelicza się sama przy buildzie.
// Układ Unity miasta: x na wschód (morze od x ≈ 84), z na północ (plaża, las), y w górę; ulica główna z ≈ 24…33.
// Klasa statyczna (nie MonoBehaviour) — dostępna z kodu edytora i z gry.
public static class BraxMiastoGranice
{
    // ---- prostokąt grywalny (ląd + akwen zatoki) — STAN BAZY 4a02329 (bez galerii i dzielnicy) ----
    // Po scaleniu galerii i dzielnicy: X0 = −296, wycięcie NW = (X0, 121) … (−60, Z1) — patrz scratchpad/plan/GRANICE.md.
    public const float X0 = -135f;   // zachód: za starą wieżą 191 (x −125…−111); ulica główna kończy się dziś na −104
    public const float X1 = 170f;    // wschód: za cyplem plaży (do 150) i mariną (do 166); dalej otwarte morze (wyspa = obszar nabrzeża)
    public const float Z0 = -74f;    // południe: koniec posadzki tyłów i portal tunelu wiaduktu (lico z = −74); dalej wzgórze „tyłów”
    public const float Z1 = 180f;    // północ: za lasem za plażą (z 160…174) i łąką

    // ---- wycięcia: części prostokąta, które NIE są grywalne (teren i las wchodzą do środka, granica idzie dookoła) ----
    // (x0, z0, szerokość, długość). Dziś: las na północny zachód od wiaduktu (za ulicą galerii z 111.6…118.4).
    public static readonly Rect[] Wyciecia = { Rect.MinMaxRect(X0, 121f, -60f, Z1) };

    // ---- wzgórze z tunelem wykonawcy „tyły” (jego siatka i kolizja; mój teren pod nim leży niżej i nie wystaje) ----
    public static readonly Rect WzgorzeTylow = Rect.MinMaxRect(-112f, -126f, -20f, -74f);

    // ---- linia brzegu (ląd na zachód od niej, morze na wschód) ----
    // Nabrzeże portu i promenada x = 84 (woda miasta od x = 84), cypel plaży i łąka do x = 150 od z ≈ 112.
    // Daleko na północy i południu brzeg odchodzi łukiem na wschód: miasto leży w zatoce, z gór na lądzie
    // schodzą do morza dwa cyple (widać je z tarasu Tower i z plaży), na wprost (wschód) otwarte morze.
    public const float BrzegPoludnie = 84f;      // x brzegu na południe od cypla
    public const float BrzegPolnoc = 150f;       // x brzegu na północ od cypla
    public const float BrzegCypelZ0 = 100f, BrzegCypelZ1 = 118f;   // przejście 84 → 150 (cypel plaży)
    public const float BrzegLukPolnocZ = 460f;   // od tego z na północ brzeg odchodzi na wschód (za końcem wody miasta)
    public const float BrzegLukPoludnieZ = -340f;// od tego z na południe brzeg odchodzi na wschód
    public const float BrzegLukNachylenie = .9f; // ile metrów na wschód na 1 m dalej na północ / południe

    public const float PoziomMorza = -1.7f;      // średni poziom wody miasta (BraxCityWaterBuild.WaterY)

    // ---- wiadukt (oś z oryginału; wykonawca „tyły” robi część grywalną, „horyzont” dalszy ciąg za granicą) ----
    // (scratchpad/plan/WIADUKT.md: oś x = −66.2, lica −70.95…−61.45, jezdnia y 8.16, filary co 12 m; część „tyłów” kończy
    //  się filarem z = 123 — dalej na północ przęsła za granicą stawia „horyzont” tym samym przekrojem)
    public const float WiaduktX = -66.2f;        // oś jezdni wiaduktu
    public const float WiaduktPomostY = 8.16f;   // wierzch jezdni pomostu
    public const float WiaduktSzerokosc = 9.5f;  // szerokość pomostu (lica)
    public const float WiaduktKoniecTylow = 123f;// ostatni filar części grywalnej (z)
    public const float WiaduktPrzeslo = 12f;     // rozstaw filarów

    public static float SrodekX => (X0 + X1) * .5f;
    public static float SrodekZ => (Z0 + Z1) * .5f;
    public static Rect Prostokat => Rect.MinMaxRect(X0, Z0, X1, Z1);

    /// <summary>x linii brzegu dla danego z (ląd dla x &lt; Brzeg(z)).</summary>
    public static float Brzeg(float z)
    {
        float t = Mathf.Clamp01((z - BrzegCypelZ0) / (BrzegCypelZ1 - BrzegCypelZ0));
        t = t * t * (3f - 2f * t);
        float x = Mathf.Lerp(BrzegPoludnie, BrzegPolnoc, t);
        x += (Mathf.Max(0f, z - BrzegLukPolnocZ) + Mathf.Max(0f, BrzegLukPoludnieZ - z)) * BrzegLukNachylenie;
        return x;
    }

    /// <summary>Czy punkt (x, z) leży na lądzie (na zachód od linii brzegu).</summary>
    public static bool Lad(float x, float z) => x < Brzeg(z);

    /// <summary>Czy punkt leży w prostokącie grywalnym.</summary>
    public static bool Wewnatrz(float x, float z, float zapas = 0f) => x >= X0 - zapas && x <= X1 + zapas && z >= Z0 - zapas && z <= Z1 + zapas;

    /// <summary>Czy punkt jest grywalny: w prostokącie i poza wycięciami (woda też — na morzu granicę robi nabrzeże).</summary>
    public static bool Grywalny(float x, float z)
    {
        if (!Wewnatrz(x, z)) return false;
        foreach (var w in Wyciecia) if (w.Contains(new Vector2(x, z))) return false;
        return true;
    }

    /// <summary>Odległość od prostokąta grywalnego (0 wewnątrz i na krawędzi).</summary>
    public static float Odleglosc(float x, float z)
    {
        float dx = Mathf.Max(0f, Mathf.Max(X0 - x, x - X1)), dz = Mathf.Max(0f, Mathf.Max(Z0 - z, z - Z1));
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
}
