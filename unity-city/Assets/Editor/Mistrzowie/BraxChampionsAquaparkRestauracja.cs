using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// AQUAPARK BRAXHORN - RESTAURACJA LAGUNA i strefy wypoczynku (owner 16.11 08:13: "ma być bogate wyposażenie, wypoczynek,
// sofy obite skórą w środku, restauracja w basenie ... perfekt"). Real Poly Haven CC0 furniture at real size (the region's
// prepared models in Source/Mistrzowie/ArenaLobby and /HotelSobczak - the same files the hotel and the arena use):
//   * RESTAURACJA LAGUNA in the lobby's north half (x -14..14, z -77..-64, behind the reception's black marble wall, the
//     glass to the lagoon): a bar with a quartz top and a walnut front, eight bar stools, a back bar of walnut and quartz with
//     shelves and vases, eight four-person tables with dining chairs, cognac leather banquettes along both side walls with
//     tables for two, pendant lamps over every table, plants; a glass door to the terrace;
//   * the TERRACE on the deck in front of it: eight table sets with chairs under four parasols, the walk to the lagoon free;
//   * the lobby's entrance lounges and a lounge in the jacuzzi hall: leather sofas, leather armchairs, round coffee tables.
// Every seat is a BraxSeat (you sit on it); tables, counters and sofas have colliders.
public static partial class BraxChampionsAquapark
{
    public static int PhModels, PhMissing, Tables;
    const string PA = "ArenaLobby/", PT = "HotelSobczak/";
    static readonly string MSofa = PA + "sofa_02/sofa_02_2k.gltf", MArm = PA + "modern_arm_chair_01/modern_arm_chair_01_2k.gltf",
        MPlant = PA + "potted_plant_01/potted_plant_01_1k.gltf", MPlantLow = PA + "potted_plant_02/potted_plant_02_1k.gltf",
        MBrass = PA + "brass_vase_01/brass_vase_01_1k.gltf", MCoffee = PT + "coffee_table_round_01/coffee_table_round_01_2k.gltf",
        MChair = PT + "dining_chair_02/dining_chair_02_2k.gltf", MStool = PT + "bar_chair_round_01/bar_chair_round_01_1k.gltf",
        MLamp = PT + "modern_ceiling_lamp_01/modern_ceiling_lamp_01_1k.gltf", MSet = PT + "outdoor_table_chair_set_01/outdoor_table_chair_set_01_1k.gltf",
        MVase1 = PT + "ceramic_vase_01/ceramic_vase_01_1k.gltf", MVase2 = PT + "ceramic_vase_02/ceramic_vase_02_1k.gltf",
        MLounge = PT + "mid_century_lounge_chair/mid_century_lounge_chair_2k.gltf";

    // a Poly Haven model at real size; `at` nominal: its floor point, or (top) the point its top hangs from; yaw turns its front (+z)
    static GameObject PH(string path, Vector3 at, float yaw, string label, bool collide, bool top = false, bool centre = false, float mul = 1f)
    {
        string full = "Assets/Source/Mistrzowie/" + path;
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(full);
        if (!src) { BraxBuildQuality.Warn("AQUAPARK_PH_MODEL_MISSING " + full + " (prepare_arena_lobby_models.py / prepare_hotel_sobczak_models.py)"); PhMissing++; return null; }
        var o = BraxChampionsModel.Postaw(models, src, "AQUAPARK | " + label + " | Poly Haven CC0", at * K, yaw, BraxChampionsRealSize.Scale(full) * mul, true, true);
        if (!o) { PhMissing++; return null; }
        var rs = o.GetComponentsInChildren<Renderer>(true);
        if (rs.Length > 0)
        {
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            float want = at.y * K, have = centre ? b.center.y : top ? b.max.y : b.min.y;
            if (Mathf.Abs(want - have) > 1e-4f) { o.transform.position += Vector3.up * (want - have); b.center += Vector3.up * (want - have); }
            if (collide)
            {
                var g = new GameObject(label + " | kolizja"); g.transform.SetParent(cols, false); g.transform.position = b.center;
                g.AddComponent<BoxCollider>().size = b.size * .9f; Colliders++;
            }
        }
        PhModels++;
        return o;
    }

    static void Seat(string label, Vector3 at, float yaw, bool lie = false)
    {
        var g = new GameObject(label.ToLowerInvariant() + " " + Seats); g.transform.SetParent(seats, false);
        g.transform.localPosition = at * K; g.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        var s = g.AddComponent<BraxSeat>(); s.label = label; s.lie = lie; s.surfaceY = at.y * K; Seats++;
    }

    // a leather sofa (Poly Haven sofa_02, 2.2 m) facing `yaw`, three places
    static void LeatherSofa(float x, float z, float yaw, string label)
    {
        if (!PH(MSofa, V(x, FFL, z), yaw, label, true)) return;
        var f = Quaternion.Euler(0, yaw, 0);
        for (int k = -1; k <= 1; k++) Seat("SOFA", V(x, FFL + .44f, z) + f * new Vector3(k * .66f, 0, .05f), yaw);
    }
    static void LeatherArmchair(float x, float z, float yaw, string label)
    {
        if (PH(MArm, V(x, FFL, z), yaw, label, true)) Seat("FOTEL", V(x, FFL + .44f, z) + Quaternion.Euler(0, yaw, 0) * new Vector3(0, 0, .05f), yaw);
    }

    static void Restaurant()
    {
        var walnut = Inside(Tex("restaurant_walnut", "smoked_walnut_veneer", 1.0f, Color.white), 1.0f);
        var cognac = Inside(BraxChampionsFoundation.LibraryLeather("aq_restaurant_cognac_leather", new Color(3.9f, 2.1f, 1.08f)), 1.0f);
        // ---- the bar: counter z -75.6..-74.9 (quartz top with a 5 cm overhang, walnut front, LED line under the top),
        // the back bar on the marble wall (walnut cabinets, quartz top, two walnut shelves with vases), the aisle .9 m
        BL(walnut, -5.5f, 5.5f, FFL, FFL + 1.04f, -75.55f, -74.95f, true);
        BL(quartz, -5.6f, 5.6f, FFL + 1.04f, FFL + 1.10f, -75.6f, -74.85f);
        BL(led, -5.5f, 5.5f, FFL + 1.0f, FFL + 1.02f, -74.86f, -74.84f);
        BL(walnut, -6.4f, 6.4f, FFL, FFL + .90f, -77.15f, -76.55f, true);
        BL(quartz, -6.45f, 6.45f, FFL + .90f, FFL + .94f, -77.15f, -76.5f);
        foreach (float y in new[] { 1.45f, 1.95f }) BL(walnut, -6.0f, 6.0f, FFL + y, FFL + y + .04f, -77.15f, -76.85f);
        BL(led, -6.0f, 6.0f, FFL + 2.40f, FFL + 2.42f, -77.15f, -77.10f);
        int v = 0;
        for (float x = -5.4f; x <= 5.41f; x += 1.2f)
            foreach (float y in new[] { 1.49f, 1.99f }) PH((v++ % 3) == 0 ? MBrass : (v % 3) == 1 ? MVase1 : MVase2, V(x + ((v % 2) * .3f), FFL + y, -77.0f), v * 37f, "bar: wazon", false);
        BraxChampionsLetters3D.Text(m, BraxChampionsHarbour.SignFace, BraxChampionsHarbour.SignDark, "RESTAURACJA LAGUNA", V(0f, FFL + 2.95f, -77.17f), 0f, .34f, .05f, .12f, 9f);
        for (float x = -4.9f; x <= 4.91f; x += 1.4f) { if (PH(MStool, V(x, FFL, -74.35f), 180f, "bar: hoker", true)) Seat("HOKER", V(x, FFL + .75f, -74.35f), 180f); }
        // ---- the dining room: two rows of four-person tables (walnut 1.2 x .8 on a black pedestal), a 4 m aisle in the middle
        // from the bar to the terrace door, dining chairs two a side, a pendant lamp over each table
        foreach (float tz in new[] { -71.4f, -67.4f })
            foreach (float tx in new[] { -9.6f, -5.4f, 5.4f, 9.6f })
            {
                Table(walnut, tx, tz, 1.2f, .8f);
                foreach (float s in new[] { -1f, 1f })
                    foreach (float dx in new[] { -.3f, .3f })
                    {
                        float cz = tz + s * .62f, yaw = s < 0 ? 0f : 180f;
                        if (PH(MChair, V(tx + dx, FFL, cz), yaw, "restauracja: krzesło", false)) Seat("KRZESŁO", V(tx + dx, FFL + .46f, cz), yaw);
                    }
                PH(MLamp, V(tx, CY - .10f, tz), 0f, "restauracja: lampa nad stołem", false, true);
            }
        // ---- cognac leather banquettes along both side walls (seat .46, back 1.05) with tables for two and a chair opposite
        foreach (float side in new[] { -1f, 1f })
        {
            float wx = side * 13.9f, bx = wx - side * .33f;                                 // the wall's panel face, the seat's centre
            BL(cognac, Mathf.Min(wx, wx - side * .62f), Mathf.Max(wx, wx - side * .62f), FFL + .02f, FFL + .46f, -76.2f, -65.2f, true);
            BL(cognac, Mathf.Min(wx, wx - side * .14f), Mathf.Max(wx, wx - side * .14f), FFL + .46f, FFL + 1.05f, -76.2f, -65.2f);
            BL(walnut, Mathf.Min(wx, wx - side * .64f), Mathf.Max(wx, wx - side * .64f), FFL, FFL + .02f, -76.2f, -65.2f);
            for (float tz = -75.0f; tz <= -66.0f; tz += 2.25f)
            {
                float tx = wx - side * 1.15f;
                Table(walnut, tx, tz, .7f, .7f);
                Seat("ŁAWA", V(bx, FFL + .46f, tz), side < 0 ? 90f : -90f);
                float cx = tx - side * .62f;
                if (PH(MChair, V(cx, FFL, tz), side < 0 ? -90f : 90f, "restauracja: krzesło", false)) Seat("KRZESŁO", V(cx, FFL + .46f, tz), side < 0 ? -90f : 90f);
                PH(MLamp, V(tx, CY - .10f, tz), 0f, "restauracja: lampa nad stołem", false, true);
            }
        }
        foreach (var (x, z) in new[] { (-7.1f, -76.3f), (7.1f, -76.3f), (-2.6f, -64.9f), (2.6f, -64.9f) })        // the corners by the glass: potted palms (Wellness)
            PH(MPlant, V(x, FFL, z), x * 13f, "restauracja: roślina", true);
        // ---- the terrace on the deck (z -63.5..-55): table sets under parasols either side of the walk to the lagoon
        foreach (float tz in new[] { -61.2f, -57.4f })
            foreach (float tx in new[] { -11.6f, -7.4f, 7.4f, 11.6f })
                if (PH(MSet, V(tx, DY, tz), 90f, "taras restauracji: stolik z krzesłami", true)) { Tables++; Seat("KRZESŁO", V(tx - .62f, DY + .45f, tz), 90f); Seat("KRZESŁO", V(tx + .62f, DY + .45f, tz), -90f); }
        foreach (float px in new[] { -9.5f, 9.5f }) foreach (float pz in new[] { -61.2f, -57.4f }) Parasol(V(px, DY, pz), 0);
    }

    // a walnut table top 4 cm on a black brushed-steel pedestal and foot
    static void Table(Material top, float x, float z, float w, float d)
    {
        BL(top, x - w * .5f, x + w * .5f, FFL + .71f, FFL + .75f, z - d * .5f, z + d * .5f);
        BL(black, x - .04f, x + .04f, FFL + .02f, FFL + .71f, z - .04f, z + .04f);
        BL(black, x - .25f, x + .25f, FFL, FFL + .02f, z - .25f, z + .25f);
        Col(CX + x - w * .5f, CX + x + w * .5f, FFL, FFL + .75f, CZ + z - d * .5f, CZ + z + d * .5f, "stół");
        Tables++;
    }

    // leather lounges: the lobby's entrance corners and the jacuzzi hall
    static void Lounges()
    {
        foreach (float x in new[] { -9.2f, 9.2f })
        {
            LeatherSofa(x, -86.6f, 180f, "hol: sofa skórzana");
            PH(MCoffee, V(x, FFL, -88.15f), 0f, "hol: stolik kawowy", true);
            foreach (float ax in new[] { -1.05f, 1.05f }) LeatherArmchair(x + ax, -89.6f, 0f, "hol: fotel skórzany");
            PH(MPlant, V(x + (x < 0 ? -2.0f : 2.0f), FFL, -86.4f), 20f, "hol: roślina", true);
        }
        // the jacuzzi hall: between the tubs and the loungers at the glass, facing the lagoon
        LeatherSofa(27f, -74.2f, 0f, "jacuzzi: sofa skórzana");
        PH(MCoffee, V(27f, FFL, -72.3f), 0f, "jacuzzi: stolik kawowy", true);
        PH(MLounge, V(24.5f, FFL, -72.4f), 70f, "jacuzzi: fotel skórzany", true); Seat("FOTEL", V(24.5f, FFL + .42f, -72.4f), 70f);
        PH(MLounge, V(29.5f, FFL, -72.4f), -70f, "jacuzzi: fotel skórzany", true); Seat("FOTEL", V(29.5f, FFL + .42f, -72.4f), -70f);
        foreach (float x in new[] { 23.2f, 30.8f }) PH(MPlant, V(x, FFL, -74.6f), x * 7f, "jacuzzi: roślina", true);
    }
}
