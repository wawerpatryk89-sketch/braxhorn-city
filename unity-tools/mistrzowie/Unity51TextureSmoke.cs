using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Real Unity 6 editor import smoke test, separated from the full Android build.
// Tests original source pixels and exact postprocessor settings on Android.
// Does not generate a scene, compile an APK, or alter project source bytes.
public static class Unity51TextureSmoke
{
    [Serializable]
    sealed class Result
    {
        public string name = "BRAXHORN MIASTO MISTRZOW - 51 native 2K textures";
        public string unity;
        public string status;
        public int expected = 51;
        public int inspected;
        public int pass;
        public List<string> failures = new List<string>();
    }

    public static void Run()
    {
        var report = new Result { unity = Application.unityVersion };
        var roots = new[] {
            "Assets/Source/Mistrzowie/CasinoT11/Textures2K",
            "Assets/Source/Mistrzowie/PoolP1/Textures2K",
        };
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var names = new List<string>();
            foreach(var root in roots)
            {
                if(!Directory.Exists(root))
                    throw new DirectoryNotFoundException("Missing original source folder: " + root);
                names.AddRange(Directory.GetFiles(root, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(p => p.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                             || p.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    .Select(p => p.Replace('\\', '/')));
            }
            names.Sort(StringComparer.Ordinal);
            if(names.Count != report.expected)
                report.failures.Add("Expected 51 source PBR maps, found " + names.Count);
            foreach(var path in names)
            {
                report.inspected++;
                try
                {
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if(!importer)throw new Exception("TextureImporter missing");
                    importer.GetSourceTextureWidthAndHeight(out int w, out int h);
                    if(w != 2048 || h != 2048)throw new Exception("Unexpected source size " + w + "x" + h);
                    if(importer.textureType != TextureImporterType.Default
                       || importer.textureShape != TextureImporterShape.Texture2D)
                        throw new Exception("Wrong TextureImporter type/shape: " +
                            importer.textureType + "/" + importer.textureShape);
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if(!texture)
                    {
                        var main = AssetDatabase.LoadMainAssetAtPath(path);
                        throw new Exception("Texture2D unavailable, Unity main asset = " +
                            (main ? main.GetType().FullName : "NULL"));
                    }
                    bool colour = path.EndsWith("_diff_2k.jpg",StringComparison.Ordinal);
                    if(importer.sRGBTexture != colour)
                        throw new Exception("Wrong sRGB for source map; expected " + colour);
                    if(!importer.mipmapEnabled || !importer.streamingMipmaps)
                        throw new Exception("Mipmap or streaming mipmap disabled");
                    var android=importer.GetPlatformTextureSettings("Android");
                    if(!android.overridden || android.maxTextureSize!=2048
                        || android.format!=TextureImporterFormat.ASTC_4x4)
                        throw new Exception("Android ASTC_4x4 max2K override missing");
                    report.pass++;
                    Debug.Log("BRAX_TEXTURE_PASS " + path + " " + w + "x" + h
                        + " type=" + texture.GetType().Name);
                }
                catch(Exception ex)
                {
                    var message=path+": "+ex.Message;
                    report.failures.Add(message);
                    Debug.LogError("BRAX_TEXTURE_FAIL "+message);
                }
            }
            report.status=report.failures.Count==0 && report.pass==51
                ?"UNITY_51_TEXTURE_IMPORT_PASS":"UNITY_51_TEXTURE_IMPORT_FAIL";
        }
        catch(Exception ex)
        {
            report.failures.Add("Setup: "+ex);
            report.status="UNITY_51_TEXTURE_SMOKE_SETUP_FAIL";
            Debug.LogError("BRAX_TEXTURE_SETUP_FAIL "+ex);
        }
        finally
        {
            var output = Path.GetFullPath("../texture-smoke-evidence");
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,"unity51-texture-import-report.json"),
                JsonUtility.ToJson(report,true));
            Debug.Log("BRAX_TEXTURE_RESULT " + report.status + " pass=" + report.pass
                + "/" + report.expected + " errors=" + report.failures.Count);
        }
        if(report.status!="UNITY_51_TEXTURE_IMPORT_PASS")
            throw new InvalidOperationException("Real Unity 6 texture import smoke failed; see report");
    }
}
