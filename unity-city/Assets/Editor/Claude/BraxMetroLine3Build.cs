using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using W = BraxWnetrzaKit;

// MIASTO R1 — JEDNO metro M1 przez całą główną ulicę, trzy stacje.
// Nie korzysta z dawnej G5 ani z nienazwanego wejścia M1. Unity 6 / Android.
// Odbiór fizyki po wygenerowaniu całego miasta: KontrolaPrzejscia() (raport).
public static class BraxMetroLine3Build
{
    public const float StreetY = .15f, ConcourseY = -4.6f, PlatformY = -11.2f, RailY = -12.6f;
    public const float TrackNorth = 32.3f, TrackSouth = 23.5f;
    public const float RouteMinX = -251f, RouteMaxX = 96f;
    public static readonly float[] Stops = { 72f, -76f, -218f };
    public static readonly string[] Names = { "NABRZEZE", "SRODMIESCIE", "ZACHOD" };

    static readonly List<string> Problems = new List<string>();
    static Transform line;
    static Material platform, tiles, wall, concrete, rails, sleepers, steel, edge, dark, glass;
    static int opened, meshes, trains, cars;

    static Bounds B(float x0, float x1, float y0, float y1, float z0, float z1)
    {
        var b = new Bounds();
        b.SetMinMax(new Vector3(x0, y0, z0), new Vector3(x1, y1, z1));
        return b;
    }
    static Transform Group(Transform parent, string name)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }
    static void Warn(string msg)
    {
        Problems.Add(msg);
        Debug.LogWarning("BRAXHORN_METRO_M1_3 " + msg);
    }

    public static void Apply(Transform cityRoot)
    {
        if (!cityRoot) { Warn("Brak korzenia miasta."); return; }
        opened = meshes = trains = cars = 0; Problems.Clear();
        line = null;
        // Jedna konstrukcja od nowa; NIE kasujemy repozytoryjnych modeli ani reszty miasta.
        var removed = W.Usun("Metro entrance and finished vestibule only",
            "G5 accessible metro platform and track infrastructure",
            "CLAUDE stacja metra BRAXHORN NABRZEŻE",
            "CLAUDE metro M1*",
            "BRAXHORN M1 — TRZY STACJE");
        try
        {
            W.Wyczysc();
            Materials();
            line = Group(cityRoot, "BRAXHORN M1 — TRZY STACJE");
            // Wycięcia poniżej ulicy oraz TRZY rzeczywiste otwory w nawierzchni.
            // Pociągi i przepisy materiałowe są współdzielone: bez kopii pełnego miasta.
            // W.Otwor tnie również kolizje oryginalnego podłoża i późniejszych rejonów.
            opened += W.Otwor(cityRoot,
                B(RouteMinX, RouteMaxX, -13.9f, -4.8f, 19.5f, 37.2f),
                "M1_3_tunel_caly", line);
            for (int i = 0; i < Stops.Length; i++)
            {
                float x = Stops[i];
                opened += W.Otwor(cityRoot,
                    B(x - 12f, x + 12f, -13.8f, -1.65f, 25.0f, 44.8f),
                    "M1_3_hala_" + i, line);
                // Uporządkuj drobne elementy miejskie wyłącznie w świetle wejścia.
                W.Uprzatnij(cityRoot,
                    B(x - 7.1f, x + 7.1f, -.2f, 3.1f, 41.22f, 44.78f),
                    line, 2.5f);
                opened += W.Otwor(cityRoot,
                    B(x - 7.1f, x + 7.1f, -5.1f, .56f, 41.22f, 44.78f),
                    "M1_3_wejscie_" + i, line);
            }

            W.Start(line); W.Komorka = 18f; W.Wnetrze = true;
            TrackAndTunnel();
            for (int i = 0; i < Stops.Length; i++) Station(i);
            meshes = W.Zapisz("M1_3_stacje_tory_i_tunele");
            AddTrains();
            var visible = line.gameObject.AddComponent<BraxWidocznoscWnetrza>();
            visible.nazwa = "metro M1 TRZY STACJE";
            visible.strefy = new[] {
                B(RouteMinX, RouteMaxX, -15f, -1.0f, 17f, 48f),
                B(63f, 82f, -1.0f, 6f, 39f, 48f),
                B(-85f, -66f, -1.0f, 6f, 39f, 48f),
                B(-227f, -208f, -1.0f, 6f, 39f, 48f)
            };
            var cameraRange = Group(line, "M1 kamera pod ziemia").gameObject.AddComponent<BraxClaudeZasiegWnetrza>();
            cameraRange.zasieg = 360f;
            cameraRange.sale = new[] { B(RouteMinX, RouteMaxX, -15f, -1.0f, 17f, 48f) };

            for (int i = 0; i < Stops.Length; i++)
            {
                W.Punkt("metro_m1_wejscie_" + i, "M1 " + Names[i],
                    new Vector3(Stops[i] + 7f, StreetY, 43f));
                W.Punkt("metro_m1_peron_" + i, "M1 peron " + Names[i],
                    new Vector3(Stops[i] + 12f, PlatformY, 29.7f));
            }
            // Identyfikatory misji zachowane dla innych systemów miasta,
            // ale ich pozycje prowadzą do NOWEJ stacji 1, nie do starej G5.
            W.Punkt("metro_entry", "M1 Nabrzeze wejscie",
                new Vector3(Stops[0] + 7f, StreetY, 43f));
            W.Punkt("metro_stairs_bottom", "M1 Nabrzeze hala",
                new Vector3(Stops[0] - 6f, ConcourseY, 43f));
            W.Punkt("metro_vestibule", "M1 Nabrzeze korytarz",
                new Vector3(Stops[0] - 7f, ConcourseY, 34f));
            W.Punkt("metro_station_boundary", "M1 Nabrzeze schody na peron",
                new Vector3(Stops[0] - 6f, ConcourseY, 28f));
            W.Punkt("metro_peron", "M1 Nabrzeze peron",
                new Vector3(Stops[0] + 12f, PlatformY, 28f));
            W.Punkt("metro_pociag", "M1 Nabrzeze wagon",
                new Vector3(Stops[0], PlatformY, 29.7f));
            Debug.Log("BRAXHORN_METRO_M1_3 nowa_linia=" + RouteMinX + ".." + RouteMaxX +
                " 3_stacje=" + string.Join(" / ", Names) +
                " wejscia=" + Stops.Length + " wyciecia=" + opened +
                " siatki=" + meshes + " pociagi=" + trains + " wagony=" + cars +
                " stare_grupy_zdiete=" + removed.Count + " ostrzezenia=" + Problems.Count +
                " (wymagana kontrola geometrii i Android)");
        }
        catch (Exception e)
        {
            if (line) UnityEngine.Object.DestroyImmediate(line.gameObject);
            line = null;
            Warn("Nieudane generowanie metra: " + e);
        }
    }

    static void Materials()
    {
        platform = BraxRegion.Pbr("M1 peron: granit antyposlizgowy", 1.2f,
            new Color(.86f, .86f, .85f), "granite_tile_02", "granite_tile");
        tiles = BraxRegion.Pbr("M1 stopnie: kamien", 1.0f,
            new Color(.78f, .78f, .77f), "granite_tile", "granite_tile_02");
        wall = BraxRegion.Pbr("M1 sciany: wielkoformatowa okladzina", 2.4f,
            new Color(.88f, .89f, .9f), "rectangular_facade_tiles", "concrete_wall_008");
        concrete = BraxRegion.Pbr("M1 tunel: beton konstrukcyjny", 2.4f,
            new Color(.38f, .39f, .40f), "concrete_wall_008", "concrete_floor_02");
        dark = BraxRegion.Pbr("M1 torowisko: beton grafit", 2.4f,
            new Color(.30f, .30f, .31f), "concrete_floor_02", "concrete_wall_008");
        rails = BraxRegion.Fitting("M1 szyny stalowe", new Color(.58f, .59f, .60f), .38f, .9f);
        sleepers = BraxRegion.Fitting("M1 podklady betonowe", new Color(.45f, .46f, .46f), .85f, 0);
        steel = BraxRegion.Fitting("M1 stal balustrad", new Color(.64f, .66f, .68f), .35f, .8f);
        edge = BraxRegion.Fitting("M1 pas ostrzegawczy", new Color(.91f, .76f, .12f), .8f, 0);
        glass = BraxRegion.Fitting("M1 szklo balustrad", new Color(.7f, .8f, .84f, .3f), .25f, .08f);
    }

    static bool StationInterval(float a, float b)
    {
        return Stops.Any(x => b > x - 24.2f && a < x + 24.2f);
    }
    static void TrackAndTunnel()
    {
        for (float x = RouteMinX; x < RouteMaxX; x += 12f)
        {
            float to = Mathf.Min(x + 12f, RouteMaxX);
            W.MM(dark, x, to, -13.6f, -13.15f, 19.5f, 37.2f, true);
            W.MM(concrete, x, to, -13.15f, -4.8f, 19.5f, 19.82f, true);
            W.MM(concrete, x, to, -13.15f, -4.8f, 36.88f, 37.2f, true);
            bool hall = StationInterval(x, to);
            if (hall)
            {
                W.MM(concrete, x, to, -5.13f, -4.83f, 19.5f, 25.25f, true);
                W.MM(concrete, x, to, -5.13f, -4.83f, 30.65f, 37.2f, true);
            }
            else W.MM(concrete, x, to, -5.13f, -4.83f, 19.5f, 37.2f, true);

            foreach (float tz in new[] { TrackSouth, TrackNorth })
            {
                // Jedno nieprzerwane torowisko od zachodniej do wschodniej głowicy.
                W.MM(dark, x, to, -13.12f, -13.06f, tz - 1.45f, tz + 1.45f);
                for (float xx = x; xx < to; xx += .84f)
                    W.MM(sleepers, xx, Mathf.Min(xx + .22f, to),
                        -13.06f, -12.76f, tz - 1.3f, tz + 1.3f);
                foreach (float dz in new[] { -.75f, .75f })
                    W.MM(rails, x, to, -12.76f, RailY, tz + dz - .07f, tz + dz + .07f);
            }
            if (((int)((x - RouteMinX) / 12f)) % 2 == 0)
                foreach (float z in new[] { 20.0f, 36.7f })
                    W.MM(steel, x + 1.0f, Mathf.Min(x + 1.06f, to),
                        -7.15f, -7.07f, z - .03f, z + .03f);
        }
    }

    static void Station(int index)
    {
        float x = Stops[index];
        var group = Group(line, "M1 STACJA " + (index + 1) + " " + Names[index]);
        W.Start(group); W.Wnetrze = true; W.Komorka = 14f;
        // Peron wyspowy, bez nadbudowy na torach, na poziomie każdej stacji.
        W.MM(platform, x - 24f, x + 24f, PlatformY - .25f, PlatformY,
            25.55f, 30.25f, true);
        W.MM(edge, x - 23.7f, x + 23.7f, PlatformY + .021f, PlatformY + .041f,
            25.7f, 25.99f);
        W.MM(edge, x - 23.7f, x + 23.7f, PlatformY + .021f, PlatformY + .041f,
            29.81f, 30.1f);
        // Górna hala połączona z szybem wejścia; nad torami przejście na peron.
        // Strop nie może przeciąć szybu schodów ulicznych. Prawdziwy otwór
        // na całej szerokości pierwszego biegu, bez ślepego podestu.
        W.MM(concrete, x - 11f, x + 11f, -1.95f, -1.72f,
            26.4f, 41.20f, true);
        W.MM(concrete, x - 11f, x - 7.2f, -1.95f, -1.72f,
            41.20f, 44.80f, true);
        W.MM(concrete, x + 7.2f, x + 11f, -1.95f, -1.72f,
            41.20f, 44.80f, true);
        W.MM(concrete, x - 11f, x + 11f, -1.95f, -1.72f,
            44.80f, 45.1f, true);
        // Korytarz biegnący od dolnego podestu schodów ulicznych do schodów na peron.
        // Jeden ciągły korytarz łączy dolną krawędź pierwszego biegu (z=43)
        // z górą drugiego biegu na peron (z=28) — bez urwanego pomostu.
        W.MM(platform, x - 9.5f, x - 6.0f, ConcourseY - .25f, ConcourseY,
            26.50f, 44.50f, true);
        W.MM(wall, x - 9.8f, x - 9.55f, ConcourseY, -1.97f,
            29.65f, 41.1f, true);
        W.MM(wall, x - 5.55f, x - 5.30f, ConcourseY, -1.97f,
            30.0f, 41.1f, true);
        // Portal wejścia od ulicy: linia czołowa schodów przy x+6, z=43.
        W.MM(steel, x + 7.2f, x + 7.38f, StreetY, 3.4f, 41.25f, 41.42f, true);
        W.MM(steel, x + 7.2f, x + 7.38f, StreetY, 3.4f, 44.58f, 44.75f, true);
        W.MM(steel, x + 7.12f, x + 7.48f, 3.18f, 3.42f, 41.25f, 44.75f);
        W.MM(glass, x - 6.2f, x + 6.2f, 2.9f, 2.96f, 41.4f, 44.6f);
        W.MM(wall, x - 6.4f, x + 6.7f, ConcourseY - .15f, StreetY - .15f,
            41.10f, 41.28f, true);
        W.MM(wall, x - 6.4f, x + 6.7f, ConcourseY - .15f, StreetY - .15f,
            44.72f, 44.90f, true);
        W.Wnetrze = false;
        W.Napis("M1  " + Names[index], new Vector3(x + 7.5f, 2.45f, 43f),
            Vector3.right, .29f, Color.white);
        W.Wnetrze = true;

        // Dwa połączone biegi schodów — nie ślepe podesty.
        Stair(x + 6f, x - 6f, StreetY, ConcourseY, 41.65f, 44.35f, 32);
        Stair(x - 6f, x + 11f, ConcourseY, PlatformY, 26.88f, 29.12f, 44);
        // Koniec płyty dokładnie w granicy wycięcia w chodniku (bez Z-fightingu).
        W.MM(tiles, x + 6f, x + 7.05f, StreetY - .14f, StreetY,
            41.65f, 44.35f, true);
        W.MM(tiles, x - 8.4f, x - 6f, ConcourseY - .14f, ConcourseY,
            41.65f, 44.35f, true);

        // Poręcze obu klatek schodowych, poza światłem ruchu Braxa.
        RailsForStairs(x + 6f, x - 6f, StreetY, ConcourseY, 41.58f, 44.42f);
        RailsForStairs(x - 6f, x + 11f, ConcourseY, PlatformY, 26.80f, 29.20f);
        // Oświetlenie liniowe i czytelny kierunek, bez kosztownych cieni dynamicznych.
        for (float tx = x - 20f; tx <= x + 20f; tx += 8f)
            W.MM(steel, tx, tx + 1.25f, -5.43f, -5.39f, 27.7f, 28.2f);
        W.Napis("M1 · " + Names[index], new Vector3(x - 18f, -7.2f, 30.35f),
            Vector3.back, .24f, Color.white);

        W.Punkt("metro_m1_hala_" + index, "M1 hala " + Names[index],
            new Vector3(x - 7f, ConcourseY, 34f));
        W.Punkt("metro_m1_zjazd_" + index, "M1 schody na peron " + Names[index],
            new Vector3(x - 6f, ConcourseY, 28f));
    }

    static void Stair(float xTop, float xBottom, float yTop, float yBottom,
                      float zMin, float zMax, int steps)
    {
        float l = Mathf.Abs(xBottom - xTop);
        if (l < 5f || yBottom >= yTop || steps < 10)
            throw new InvalidOperationException("M1: nieprawidlowa geometria schodow");
        for (int i = 0; i < steps; i++)
        {
            float a = Mathf.Lerp(xTop, xBottom, i / (float)steps);
            float b = Mathf.Lerp(xTop, xBottom, (i + 1f) / steps);
            float yPrev = Mathf.Lerp(yTop, yBottom, i / (float)steps);
            float y = Mathf.Lerp(yTop, yBottom, (i + 1f) / steps);
            W.MM(tiles, Mathf.Min(a, b), Mathf.Max(a, b), y - .055f, y,
                zMin, zMax);
            // Lico podstopnicy — żadnych prześwitów pomiędzy stopniami.
            W.MM(tiles, a - .022f, a + .022f, y, yPrev, zMin, zMax);
        }
        // Jedyna ciągła kolizja na stopniach — pochyła, dopasowana do trasy,
        // bez pudełka zamykającego otwór pomiędzy piętrami.
        W.Rampa(new Vector3(xTop, yTop, (zMin + zMax) * .5f),
            new Vector3(xBottom, yBottom, (zMin + zMax) * .5f), zMax - zMin);
    }

    static void RailsForStairs(float xTop, float xBottom,
                              float yTop, float yBottom, float z0, float z1)
    {
        foreach (float z in new[] { z0, z1 })
        {
            var a = new Vector3(xTop, yTop + 1.12f, z);
            var b = new Vector3(xBottom, yBottom + 1.12f, z);
            W.Rura(steel, a, b, .038f, 8, false);
            for (int i = 0; i <= 7; i++)
            {
                float t = i / 7f;
                var p = Vector3.Lerp(a, b, t);
                W.Rura(steel, p - Vector3.up * .82f, p, .025f, 8, false);
            }
        }
    }

    static Bounds RenderBounds(GameObject go)
    {
        Bounds result = new Bounds(go.transform.position, Vector3.zero);
        bool any = false;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            if (!any) { result = r.bounds; any = true; }
            else result.Encapsulate(r.bounds);
        }
        return any ? result : new Bounds(go.transform.position, Vector3.zero);
    }

    static void AddTrains()
    {
        const string path = "Assets/Source/Claude/Metro/metro_train.glb";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (!source) { Warn("Brak oryginalnego modelu metro_train.glb — bez sztucznej atrapy"); return; }
        for (int n = 0; n < 2; n++)
        {
            int ix = n == 0 ? 0 : 2;
            float z = n == 0 ? TrackNorth : TrackSouth;
            var carRoot = Group(line, "M1 POCIAG " + (n + 1) + " ORYGINAL METRO_TRAIN");
            carRoot.position = new Vector3(Stops[ix], 0f, z);
            var model = (GameObject)UnityEngine.Object.Instantiate(source, carRoot, false);
            model.name = "Metro train — oryginalny model, bez zmiany tekstur";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            var original = RenderBounds(model);
            if (original.size.y <= .05f || Mathf.Min(original.size.x, original.size.z) <= .05f)
            { UnityEngine.Object.DestroyImmediate(carRoot.gameObject); Warn("Model pociagu bez poprawnych bounds"); continue; }
            bool longZ = original.size.z > original.size.x;
            float width = longZ ? original.size.x : original.size.z;
            float uniform = 2.7f * 1.294f / width;
            if (uniform <= .0001f || uniform >= 1000f)
            { UnityEngine.Object.DestroyImmediate(carRoot.gameObject); Warn("Niepoprawna skala oryginalnego pociagu"); continue; }
            model.transform.localScale = Vector3.one * uniform;
            if (longZ) model.transform.localRotation = Quaternion.Euler(0, 90f, 0);
            var fitted = RenderBounds(model);
            if (fitted.size.z > 3.85f || fitted.size.y > 6.5f || fitted.size.x > 48f)
            { UnityEngine.Object.DestroyImmediate(carRoot.gameObject); Warn("Pociag nie miesci sie w skrajni tunelu: " + fitted.size); continue; }
            model.transform.position += new Vector3(carRoot.position.x - fitted.center.x,
                RailY - fitted.min.y, z - fitted.center.z);
            foreach (var c in model.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(c); // tylko bryly dekoracyjne modelu; brak losowej kolizji w przejsciu
            foreach (var r in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                r.sharedMaterials = r.sharedMaterials.Select(BraxRegion.CityMaterial).ToArray();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            var svc = carRoot.gameObject.AddComponent<BraxMetro3StopRide>();
            svc.stationX = Stops.ToArray(); svc.stationNames = Names.ToArray();
            svc.stationIndex = ix; svc.direction = n == 0 ? 1 : -1;
            svc.trackZ = z; svc.platformY = PlatformY;
            svc.boardingZ = n == 0 ? 29.72f : 26.10f;
            svc.riderLocalPosition = new Vector3(0f, PlatformY + .09f, 0f);
            trains++; cars++;
        }
    }

    // Wywołanie po BraxKontrola.Wszystko — daje nazwy kolizji i braków.
    // Wynik jest ostrzeżeniem, nigdy samoczynną blokadą APK.
    public static void KontrolaPrzejscia()
    {
        if (!line) { Debug.LogWarning("BRAXHORN_METRO_M1_3_AUDYT brak nowej linii"); return; }
        Physics.SyncTransforms();
        var player = UnityEngine.Object.FindAnyObjectByType<BraxGymPlayer>();
        float radius = player && player.body ? player.body.radius : .35f;
        float height = player && player.body ? player.body.height : 2.25f;
        int mask = ~((1 << 8) | (1 << 29) | (1 << 31));
        int samples = 0, failures = 0;
        var names = new HashSet<string>();
        void Sample(float x, float y, float z, string area)
        {
            samples++;
            Vector3 foot = new Vector3(x, y, z);
            bool floor = Physics.RaycastAll(foot + Vector3.up * .75f, Vector3.down, 1.15f,
                mask, QueryTriggerInteraction.Ignore).Any(h => h.collider &&
                    h.normal.y > .60f && Mathf.Abs(h.point.y - y) < .24f);
            if (!floor) { failures++; names.Add(area + " BRAK PODLOGI @" + foot.ToString("F1")); }
            foreach (var c in Physics.OverlapCapsule(foot + Vector3.up * (radius + .12f),
                foot + Vector3.up * (height - radius), radius, mask,
                QueryTriggerInteraction.Ignore))
            {
                if (!c || !c.enabled || c.isTrigger ||
                    c.GetComponentInParent<BraxGymPlayer>()) continue;
                failures++;
                if (names.Count < 50) names.Add(area + " " + c.name + " @" + foot.ToString("F1"));
            }
        }
        for (int i = 0; i < Stops.Length; i++)
        {
            float x = Stops[i];
            for (int j = 0; j <= 32; j++)
            {
                float t = j / 32f;
                Sample(Mathf.Lerp(x + 6f, x - 6f, t), Mathf.Lerp(StreetY, ConcourseY, t), 43f,
                    "M1_" + i + "_SCHODY_GORA");
            }
            for (float z = 41f; z >= 30f; z -= .5f)
                Sample(x - 7f, ConcourseY, z, "M1_" + i + "_HALA");
            for (int j = 0; j <= 44; j++)
            {
                float t = j / 44f;
                Sample(Mathf.Lerp(x - 6f, x + 11f, t), Mathf.Lerp(ConcourseY, PlatformY, t), 28f,
                    "M1_" + i + "_SCHODY_PERON");
            }
            for (float xx = x - 21f; xx <= x + 21f; xx += 3f)
                Sample(xx, PlatformY, 29.6f, "M1_" + i + "_PERON");
        }
        string report = "BRAXHORN_METRO_M1_3_AUDYT stacje=" + Stops.Length +
            " odcinki=" + (Stops.Length - 1) + " probki=" + samples +
            " problemy=" + failures + " nazwy=" + string.Join(" | ", names.Take(40));
        if (failures == 0 && trains == 2) Debug.Log(report + " SCENA_GEOMETRIA_OK (Android niepotwierdzony)");
        else Debug.LogWarning(report + " NIEGOTOWE — bez wylaczania fizyki");
    }
}
