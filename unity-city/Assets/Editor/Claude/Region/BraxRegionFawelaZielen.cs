using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// FAWELA — dzika zieleń stoku (właściciel 9.11: „nic nie ma być sztuczne”; podgląd 9.11 23:40: łyse, równo zielone wzgórze nad
// zabudową wyglądało jak makieta). Poza działkami, drogami, schodami i tarasami: kępy drzew (drzewo_verdant z biblioteki — LOD-y
// pnia i liści), krzewy (park_shrub z biblioteki, shrub_02 / shrub_03 z Poly Haven), kamienie i głazy na stromiznach (park_rock
// z biblioteki, boulder_01 z Poly Haven). Gęstość z szumu w dużej skali — zagajniki i polany, nie równa siatka; w zabudowie tylko
// pojedyncze drzewa i krzaki w przerwach. Limity sztuk pilnują telefonu (krzewy i kamienie znikają dalej niż ~45 m).
public static partial class BraxRegionFawela
{
    internal static int drzew, krzewow, kamieni;
    const int MaxDrzew = 80, MaxKrzewow = 260, MaxKamieni = 52;

    static void Zielen()
    {
        drzew = krzewow = kamieni = 0;
        var tr = Grupa("Fawela: zieleń dzika stoku (drzewa, krzewy, kamienie)");
        var drzewo = BraxRegion.Model("drzewo_verdant");
        var krzewy = new[] { "park_shrub", "park_shrub", "shrub_02", "shrub_03" }.Select(id => BraxRegion.Model(id)).Where(m => m).ToList();
        var kamien = BraxRegion.Model("park_rock"); var glaz = BraxRegion.Model("boulder_01");
        if (!drzewo) brakModeli.Add("drzewo_verdant"); if (krzewy.Count == 0) brakModeli.Add("park_shrub"); if (!kamien) brakModeli.Add("park_rock");
        var r = new System.Random(6061);
        float R(float a, float b) => a + (float)r.NextDouble() * (b - a);
        var drzewa = new List<Vector2>(); var krz = new List<Vector2>();
        float x0 = BraxMiastoGranice.FawelaX0 + 5f, x1 = BraxMiastoGranice.FawelaX1 - 5f, zS = BraxMiastoGranice.FawelaZSzczyt + 5f;
        for (float x = x0; x < x1; x += 6.5f)
            for (float z = -70f; z > zS; z -= 6.5f)
            {
                float px = x + R(-2.6f, 2.6f), pz = z + R(-2.6f, 2.6f);
                if (px < x0 || px > x1 || pz < zS) continue;
                float d = Glebokosc(px, pz); if (d < 9f) continue;
                if (px > -61.4f && pz > -74f) continue;
                // wolne miejsce: z dala od działek, jezdni, schodów, placu, sali z podwórzem
                float wolne = Mathf.Min(Mathf.Min(DoDzialek(px, pz) - 1.8f, DoDrog(px, pz, 10f) - 3.2f), Mathf.Min(DoSchodow(px, pz) - 2f,
                    Mathf.Min(Mathf.Min(DoProst(Plac, px, pz), DoProst(Sala, px, pz)), DoProst(PodworzeSali, px, pz)) - 3f));
                if (wolne < 0f) continue;
                float y = WysTerenu(px, pz);
                float gx = WysTerenu(px + 1f, pz) - WysTerenu(px - 1f, pz), gz = WysTerenu(px, pz + 1f) - WysTerenu(px, pz - 1f);
                float stromo = Mathf.Sqrt(gx * gx + gz * gz) * .5f;
                bool gora = d > Pas2 + 4f;
                float las = Szum(px / 41f + 13.7f, pz / 41f + 4.2f) * .75f + Szum(px / 11f + 2.3f, pz / 11f + 9.1f) * .25f;   // zagajniki i polany
                double los = r.NextDouble();
                // drzewo: na górze w zagajnikach, w zabudowie pojedynczo (więcej miejsca dookoła)
                float pDrz = gora ? S01(.46f, .72f, las) * .55f : (wolne > 3.5f ? .07f : 0f);
                if (drzewo && drzewa.Count < MaxDrzew && los < pDrz && stromo < .7f && wolne > 2.6f && !drzewa.Any(t => (t - new Vector2(px, pz)).sqrMagnitude < 8.5f * 8.5f))
                {
                    var o = BraxRegion.Put(tr, drzewo, "Fawela: drzewo na stoku", new Vector3(px, y - .1f, pz), R(0f, 360f), R(.82f, 1.18f), false, 0f, .05f);
                    if (o) { drzewa.Add(new Vector2(px, pz)); drzew++; Kol(tr, new Vector3(px - .28f, y - .1f, pz - .28f), new Vector3(px + .28f, y + 2.6f, pz + .28f)); }
                    continue;
                }
                // kamienie i głazy: stromizny i skraj polan
                double los2 = r.NextDouble();
                float pKam = gora ? .05f + S01(.35f, .8f, stromo) * .35f : .02f;
                if (kamieni < MaxKamieni && los2 < pKam && (kamien || glaz))
                {
                    bool duzy = glaz && kamieni % 6 == 5;
                    var o = Rekw(tr, new[] { duzy ? "boulder_01" : "park_rock" }, new Vector3(px, y - (duzy ? .35f : .12f), pz), R(0f, 360f), duzy ? R(.9f, 1.3f) : R(.45f, 1.05f), false, duzy ? 60f : 40f, duzy ? "głaz na stoku" : "kamień na stoku", true);
                    if (o) { kamieni++; var b = BraxRegion.Bounds(o); if (b.size.y > .5f) BraxRegionMeble.Kolizja(tr, "Fawela: kolizja kamienia", b.center, Vector3.Scale(b.size, new Vector3(.8f, .9f, .8f))); }
                    continue;
                }
                // krzewy: wszędzie w kępach (pod drzewami i na skraju zagajników gęściej), w zabudowie przy murach i plecach domów
                float pKrz = gora ? .16f + S01(.3f, .62f, las) * .4f : .18f;
                if (krzewy.Count > 0 && krzewow < MaxKrzewow && r.NextDouble() < pKrz && stromo < .9f && !krz.Any(t => (t - new Vector2(px, pz)).sqrMagnitude < 3.2f * 3.2f))
                {
                    var src = krzewy[r.Next(krzewy.Count)];
                    var o = BraxRegion.Put(tr, src, "Fawela: krzew na stoku", new Vector3(px, y - .05f, pz), R(0f, 360f), R(.75f, 1.25f), false, 0f, .05f);
                    if (o)
                    {
                        krz.Add(new Vector2(px, pz)); krzewow++;
                        var rs = o.GetComponentsInChildren<Renderer>(true);
                        if (rs.Length > 0 && !o.GetComponent<LODGroup>())
                        {
                            var lod = o.AddComponent<LODGroup>();
                            lod.SetLODs(new[] { new LOD(.035f, rs) });   // ok. 45 m dla krzewu 1.7 m
                            lod.RecalculateBounds();
                        }
                    }
                }
            }
        roslin += drzew + krzewow + kamieni;
        BraxRegion.Note("fawela: zieleń stoku — drzew " + drzew + ", krzewów " + krzewow + ", kamieni i głazów " + kamieni);
    }
}
