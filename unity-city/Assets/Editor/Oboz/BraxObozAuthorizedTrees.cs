using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

// Only the two exact Poly Haven sources named by the owner. The original six
// fir prototypes keep their indices; these definitions are appended after them.
public static class BraxObozAuthorizedTrees
{
    public const int LegacyCount=6;
    public const int MaximumPerSpecies=12;
    public const string Root="Assets/Source/Prywatne/Oboz/environment/vegetation/";
    public const string CatalogPath=Root+"authorized-tree-catalog.json";
    static readonly string[] Names={"pine_tree_01_b","tree_small_02"};
    static readonly string[] OriginalSix={"fir_tree_01_a","fir_tree_01_b","fir_tree_01_c",
        "fir_sapling_medium_a","fir_sapling_medium_b","fir_sapling_medium_c"};
    static readonly string[] BlendHashes={
        "788d5839aa1d256c20e50adc53ae830b35511d8bb2bc7c16d110dcfc0037651e",
        "3c28e4ebded582d6539afeac7e5ccce6cb950830a94f1fe38135a45264145de5"};
    [Serializable] public sealed class LodFile
    {
        public int level,triangles,sourceTriangles,vertices;
        public string file,sha256,sourceObject;
        public long bytes,gltfGeometryBufferViewBytes;
    }
    [Serializable] public sealed class TextureFile
    {
        public string file,sha256;
        public long bytes;
        public int width,height;
    }
    [Serializable] public sealed class Definition
    {
        public string name,sourceAsset,sourceBlendSha256,license,nearFile,midFile;
        public bool missingAuthoredLod2;
        public int maxSceneInstances;
        public LodFile[] lods;
        public BraxObozVegetation.Atlas billboard;
        public string[] knownLimitations;
    }
    [Serializable] sealed class Catalog
    {
        public int schema;
        public bool complete,lod0Imported,geometryDecimated;
        public string assetRoot;
        public Definition[] prototypes;
        public TextureFile[] textures;
    }
    [Serializable] sealed class SceneRow
    {
        public string name;
        public int prototypeIndex,instances,groundMeasured,nearTriangles,midTriangles;
        public bool missingAuthoredLod2;
        public float[] referenceDistancesMetres;
    }
    [Serializable] sealed class SceneReport
    {
        public string title="OBÓZ TRENINGOWY — wskazane gatunki w rzeczywistej scenie";
        public string revision;
        public bool apkBuilt=false,phoneVerified=false,lod0Imported=false;
        public bool originalSixIndicesPreserved;
        public int maximumPerSpecies=MaximumPerSpecies;
        public float referenceVerticalFov=60,referenceLodBias=1;
        public SceneRow[] species;
        public string[] warnings;
    }
    static readonly List<string> Warnings=new List<string>();
    static void Warn(string text)
    {
        Warnings.Add(text);BraxBuildQuality.Warn("OBÓZ TRENINGOWY — wskazane drzewa: "+text);
    }
    static string Full(string asset)=>Path.GetFullPath(Path.Combine(Application.dataPath,"..",asset));
    static void VerifyFile(string relative,long bytes,string expected)
    {
        if(string.IsNullOrEmpty(relative)||relative.Contains("..")||relative.Contains('\\')||Path.IsPathRooted(relative))
            throw new InvalidDataException("nieprawidłowa ścieżka źródłowa");
        string file=Full(Root+relative);
        if(!File.Exists(file)||new FileInfo(file).Length!=bytes||string.IsNullOrEmpty(expected))
            throw new InvalidDataException("brak lub zmieniony plik "+relative);
        using(var stream=File.OpenRead(file))
        using(var sha=SHA256.Create())
            if(BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant()!=expected)
                throw new InvalidDataException("SHA256 źródła niezgodne: "+relative);
    }
    public static Definition[] Load()
    {
        Warnings.Clear();
        try
        {
            if(!File.Exists(Full(CatalogPath)))throw new FileNotFoundException("brak zweryfikowanego katalogu; zachowano istniejące jodły");
            var catalog=JsonUtility.FromJson<Catalog>(File.ReadAllText(Full(CatalogPath)));
            if(catalog==null||catalog.schema!=1||!catalog.complete||catalog.assetRoot!=Root||
                catalog.lod0Imported||catalog.geometryDecimated||catalog.prototypes==null||catalog.prototypes.Length!=2)
                throw new InvalidDataException("katalog nie opisuje dokładnie dwóch oryginalnych gatunków");
            for(int i=0;i<Names.Length;i++)
            {
                var definition=catalog.prototypes[i];int[] counts=i==0?new[]{514980,267343}:new[]{495533};
                if(definition.name!=Names[i]||definition.sourceAsset!=(i==0?"pine_tree_01":"tree_small_02")||
                    definition.sourceBlendSha256!=BlendHashes[i]||definition.license!="CC0-1.0"||
                    definition.maxSceneInstances!=MaximumPerSpecies||definition.lods==null||definition.lods.Length!=counts.Length||
                    definition.missingAuthoredLod2!=(i==1)||definition.nearFile!=Names[i]+"_LOD1__unity.glb"||
                    (i==0?definition.midFile!=Names[i]+"_LOD2__unity.glb":!string.IsNullOrEmpty(definition.midFile)))
                    throw new InvalidDataException("niezgodna definicja źródła "+Names[i]);
                for(int level=0;level<counts.Length;level++)
                {
                    var file=definition.lods[level];
                    if(file.level!=level+1||file.triangles!=counts[level]||file.sourceTriangles!=counts[level]||
                        file.file!=Names[i]+"_LOD"+(level+1)+"__unity.glb")
                        throw new InvalidDataException("niezgodny autorski poziom LOD "+Names[i]);
                    VerifyFile(file.file,file.bytes,file.sha256);
                }
                if(definition.billboard==null||!definition.billboard.sourceBoundsMeasured||!definition.billboard.trunkCenteredSpan||
                    definition.billboard.model!=Names[i]+"_LOD"+counts.Length+".glb"||
                    definition.billboard.file!=Names[i]+"_Billboard_Color.png")
                    throw new InvalidDataException("brak zgodnego atlasu źródłowego "+Names[i]);
                foreach(string limitation in definition.knownLimitations??Array.Empty<string>())Warn(limitation);
            }
            if(catalog.textures==null||catalog.textures.Length==0)throw new InvalidDataException("brak źródłowych map drzew");
            foreach(var image in catalog.textures)
            {
                if(image.width<=0||image.height<=0||image.width>2048||image.height>2048||
                    !(image.file.StartsWith("authorized_forest_textures/",StringComparison.Ordinal)||image.file.StartsWith("billboards/",StringComparison.Ordinal)))
                    throw new InvalidDataException("niezgodna mapa 2K "+image.file);
                VerifyFile(image.file,image.bytes,image.sha256);
            }
            return catalog.prototypes;
        }
        catch(Exception ex){Warn(ex.ToString());return Array.Empty<Definition>();}
    }
    static int Triangles(Renderer[] renderers)
    {
        int count=0;
        foreach(var renderer in renderers)
        {
            var filter=renderer?renderer.GetComponent<MeshFilter>():null;
            if(!filter||!filter.sharedMesh)continue;
            for(int sub=0;sub<filter.sharedMesh.subMeshCount;sub++)
                if(filter.sharedMesh.GetTopology(sub)==MeshTopology.Triangles)
                    count+=checked((int)filter.sharedMesh.GetIndexCount(sub)/3);
        }
        return count;
    }
    public static int Plant(Transform forest,GameObject[] prototypes,Definition[] definitions)
    {
        var rows=new List<SceneRow>();int total=0;
        for(int species=0;species<definitions.Length;species++)
        {
            int index=LegacyCount+species;var definition=definitions[species];
            var row=new SceneRow {name=definition.name,prototypeIndex=index,
                missingAuthoredLod2=definition.missingAuthoredLod2,
                referenceDistancesMetres=species==0?new[]{20f,35f,300f}:new[]{20f,300f}};
            rows.Add(row);
            if(index>=prototypes.Length||!prototypes[index]){Warn(definition.name+": brak prototypu po imporcie");continue;}
            var group=prototypes[index].GetComponent<LODGroup>();
            if(!group){Warn(definition.name+": brak źródłowej grupy LOD po imporcie");continue;}
            var lods=group.GetLODs();
            if(lods.Length!=(definition.missingAuthoredLod2?2:3))
            {Warn(definition.name+": liczba poziomów LOD niezgodna z katalogiem; nie rozmieszczono");continue;}
            row.nearTriangles=Triangles(lods[0].renderers);
            row.midTriangles=definition.missingAuthoredLod2?0:Triangles(lods[1].renderers);
            if(row.nearTriangles!=definition.lods[0].triangles||
                (!definition.missingAuthoredLod2&&row.midTriangles!=definition.lods[1].triangles))
            {Warn(definition.name+": trójkąty importu niezgodne z autorskim źródłem; nie rozmieszczono");continue;}
            for(int slot=0;slot<MaximumPerSpecies;slot++)
            {
                // Twenty-four dispersed positions around the outer forest.
                // Existing fir indices/layout and the front entrance stay intact.
                float angle=(slot*2+species+.43f)*Mathf.PI*2/(MaximumPerSpecies*2);
                const float offset=13f;
                float x=(BraxObozCampusLayout.PlotRadiusX+offset)*Mathf.Cos(angle);
                float z=BraxObozCampusLayout.PlotCenterZ+(BraxObozCampusLayout.PlotRadiusZ+offset)*Mathf.Sin(angle);
                if(Mathf.Abs(x)>135f||Mathf.Abs(z)>135f||z< -105f&&Mathf.Abs(x)<23f||BraxObozCampusLayout.Reserved(x,z,3f))continue;
                var tree=Object.Instantiate(prototypes[index],forest);
                tree.name=definition.name+" — wskazany gatunek przy ogrodzeniu";
                tree.transform.localPosition=new Vector3(x,0,z)*BraxObozPlacement.K;
                tree.transform.localRotation=Quaternion.Euler(0,(slot*137+species*53)%360,0);
                tree.transform.localScale=Vector3.one*BraxObozPlacement.K;
                if(BraxObozTreePlacement.Place(tree.transform))row.groundMeasured++;
                row.instances++;total++;
            }
            if(row.instances==0)Warn(definition.name+": nie znaleziono miejsca poza dojazdem i pasem bieżni");
        }
        var report=new SceneReport {revision=Environment.GetEnvironmentVariable("GITHUB_SHA")??"local",
            originalSixIndicesPreserved=prototypes.Length>=LegacyCount&&
                OriginalSix.Select((name,index)=>prototypes[index]&&prototypes[index].name==name).All(equal=>equal),
            species=rows.ToArray(),warnings=Warnings.ToArray()};
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../build/CampLifeReview"));
        Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"authorized-tree-scene.json"),JsonUtility.ToJson(report,true));
        Debug.Log("BRAXHORN_OBOZ_NAMED_TREES actual="+total+" maximum_each="+MaximumPerSpecies+" source_lod0=false apk=false");
        return total;
    }
}
