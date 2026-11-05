using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Wspolny wymog podpisu Android APK dla kazdego Unity 6 BuildPlayer w tym projekcie.
// Brak sekretow nie moze nigdy skutkowac instalatorem z kluczem debug.
public sealed class BraxMandatoryAndroidSigning : IPreprocessBuildWithReport
{
    public int callbackOrder => -10000;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android) return;
        string root = Directory.GetParent(Application.dataPath).FullName;
        string key = Path.Combine(root, "braxhorn.keystore");
        string passFile = Path.Combine(root, "braxhorn.keystore.pass");
        string versionFile = Path.Combine(root, "braxhorn.versioncode");
        if (!File.Exists(key) || new FileInfo(key).Length == 0 || !File.Exists(passFile))
            throw new BuildFailedException("BRAXHORN_PODPIS: staly klucz release jest obowiazkowy; debug APK ZABRONIONE");
        string pass = File.ReadAllText(passFile).Trim();
        if (pass.Length == 0)
            throw new BuildFailedException("BRAXHORN_PODPIS: brak hasla do stalego keystore");
        if (!File.Exists(versionFile) ||
            !int.TryParse(File.ReadAllText(versionFile).Trim(), out int versionCode) ||
            versionCode <= 0)
            throw new BuildFailedException("BRAXHORN_WERSJA: wymagany poprawny rosnacy versionCode");
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = key;
        PlayerSettings.Android.keystorePass = pass;
        PlayerSettings.Android.keyaliasName = "braxhorn";
        PlayerSettings.Android.keyaliasPass = pass;
        PlayerSettings.Android.bundleVersionCode = versionCode;
        PlayerSettings.bundleVersion = "1.0." + versionCode;
        Debug.Log("BRAXHORN_PODPIS_GLOBAL: staly podpis wymagany; versionCode=" + versionCode);
    }
}
