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
    // ---- prostokąt grywalny (ląd + akwen zatoki) — ETAP 1 rozbudowy (B, 2.11: rondo (−126, 28.5), Aleja Północna i Południowa) ----
    // Rozbudowa w kolejnych etapach zmienia TYLKO te liczby i wycięcia (reszta horyzontu przelicza się sama).
    public const float X0 = -872f;   // zachód (nowe dzielnice ze Sketchfaba, 2.11): za placem ATLANTY na zamknięciu Alei Centralnej — wcześniej −264 (ETAP 2), −162
    public const float X1 = 170f;    // wschód: za cyplem plaży (do 150) i mariną (do 166); dalej otwarte morze (wyspa = obszar nabrzeża)
    public const float Z0 = -85.45f; // południe: tył muru portalu tunelu na końcu Alei Południowej (lico portalu z = −84)
    public const float Z1 = 180f;    // północ: za lasem za plażą (z 160…174) i łąką

    // ---- wycięcia: części prostokąta, które NIE są grywalne (teren i las wchodzą do środka, granica idzie dookoła) ----
    // Las na północny zachód (za pętlą Alei Północnej, z ≥ 126) i wzgórze za placem wieżowców (na płd. od z −68, na wschód od x −61.4).
    // Nowe dzielnice (x X0…−262) kończą się na północy na z 156 (ciąg pieszy za rzędem B) — dalej las.
    // 9.11 (właściciel: fawela na wzgórzu za miastem): wzgórze za placem wieżowców (x −61.4…70) jest teraz FAWELĄ (grywalne) —
    // wycięciem zostaje tylko pas przy morzu x 70…84 (skarpa nad wodą, poza fawelą).
    public static readonly Rect[] Wyciecia = { Rect.MinMaxRect(-262f, 126f, -40f, Z1), Rect.MinMaxRect(X0, 156f, -262f, Z1), Rect.MinMaxRect(FawelaX1, Z0, 84f, -68f) };

    // ---- FAWELA NA WZGÓRZU (9.11, właściciel: „slamsy… z delikatnym przejściem… nie ma być teren mały”) ----
    // Całe wzgórze na południe od miasta: od linii miasta (z −68 za parkiem wieżowców na wschód od x −61.4, z Z0 dalej na zachód)
    // do szczytu z ≈ −357; od x −470 (za nowymi dzielnicami) do x 70 (stok schodzi ku morzu). Grywalne — granicę gry robi
    // murek na jej obrzeżu (krok horyzontu), teren za nią (las, góry) przedłuża stok.
    // 11.11 (właściciel: „rozdziel wtedy te fawele od miasta dworcem, torami i lasem”): fawela odsunięta o FawelaOdsuniecie na
    // południe — między linią miasta a fawelą pas lasu, dworzec autobusowo-kolejowy i tory (grywalne, ta sama granica gry).
    // Obszar „fawela” w tej klasie = pas dworca + fawela (od linii miasta do szczytu); sama fawela zaczyna się KrawedzFaweliOdsunietej.
    public const float FawelaOdsuniecie = 115f;
    public const float FawelaX0 = -470f, FawelaX1 = 70f, FawelaZSzczyt = -357f - FawelaOdsuniecie;
    /// <summary>Linia miasta nad pasem dworca i fawelą (z, od której zaczyna się teren za miastem) dla danego x.</summary>
    public static float FawelaKrawedz(float x) => x > -61.4f ? -68f : Z0;
    /// <summary>Górna krawędź samej faweli (za lasem i torami) dla danego x.</summary>
    public static float KrawedzFaweliOdsunietej(float x) => FawelaKrawedz(x) - FawelaOdsuniecie;
    /// <summary>Czy punkt leży w pasie dworca, torów i lasu między miastem a fawelą.</summary>
    public static bool WPasieDworca(float x, float z) => x >= FawelaX0 && x <= FawelaX1 && z <= FawelaKrawedz(x) && z > KrawedzFaweliOdsunietej(x);
    /// <summary>Czy punkt leży w faweli albo w pasie dworca przed nią (grywalne — teren za miastem).</summary>
    public static bool WFaweli(float x, float z) => x >= FawelaX0 && x <= FawelaX1 && z >= FawelaZSzczyt && z <= FawelaKrawedz(x);
    /// <summary>Prostokąt obejmujący miasto RAZEM z fawelą (zasięg siatek granicy i terenu za granicą).</summary>
    public static Rect ProstokatCaly => Rect.MinMaxRect(X0, FawelaZSzczyt, X1, Z1);

    // ---- portal tunelu wyjazdu na osiedle (mur portalu w poprzek końca Alei Południowej): przerwa w ogrodzeniu granicy ----
    public static readonly Rect PortalTunelu = Rect.MinMaxRect(-148.5f, Z0 - 3f, -103.5f, Z0 + 3f);

    // ---- wzgórze z tunelem wykonawcy „tyły” (jego siatka i kolizja; mój teren pod nim leży niżej i nie wystaje) ----
    public static readonly Rect WzgorzeTylow = Rect.MinMaxRect(-206f, -157.5f, -60f, -85.45f);   // zbocze nad tunelem (BraxRegionMiastoB.Zbocze)

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
        if (WFaweli(x, z)) return true;
        if (!Wewnatrz(x, z)) return false;
        foreach (var w in Wyciecia) if (w.Contains(new Vector2(x, z))) return false;
        return true;
    }

    /// <summary>Odległość od prostokąta grywalnego (0 wewnątrz i na krawędzi).</summary>
    public static float Odleglosc(float x, float z)
    {
        float dx = Mathf.Max(0f, Mathf.Max(X0 - x, x - X1)), dz = Mathf.Max(0f, Mathf.Max(Z0 - z, z - Z1));
        float fx = Mathf.Max(0f, Mathf.Max(FawelaX0 - x, x - FawelaX1)), fz = Mathf.Max(0f, Mathf.Max(FawelaZSzczyt - z, z - FawelaKrawedz(x)));
        return Mathf.Min(Mathf.Sqrt(dx * dx + dz * dz), Mathf.Sqrt(fx * fx + fz * fz));
    }

    /// <summary>Odległość od faweli (0 w środku i na krawędzi) i najbliższy punkt jej obrysu (przedłużanie stoku za granicą).</summary>
    public static float OdlegloscFaweli(float x, float z, out Vector2 najblizszy)
    {
        float cx = Mathf.Clamp(x, FawelaX0, FawelaX1);
        float cz = Mathf.Clamp(z, FawelaZSzczyt, FawelaKrawedz(cx));
        najblizszy = new Vector2(cx, cz);
        return Vector2.Distance(new Vector2(x, z), najblizszy);
    }
}
