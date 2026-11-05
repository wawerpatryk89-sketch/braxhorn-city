using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Explicitly distinguishes source checks from Play Mode and owner approval.
public static class BraxObozValidation
{
    [Serializable] public sealed class Report
    {
        public string kind = "editor-preflight";
        public bool passed, unityRuntimeVerified;
        public bool structuralPassed;
        public string[] structuralErrors, sourceIssues;
        public string[] errors, manualChecks;
    }
    public static Report Check(BraxObozSceneInfo scene)
    {
        var errors = new List<string>();
        if (PlayerSettings.colorSpace != ColorSpace.Linear) errors.Add("Wymagana przestrzeń kolorów Linear");
        if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline) errors.Add("Obóz wymaga Built-in RP");
        if (!scene) errors.Add("Nie ma kompletnej sceny obozu");
        else
        {
            if (!scene.player || scene.player.name.IndexOf("Monika", StringComparison.OrdinalIgnoreCase) < 0) errors.Add("Graczem musi być Monika");
            var expected = new[] { "vorn", "zyro", "rakk", "kade", "gorr", "mis" };
            if (scene.npcs.Length != 6 || scene.npcs.Any(n => !n) ||
                !scene.npcs.Where(n => n).Select(n => n.characterId).OrderBy(x => x).SequenceEqual(expected.OrderBy(x => x))) errors.Add("Obsada musi zawierać sześciu różnych uzgodnionych NPC");
            var actual = UnityEngine.Object.FindObjectsByType<BraxObozNpc>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(n => n.gameObject.scene == scene.gameObject.scene).ToArray();
            if (actual.Length != 6) errors.Add("W scenie jest inna liczba NPC niż sześć");
            if (scene.bags.Length != 24 || scene.bags.Any(b => !b) || scene.bags.Distinct().Count() != 24) errors.Add("Wymagane 24 różne worki");
            if (scene.coveredBags.Length != 12 || scene.coveredBags.Any(b => !b) || scene.coveredBags.Distinct().Count() != 12) errors.Add("Wymagane 12 różnych worków pod zadaszeniem");
            foreach (var b in scene.coveredBags) if (b && !scene.bags.Any(x => x && x.transform == b)) errors.Add("Zadaszony worek nie należy do grupy 24 worków");
            if (scene.cabinDoors.Length != 4 || scene.cabinDoors.Distinct().Count() != 4 || scene.cabinDoors.Any(d => !d || d.leaves == null || d.leaves.Length == 0)) errors.Add("Cztery domki potrzebują różnych działających drzwi");
            if (scene.arenas.Length != 3 || scene.arenas.Any(a => !a)) errors.Add("Wymagane dwa ringi i oktagon");
            // Obowiązkowe zaplecze sanitarne: rzeczywiste kabiny i wyposażenie,
            // osobne od modeli sauny i pryszniców otrzymanych od właściciela.
            var sanitary=scene.GetComponentsInChildren<Transform>(true)
                .Where(t=>t.name=="BRAXHORN — WC / ŁAŹNIA / UMYWALNIA").ToArray();
            if(sanitary.Length!=1) errors.Add("W obozie brakuje osobnych sanitariatów");
            var toilets=scene.GetComponentsInChildren<BraxSeat>(true).Count(s=>s.label=="TOALETA");
            if(toilets!=2) errors.Add("Wymagane dwie fizyczne kabiny WC");
            var showerControls=scene.GetComponentsInChildren<BraxObozFacility>(true)
                .Count(f=>f.kind==BraxObozFacility.Kind.Shower && f.effect);
            if(showerControls<2) errors.Add("Brakuje dwóch działających pryszniców w łaźni");
            var stations=scene.GetComponentsInChildren<BraxObozTrainingStation>(true);
            if(stations.Length!=9||stations.Any(s=>!s.player))
                errors.Add("Wymagane sześć stanowisk plenerowych i trzy aktywne stanowiska pod dachem");
            var buses=scene.GetComponentsInChildren<Transform>(true).Where(t=>t.name==BraxObozParkedBus.Name).ToArray();
            if(buses.Length!=1||!buses[0].GetComponent<BoxCollider>()||buses[0].GetComponentsInChildren<BraxAutobus>(true).Length!=0)
                errors.Add("Wymagany jeden zaparkowany autobus z kolizją, bez sterownika jazdy");
            var gates=scene.GetComponentsInChildren<BraxObozSlidingGate>(true);
            if(gates.Length!=2||gates.Any(g=>!g.leaf||!g.player))errors.Add("Brama i furtka muszą mieć skrzydło oraz gracza");
            if(scene.GetComponentsInChildren<Transform>(true).Any(t=>t.name.Contains("Gym_Mesh_")||t.name.Contains("źródłowa salka")))errors.Add("Stara siłownia nadal jest w obozie");
            foreach (var n in scene.npcs.Where(n => n))
            {
                if (!n.body || !n.clips || !n.player) errors.Add(n.characterId + ": brak sterowania/animacji");
                if (n.route.Length == 0 || n.route.Any(p => !p)) errors.Add(n.characterId + ": brak kompletnej trasy");
                if (!n.glovesVerified) errors.Add(n.characterId + ": niepotwierdzone rękawice");
                foreach (var activity in n.activities) if (!n.Allows(activity)) errors.Add(n.characterId + ": niedozwolone zajęcie " + activity);
            }
            var routine=scene.GetComponent<BraxObozTrainingRoutine>();
            if(!routine||!routine.navigationData||routine.participants.Length!=6||routine.places.Length!=6||routine.trainingBags.Length!=2||
                routine.sparringPlaces.Length!=6||routine.sparringArenas.Length!=3||routine.sparringPlaces.Any(p=>!p)||
                routine.sparringArenas.Any(a=>!a||a.trainingRoutine!=routine))
                errors.Add("Brak pełnego planu ruchu, worków i sparingu NPC");
            else if(routine.participants.Distinct().Count()!=6||routine.participants.Any(n=>!n||n.routine!=routine||!n.navigation)||routine.places.Any(p=>!p))
                errors.Add("Nieprawidłowe powiązania planu NPC");
            if(!routine||!routine.audio||routine.audio.lines.Length!=47||routine.audio.lines.Any(l=>!l.clip))
                errors.Add("Niepełne nagrania polskich rozmów obozu");
            if(scene.npcs.Any(n=>n&&!n.GetComponent<BraxObozContactAudio>()))
                errors.Add("Brak dźwięku kontaktu w sparingu");
        }
        var structuralErrors = errors.ToArray();
        var sourceIssues = new List<string>();
        var manifest = BraxObozCharacters.ReadManifest();
        foreach (var c in manifest.characters)
        {
            if (!File.Exists(c.modelPath)) { errors.Add(c.name + ": brak oryginalnego FBX"); structuralErrors = structuralErrors.Concat(new[] { c.name + ": brak oryginalnego FBX" }).ToArray(); }
            if (c.realHeight <= 0) sourceIssues.Add(c.name + ": nie odzyskano potwierdzonego wzrostu; podgląd używa wartości roboczej");
            if (c.sourceIssues != null && c.sourceIssues.Length > 0 && !c.qualityExceptionApproved) sourceIssues.Add(c.name + ": niezatwierdzone odchylenia źródła");
        }
        errors.AddRange(sourceIssues);
        return new Report { passed = errors.Count == 0, errors = errors.ToArray(), unityRuntimeVerified = false,
            structuralPassed = structuralErrors.Length == 0, structuralErrors = structuralErrors, sourceIssues = sourceIssues.ToArray(),
            manualChecks = new[] { "65 kości, T-pose, brak rozciągania", "Stopy i przejścia animacji", "Przejście przez każde wejście", "Trasy i pauza NPC", "Ringi, sparing, trafienia i zasięg", "Polskie głosy 3D i ich warianty", "Logotypy i dłonie GORR", "APK na Motoroli, podpis, aktualizacja i zachowanie zapisu", "Wygląd zaakceptowany przez właściciela" } };
    }
    [MenuItem("BRAXHORN/Obóz/Sprawdź przygotowanie sceny (bez APK)")]
    public static void Run()
    {
        var report = Check(UnityEngine.Object.FindFirstObjectByType<BraxObozSceneInfo>());
        Directory.CreateDirectory(BraxObozCharacters.Generated);
        File.WriteAllText(BraxObozCharacters.Generated + "/preflight.json", JsonUtility.ToJson(report, true));
        if (!report.passed) Debug.LogError("BRAXHORN_OBOZ niegotowy: " + string.Join("; ", report.errors));
        else Debug.Log("BRAXHORN_OBOZ preflight OK; Play Mode/telefon NIEZWERYFIKOWANE");
    }
}
