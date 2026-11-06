using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

// Real Unity 6 + same glTFast version as the game. Read-only source import QA.
// No scene generation, no content material changes, no APK signing/compilation.
public static class UnityTwoGlbImportSmoke
{
    [Serializable] sealed class Model
    {
        public string path;
        public string sourceSha256;
        public string state = "NOT_TESTED";
        public string importer;
        public int meshes;
        public int vertices;
        public int materials;
        public int errors;
        public List<string> messages = new List<string>();
    }
    [Serializable] sealed class Report
    {
        public string unity;
        public string status;
        public bool apkBuilt = false;
        public bool originalVisualsModified = false;
        public int expected = 2;
        public int checkedModels;
        public int accepted;
        public List<Model> models = new List<Model>();
    }
    static string Sha256(string path)
    {
        using(var stream=File.OpenRead(path))
        using(var sha=SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
    }
    static Model Audit(string path)
    {
        var record=new Model { path=path };
        try
        {
            if(!File.Exists(path)) throw new FileNotFoundException("Source GLB not staged",path);
            record.sourceSha256=Sha256(path);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport |
                                         ImportAssetOptions.ForceUpdate);
            var importer=AssetImporter.GetAtPath(path);
            record.importer=importer?importer.GetType().FullName:"NULL";
            if(!importer)throw new InvalidOperationException("GLB has no importer");
            var imported=new SerializedObject(importer);
            var items=imported.FindProperty("reportItems");
            if(items!=null && items.isArray)
            {
                for(int i=0;i<items.arraySize;i++)
                {
                    var item=items.GetArrayElementAtIndex(i);
                    var type=item.FindPropertyRelative("type");
                    var code=item.FindPropertyRelative("code");
                    var args=item.FindPropertyRelative("messages");
                    if(type==null)continue;
                    var severity=(LogType)type.intValue;
                    string message=severity+" code="+(code!=null?code.intValue.ToString():"?")+" ";
                    if(args!=null && args.isArray)
                        for(int j=0;j<args.arraySize;j++)
                            message+=args.GetArrayElementAtIndex(j).stringValue+" ";
                    if(severity==LogType.Error || severity==LogType.Exception ||
                       severity==LogType.Assert)record.errors++;
                    record.messages.Add(message.Trim());
                }
            }
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(path) ??
                AssetDatabase.LoadAllAssetsAtPath(path).OfType<GameObject>().FirstOrDefault();
            if(!model)throw new InvalidOperationException("Unity imported no GameObject prefab");
            foreach(var f in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if(!f.sharedMesh)continue;
                record.meshes++;record.vertices+=f.sharedMesh.vertexCount;
            }
            foreach(var s in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if(!s.sharedMesh)continue;
                record.meshes++;record.vertices+=s.sharedMesh.vertexCount;
            }
            record.materials=model.GetComponentsInChildren<Renderer>(true)
                .SelectMany(x=>x.sharedMaterials).Where(x=>x).Distinct().Count();
            if(record.meshes<=0)throw new InvalidOperationException("Imported prefab has no meshes");
            if(record.errors>0)
                record.state="IMPORTED_WITH_GLTF_ERRORS_MANUAL_REVIEW_REQUIRED";
            else
                record.state="IMPORTED_WITHOUT_GLTF_ERRORS";
        }
        catch(Exception ex)
        {
            record.state="IMPORT_FAILED";
            record.errors++;
            record.messages.Add(ex.ToString());
        }
        Debug.Log("BRAX_REAL_GLTF_SMOKE "+path+" "+record.state+
                  " meshes="+record.meshes+" vertices="+record.vertices+
                  " materials="+record.materials+" errors="+record.errors);
        return record;
    }
    public static void Run()
    {
        var report=new Report { unity=Application.unityVersion };
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var path in new[]{
                "Assets/Source/Mistrzowie/PoolP1/OwnerEquipment/stack_of_towels_2k.glb",
                "Assets/Source/HarborBeach/Models/harbor_crane.glb"})
            {
                var result=Audit(path);
                report.models.Add(result);
                report.checkedModels++;
                if(result.state=="IMPORTED_WITHOUT_GLTF_ERRORS")report.accepted++;
            }
            report.status=report.accepted==report.expected
                ?"UNITY_2_GLB_IMPORT_PASS":"UNITY_2_GLB_IMPORT_ISSUES_FOUND";
        }
        catch(Exception ex)
        {
            report.status="UNITY_2_GLB_SETUP_ERROR";
            Debug.LogException(ex);
        }
        finally
        {
            var output=Path.GetFullPath("../mistrzowie-glb-unity-evidence");
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,"unity2-glb-import-report.json"),
                              JsonUtility.ToJson(report,true));
            Debug.Log("BRAX_REAL_GLTF_REPORT "+report.status+" accepted="+
                      report.accepted+"/"+report.expected);
        }
        if(report.status!="UNITY_2_GLB_IMPORT_PASS")
            throw new InvalidOperationException("Real Unity glTFast import not clean: "+report.status);
    }
}
