using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// City-owned input for the shared update tools. This is a registry, not an updater.
// Content and private models stay in the encrypted generated-world artifact.
public static class BraxCityZoneRegistry
{
    public const string RegistryPath = "Assets/PlayableGenerated/miasto-strefy.json";
    static readonly Regex Sector = new Regex(@"^BraxhornCity_K_x(-?\d+)_z(-?\d+)$");

    [Serializable] class Dependency { public string path, guid, hash; }
    [Serializable] class Entry { public string address, scenePath, guid, hash; public Dependency[] dependencies; }
    [Serializable] class Registry
    {
        public int schema = 1;
        public string zone = "miasto", unity, sourceCommit, platform;
        public string delivery = "embedded-apk"; // Do not claim Addressables delivery before the shared loader exists.
        public Entry[] scenes;
    }

    public static string SectorSceneName(int x, int z)
    {
        return "BraxhornCity_K_x" + x.ToString(CultureInfo.InvariantCulture) + "_z" + z.ToString(CultureInfo.InvariantCulture);
    }

    static string Address(string path)
    {
        string name = Path.GetFileNameWithoutExtension(path);
        switch (name)
        {
            case "BraxhornCity": return "miasto/scenes/centrum";
            case "BraxhornCity_Nabrzeze": return "miasto/scenes/port";
            case "BraxhornCity_Wyspa": return "miasto/scenes/wyspa";
        }
        var match = Sector.Match(name);
        if (match.Success) return "miasto/scenes/sektory/x" + match.Groups[1].Value + "_z" + match.Groups[2].Value;
        throw new InvalidOperationException("City scene has no stable update address: " + path);
    }

    public static void Write(IEnumerable<string> scenes, string sourceCommit, BuildTarget target)
    {
        var entries = new List<Entry>();
        var addresses = new HashSet<string>(StringComparer.Ordinal);
        foreach (string path in scenes.Where(p => Path.GetFileName(p).StartsWith("BraxhornCity", StringComparison.Ordinal)).Distinct().OrderBy(p => p, StringComparer.Ordinal))
        {
            string address = Address(path), guid = AssetDatabase.AssetPathToGUID(path);
            if (!File.Exists(path) || string.IsNullOrEmpty(guid) || !File.Exists(path + ".meta"))
                throw new InvalidOperationException("City update registry requires a saved scene and .meta: " + path);
            if (!addresses.Add(address)) throw new InvalidOperationException("Duplicate city address: " + address);
            var dependencies = AssetDatabase.GetDependencies(path, true).Where(p => p != path).OrderBy(p => p, StringComparer.Ordinal)
                .Select(p => new Dependency { path = p, guid = AssetDatabase.AssetPathToGUID(p), hash = AssetDatabase.GetAssetDependencyHash(p).ToString() }).ToArray();
            entries.Add(new Entry { address = address, scenePath = path, guid = guid,
                hash = AssetDatabase.GetAssetDependencyHash(path).ToString(), dependencies = dependencies });
        }
        if (entries.Count == 0) throw new InvalidOperationException("Empty city update registry");
        var registry = new Registry { unity = Application.unityVersion, sourceCommit = sourceCommit,
            platform = target.ToString(), scenes = entries.ToArray() };
        File.WriteAllText(RegistryPath, JsonUtility.ToJson(registry, true));
        Debug.Log("BRAXHORN_CITY_REGISTRY scenes=" + entries.Count + " delivery=embedded-apk path=" + RegistryPath);
    }
}
