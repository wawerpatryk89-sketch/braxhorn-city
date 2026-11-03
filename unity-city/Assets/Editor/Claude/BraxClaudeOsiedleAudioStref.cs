using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// AUDIO STREF (runda 5, koordynator: „dźwięk stref we WSZYSTKICH pomieszczeniach: w środku głośno, przy otwartych drzwiach
// ciszej, za ścianą bez drzwi cisza”). Strefy z dźwiękiem z syntezy (BraxDzwiekiStref) dla pomieszczeń wykonawcy B: salon,
// warsztat, wulkanizacja, sklep stacji, hala myjni; strefy muzyki (kasyno, siłownia A, dyskoteka — MusicZones) przechodzą
// na ten sam tryb otworów (BraxClaudeMuzyka: drzwi BraxCityDoor przy ścianach + otwory stałe). Log BRAXHORN_AUDIO_STREFY:
// każda strefa z liczbą drzwi i otworów; strefa bez otworów = stary tryb zza ściany (wypisana jako DO POPRAWKI).
public static partial class BraxClaudeOsiedle
{
    static void AudioStref()
    {
        var g0 = group; group = "Osiedle A / audio stref";
        float yE = 4.3f * K, xb = 29.5f, bw = (47f - .3f - xb) / 3f, xw = xb + 2f * bw;
        var strefy = new List<(string n, Bounds b, string syn, float vol, Vector3[] otw)>();
        if (SalonWlaczony) strefy.Add(("salon samochodowy", MinMax(SBX0, SBX1, 0f, SYR, SBZ0, SBZ1), "salon", .28f, new[] { new Vector3((SDX0 + SDX1) * .5f, 1.6f, SBZ1) }));
        if (WarsztatWlaczony)
        {
            strefy.Add(("warsztat (stanowiska 1–2)", MinMax(xb, xw, 0f, yE, -54f, -42f), "warsztat", .32f, new[] { new Vector3(xb + bw * .5f, 2f, -42f) }));
            strefy.Add(("wulkanizacja (stanowisko 3)", MinMax(xw, 47f, 0f, yE, -54f, -42f), "wulkanizacja", .32f, new[] { new Vector3(xb + bw * 2.5f, 2f, -42f) }));
        }
        if (StacjaWlaczona)
        {
            strefy.Add(("sklep stacji", MinMax(-36.5f, -21f, 0f, 3.4f * K, -60.5f, -49f), "sklep", .22f, new[] { new Vector3(-28.75f, 1.6f, -49f) }));
            strefy.Add(("myjnia automatyczna", MinMax(-4f, 5.5f, 0f, 6f, -62f, -48.5f), "myjnia", .3f, new[] { new Vector3(.75f, 2f, -48.5f) }));
        }
        var opis = new List<string>(); int zza = 0;
        foreach (var (n, b, syn, vol, otw) in strefy)
        {
            var go = new GameObject("dźwięk strefy: " + n); go.transform.SetParent(Group(group), false); go.transform.position = b.center;
            var mz = go.AddComponent<BraxClaudeMuzyka>(); mz.synteza = syn; mz.wnetrze = b; mz.glosnosc = vol; mz.wygaszenie = 2f; mz.otwory = otw; mz.zasiegOtworu = 12f;
            int d = DrzwiPrzy(b); opis.Add(n + ": drzwi=" + d + " otwory=" + otw.Length + " synteza=" + syn);
        }
        // strefy muzyki z MusicZones (i inne BraxClaudeMuzyka w scenie): ten sam tryb otworów
        foreach (var mz in Object.FindObjectsByType<BraxClaudeMuzyka>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(m => string.IsNullOrEmpty(m.synteza)))
        {
            int d = DrzwiPrzy(mz.wnetrze), o = mz.otwory != null ? mz.otwory.Length : 0;
            if (d + o == 0) zza++;
            opis.Add(mz.name.Replace("muzyka: ", "muzyka ") + ": drzwi=" + d + " otwory=" + o + (d + o == 0 ? " DO POPRAWKI: brak otworów — stary tryb zza ściany" : ""));
        }
        string log = "BRAXHORN_AUDIO_STREFY strefy=" + (strefy.Count + opis.Count - strefy.Count) + " z_syntezy=" + strefy.Count + " zza_sciany_bez_otworow=" + zza +
                     " (w środku pełna głośność; na zewnątrz tylko przez otwarte drzwi/otwory: przy otworze ~50 %, zanik do 12–14 m, stłumione; za ścianą bez drzwi cisza) [" + string.Join("; ", opis) + "]";
        Debug.Log(log); rep.notes.Add(log.Replace("BRAXHORN_AUDIO_STREFY ", "audio stref: "));
        group = g0;
    }
    static int DrzwiPrzy(Bounds b)
    {
        var e = b; e.Expand(new Vector3(3f, 1f, 3f));
        return Object.FindObjectsByType<BraxCityDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(d => d && e.Contains(d.transform.position));
    }
}
