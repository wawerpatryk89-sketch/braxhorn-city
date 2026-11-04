using UnityEngine;

// AKWEN MIASTA (pkt 15, właściciel 2.11 23:13: „popłynąłem do końca morza i wypadłem z terenu”).
//  a) MORZE BEZ KRAWĘDZI: BraxMorzeZaKamera — siatka wody 3.2 × 3.2 km przesuwana za kamerą skokowo co 100 m (siatka wyrównana
//     do wody portu x 84…884, z −340…460 — tej nie dubluje, komórki na lądzie pominięte). Shader wody liczy fale ze świata
//     (SwellPhase(world.xz)), więc przesunięcie nie „przesuwa” fal. Kamera ma zasięg 600 m — końca wody nie widać nigdy.
//  b) GRANICA MIĘKKA: na wschód (otwarte morze) od x 2300, wzdłuż wybrzeża na północ od z 1150 i na południe od z −1050,
//     przy brzegu poza wodami miasta (teren za granicą nie ma kolizji) 70 m od linii brzegu: komunikat „Silny prąd — dalej
//     nie płyń, zawróć”, łódź zwalnia i jest łagodnie obracana do środka akwenu; twarda granica 150 m dalej (55 m przy brzegu).
//  c) SIATKA BEZPIECZEŃSTWA (BraxGymPlayer): ostatnia bezpieczna pozycja co 1 s (na gruncie albo na łodzi w akwenie);
//     spadek w pustkę / poza świat → powrót tam (log BRAXHORN_SIATKA_BEZP), zamiast przeniesienia na start.
public static class BraxAkwen
{
    public const float WodaY = -1.7f;
    public const float MiekkaX = 2300f, TwardaX = 2450f, MiekkaZN = 1150f, TwardaZN = 1300f, MiekkaZS = -1050f, TwardaZS = -1200f;
    public const float BrzegMiekki = 70f, BrzegTwardy = 15f;
    public static readonly Vector3 Srodek = new Vector3(900f, WodaY, 40f);
    static readonly Rect WodyMiasta = Rect.MinMaxRect(84f, -340f, 884f, 470f);   // tu brzeg mają nabrzeża i plaże z kolizjami
    public static float Ostatni;   // ostatnia wartość prądu (do komunikatu)
    public static float CzasKomunikatu;

    // pkt 15b′ (właściciel 23:14 „wypływasz z drugiej strony”): na końcach północnym i południowym wzdłuż wybrzeża PAS MGŁY
    // 150 m; w jego połowie łódź (z graczem) przenosi się do połowy pasa na drugim końcu — ten sam kurs, prędkość i odległość
    // od brzegu — i wypływa z mgły (krótkie przyciemnienie). Pkt 17: od otwartego morza (wschód) SZTORM: ciemno, deszcz, wiatr,
    // wysokie fale, „Sztorm — zawróć, bo utoniesz”, prąd zawraca łódź; twarda granica 150 m dalej.
    public const float PetlaN = 1150f, PetlaS = -1050f, PasMgly = 150f;
    public static int Rodzaj;                 // 0 brak, 1 sztorm (wschód), 2 za pętlą (awaryjnie), 4 brzeg bez kolizji
    public static float Ciemno;               // do kiedy przyciemnienie po przeniesieniu (Time.unscaledTime)

    /// <summary>0 w akwenie, 0…1 w pasie miękkim, ≥ 1 za twardą granicą; doSrodka — kierunek powrotu (poziomo).</summary>
    public static float Prad(Vector3 p, out Vector3 doSrodka)
    {
        float t = 0f; Rodzaj = 0;
        float te = (p.x - MiekkaX) / (TwardaX - MiekkaX); if (te > t) { t = te; Rodzaj = 1; }
        // za pasem mgły (gdyby przeniesienie zawiodło) — granica awaryjna
        float tn = Mathf.Max((p.z - (PetlaN + PasMgly + 60f)) / 100f, ((PetlaS - PasMgly - 60f) - p.z) / 100f); if (tn > t) { t = tn; Rodzaj = 2; }
        // brzeg poza wodami miasta (teren za granicą bez kolizji) — tylko na morzu na wschód od prostokąta grywalnego miasta
        // (miasto na lądzie x < 170 nigdy nie jest „za granicą akwenu”)
        if (p.x > BraxMiastoGranice.X1 && !WodyMiasta.Contains(new Vector2(p.x, p.z)))
        {
            float odBrzegu = p.x - BraxMiastoGranice.Brzeg(p.z);
            float tb = (BrzegMiekki - odBrzegu) / (BrzegMiekki - BrzegTwardy); if (tb > t) { t = tb; Rodzaj = 4; }
        }
        doSrodka = Srodek - p; doSrodka.y = 0f;
        doSrodka = doSrodka.sqrMagnitude > 1e-4f ? doSrodka.normalized : Vector3.right;
        return Mathf.Max(0f, t);
    }
    public static float Prad(Vector3 p) => Prad(p, out _);

    /// <summary>Gęstość mgły pasa pętli (0 poza pasem, 1 w jego połowie).</summary>
    public static float Mgla(Vector3 p) => Mathf.Max(Mathf.InverseLerp(PetlaN, PetlaN + PasMgly * .5f, p.z), Mathf.InverseLerp(PetlaS, PetlaS - PasMgly * .5f, p.z));

    /// <summary>Pętla wybrzeża: punkt za połową pasa mgły → odpowiadający punkt na drugim końcu (ta sama odległość od brzegu,
    /// ten sam zapas za połową pasa, kurs bez zmian — wypływa się z mgły do środka akwenu). false = bez przeniesienia.</summary>
    public static bool Petla(Vector3 p, out Vector3 cel)
    {
        float zN = PetlaN + PasMgly * .5f, zS = PetlaS - PasMgly * .5f; cel = p;
        if (p.z > zN) cel.z = zS + (p.z - zN);
        else if (p.z < zS) cel.z = zN + (p.z - zS);
        else return false;
        float odBrzegu = p.x - BraxMiastoGranice.Brzeg(p.z);
        cel.x = Mathf.Min(MiekkaX - 20f, BraxMiastoGranice.Brzeg(cel.z) + Mathf.Max(odBrzegu, BrzegMiekki + 10f));
        return true;
    }
    public static void Komunikat(float t) { Ostatni = t; CzasKomunikatu = Time.unscaledTime + .6f; }
}
