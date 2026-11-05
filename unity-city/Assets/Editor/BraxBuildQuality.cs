using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Owner decision 2026-11-05: gameplay/appearance is reviewed in the APK.
// Never use this for compilation, generation, IO, signing or artifact errors.
public static class BraxBuildQuality
{
    public static bool Advisory => !File.Exists(Path.Combine(Application.dataPath,
        "../ProjectSettings/brax-quality-strict.txt"));
    [Serializable] sealed class ReviewReport
    {
        public string status = "MANUAL_DEVICE_REVIEW_REQUIRED";
        public bool qualityAccepted = false, deviceTested = false;
        public List<string> warnings = new List<string>();
    }
    static readonly ReviewReport report = new ReviewReport();
    public static void Warn(string message)
    {
        report.warnings.Add(message);
        Debug.LogWarning("BRAXHORN_QUALITY_ADVISORY: " + message);
        try
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/QualityReview"));
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "quality-review.json"), JsonUtility.ToJson(report, true));
        }
        catch (IOException e) { Debug.LogWarning("Quality report write failed: " + e.Message); }
        catch (UnauthorizedAccessException e) { Debug.LogWarning("Quality report write failed: " + e.Message); }
    }
    public static void Issue(Exception problem)
    {
        if (!Advisory) throw problem;
        Warn(problem.ToString());
    }
}

public sealed class BraxBuildQualityNotice : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;
    public void OnPreprocessBuild(BuildReport report)
    {
        if (BraxBuildQuality.Advisory)
            BraxBuildQuality.Warn("OWNER_REQUESTED: quality gates are advisory; physical colliders remain; compilation/signing/artifact checks remain blocking. APK is not quality-approved.");
    }
}
