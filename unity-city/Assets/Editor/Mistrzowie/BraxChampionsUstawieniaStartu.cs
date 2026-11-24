using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>24.11.2026 build #54 (run 45334082585): every texture was imported THREE times - 48 min at Unity's start
/// (the project has no committed ProjectSettings.asset, so Gamma and the default Android graphics APIs), 45 min again
/// when BraxChampionsBuild set PlayerSettings.colorSpace = Linear (a texture's import depends on the colour space), and
/// in #52 (18.11) 88 min a third time inside BuildPlayer after BuildExplorationApk set the Android graphics APIs to
/// OpenGLES3 only (a texture's Android import records the graphics APIs too - Unity issue "changing graphics APIs in
/// Android platform reimports most of the assets"). #54 reached the APK steps 3 h 28 min in, past what the 6 h job
/// could still finish, and its memory with them.
/// In a batch-mode (CI) editor these two settings are now the build's own values from the first domain load - before
/// the bulk import of the assets (Unity compiles the scripts first, then imports the rest) - so each texture is
/// imported once, in its final form, and the build's own later assignments change nothing. Off in the interactive
/// editor; nothing else is touched.</summary>
[InitializeOnLoad]
static class BraxChampionsUstawieniaStartu
{
    static BraxChampionsUstawieniaStartu()
    {
        if (!Application.isBatchMode) return;
        try
        {
            string zmiana = "";
            if (PlayerSettings.colorSpace != ColorSpace.Linear) { PlayerSettings.colorSpace = ColorSpace.Linear; zmiana += " colorSpace"; }
            var api = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            if (PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android) || api.Length != 1 || api[0] != GraphicsDeviceType.OpenGLES3)
            {
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
                zmiana += " androidGraphicsApis";
            }
            Debug.Log("BRAX_START_SETTINGS colorSpace=" + PlayerSettings.colorSpace + " androidApis=" +
                      string.Join(",", PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).Select(a => a.ToString())) +
                      " changed=" + (zmiana.Length == 0 ? "none" : zmiana.Trim()) + " (textures imported once, in their final form)");
        }
        catch (System.Exception e) { Debug.LogWarning("BRAX_START_SETTINGS not applied: " + e.Message); }
    }
}
