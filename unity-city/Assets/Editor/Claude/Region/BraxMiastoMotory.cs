using UnityEngine;
using UnityEngine.Rendering;

// MOTOCYKLE I ROWERY WŁAŚCICIELA (12.11.2026, 01:30: „Tu masz motory globalnie do wszystkich miast, rejonów”; 01:32: „rowery
// globalnie dla całej gry”; 02:05: „dokończ to, co masz”). Modele z paczek globalnych Tripo (pojazdy_motory_01: sportowy,
// naked, adventure z kuframi, cruiser, turystyczny; pojazdy_rowery_01: fatbike, trekkingowy elektryczny) — prawdziwe metry
// × K, przód +Z, podstawa w środku obrysu na y = 0.
//  • Motocykle na stanowiskach parkingu: dwa obok siebie w poprzek stanowiska (kierownice się nie stykają), tyłem do
//    krawężnika, przodem do alejki; pochylone 6° na stopce bocznej (obrót wokół osi wzdłużnej przez styk kół).
//  • Rowery: stoją w lokalu (warsztat rowerowy) — nie na chodniku (zasada 11.11: na chodnikach tylko kosze).
// Bez paczki (brak klucza w CI): 0 i miejsce wraca do aut. Kolizja z obrysu, LOD (znikają przy ~1.5 % ekranu).
// Wspólne dla innych rejonów (kopiować): NaStanowisku, Rower.
internal static class BraxMiastoMotory
{
    static float K => BraxClaudeOsiedle.K;
    public const string PaczkaMotory = "pojazdy_motory_01", PaczkaRowery = "pojazdy_rowery_01";
    public static readonly string[] Motory = { "motor_sportowy_czerwony", "motor_naked_grafitowy", "motor_adventure_bezowy_kufry", "motor_cruiser_bordowy", "motor_turystyczny_granatowy" };
    public static readonly string[] Rowery = { "rower_fatbike_czarny", "rower_trekking_elektryczny_czarny" };
    public static bool SaMotory => BraxMiastoAssety.Jest(PaczkaMotory);

    /// <summary>Motocykle na stanowisku (c = środek stanowiska na nawierzchni, przod = kierunek wjazdu auta, rozmiar.x = szerokość
    /// w poprzek): ile ≤ 2 sztuk od modelu nr od. Zwraca liczbę postawionych.</summary>
    public static int NaStanowisku(Transform rodzic, Vector3 c, Vector3 przod, Vector2 rozmiar, int od, int ile)
    {
        if (!SaMotory) return 0;
        przod.y = 0f; przod.Normalize(); var bok = Vector3.Cross(Vector3.up, przod).normalized;
        ile = Mathf.Clamp(ile, 1, 2); int n = 0; float krok = rozmiar.x / ile;
        for (int i = 0; i < ile; i++)
        {
            string plik = Motory[(od + i) % Motory.Length];
            var m = BraxMiastoAssety.Model(PaczkaMotory, plik); if (!m) continue;
            var p = c + bok * ((i + .5f) * krok - rozmiar.x * .5f) + przod * ((i % 2 == 0 ? .2f : -.2f) * K);
            float yaw = Quaternion.LookRotation(-przod, Vector3.up).eulerAngles.y + (i == 0 ? -5f : 6f);
            var o = BraxMiastoAssety.Postaw(rodzic, m, "Motocykl na parkingu · " + plik, p, yaw, 1f, true, true);
            if (!o) continue;
            o.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(0f, 0f, 6f);   // na stopce (w lewo)
            BraxMiastoAssety.Kolizja(rodzic, o, "kolizja: motocykl " + plik, .05f);
            Lod(o, .015f);
            n++;
        }
        return n;
    }

    /// <summary>Rower (nr 0 fatbike, 1 trekkingowy elektryczny) stojący w punkcie p, przodem w kierunku przod; pochylony 5°
    /// na nóżce. null = brak paczki.</summary>
    public static GameObject Rower(Transform rodzic, int nr, Vector3 p, Vector3 przod, bool kolizja = true)
    {
        if (!BraxMiastoAssety.Jest(PaczkaRowery)) return null;
        string plik = Rowery[Mathf.Abs(nr) % Rowery.Length];
        var m = BraxMiastoAssety.Model(PaczkaRowery, plik); if (!m) return null;
        przod.y = 0f; float yaw = Quaternion.LookRotation(przod.normalized, Vector3.up).eulerAngles.y;
        var o = BraxMiastoAssety.Postaw(rodzic, m, "Rower · " + plik, p, yaw, 1f, true, true);
        if (!o) return null;
        o.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(0f, 0f, 5f);
        if (kolizja) BraxMiastoAssety.Kolizja(rodzic, o, "kolizja: rower " + plik, .05f);
        Lod(o, .012f);
        return o;
    }

    static void Lod(GameObject o, float prog)
    {
        var rs = o.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) return;
        var lod = o.GetComponent<LODGroup>() ?? o.AddComponent<LODGroup>();
        lod.SetLODs(new[] { new LOD(prog, rs) }); lod.RecalculateBounds();
        foreach (var r in rs) r.shadowCastingMode = ShadowCastingMode.On;
    }
}
