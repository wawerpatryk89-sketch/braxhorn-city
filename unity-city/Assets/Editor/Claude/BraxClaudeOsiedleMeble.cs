using UnityEngine;

// MEBLE BEZ KLOCKÓW (runda 5, koordynator: „stoły, krzesła, lady, regały, automaty, wieszaki z gołych prostopadłościanów —
// wymień na model albo porządnie modeluj w kodzie: fazy, nogi, blaty z grubością, uchwyty”). Wspólne bryły z detalem:
//  • ŁAWKA: 3 deski siedziska z przerwami 1 cm (fazowane — węższa listwa pod spodem), rama stalowa z 2 wspornikami w kształcie
//    odwróconego U ze stopkami i poprzeczką; kolizja jedna;
//  • SZAFKA (szatniowa): korpus z cokołem cofniętym, drzwi z fugą 6 mm od korpusu (cień), żaluzja wentylacyjna (6 szczelin),
//    tabliczka z numerem, uchwyt z zamkiem, daszek skośny (zamiast płaskiej góry);
//  • SEJF: korpus z ramą drzwi (fuga), pokrętło szyfru z tarczą, rączka trójramienna, zawiasy, cokół.
public static partial class BraxClaudeOsiedle
{
    static int mebleDetal;
    // ławka wzdłuż X (x0…x1) przy linii z (środek siedziska), siedzisko na wys. h
    static void LawkaX(Material deska, float x0, float x1, float zc, float h = .45f, float yF = 0f)
    {
        float d = .4f, t = .045f; float[] pas = { -.19f, -.06f, .07f };
        foreach (float o in pas)
        {
            BoxMinMax(deska, x0, x1, yF + h - t, yF + h, zc + o, zc + o + .12f, false, true);
            BoxMinMax(deska, x0 + .01f, x1 - .01f, yF + h - t - .008f, yF + h - t, zc + o + .01f, zc + o + .11f, false, true);   // faza (węższy spód)
        }
        foreach (float x in new[] { x0 + .25f, x1 - .25f })
        {
            BoxMinMax(Steel, x - .025f, x + .025f, yF + h - t - .04f, yF + h - t - .008f, zc - d * .5f, zc + d * .5f, false, true);   // wspornik pod deskami
            foreach (float z in new[] { zc - d * .5f + .02f, zc + d * .5f - .06f }) BoxMinMax(Steel, x - .02f, x + .02f, yF + .02f, yF + h - t - .04f, z, z + .04f, false);
            BoxMinMax(Steel, x - .035f, x + .035f, yF, yF + .02f, zc - d * .5f, zc + d * .5f, false);                                  // stopka
        }
        BoxMinMax(Steel, x0 + .25f, x1 - .25f, yF + .16f, yF + .19f, zc - .015f, zc + .015f, false);                                  // poprzeczka
        Collider(new Vector3((x0 + x1) * .5f, yF + h * .5f, zc), new Vector3(x1 - x0, h, d));
        mebleDetal++;
    }
    // szafka szatniowa: front w stronę „przod” (+x / −x / +z / −z), szerokość w wzdłuż frontu, głębokość g
    static void Szafka(Material korpus, Material drzwi, Vector3 c, Vector3 przod, float w = .4f, float g = .5f, float h = 1.9f, int nr = 0, float yF = 0f)
    {
        przod.y = 0; przod.Normalize(); var bok = Vector3.Cross(Vector3.up, przod);
        void B(Material m, float a0, float a1, float y0, float y1, float f0, float f1, bool bottom = false)
        {   // a: wzdłuż frontu, f: w głąb (0 = lico frontu, ujemne do tyłu)
            var p0 = c + bok * a0 + przod * f0; var p1 = c + bok * a1 + przod * f1;
            BoxMinMax(m, Mathf.Min(p0.x, p1.x), Mathf.Max(p0.x, p1.x), yF + y0, yF + y1, Mathf.Min(p0.z, p1.z), Mathf.Max(p0.z, p1.z), false, bottom);
        }
        float hw = w * .5f;
        B(korpus, -hw + .01f, hw - .01f, 0f, .1f, -g + .03f, -.05f);                      // cokół cofnięty
        B(korpus, -hw, hw, .1f, h, -g, -.02f);                                             // korpus
        B(korpus, -hw - .005f, hw + .005f, h, h + .02f, -g - .005f, .005f, true);          // daszek (okap)
        B(drzwi, -hw + .02f, hw - .02f, .14f, h - .03f, -.014f, 0f);                       // drzwi (fuga 6 mm wokół: korpus cofnięty do −0.02)
        var ciemny = Plain("szczelina wentylacji", new Color(.05f, .05f, .06f), .1f);
        for (int k = 0; k < 6; k++) B(ciemny, -hw + .07f, hw - .07f, h - .25f + k * .025f, h - .25f + k * .025f + .008f, -.001f, .001f);
        B(Plain("tabliczka numeru", new Color(.85f, .85f, .82f), .4f), -.04f, .04f, h - .4f, h - .34f, .0f, .003f);
        B(Chrom, hw - .09f, hw - .06f, 1.0f, 1.16f, 0f, .025f);                            // uchwyt
        B(Plain("wkładka zamka", new Color(.7f, .6f, .2f), .7f, .8f), hw - .085f, hw - .065f, 1.19f, 1.21f, 0f, .012f);
        mebleDetal++;
    }
    static void Sejf(Vector3 c, Vector3 przod, float w = .6f, float g = .6f, float h = .95f, float yF = 0f)
    {
        przod.y = 0; przod.Normalize(); var bok = Vector3.Cross(Vector3.up, przod);
        void B(Material m, float a0, float a1, float y0, float y1, float f0, float f1, bool solid = false)
        {
            var p0 = c + bok * a0 + przod * f0; var p1 = c + bok * a1 + przod * f1;
            BoxMinMax(m, Mathf.Min(p0.x, p1.x), Mathf.Max(p0.x, p1.x), yF + y0, yF + y1, Mathf.Min(p0.z, p1.z), Mathf.Max(p0.z, p1.z), solid, true);
        }
        float hw = w * .5f; var korp = Plain("sejf grafit", new Color(.16f, .17f, .18f), .5f, .6f);
        B(korp, -hw + .02f, hw - .02f, 0f, .05f, -g + .02f, -.03f);                        // cokół
        B(korp, -hw, hw, .05f, h, -g, -.03f, true);                                        // korpus
        B(korp, -hw + .05f, hw - .05f, .1f, h - .05f, -.03f, 0f);                          // drzwi (fuga 5 cm ramy)
        var srebro = Chrom; var p = c + przod * .01f + Vector3.up * (yF + h * .62f);
        Tube(srebro, p, p + przod * .05f, .06f, 16);                                       // tarcza szyfru
        Tube(Plain("pokrętło czarne", new Color(.04f, .04f, .05f), .5f), p + przod * .05f, p + przod * .08f, .035f, 12);
        var r = c + przod * .01f + Vector3.up * (yF + h * .42f);
        for (int k = 0; k < 3; k++) { float a = k * Mathf.PI * 2f / 3f; Tube(srebro, r + przod * .03f, r + przod * .03f + (bok * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a)) * .1f, .012f, 8); }
        Tube(srebro, r, r + przod * .03f, .02f, 8);
        foreach (float y in new[] { .25f, h - .2f }) B(srebro, -hw + .03f, -hw + .06f, y, y + .1f, -.005f, .02f);   // zawiasy
        mebleDetal++;
    }
}
