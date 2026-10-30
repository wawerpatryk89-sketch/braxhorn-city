using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Unity 6 Editor-only scene registry. Entries must refer to saved, real Unity
// scenes. Adding future districts never silently invents buildings or alters Brax.
public static class BraxUnityDistrictRegistry
{
    const string RegistryPath="Assets/UnityDistricts/registry.json";
    [Serializable] private sealed class District
    {
        public string id,scenePath;
        public bool buildEnabled;
    }
    [Serializable] private sealed class Registry
    {
        public int schemaVersion;
        public string engine,startDistrict;
        public District[] districts;
    }
    [Serializable] private sealed class BuildReceipt
    {
        public string engine="Unity6",commit,entryDistrict="city",status="BUILT_UNITY_TEST__VISUAL_ACCEPTANCE_PENDING";
        public string[] scenePaths,districtIds;
        public bool originalBraxPreserved=true,originalGymSourcePreserved=true,productionChanged=false;
        public bool externalThreeRuntimeUsed=false;
    }
    static Registry Read()
    {
        if(!File.Exists(RegistryPath))throw new InvalidOperationException("Unity6 district registry missing: "+RegistryPath);
        var data=JsonUtility.FromJson<Registry>(File.ReadAllText(RegistryPath));
        if(data==null||data.schemaVersion!=1||data.engine!="Unity6"||data.startDistrict!="city"||data.districts==null)
            throw new InvalidOperationException("Invalid Unity6 district registry contract");
        return data;
    }
    public static string[] Resolve(string cityScene,string estateScene)
    {
        var data=Read();
        var used=new HashSet<string>(StringComparer.Ordinal);
        var scenes=new List<string>();
        foreach(var item in data.districts)
        {
            if(item==null||string.IsNullOrWhiteSpace(item.id)||!used.Add(item.id))
                throw new InvalidOperationException("Invalid or duplicate Unity6 district identity");
            if(!item.buildEnabled)continue;
            if(string.IsNullOrWhiteSpace(item.scenePath)
                ||!item.scenePath.StartsWith("Assets/",StringComparison.Ordinal)
                ||!item.scenePath.EndsWith(".unity",StringComparison.Ordinal)
                ||!File.Exists(item.scenePath))
                throw new InvalidOperationException("Missing REAL imported Unity6 scene for district "+item.id);
            if(scenes.Contains(item.scenePath))
                throw new InvalidOperationException("Duplicate Unity6 scene "+item.scenePath);
            scenes.Add(item.scenePath);
        }
        // Keep the approved original gym+city as entry scene; estate is second.
        if(scenes.Count<2||scenes[0]!=cityScene||scenes[1]!=estateScene
           ||data.districts[0].id!="city"||data.districts[1].id!="estate")
            throw new InvalidOperationException("Unity6 integration must start in unchanged gym+city and include real estate");
        Debug.Log("UNITY6_DISTRICTS_SCENES_VERIFIED "+string.Join(" | ",scenes));
        return scenes.ToArray();
    }
    public static void WriteBuildReceipt(string output,string commit,string[] scenes)
    {
        var data=Read();
        var ids=new List<string>();
        foreach(var district in data.districts)if(district.buildEnabled)ids.Add(district.id);
        if(ids.Count!=scenes.Length)throw new InvalidOperationException("Unity6 district count changed during build");
        var receipt=new BuildReceipt{commit=commit,scenePaths=scenes,districtIds=ids.ToArray()};
        File.WriteAllText(Path.Combine(output,"unity6-district-registry.json"),JsonUtility.ToJson(receipt,true));
    }
}
