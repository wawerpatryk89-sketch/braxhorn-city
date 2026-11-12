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
// Wspólne dla innych rejonów (kopiować): NaStanowisku, Motor, RowerJazda, Hulajnoga, Stacja, Rower (statyczny).
internal static class BraxMiastoMotory
{
    static float K => BraxClaudeOsiedle.K;
    public const string PaczkaMotory = "pojazdy_motory_01", PaczkaRowery = "pojazdy_rowery_01";
    public static readonly string[] Motory = { "motor_sportowy_czerwony", "motor_naked_grafitowy", "motor_adventure_bezowy_kufry", "motor_cruiser_bordowy", "motor_turystyczny_granatowy" };
    public static readonly string[] Rowery = { "rower_fatbike_czarny", "rower_trekking_elektryczny_czarny" };
    public static bool SaMotory => BraxMiastoAssety.Jest(PaczkaMotory);

    /// <summary>Motocykle na stanowisku (c = środek stanowiska na nawierzchni, przod = kierunek wjazdu auta, rozmiar.x = szerokość
    /// w poprzek): ile ≤ 2 sztuk od modelu nr od — DO JAZDY (12.11 02:32). Zwraca liczbę postawionych.</summary>
    public static int NaStanowisku(Transform rodzic, Vector3 c, Vector3 przod, Vector2 rozmiar, int od, int ile)
    {
        if (!SaMotory) return 0;
        przod.y = 0f; przod.Normalize(); var bok = Vector3.Cross(Vector3.up, przod).normalized;
        ile = Mathf.Clamp(ile, 1, 2); int n = 0; float krok = rozmiar.x / ile;
        for (int i = 0; i < ile; i++)
        {
            var p = c + bok * ((i + .5f) * krok - rozmiar.x * .5f) + przod * ((i % 2 == 0 ? .2f : -.2f) * K);
            if (Motor(rodzic, (od + i) % Motory.Length, p, Quaternion.LookRotation(-przod, Vector3.up).eulerAngles.y + (i == 0 ? -5f : 6f))) n++;
        }
        return n;
    }

    // ------------------------------------------------------------------ JAZDA (właściciel 12.11 02:32: „Zrób tak, żeby Brax mógł nim
    // jeździć. Żeby nie musiał chodzić pieszo. Hulajnogi, motory, rowery. Zrób to i puszczamy”). Pojazd = korzeń z BraxVessel
    // (tryb lad), w nim model (z paczki Tripo albo z kodu), kolizja, uchwyty kierownicy (cele dłoni), siedzisko. Akcja: WSIĄDŹ
    // przy pojeździe (3.2 m), joystick: gaz / hamulec / kierownica, ZEJDŹ — pojazd zostaje na stopce tam, gdzie go zostawiono.
    static BraxVessel Korzen(Transform rodzic, string nazwa, Vector3 p, float yaw, string label, float dl, float szer)
    {
        var g = new GameObject(nazwa); g.transform.SetParent(rodzic, true); g.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, yaw, 0f));
        var v = g.AddComponent<BraxVessel>();
        v.lad = true; v.label = label; v.waterY = -1000f; v.draft = 0f; v.length = dl; v.width = szer; v.wejscie = Vector3.zero;
        return v;
    }
    static Transform Marker(Transform v, string n, Vector3 lokalnie)
    { var t = new GameObject(n).transform; t.SetParent(v, false); t.localPosition = lokalnie; return t; }
    static void Kolizja(Transform v, Vector3 lo, Vector3 hi)
    { var c = new GameObject("kolizja pojazdu").AddComponent<BoxCollider>(); c.transform.SetParent(v, false); c.center = (lo + hi) * .5f; c.size = hi - lo; }

    /// <summary>Motocykl do jazdy (nr modelu 0–4). null = brak paczki.</summary>
    public static BraxVessel Motor(Transform rodzic, int nr, Vector3 p, float yaw)
    {
        string plik = Motory[Mathf.Abs(nr) % Motory.Length];
        var m = SaMotory ? BraxMiastoAssety.Model(PaczkaMotory, plik) : null; if (!m) return null;
        var o = BraxMiastoAssety.Postaw(rodzic, m, "model · " + plik, p, yaw, 1f, false, true); if (!o) return null;
        var b = BraxMiastoAssety.Granice(o); float L = b.size.z > b.size.x ? b.size.z : b.size.x, H = b.size.y;
        var v = Korzen(rodzic, "Motocykl do jazdy · " + plik, p, yaw, "MOTOR", L, .9f * K);
        o.transform.SetParent(v.transform, true);
        v.maxSpeed = 24f; v.accel = 6.5f; v.turnRate = 72f; v.maxPrzechyl = 40f; v.stopka = 7f; v.dzwiek = "MOTOR"; v.sit = true;
        v.rozstaw = .68f * L; v.rKola = .31f * K;
        v.seat = new Vector3(0f, .6f * H, -.08f * L);
        v.leftGrip = Marker(v.transform, "uchwyt lewy", new Vector3(-.32f * K, .76f * H, .3f * L));
        v.rightGrip = Marker(v.transform, "uchwyt prawy", new Vector3(.32f * K, .76f * H, .3f * L));
        Kolizja(v.transform, new Vector3(-.3f * K, .1f, -.45f * L), new Vector3(.3f * K, .85f * H, .45f * L));
        Lod(v.gameObject, .012f);
        return v;
    }

    /// <summary>Rower do jazdy (0 fatbike, 1 trekkingowy elektryczny). null = brak paczki.</summary>
    public static BraxVessel RowerJazda(Transform rodzic, int nr, Vector3 p, float yaw)
    {
        string plik = Rowery[Mathf.Abs(nr) % Rowery.Length];
        var m = BraxMiastoAssety.Jest(PaczkaRowery) ? BraxMiastoAssety.Model(PaczkaRowery, plik) : null; if (!m) return null;
        var o = BraxMiastoAssety.Postaw(rodzic, m, "model · " + plik, p, yaw, 1f, false, true); if (!o) return null;
        var b = BraxMiastoAssety.Granice(o); float L = Mathf.Max(b.size.z, b.size.x), H = b.size.y;
        var v = Korzen(rodzic, "Rower do jazdy · " + plik, p, yaw, "ROWER", L, .7f * K);
        o.transform.SetParent(v.transform, true);
        v.maxSpeed = 9.5f; v.accel = 2.8f; v.turnRate = 88f; v.maxPrzechyl = 26f; v.stopka = 6f; v.dzwiek = ""; v.sit = true;
        v.rozstaw = .58f * L; v.rKola = .34f * K;
        v.seat = new Vector3(0f, .86f * H, -.17f * L);
        v.leftGrip = Marker(v.transform, "uchwyt lewy", new Vector3(-.3f * K, .88f * H, .28f * L));
        v.rightGrip = Marker(v.transform, "uchwyt prawy", new Vector3(.3f * K, .88f * H, .28f * L));
        Kolizja(v.transform, new Vector3(-.2f * K, .1f, -.45f * L), new Vector3(.2f * K, .8f * H, .45f * L));
        Lod(v.gameObject, .012f);
        return v;
    }

    /// <summary>Hulajnoga elektryczna Z KODU (właściciel nie przysłał modelu — prawdziwe materiały, bez klocków): deska z gumą,
    /// kolumna kierownicy z widelcem, kierownica z chwytami, lampka, nóżka, koła z oponą i felgą (kręcą się), kierownica
    /// skręca. Wymiary hulajnogi miejskiej: dł. 1.15 m, kierownica 1.05 m × K.</summary>
    public static BraxVessel Hulajnoga(Transform rodzic, Vector3 p, float yaw, int kolor)
    {
        var v = Korzen(rodzic, "Hulajnoga elektryczna do jazdy " + (kolor + 1), p, yaw, "HULAJNOGA", 1.15f * K, .55f * K);
        var czern = BraxMiastoA.Mat("hulajnoga: rama czarny mat", new Color(.05f, .05f, .055f), .45f, .4f);
        var akcent = kolor % 2 == 0 ? BraxMiastoA.Mat("hulajnoga: akcent czerwony", new Color(.62f, .05f, .05f), .35f, .3f) : BraxMiastoA.Mat("hulajnoga: akcent grafit", new Color(.32f, .33f, .35f), .3f, .7f);
        var guma = BraxMiastoA.Mat("hulajnoga: guma (opony, chwyty, deska)", new Color(.025f, .025f, .028f), .9f, 0f);
        var stal = BraxMiastoA.Mat("hulajnoga: aluminium szczotkowane", new Color(.72f, .73f, .75f), .28f, .9f);
        var lampa = BraxMiastoA.Swieci("hulajnoga: lampka LED", Color.white, new Color(1.6f, 1.6f, 1.5f));
        Vector3 L(float x, float y, float z) => new Vector3(x, y, z) * K;
        float rk = .11f, zt = -.44f, zp = .45f;
        // deska z gumą (górny pas z gumy osobną bryłą NAD aluminium — przeciwne normalne na styku), błotnik tylny
        var deska = new UlSiatka(); deska.Poducha(L(-.075f, .085f, -.36f), L(.075f, .118f, .33f), .012f * K, 0f, 3);
        BraxMiastoA.Obiekt(v.transform, "Hulajnoga: deska", deska, czern, true);
        var gumaD = new UlSiatka(); gumaD.Poducha(L(-.065f, .118f, -.33f), L(.065f, .126f, .3f), .004f * K, 0f, 2);
        BraxMiastoA.Obiekt(v.transform, "Hulajnoga: guma na desce", gumaD, guma, true);
        var bl = new UlSiatka(); bl.Rura(L(0f, .275f, zt - .02f), L(0f, .245f, zt + .16f), .04f * K, 10); bl.Rura(L(0f, .118f, -.34f), L(0f, .24f, zt + .14f), .02f * K, 8);
        bl.Rura(L(0f, .118f, .31f), L(0f, .2f, zp - .04f), .03f * K, 10);                              // szyjka składania (stała, przy desce)
        BraxMiastoA.Obiekt(v.transform, "Hulajnoga: błotnik", bl, akcent, true);
        var nozka = new UlSiatka(); nozka.Rura(L(-.07f, .09f, -.05f), L(-.2f, .01f, -.12f), .012f * K, 8);
        BraxMiastoA.Obiekt(v.transform, "Hulajnoga: nóżka", nozka, stal, true);
        // koło tylne (osobny obiekt — kręci się)
        Transform Kolo(Transform rodzicK, string n, Vector3 os)
        {
            var t = new GameObject(n).transform; t.SetParent(rodzicK, false); t.position = rodzicK.TransformPoint(os); t.localRotation = Quaternion.identity;
            var op = new UlSiatka(); op.Rura(L(-.022f, 0f, 0f), L(.022f, 0f, 0f), rk * K, 24);
            BraxMiastoA.Obiekt(t, n + ": opona", op, guma, true);
            // piasta wystaje 2.4 cm za lico opony z obu stron (zero migotania: żadnych równoległych lic bliżej niż 2 cm)
            var fe = new UlSiatka(); fe.Rura(L(-.046f, 0f, 0f), L(.046f, 0f, 0f), .032f * K, 14);
            BraxMiastoA.Obiekt(t, n + ": piasta", fe, stal, true);
            return t;
        }
        v.kolT = Kolo(v.transform, "Hulajnoga: koło tylne", L(0f, rk, zt));
        // przód (skręca): widelec, kolumna, kierownica z chwytami, lampka, koło przednie
        var przod = new GameObject("Hulajnoga: kierownica i przód (skręca)").transform; przod.SetParent(v.transform, false); przod.localPosition = L(0f, 0f, zp);
        var kol = new UlSiatka();
        kol.Rura(L(0f, .2f, -.05f), L(0f, 1.0f, -.12f), .021f * K, 12);                                 // kolumna kierownicy
        kol.Rura(L(-.042f, rk, 0f), L(-.042f, .22f, -.05f), .012f * K, 8); kol.Rura(L(.042f, rk, 0f), L(.042f, .22f, -.05f), .012f * K, 8);   // widelec (poza oponą)
        kol.Rura(L(-.24f, 1.02f, -.12f), L(.24f, 1.02f, -.12f), .014f * K, 10);                         // kierownica
        BraxMiastoA.Obiekt(przod, "Hulajnoga: kolumna i kierownica", kol, czern, true);
        var szyja = new UlSiatka(); szyja.Rura(L(0f, .17f, -.04f), L(0f, .27f, -.06f), .03f * K, 12);                     // główka ramy
        BraxMiastoA.Obiekt(przod, "Hulajnoga: szyjka (akcent)", szyja, akcent, true);
        var chw = new UlSiatka(); chw.Rura(L(-.27f, 1.02f, -.12f), L(-.15f, 1.02f, -.12f), .019f * K, 12); chw.Rura(L(.15f, 1.02f, -.12f), L(.27f, 1.02f, -.12f), .019f * K, 12);
        BraxMiastoA.Obiekt(przod, "Hulajnoga: chwyty", chw, guma, true);
        var la = new UlSiatka(); la.Rura(L(0f, .86f, -.08f), L(0f, .86f, -.065f), .026f * K, 14);
        BraxMiastoA.Obiekt(przod, "Hulajnoga: lampka", la, lampa, false);
        v.kolP = Kolo(przod, "Hulajnoga: koło przednie", L(0f, rk, 0f));
        v.przod = przod;
        v.leftGrip = Marker(przod, "uchwyt lewy", L(-.21f, 1.02f, -.12f));
        v.rightGrip = Marker(przod, "uchwyt prawy", L(.21f, 1.02f, -.12f));
        v.seat = L(0f, .126f, -.08f);
        v.maxSpeed = 7f; v.accel = 3f; v.turnRate = 110f; v.maxPrzechyl = 16f; v.stopka = 5f; v.dzwiek = "ELEKTRYK"; v.sit = false;
        v.rozstaw = (zp - zt) * K; v.rKola = rk * K;
        Kolizja(v.transform, L(-.1f, .05f, -.5f), L(.1f, 1.05f, .5f));
        Lod(v.gameObject, .01f);
        return v;
    }

    /// <summary>Stacja jednośladów (2 hulajnogi, 2 rowery, gdy są): w rzędzie wzdłuż kierunku wzdluz co 1.25 m, przodem w stronę
    /// przodem; każde miejsce sprawdzane (płaska nawierzchnia, wolne 1 × 2.6 m). Zwraca liczbę postawionych.</summary>
    public static int Stacja(Transform rodzic, string nazwa, Vector3 start, Vector3 wzdluz, Vector3 przodem)
    {
        var g = new GameObject("Jednoślady do jazdy · " + nazwa).transform; g.SetParent(rodzic, true); g.position = Vector3.zero;
        wzdluz.y = 0f; wzdluz.Normalize(); przodem.y = 0f; przodem.Normalize(); float yaw = Quaternion.LookRotation(przodem).eulerAngles.y;
        Physics.SyncTransforms();
        int n = 0;
        for (int i = 0; i < 4; i++)
        {
            var p = start + wzdluz * (i * 1.25f * K);
            if (!Physics.Raycast(p + Vector3.up * 2f, Vector3.down, out var h, 4f, ~((1 << 8) | (1 << 29)), QueryTriggerInteraction.Ignore) || h.normal.y < .95f) continue;
            p.y = h.point.y;
            if (!BraxRegion.Free(p + Vector3.up * 1.0f, new Vector3(.9f, 1.6f, 2.5f * K))) continue;
            var v = i < 2 ? Hulajnoga(g, p, yaw, i) : RowerJazda(g, i - 2, p, yaw);
            if (v) { n++; Physics.SyncTransforms(); }
        }
        Debug.Log("BRAXHORN_JEDNOSLADY " + nazwa + ": do jazdy " + n + " (hulajnogi z kodu, rowery z paczki właściciela)");
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
