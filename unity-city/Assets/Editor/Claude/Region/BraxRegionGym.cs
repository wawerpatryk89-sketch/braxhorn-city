using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// BRAXHORN CITY — siłownia (wizytówka): strefa judo z czerwonym tatami (Poly Haven tatami_mat),
// zegar ścienny, gaśnica, kosz, roślina przy wyjściu. Stawiane tylko w wolnym miejscu hali
// (kolizje sprawdzane po odbudowie sali i sprzętu), ring, worki i maty treningowe bez zmian.
// Wywołanie: BraxGymBuild.BuildInternal po BraxGymReferenceEquipment.Apply.
public static class BraxRegionGym
{
    [Serializable] sealed class Report { public bool tatami; public Vector3 tatamiAt; public int props; public List<string> notes = new List<string>(); }

    public static void Apply(GameObject gym, BraxGymPlayer runtime)
    {
        if (!BraxRegion.Enabled) return;
        var rep = new Report();
        try { Build(gym, rep); }
        catch (Exception e) { rep.notes.Add("BŁĄD: " + e.Message); Debug.LogError("BRAXHORN_CLAUDE_GYM " + e); }
        try { NicerBags(runtime, rep); }
        catch (Exception e) { rep.notes.Add("worki: BŁĄD " + e.Message); Debug.LogError("BRAXHORN_CLAUDE_GYM worki " + e); }
        try { PremiumBags(gym, rep); }
        catch (Exception e) { rep.notes.Add("worki: BŁĄD " + e.Message); Debug.LogError("BRAXHORN_CLAUDE_GYM worki " + e); }
        try { SprzetBraxa(gym); }
        catch (Exception e) { Debug.LogWarning("BRAXHORN_SILOWNIA_WYPOSAZENIE DO POPRAWKI " + e); }
        Directory.CreateDirectory(BraxRegion.Out);
        File.WriteAllText(BraxRegion.Out + "/gym.json", JsonUtility.ToJson(rep, true));
        Debug.Log("BRAXHORN_CLAUDE_GYM " + JsonUtility.ToJson(rep));
    }

    // Worki (decyzja właściciela: „worki ładniejsze”). Worek z Blendera to matowa, prawie czarna skóra
    // (#171a1b, roughness .69, ziarno .38) — na telefonie czarna plama bez kształtu. Tu: ciemny grafit ze
    // sheenem (roughness .42), wyraźne ziarno skóry (normal ×1.0), wzmocnienia ciemniejsze z lekkim połyskiem,
    // chromowane okucia i łańcuch. Materiały zostają na shaderze importu (reagują na światła sali, warstwa 10).
    // 31.10 (właściciel): gruszka na stojaku (reflex bag) i speed bag na platformie — premium, skóra z połyskiem, chrom. Z kodu.
    static void PremiumBags(GameObject gym, Report rep)
    {
        var root = new GameObject("CLAUDE siłownia: gruszki premium").transform;
        root.SetParent(gym ? gym.transform.parent : null, false); root.position = Vector3.zero;
        var leather = BraxRegion.Pbr("Gruszka: skóra czarna z połyskiem", .6f, new Color(.12f, .12f, .13f), "leather_red_03", "leather_red_02", "leather_white");
        if (leather) { if (leather.HasProperty("_Roughness")) leather.SetFloat("_Roughness", .22f); if (leather.HasProperty("_Glossiness")) leather.SetFloat("_Glossiness", .78f); }
        var chrome = BraxRegion.Fitting("Gruszka: chrom", new Color(.85f, .86f, .88f), .18f, 1f);
        var steel = BraxRegion.Fitting("Gruszka: stal czarna", new Color(.1f, .1f, .11f), .5f, .6f);
        var wood = BraxRegion.Pbr("Speed bag: platforma (sklejka)", 1f, null, "plank_flooring_02", "wood_floor", "wooden_planks");
        Physics.SyncTransforms();
        int placed = 0;
        // wolne miejsca przy ścianie za ramą worków (x 10..16, z -7..7), potem gdziekolwiek na podłodze hali
        var spots = new List<Vector3>();
        for (float z = -6f; z <= 6f; z += 2.5f) for (float x = 16.5f; x >= 9.5f; x -= 1.5f) spots.Add(new Vector3(x, 0, z));
        for (float z = 7.5f; z >= -7.5f; z -= 1.5f) for (float x = -18f; x <= 9f; x += 1.5f) spots.Add(new Vector3(x, 0, z));
        int reflex = 0, speed = 0;
        foreach (var sp in spots)
        {
            if (reflex >= 2 && speed >= 2) break;
            RaycastHit h; if (!BraxRegion.Ground(sp.x, sp.z, -.5f, .6f, out h) || h.point.y > .2f) continue;
            bool wantReflex = reflex < 2;
            float need = wantReflex ? 1.6f : 1.4f;
            if (!BraxRegion.Free(new Vector3(sp.x, h.point.y + 1.3f, sp.z), new Vector3(need, 2.4f, need))) continue;
            var c = new Vector3(sp.x, h.point.y, sp.z);
            if (wantReflex)
            {
                // podstawa (dysk), słup chrom, sprężyna, gruszka skórzana (elipsoida z dwóch kul)
                Cyl(root, "Gruszka: podstawa", c + Vector3.up * .05f, .42f, .1f, steel);
                Cyl(root, "Gruszka: obciążenie", c + Vector3.up * .15f, .3f, .1f, steel);
                Cyl(root, "Gruszka: słup", c + Vector3.up * .85f, .035f, 1.3f, chrome);
                Cyl(root, "Gruszka: sprężyna", c + Vector3.up * 1.6f, .05f, .2f, chrome);
                Cyl(root, "Gruszka: trzpień", c + Vector3.up * 1.78f, .025f, .16f, chrome);
                Sph(root, "Gruszka: skóra dół", c + Vector3.up * 1.95f, new Vector3(.3f, .34f, .3f), leather);
                Sph(root, "Gruszka: skóra góra", c + Vector3.up * 2.12f, new Vector3(.2f, .26f, .2f), leather);
                var col = new GameObject("Gruszka: kolizja").AddComponent<CapsuleCollider>(); col.transform.SetParent(root, false); col.transform.position = c + Vector3.up * 1.1f; col.radius = .22f; col.height = 2.3f;
                reflex++;
            }
            else
            {
                // speed bag: wspornik na słupie przy miejscu, platforma okrągła z drewna, obrotowy sworzeń, mała gruszka
                Cyl(root, "Speed bag: podstawa", c + Vector3.up * .05f, .35f, .1f, steel);
                Cyl(root, "Speed bag: słup", c + Vector3.up * 1.25f, .04f, 2.4f, steel);
                Cyl(root, "Speed bag: platforma", c + Vector3.up * 2.45f, .6f, .06f, wood ? wood : steel);
                Cyl(root, "Speed bag: sworzeń", c + Vector3.up * 2.37f, .02f, .1f, chrome);
                Sph(root, "Speed bag: gruszka", c + Vector3.up * 2.14f, new Vector3(.17f, .26f, .17f), leather);
                var col = new GameObject("Speed bag: kolizja").AddComponent<CapsuleCollider>(); col.transform.SetParent(root, false); col.transform.position = c + Vector3.up * 1.2f; col.radius = .2f; col.height = 2.5f;
                speed++;
            }
            placed++;
        }
        rep.notes.Add("gruszki premium: reflex " + reflex + ", speed bag " + speed);
    }
    static void Cyl(Transform root, string name, Vector3 center, float radius, float height, Material m)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name = name; g.transform.SetParent(root, false);
        g.transform.position = center; g.transform.localScale = new Vector3(radius * 2f, height * .5f, radius * 2f);
        UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        var r = g.GetComponent<MeshRenderer>(); if (m) r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.On;
    }
    static void Sph(Transform root, string name, Vector3 center, Vector3 size, Material m)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); g.name = name; g.transform.SetParent(root, false);
        g.transform.position = center; g.transform.localScale = size;
        UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        var r = g.GetComponent<MeshRenderer>(); if (m) r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.On;
    }

    static void NicerBags(BraxGymPlayer runtime, Report rep)
    {
        if (!runtime || runtime.bags == null) return;
        var swapped = new Dictionary<Material, Material>();
        int renderers = 0;
        foreach (var pivot in runtime.bags)
        {
            if (!pivot) continue;
            foreach (var r in pivot.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = r.sharedMaterials; bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i]; if (!m) continue;
                    Material o;
                    if (!swapped.TryGetValue(m, out o))
                    {
                        string n = m.name.ToLowerInvariant();
                        if (n.Contains("matte black leather")) o = Tuned(m, "Worek: skóra grafitowa z połyskiem", new Color(.13f, .14f, .15f), .24f, .8f);   // 31.10: skóra ma się błyszczeć
                        else if (n.Contains("reinforced leather")) o = Tuned(m, "Worek: wzmocnienia i taśmy", new Color(.09f, .10f, .10f), .52f, .9f);
                        else if (n.Contains("suspension hardware")) o = Tuned(m, "Worek: chromowane okucia", new Color(.80f, .81f, .82f), .22f, 0f);
                        else if (n.Contains("stitching")) o = Tuned(m, "Worek: przeszycia", new Color(.52f, .55f, .50f), .80f, 0f);
                        else o = m;
                        swapped[m] = o;
                    }
                    if (o != m) { mats[i] = o; changed = true; }
                }
                if (changed) { r.sharedMaterials = mats; renderers++; }
            }
        }
        rep.notes.Add("worki: materiałów " + swapped.Count(kv => kv.Key != kv.Value) + ", rendererów " + renderers);
    }

    static Material Tuned(Material src, string name, Color baseColour, float roughness, float normalScale)
    {
        var m = new Material(src) { name = name };
        if (m.HasProperty("baseColorFactor")) m.SetColor("baseColorFactor", baseColour);
        if (m.HasProperty("roughnessFactor")) m.SetFloat("roughnessFactor", roughness);
        if (normalScale > 0 && m.HasProperty("normalTexture_scale")) m.SetFloat("normalTexture_scale", normalScale);
        if (m.HasProperty("_Color")) m.SetColor("_Color", baseColour);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 1f - roughness);
        if (m.HasProperty("_Roughness")) m.SetFloat("_Roughness", roughness);
        Directory.CreateDirectory(BraxRegion.Out + "/Materials");
        BraxZapisAssetu.Utworz(m, BraxRegion.Out + "/Materials/worek-" + name.Replace(' ', '_').Replace(':', '_') + ".mat");
        return m;
    }

    static void Build(GameObject gym, Report rep)
    {
        Physics.SyncTransforms();
        var root = new GameObject("CLAUDE siłownia: strefa judo i dodatki").transform;
        root.SetParent(gym ? gym.transform.parent : null, false);
        root.position = Vector3.zero;

        // 1) Strefa judo 6×6 m: szukamy wolnego miejsca w hali (x -19..10, z -8..8), z dala od ringu i worków.
        var tatami = BraxRegion.Pbr("Siłownia: tatami judo (czerwone)", 1.0f, new Color(.80f, .16f, .14f), "tatami_mat", "rubber_tiles", "leather_white");
        var tatamiGreen = BraxRegion.Pbr("Siłownia: tatami judo (zielone pole)", 1.0f, new Color(.25f, .55f, .22f), "tatami_mat", "rubber_tiles", "leather_white");
        var edge = BraxRegion.Fitting("Siłownia: listwa krawędziowa tatami", new Color(.08f, .08f, .08f), .7f, 0f);
        Vector3? spot = null;
        for (float z = 7.5f; z >= -7.5f && !spot.HasValue; z -= 1.5f)
            for (float x = -18f; x <= 9f; x += 1.5f)
            {
                RaycastHit h;
                if (!BraxRegion.Ground(x, z, -.5f, .6f, out h)) continue;
                if (h.point.y > .2f) continue;                                     // tylko podłoga hali
                if (!BraxRegion.Free(new Vector3(x, h.point.y + 1.35f, z), new Vector3(7.2f, 2.0f, 7.2f))) continue;
                if (Mathf.Abs(x - (-9f)) < 8.5f && Mathf.Abs(z - (-5.1f)) < 8.8f) continue;   // ring
                if (x > 10f) continue;                                               // rama worków przy x=14
                spot = new Vector3(x, h.point.y, z); break;
            }
        if (spot.HasValue)
        {
            var c = spot.Value;
            const float S = 6f, T = .06f;
            BraxRegion.Box("Siłownia: tatami pole walki", root, c + new Vector3(0, T * .5f + .005f, 0), new Vector3(S - 1.2f, T, S - 1.2f), tatami, true, 1f);
            // obwódka zielona (strefa bezpieczeństwa) jak na prawdziwym tatami
            BraxRegion.Box("Siłownia: tatami strefa N", root, c + new Vector3(0, T * .5f, S * .5f - .3f), new Vector3(S, T, .6f), tatamiGreen, true, 1f);
            BraxRegion.Box("Siłownia: tatami strefa S", root, c + new Vector3(0, T * .5f, -S * .5f + .3f), new Vector3(S, T, .6f), tatamiGreen, true, 1f);
            BraxRegion.Box("Siłownia: tatami strefa W", root, c + new Vector3(-S * .5f + .3f, T * .5f, 0), new Vector3(.6f, T, S - 1.2f), tatamiGreen, true, 1f);
            BraxRegion.Box("Siłownia: tatami strefa E", root, c + new Vector3(S * .5f - .3f, T * .5f, 0), new Vector3(.6f, T, S - 1.2f), tatamiGreen, true, 1f);
            foreach (var (dx, dz, sx, sz) in new[] { (0f, S * .5f + .02f, S + .08f, .04f), (0f, -S * .5f - .02f, S + .08f, .04f), (S * .5f + .02f, 0f, .04f, S + .08f), (-S * .5f - .02f, 0f, .04f, S + .08f) })
                BraxRegion.Box("Siłownia: listwa tatami", root, c + new Vector3(dx, T * .5f, dz), new Vector3(sx, T + .004f, sz), edge);
            rep.tatami = true; rep.tatamiAt = c;
        }
        else rep.notes.Add("brak wolnego miejsca 7×7 m na tatami");

        // 2) Dodatki z Poly Haven: zegar nad wyjściem, gaśnica, kosz, roślina przy drzwiach (drzwi ok. (6, 11.4)).
        var clock = BraxRegion.Model("wall_clock");
        var ext = BraxRegion.Model("korean_fire_extinguisher_01");
        var bin = BraxRegion.Model("metal_trash_can");
        var pot = BraxRegion.Model("potted_plant_04", "potted_plant_02");
        if (clock)
        {
            var o = (GameObject)UnityEngine.Object.Instantiate(clock, root, false);
            o.name = "Siłownia: zegar"; o.transform.position = Vector3.zero;
            o.transform.rotation = Quaternion.Euler(0, 180, 0) * o.transform.rotation;
            var b = BraxRegion.Bounds(o); float big = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (big > .5f && big > .001f) { o.transform.localScale *= .5f / big; b = BraxRegion.Bounds(o); }
            RaycastHit wall;
            // ściana za wyjściem: promień z hali w stronę drzwi (+Z)
            if (Physics.Raycast(new Vector3(2.5f, 2.9f, 6f), Vector3.forward, out wall, 8f, ~0, QueryTriggerInteraction.Ignore))
            { o.transform.position = wall.point + Vector3.back * .06f - b.center + new Vector3(0, 0, 0); rep.props++; }
            else UnityEngine.Object.DestroyImmediate(o);
        }
        foreach (var (model, label, at, h) in new[] { (ext, "Siłownia: gaśnica", new Vector3(8.8f, 0, 10.6f), .7f), (bin, "Siłownia: kosz", new Vector3(3.2f, 0, 10.6f), .95f), (pot, "Siłownia: roślina przy wyjściu", new Vector3(9.6f, 0, 10.2f), 1.6f) })
        {
            if (!model) continue;
            RaycastHit g;
            if (!BraxRegion.Ground(at.x, at.z, -.5f, .6f, out g) || g.point.y > .2f) continue;
            if (!BraxRegion.Free(g.point + Vector3.up * .6f, new Vector3(.9f, 1.1f, .9f))) continue;
            if (BraxRegion.Put(root, model, label, g.point, 0f, 1f, true, h)) rep.props++;
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    // SPRZĘT SIŁOWNI BRAXA (właściciel 02:26: „nic tam nie ma, a sprzętu dużo ściągnąłem”). Tylko DOKŁADANE w wolnym miejscu
    // hali (BraxRegion.Free z zapasem 0.9 m na przejścia), ring, worki, lustra, drzwi, punkty treningu i strefa chroniona bez
    // zmian. Modele z paczki miasto_silownia_metro (klatka do przysiadów z gryfem, atlas, bieżnie, ławki — × K, kolizja z
    // obrysu); z kodu (z detalem, bez klocków): stojak hantli z 10 parami (gryfy z radełkiem, głowice sześciokątne), drzewko
    // na talerze z talerzami, stojak na gryfy, ławka płaska; pod wolnymi ciężarami podłoga gumowa (osobne płyty 2 cm z fugą).
    // Log BRAXHORN_SILOWNIA_WYPOSAZENIE.
    static void SprzetBraxa(GameObject gym)
    {
        var root = new GameObject("CLAUDE siłownia: sprzęt Braxa (stojaki, ławki, hantle, bieżnie, guma)").transform;
        root.SetParent(gym ? gym.transform.parent : null, false); root.position = Vector3.zero;
        float K = BraxMiastoA.K;
        var stal = BraxRegion.Fitting("Sprzęt: stal czarna malowana", new Color(.07f, .07f, .08f), .45f, .6f);
        var chrom = BraxRegion.Fitting("Sprzęt: chrom (gryfy)", new Color(.82f, .83f, .85f), .15f, 1f);
        var guma = BraxRegion.Fitting("Sprzęt: guma (talerze, głowice, podłoga)", new Color(.06f, .06f, .065f), .85f, 0f);
        var gumaPodl = BraxRegion.Pbr("Sprzęt: podłoga gumowa (płyty)", 1f, new Color(.16f, .16f, .17f), "rubber_tiles", "asphalt_02", "concrete_floor_02");
        var skora = BraxRegion.Fitting("Sprzęt: tapicerka ławki (czarna skóra)", new Color(.05f, .05f, .055f), .45f, 0f);
        var czerw = BraxRegion.Fitting("Sprzęt: czerwień marki (opaski talerzy)", new Color(.72f, .07f, .06f), .5f, 0f);
        Physics.SyncTransforms();
        var spots = new List<Vector3>();
        for (float z = -14.5f; z <= 9.5f; z += 1f) for (float x = -17.5f; x <= 17.5f; x += 1f) spots.Add(new Vector3(x, 0, z));
        var opis = new List<string>(); int modele = 0, kod = 0;
        bool Miejsce(float sx, float sz, float h, out Vector3 c, out float yaw)
        {
            c = default; yaw = 0f;
            foreach (var sp in spots)
                foreach (float yw in new[] { 0f, 90f })
                {
                    float ax = yw == 0f ? sx : sz, az = yw == 0f ? sz : sx;
                    RaycastHit h0; if (!BraxRegion.Ground(sp.x, sp.z, -.5f, .6f, out h0) || h0.point.y > .2f) continue;
                    if (!BraxRegion.Free(new Vector3(sp.x, h0.point.y + h * .5f + .12f, sp.z), new Vector3(ax + 1.8f, h, az + 1.8f))) continue;
                    // podłoga pod całym obrysem (bez wystawania poza halę)
                    bool ok = true;
                    foreach (var (dx, dz) in new[] { (-.5f, -.5f), (.5f, -.5f), (-.5f, .5f), (.5f, .5f) })
                    { RaycastHit h1; if (!BraxRegion.Ground(sp.x + dx * ax, sp.z + dz * az, -.5f, .6f, out h1) || Mathf.Abs(h1.point.y - h0.point.y) > .03f) { ok = false; break; } }
                    if (!ok) continue;
                    c = new Vector3(sp.x, h0.point.y, sp.z); yaw = yw; return true;
                }
            return false;
        }
        void Kolizja(string n, Bounds b) { var g = new GameObject(n); g.transform.SetParent(root, false); g.transform.position = b.center; g.AddComponent<BoxCollider>().size = b.size; Physics.SyncTransforms(); }
        void Model(string plik, string nazwa, float sx, float sz, float h)
        {
            var m = BraxMiastoAssety.Jest("miasto_silownia_metro") ? BraxMiastoAssety.Model("miasto_silownia_metro", plik) : null;
            if (!m) { opis.Add(nazwa + ": brak paczki"); return; }
            if (!Miejsce(sx, sz, h, out var c, out float yaw)) { opis.Add(nazwa + ": brak miejsca"); return; }
            var o = BraxMiastoAssety.Postaw(root, m, "Siłownia Braxa: " + nazwa + " (model)", c, yaw);
            var b = BraxMiastoAssety.Granice(o); o.transform.position += new Vector3(c.x - b.center.x, c.y - b.min.y, c.z - b.center.z);
            Kolizja("kolizja: " + nazwa, BraxMiastoAssety.Granice(o)); modele++; opis.Add(nazwa + " @" + c.ToString("F1"));
        }
        // podłoga gumowa pod strefą wolnych ciężarów: płyty 1 × 1 m z fugą 4 mm (osobne bryły 2 cm)
        if (Miejsce(5.2f, 4.2f, 2.6f, out var gc, out float gy))
        {
            float ax = gy == 0f ? 5.2f : 4.2f, az = gy == 0f ? 4.2f : 5.2f;
            for (float x = -ax * .5f; x < ax * .5f - .01f; x += 1f) for (float z = -az * .5f; z < az * .5f - .01f; z += 1f)
                BraxRegion.Box("Siłownia Braxa: płyta gumowa", root, gc + new Vector3(x + .5f, .01f, z + .5f), new Vector3(.996f, .02f, .996f), gumaPodl, false, 1f);
            var gb = new Bounds(gc + Vector3.up * .01f, new Vector3(ax, .02f, az)); Kolizja("kolizja: podłoga gumowa (krawędź)", gb);
            opis.Add("guma " + ax + "×" + az + " @" + gc.ToString("F1"));
        }
        Model("silownia_klatka", "klatka do przysiadów z gryfem 1", 3.2f, 2.4f, 2.8f);
        Model("silownia_klatka", "klatka do przysiadów z gryfem 2", 3.2f, 2.4f, 2.8f);
        Model("silownia_atlas", "atlas wielofunkcyjny", 2.8f, 2.5f, 2.6f);
        Model("silownia_bieznia", "bieżnia 1", 1.1f, 2.7f, 1.8f);
        Model("silownia_bieznia", "bieżnia 2", 1.1f, 2.7f, 1.8f);
        Model("silownia_lawka", "ławka skośna", 1.7f, .8f, 1.3f);
        // stojak hantli z 10 parami (z kodu)
        if (Miejsce(2.6f, .8f, 1.0f, out var hc, out float hy))
        {
            var q = Quaternion.Euler(0, hy, 0); var r = q * Vector3.right; var f = q * Vector3.forward;
            foreach (float s in new[] { -1.2f, 1.2f }) foreach (float d in new[] { -.3f, .3f }) BraxRegion.Box("Stojak hantli: noga", root, hc + r * s + f * d + Vector3.up * .45f, new Vector3(.06f, .9f, .06f), stal, false, 1f);
            for (int p2 = 0; p2 < 2; p2++)
            {
                float y = .5f + p2 * .32f;
                foreach (float d in new[] { -.25f, .25f }) Walec(root, "Stojak hantli: belka", hc + f * d + Vector3.up * y, r, 2.45f, .03f, stal);
                for (int i = 0; i < 5; i++)
                {
                    float s = -1.0f + i * .5f, w = .03f + (p2 * 5 + i) * .003f;
                    var c = hc + r * s + Vector3.up * (y + .09f + w * .3f);
                    Walec(root, "Hantla: gryf", c, f, .34f, .016f, chrom);
                    foreach (float d in new[] { -.13f, .13f }) Walec(root, "Hantla: głowica", c + f * d, f, .09f, .065f + w, guma, 6);
                }
            }
            Kolizja("kolizja: stojak hantli", new Bounds(hc + Vector3.up * .5f, hy == 0f ? new Vector3(2.6f, 1f, .8f) : new Vector3(.8f, 1f, 2.6f)));
            kod++; opis.Add("stojak hantli 10 par @" + hc.ToString("F1"));
        }
        // drzewko na talerze z talerzami i stojak na gryfy
        if (Miejsce(1.6f, .8f, 1.8f, out var tc, out float ty))
        {
            var r = Quaternion.Euler(0, ty, 0) * Vector3.right;
            var d0 = tc - r * .45f;
            Walec(root, "Drzewko: podstawa", d0 + Vector3.up * .03f, Vector3.up, .06f, .28f, stal, 12);
            Walec(root, "Drzewko: słup", d0 + Vector3.up * .7f, Vector3.up, 1.3f, .035f, stal, 10);
            float[] rr = { .225f, .225f, .19f, .16f, .13f };
            for (int i = 0; i < rr.Length; i++)
            {
                float y = .35f + (i / 2) * .45f; var dir = (i % 2 == 0 ? 1f : -1f) * (Quaternion.Euler(0, ty, 0) * Vector3.forward);
                Walec(root, "Drzewko: trzpień", d0 + Vector3.up * y + dir * .12f, dir, .24f, .02f, stal, 8);
                Walec(root, "Talerz", d0 + Vector3.up * y + dir * .14f, dir, .05f, rr[i], guma, 22);
                Walec(root, "Talerz: opaska", d0 + Vector3.up * y + dir * .168f, dir, .006f, rr[i] * .55f, czerw, 18);
            }
            var g0 = tc + r * .45f;
            Walec(root, "Stojak gryfów: podstawa", g0 + Vector3.up * .05f, Vector3.up, .1f, .25f, stal, 12);
            for (int i = 0; i < 3; i++) { var a = Quaternion.Euler(0, i * 120f, 0) * Vector3.forward * .1f; Walec(root, "Gryf olimpijski (pion)", g0 + a + Vector3.up * 1.1f, Vector3.up, 2.2f, .015f, chrom, 8); }
            Kolizja("kolizja: drzewko i stojak gryfów", new Bounds(tc + Vector3.up * .9f, ty == 0f ? new Vector3(1.6f, 1.8f, .8f) : new Vector3(.8f, 1.8f, 1.6f)));
            kod++; opis.Add("drzewko talerzy + stojak gryfów @" + tc.ToString("F1"));
        }
        // ławka płaska (z kodu): rama, nogi, tapicerka
        if (Miejsce(1.3f, .5f, .6f, out var lc, out float ly))
        {
            var q = Quaternion.Euler(0, ly, 0); var r = q * Vector3.right;
            bool wx = ly == 0f;
            BraxRegion.Box("Ławka płaska: tapicerka", root, lc + Vector3.up * .5f, wx ? new Vector3(1.2f, .09f, .32f) : new Vector3(.32f, .09f, 1.2f), skora, false, 1f);
            foreach (float s in new[] { -.5f, .5f }) BraxRegion.Box("Ławka płaska: noga", root, lc + r * s + Vector3.up * .23f, wx ? new Vector3(.08f, .46f, .42f) : new Vector3(.42f, .46f, .08f), stal, false, 1f);
            Walec(root, "Ławka płaska: rama", lc + Vector3.up * .42f, r, 1.1f, .035f, stal, 10);
            Kolizja("kolizja: ławka płaska", new Bounds(lc + Vector3.up * .28f, ly == 0f ? new Vector3(1.3f, .55f, .5f) : new Vector3(.5f, .55f, 1.3f)));
            kod++; opis.Add("ławka płaska @" + lc.ToString("F1"));
        }
        Debug.Log("BRAXHORN_SILOWNIA_WYPOSAZENIE modele=" + modele + " z_kodu=" + kod + " ring=bez_zmian worki=bez_zmian lustra=bez_zmian [" + string.Join("; ", opis) + "]");
    }

    static void Walec(Transform p, string name, Vector3 c, Vector3 os, float dl, float r, Material m, int boki = 16)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name = name;
        UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(p, false); g.transform.position = c;
        g.transform.rotation = Quaternion.FromToRotation(Vector3.up, os.normalized);
        g.transform.localScale = new Vector3(r * 2f, dl * .5f, r * 2f);
        g.GetComponent<MeshRenderer>().sharedMaterial = m;
    }
}
