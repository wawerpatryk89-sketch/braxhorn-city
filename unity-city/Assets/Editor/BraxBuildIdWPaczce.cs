using System;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

// IDENTYFIKATOR BUILDU W PACZCE (build 14, recenzja CI 2.11 — W4): sceny z pamięci podręcznej rejonu mają wpisany commit
// buildu, który je wygenerował. Przy pakowaniu (tylko kopia sceny w APK/WebGL, sceny na dysku bez zmian) każdy BraxGymPlayer
// dostaje bieżący GITHUB_SHA — diagnostyka, BraxhornState.build i dziennik startu pokazują commit, z którego jest kod gry.
// Zwykła klasa edytora (nie komponent) — bez danych w scenie.
public class BraxBuildIdWPaczce : IProcessSceneWithReport
{
    public int callbackOrder => 0;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (report == null) return;                                   // tryb Play w edytorze — nic
        string sha = Environment.GetEnvironmentVariable("GITHUB_SHA");
        if (string.IsNullOrEmpty(sha)) return;
        int n = 0;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var p in root.GetComponentsInChildren<BraxGymPlayer>(true))
            {
                if (p.buildId != sha) { p.buildId = sha; n++; }
            }
        if (n > 0) Debug.Log("BRAXHORN_BUILD_ID " + scene.name + ": " + n + " × buildId → " + sha.Substring(0, Math.Min(12, sha.Length)) + " (scena z pamięci rejonu albo starszy wpis)");
    }
}
