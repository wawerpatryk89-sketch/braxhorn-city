using UnityEditor;
using UnityEngine;

// THE RACE RIVALS' RIDERS (owner 24.11.2026 12:57: "wyścig z rywalami"), until the owner's Tripo MX rider is in (asked for
// 24.11: a helmet with goggles, a jersey, pants, boots, seated, hands forward). A seated motocross rider from the region's
// mesh kit in a real man's proportions (1.78 m: thigh 0.45, shin 0.44, upper arm 0.31, forearm 0.27), sat on the quad's own
// saddle with his hands on its grips and his boots on its footrests (both measured on the owner's models), leaning 28 deg
// forward over the bars as a rider does; the full-face MX helmet with its peak, chin bar and goggles, the jersey in his
// quad's colour with accent side panels, black pants, the tall white boots. A child of the quad (he rides with it, so not
// static); no collider (the quad's body box is his).
public static class BraxChampionsKladZawodnik
{
    static float K => BraxClaudeOsiedle.K;
    public static int Trojkaty;

    /// <summary>The quad's saddle point (the catalogue's), its right grip and its right footrest (the bars' and the body's
    /// meshes, unity-tools/mistrzowie/klady), real metres in the quad's frame.</summary>
    public static (Vector3 siodlo, Vector3 chwyt, Vector3 podnozek) Wymiary(string model) => model == "raptor"
        ? (new Vector3(0f, .808f, -.019f), new Vector3(.44f, .99f, .366f), new Vector3(.34f, .30f, .03f))
        : (new Vector3(0f, .901f, -.135f), new Vector3(.44f, 1.135f, .216f), new Vector3(.36f, .40f, -.02f));

    public const float Udo = .45f, Golen = .44f, Ramie = .31f, Przedramie = .27f, Pochylenie = 28f;

    /// <summary>The middle joint of a two-bone limb (hip-knee-ankle, shoulder-elbow-wrist): the bend towards the hint.</summary>
    public static Vector3 Staw(Vector3 a, Vector3 c, float l1, float l2, Vector3 wskazowka)
    {
        var ac = c - a; float d = Mathf.Clamp(ac.magnitude, .01f, (l1 + l2) * .999f); var dir = ac.normalized;
        float x = (l1 * l1 - l2 * l2 + d * d) / (2f * d), h = Mathf.Sqrt(Mathf.Max(0f, l1 * l1 - x * x));
        var bend = (wskazowka - dir * Vector3.Dot(wskazowka, dir)).normalized;
        return a + dir * x + bend * h;
    }

    static void Konczyna(BraxChampionsMeshBatch b, Material m, Vector3 a, Vector3 c, float r1, float r2)
    {
        var d = c - a; if (d.sqrMagnitude < 1e-6f) return;
        b.Cylinder(m, a, r1, d.magnitude, 10, Quaternion.FromToRotation(Vector3.up, d), false, false, r2);
    }
    static void Kula(BraxChampionsMeshBatch b, Material m, Vector3 c, float r)
    {
        var p = new Vector2[7];
        for (int k = 0; k <= 6; k++) { float a = Mathf.PI * k / 6f; p[k] = new Vector2(r * Mathf.Sin(a), -r * Mathf.Cos(a)); }
        b.Lathe(m, c, p, 10, Quaternion.identity, false, false);
    }

    static string Hex(Color c) => Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f).ToString("x2") + Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f).ToString("x2") + Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f).ToString("x2");

    public static Transform Zbuduj(Transform quad, string model, Color stroj, Color akcent, string folder)
    {
        var (siodlo, chwyt, podnozek) = Wymiary(model);
        var b = new BraxChampionsMeshBatch();
        string id = Hex(stroj), ida = Hex(akcent);
        var mStroj = BraxChampionsHarbour.Surface("zawodnik_stroj_" + id, stroj, .72f, 0f);
        var mAkcent = BraxChampionsHarbour.Surface("zawodnik_akcent_" + ida, akcent, .66f, 0f);
        var mKask = BraxChampionsHarbour.Surface("zawodnik_kask_" + id, stroj, .2f, .05f);             // the glossy shell
        var mSpodnie = BraxChampionsHarbour.Surface("zawodnik_spodnie", new Color(.08f, .08f, .09f), .78f, 0f);
        var mButy = BraxChampionsHarbour.Surface("zawodnik_buty", new Color(.9f, .9f, .88f), .45f, 0f);
        var mCzern = BraxChampionsHarbour.Surface("zawodnik_czern", new Color(.03f, .03f, .035f), .55f, 0f);
        var mGogle = BraxChampionsHarbour.Surface("zawodnik_gogle", new Color(.9f, .5f, .1f), .06f, .9f);  // the mirrored lens

        // the trunk: over the saddle, leaning forward
        var pelvis = siodlo + new Vector3(0f, .09f, -.03f);
        float lean = Pochylenie * Mathf.Deg2Rad; var spine = new Vector3(0f, Mathf.Cos(lean), Mathf.Sin(lean));
        var tilt = Quaternion.FromToRotation(Vector3.up, spine);
        var neck = pelvis + spine * .52f;
        b.RoundedBox(mSpodnie, pelvis + spine * .07f, new Vector3(.34f, .2f, .25f), .07f, tilt, 2);
        b.RoundedBox(mStroj, pelvis + spine * .31f, new Vector3(.40f, .40f, .25f), .08f, tilt, 2);
        foreach (float sx in new[] { -1f, 1f })
            b.RoundedBox(mAkcent, pelvis + spine * .30f + tilt * new Vector3(sx * .2f, 0f, 0f), new Vector3(.05f, .30f, .19f), .02f, tilt, 1);
        // the neck and the helmet: the turned shell, the chin bar, the peak, the goggles and their strap
        var head = neck + new Vector3(0f, .17f, .05f);
        Konczyna(b, mCzern, neck - spine * .03f, head - new Vector3(0f, .06f, 0f), .062f, .058f);
        var helm = Quaternion.Euler(12f, 0f, 0f);
        var shell = new[] { new Vector2(.118f, 0f), new Vector2(.142f, .04f), new Vector2(.156f, .11f), new Vector2(.154f, .17f), new Vector2(.134f, .225f), new Vector2(.094f, .266f), new Vector2(.046f, .287f), new Vector2(0f, .292f) };
        b.Lathe(mKask, head + helm * new Vector3(0f, -.13f, 0f), shell, 16, helm, true, false);
        b.RoundedBox(mKask, head + helm * new Vector3(0f, -.085f, .135f), new Vector3(.17f, .1f, .13f), .045f, helm * Quaternion.Euler(22f, 0f, 0f), 2);
        b.RoundedBox(mAkcent, head + helm * new Vector3(0f, .125f, .155f), new Vector3(.25f, .018f, .17f), .008f, helm * Quaternion.Euler(-14f, 0f, 0f), 1);
        b.RoundedBox(mGogle, head + helm * new Vector3(0f, .02f, .147f), new Vector3(.2f, .066f, .06f), .022f, helm, 1);
        b.RoundedBox(mCzern, head + helm * new Vector3(0f, .02f, .0f), new Vector3(.322f, .04f, .25f), .012f, helm, 1);
        foreach (float sx in new[] { -1f, 1f })
        {
            // the arms: shoulder - elbow (out, a little down and back) - the glove round the grip
            var bark = neck + tilt * new Vector3(sx * .2f, -.075f, -.02f);
            var dlon = new Vector3(sx * chwyt.x, chwyt.y + .02f, chwyt.z);
            var lokiec = Staw(bark, dlon, Ramie, Przedramie, new Vector3(sx, -.6f, -.4f));
            Kula(b, mStroj, bark, .066f);
            Konczyna(b, mStroj, bark, lokiec, .056f, .048f);
            Kula(b, mStroj, lokiec, .049f);
            Konczyna(b, mStroj, lokiec, dlon - (dlon - lokiec).normalized * .07f, .046f, .04f);
            b.RoundedBox(mCzern, dlon, new Vector3(.09f, .095f, .11f), .035f, Quaternion.LookRotation(dlon - lokiec, Vector3.up), 1);
            // the legs: hip - knee (forward and out, over the fender) - the boot on the footrest
            var biodro = pelvis + new Vector3(sx * .1f, 0f, 0f);
            var kostka = new Vector3(sx * podnozek.x, podnozek.y + .07f, podnozek.z);
            var kolano = Staw(biodro, kostka, Udo, Golen, new Vector3(sx * .35f, .4f, 1f));
            Kula(b, mSpodnie, biodro, .085f);
            Konczyna(b, mSpodnie, biodro, kolano, .085f, .064f);
            Kula(b, mAkcent, kolano, .068f);                                                // the knee brace
            var cholewka = Vector3.Lerp(kolano, kostka, .3f);
            Konczyna(b, mSpodnie, kolano, cholewka, .06f, .058f);
            Konczyna(b, mButy, cholewka - (kostka - kolano).normalized * .02f, kostka, .07f, .062f);
            b.RoundedBox(mButy, new Vector3(kostka.x, podnozek.y + .015f, kostka.z + .07f), new Vector3(.115f, .1f, .3f), .035f, Quaternion.identity, 1);
            b.Box(mCzern, new Vector3(kostka.x, podnozek.y - .04f, kostka.z + .07f), new Vector3(.118f, .03f, .3f), Quaternion.identity);
        }
        Trojkaty += b.Triangles;
        var t = b.Build(quad, "zawodnik (do modelu Tripo właściciela)", folder, true, K);
        // he rides with the quad: nothing of him static
        foreach (var tr in t.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(tr.gameObject, (StaticEditorFlags)0);
        return t;
    }
}
