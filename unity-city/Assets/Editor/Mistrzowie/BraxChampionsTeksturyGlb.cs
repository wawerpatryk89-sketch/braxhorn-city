using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;

// TEKSTURY Z PLIKÓW GLB SKOMPRESOWANE NA TELEFON (właściciel 13.11.2026 09:54: „dlaczego to tak dużo waży … to jest
// niemożliwe przez dwa filmy”). APK 43 (2,56 GB) rozebrany na części: filmy 333 MB, a ~2 GB tekstur (liczone po
// rozpakowaniu) to obrazy zaszyte w plikach .glb, które glTFast 6.20 importuje BEZ kompresji (RGBA32/ARGB32/RGB24;
// glTFast nie ma takiego ustawienia). Łodzie HarborBeach 824 MB, Prywatne/Miasto 724 MB, kasyno T11, metro, VIP:
// jedna tekstura 2048² = 22 MB zamiast ~2 MB w ASTC — w paczce i w pamięci telefonu przy każdym wczytanym kawałku.
// Ostatni krok przed podziałem sceny na obszary (BraxChampionsBuild):
//   - dla każdej nieskompresowanej tekstury z .glb używanej przez renderer sceny jej ORYGINALNY obraz (PNG/JPEG z bloku
//     BIN pliku .glb, bajt w bajt) trafia do Assets/PlayableGenerated/Mistrzowie/TeksturyGlb jako zwykły plik obrazu,
//     importowany z kompresją ASTC 6x6 (mipmapy, bez kopii do odczytu, rozdzielczość bez zmian, najwyżej 4096);
//   - materiał z taką teksturą dostaje kopię (PlayableGenerated/Mistrzowie/MaterialyGlb) z podpiętymi skompresowanymi
//     obrazami; renderer sceny wskazuje na kopię. Pliki .glb, ich meta i pliki innych rejonów — nietknięte.
//   - GUID pliku obrazu jest stały (z nazwy), więc zapisana pamięć importu (Library) nie kompresuje go drugi raz.
// Obraz, którego nie da się jednoznacznie znaleźć w pliku (dwa obrazy o tej samej nazwie, obraz zewnętrzny, WebP/KTX),
// zostaje oryginalny — nigdy pusta tekstura. Błąd całego kroku = scena bez zmian (nie jest zapisywana), build idzie dalej.
public static class BraxChampionsTeksturyGlb
{
    public const string Folder = "Assets/PlayableGenerated/Mistrzowie/TeksturyGlb";
    const string MatFolder = "Assets/PlayableGenerated/Mistrzowie/MaterialyGlb";

    [Serializable] sealed class GImage { public string name; public string mimeType; public string uri; public int bufferView; }
    [Serializable] sealed class GView { public int buffer; public int byteOffset; public int byteLength; }
    [Serializable] sealed class GRoot { public GImage[] images; public GView[] bufferViews; }

    sealed class Obraz { public int index; public int offset, length; public string ext; }
    sealed class Plik { public byte[] bytes; public Dictionary<string, Obraz> poNazwie; }

    static bool Nieskompresowana(TextureFormat f) =>
        f == TextureFormat.RGBA32 || f == TextureFormat.ARGB32 || f == TextureFormat.RGB24 || f == TextureFormat.BGRA32;

    static bool Glb(string path) => path.EndsWith(".glb", StringComparison.OrdinalIgnoreCase);

    // the images of one .glb by the name glTFast gives the texture (GltfImport.GetTextureName: image name or "image_<i>");
    // a name used twice maps to null (ambiguous -> the texture stays as it is)
    static Plik Czytaj(string path)
    {
        var b = File.ReadAllBytes(path);
        if (b.Length < 20 || BitConverter.ToUInt32(b, 0) != 0x46546C67u) return null;              // "glTF"
        int jsonLen = BitConverter.ToInt32(b, 12);
        if (BitConverter.ToUInt32(b, 16) != 0x4E4F534Au || 20 + jsonLen > b.Length) return null;   // "JSON"
        int p = 20 + jsonLen, bin = -1, binLen = 0;
        if (p + 8 <= b.Length && BitConverter.ToUInt32(b, p + 4) == 0x004E4942u) { binLen = BitConverter.ToInt32(b, p); bin = p + 8; }  // "BIN\0"
        var root = JsonUtility.FromJson<GRoot>(Encoding.UTF8.GetString(b, 20, jsonLen));
        var plik = new Plik { bytes = b, poNazwie = new Dictionary<string, Obraz>() };
        if (root?.images == null) return plik;
        for (int i = 0; i < root.images.Length; i++)
        {
            var img = root.images[i];
            string nazwa = string.IsNullOrEmpty(img.name) ? "image_" + i : img.name;
            Obraz o = null;
            if (string.IsNullOrEmpty(img.uri) && bin >= 0 && root.bufferViews != null && img.bufferView >= 0 && img.bufferView < root.bufferViews.Length)
            {
                var v = root.bufferViews[img.bufferView];
                int start = bin + v.byteOffset;
                if (v.buffer == 0 && v.byteLength > 8 && v.byteOffset >= 0 && v.byteOffset + v.byteLength <= binLen && start + v.byteLength <= b.Length)
                {
                    // the bytes must really be the image the JSON says: PNG or JPEG signature
                    bool png = b[start] == 0x89 && b[start + 1] == 0x50 && b[start + 2] == 0x4E && b[start + 3] == 0x47;
                    bool jpg = b[start] == 0xFF && b[start + 1] == 0xD8 && b[start + 2] == 0xFF;
                    if (png || jpg) o = new Obraz { index = i, offset = start, length = v.byteLength, ext = png ? "png" : "jpg" };
                }
            }
            if (plik.poNazwie.ContainsKey(nazwa)) plik.poNazwie[nazwa] = null; else plik.poNazwie[nazwa] = o;
        }
        return plik;
    }

    static string StalyGuid(string klucz)
    {
        using (var md5 = MD5.Create())
            return string.Concat(md5.ComputeHash(Encoding.UTF8.GetBytes("brax-champions-glb-texture/" + klucz)).Select(x => x.ToString("x2")));
    }

    /// <summary>Swaps the uncompressed .glb textures of the scene's renderers for compressed copies and saves the scene.
    /// Returns the number of textures swapped.</summary>
    public static int Zastosuj(Scene scene)
    {
        var t0 = DateTime.UtcNow;
        var renderers = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)).ToArray();

        // 1. every uncompressed .glb texture used by a material of the scene
        var pliki = new Dictionary<string, Plik>();
        var plan = new Dictionary<Texture2D, string>();                     // texture -> image file to write
        int pominiete = 0; long przed = 0;
        foreach (var r in renderers)
        foreach (var m in r.sharedMaterials)
        {
            if (!m) continue;
            foreach (int id in m.GetTexturePropertyNameIDs())
            {
                var tex = m.GetTexture(id) as Texture2D;
                if (!tex || plan.ContainsKey(tex) || !Nieskompresowana(tex.format)) continue;
                string path = AssetDatabase.GetAssetPath(tex);
                if (string.IsNullOrEmpty(path) || !Glb(path)) continue;
                if (!pliki.TryGetValue(path, out var plik)) { try { plik = Czytaj(path); } catch (Exception) { plik = null; } pliki[path] = plik; }
                if (plik == null || !plik.poNazwie.TryGetValue(tex.name, out var o) || o == null) { pominiete++; plan[tex] = null; continue; }
                bool liniowa = !GraphicsFormatUtility.IsSRGBFormat(tex.graphicsFormat);
                string zawijanie = tex.wrapModeU == TextureWrapMode.Clamp ? "__clamp" : tex.wrapModeU == TextureWrapMode.Mirror ? "__mirror" : "";
                string plikObrazu = Folder + "/" + AssetDatabase.AssetPathToGUID(path) + "/" + o.index + (liniowa ? "__lin" : "") + zawijanie + "." + o.ext;
                plan[tex] = plikObrazu;
                przed += Mathf.RoundToInt(tex.width * tex.height * (tex.format == TextureFormat.RGB24 ? 3 : 4) * (tex.mipmapCount > 1 ? 1.333f : 1f));
                if (!File.Exists(plikObrazu))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(plikObrazu));
                    using (var f = new FileStream(plikObrazu, FileMode.Create, FileAccess.Write)) f.Write(plik.bytes, o.offset, o.length);
                    File.WriteAllText(plikObrazu + ".meta", "fileFormatVersion: 2\nguid: " + StalyGuid(plikObrazu.Substring(Folder.Length + 1)) + "\n");
                }
            }
        }
        pliki.Clear();
        int doZamiany = plan.Values.Count(v => v != null);
        if (doZamiany == 0)
        {
            Debug.Log("BRAX_GLB_TEXTURES none to compress (skipped " + pominiete + ")");
            return 0;
        }

        // 2. the images are imported compressed (BraxChampionsTeksturyGlbImport)
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var nowe = new Dictionary<Texture2D, Texture2D>();
        foreach (var kv in plan)
        {
            if (kv.Value == null) continue;
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(kv.Value);
            if (t) nowe[kv.Key] = t; else { pominiete++; Debug.LogWarning("BRAX_GLB_TEXTURES not imported: " + kv.Value); }
        }

        // 3. a copy of every material that uses one of them, the scene's renderers pointed at the copies
        BraxChampionsMeshBatch.Folder(MatFolder);
        var kopie = new Dictionary<Material, Material>();
        int rendererow = 0;
        foreach (var r in renderers)
        {
            var mats = r.sharedMaterials; bool zmiana = false;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (!m) continue;
                if (!kopie.TryGetValue(m, out var c))
                {
                    c = null;
                    var ids = m.GetTexturePropertyNameIDs().Where(id => m.GetTexture(id) is Texture2D t && nowe.ContainsKey(t)).ToArray();
                    if (ids.Length != 0)
                    {
                        c = new Material(m) { name = m.name };
                        foreach (int id in ids) c.SetTexture(id, nowe[(Texture2D)m.GetTexture(id)]);
                        string nazwa = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m, out string g, out long lid) ? g + "_" + lid.ToString("x") : Guid.NewGuid().ToString("N");
                        BraxZapisAssetu.Utworz(c, MatFolder + "/" + nazwa + ".mat");
                    }
                    kopie[m] = c;
                }
                if (c) { mats[i] = c; zmiana = true; }
            }
            if (!zmiana) continue;
            r.sharedMaterials = mats;
            if (PrefabUtility.IsPartOfPrefabInstance(r)) PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            EditorUtility.SetDirty(r);
            rendererow++;
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("scene not saved after the texture swap");

        long po = nowe.Values.Sum(t => (long)(Mathf.CeilToInt(t.width / 6f) * Mathf.CeilToInt(t.height / 6f) * 16 * (t.mipmapCount > 1 ? 1.333f : 1f)));
        Debug.Log("BRAX_GLB_TEXTURES compressed=" + nowe.Count + " skipped=" + pominiete + " materials=" + kopie.Values.Count(c => c) +
                  " renderers=" + rendererow + " before~" + (przed >> 20) + " MB after~" + (po >> 20) + " MB t=" +
                  (DateTime.UtcNow - t0).TotalSeconds.ToString("F0") + " s");
        return nowe.Count;
    }
}

// the import settings of the extracted images: ASTC 6x6 on Android, mipmaps, no CPU copy, the original resolution
// ("__lin" = data image read linear by glTFast: normals, metallic-roughness, occlusion; "__clamp"/"__mirror" = wrap)
public sealed class BraxChampionsTeksturyGlbImport : AssetPostprocessor
{
    public override int GetPostprocessOrder() { return 200; }

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(BraxChampionsTeksturyGlb.Folder + "/")) return;
        var t = (TextureImporter)assetImporter;
        string n = Path.GetFileNameWithoutExtension(assetPath);
        t.textureType = TextureImporterType.Default;
        t.sRGBTexture = !n.Contains("__lin");
        t.mipmapEnabled = true; t.isReadable = false; t.anisoLevel = 4;
        t.npotScale = TextureImporterNPOTScale.None;
        t.alphaSource = TextureImporterAlphaSource.FromInput; t.alphaIsTransparency = false;
        t.wrapMode = n.Contains("__clamp") ? TextureWrapMode.Clamp : n.Contains("__mirror") ? TextureWrapMode.Mirror : TextureWrapMode.Repeat;
        t.maxTextureSize = 4096; t.textureCompression = TextureImporterCompression.Compressed;
        var ps = t.GetPlatformTextureSettings("Android");
        ps.name = "Android"; ps.overridden = true; ps.maxTextureSize = 4096;
        ps.format = TextureImporterFormat.ASTC_6x6; ps.textureCompression = TextureImporterCompression.Compressed;
        ps.compressionQuality = 50;                                                    // normal (R1 3.11: 100 = minutes per image)
        t.SetPlatformTextureSettings(ps);
    }
}
