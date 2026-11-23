using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// 23.11 (owner 17:48: "to koło fortuny się nie kręciło"): a part the game moves must never be static. The mesh batch marks
// every renderer it writes BatchingStatic | OccludeeStatic - right for the buildings - and Unity bakes such renderers into
// combined meshes when the APK is built: a baked part stays drawn where it was built however its transform turns. The money
// wheel and its clapper, the Sic Bo dice, the hidden office's wardrobe door, the villas' garage doors, the gallery's sliding
// doors and the rotisserie's spit were all built that way, so their games and doors ran while they stood still.
// Run once on the finished exploration scene (before it is saved and split into the areas): every transform a runtime
// component of the region moves loses the static flags of its whole subtree; the buildings stay batched.
public static class BraxChampionsRuchome
{
    public static int Czesci, Odblokowane;

    static IEnumerable<T> All<T>(Scene scene) where T : Component
    {
        foreach (var r in scene.GetRootGameObjects()) foreach (var c in r.GetComponentsInChildren<T>(true)) yield return c;
    }

    public static void Odblokuj(Scene scene)
    {
        var cele = new List<Transform>();
        void D(Transform t) { if (t) cele.Add(t); }
        void A(IEnumerable<Transform> ts) { if (ts != null) foreach (var t in ts) D(t); }
        foreach (var c in All<BraxChampionsObrot>(scene)) D(c.transform);
        foreach (var c in All<BraxChampionsKoloFortuny>(scene)) { D(c.kolo); D(c.klapka); }
        foreach (var c in All<BraxChampionsSicBo>(scene)) A(c.kostki);
        foreach (var c in All<BraxChampionsSzafa>(scene)) D(c.zawias);
        foreach (var c in All<BraxChampionsD2DrzwiPociagu>(scene)) { A(c.skrzydla); D(c.stopien); }
        foreach (var c in All<BraxChampionsZegar>(scene)) { D(c.godzinowa); D(c.minutowa); D(c.sekundowa); }
        foreach (var c in All<BraxChampionsLototo>(scene)) { D(c.beben); A(c.kule); }
        foreach (var c in All<BraxChampionsKregle>(scene)) A(c.kregle);
        foreach (var c in All<BraxChampionsBilard>(scene)) { D(c.kij); if (c.bile != null) foreach (var b in c.bile) if (b) D(b.transform); }
        foreach (var c in All<BraxChampionsHelikopter>(scene)) D(c.transform);
        foreach (var c in All<BraxChampionsLeniwaRzeka>(scene)) A(c.kola);
        foreach (var c in All<BraxChampionsD2Pociag>(scene)) D(c.transform);
        foreach (var c in All<BraxChampionsCar>(scene)) D(c.transform);
        foreach (var c in All<BraxChampionsRuletka>(scene)) if (c.stoly != null) foreach (var t in c.stoly) if (t != null) { D(t.rotor); D(t.kulka); }
        foreach (var c in All<BraxChampionsAutomaty>(scene))
            if (c.maszyny != null)
                foreach (var m in c.maszyny)
                {
                    if (m == null) continue;
                    A(m.bebny); D(m.dzwignia); D(m.przycisk); D(m.kolo); D(m.wskaznik);
                    if (m.kolumny != null) foreach (var f in m.kolumny) if (f) D(f.transform);
                    if (m.warstwa) D(m.warstwa.transform);
                    if (m.licznik) D(m.licznik.transform);
                }
        int n = 0;
        foreach (var t in cele)
            foreach (var x in t.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetStaticEditorFlags(x.gameObject) == 0) continue;
                GameObjectUtility.SetStaticEditorFlags(x.gameObject, 0); n++;
            }
        Czesci = cele.Count; Odblokowane = n;
        Debug.Log("CHAMPIONS_RUCHOME moving parts=" + cele.Count + " staticFlagsCleared=" + n +
                  " (money wheel, clapper, dice, wardrobe and garage doors, sliding doors, spit, reels, rotors: never static-batched)");
    }
}
