using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Executes real Unity transform/physics behavior in an isolated temporary
// scene. Test shapes are destroyed and never become part of the camp.
public static class BraxObozPlacementRegression
{
    [Serializable] sealed class Result
    {
        public string revision;
        public int passed,failed;
        public string[] checks;
    }

    public static void Run()
    {
        var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var checks=new List<string>();int passed=0,failed=0;
        void Check(bool ok,string name)
        {
            checks.Add((ok?"PASS ":"FAIL ")+name);
            if(ok)passed++;else failed++;
        }
        try
        {
            const float k=BraxObozPlacement.K;
            foreach(float yaw in new[]{0f,90f,-90f,37f})
            foreach(float parentScale in new[]{1f,k})
            {
                var frame=new GameObject("test frame").transform;
                frame.position=new Vector3(9000,0,9000);
                frame.rotation=Quaternion.Euler(0,yaw,0);
                frame.localScale=Vector3.one*parentScale;
                var model=new GameObject("test model").transform;model.SetParent(frame,false);
                var geometry=GameObject.CreatePrimitive(PrimitiveType.Cube);
                geometry.transform.SetParent(model,false);
                geometry.transform.localScale=new Vector3(.9f,.45f,2f);
                var renderers=geometry.GetComponentsInChildren<Renderer>();
                BraxObozPlacement.RealDimension(model,renderers,2f,
                    BraxObozPlacement.Axis.Z,"regression",frame);
                // Independent measurement of two known mesh endpoints. This
                // does not call the dimension helper being tested.
                float length=Vector3.Distance(geometry.transform.TransformPoint(Vector3.forward*.5f),
                    geometry.transform.TransformPoint(Vector3.back*.5f));
                float width=Vector3.Distance(geometry.transform.TransformPoint(Vector3.right*.5f),
                    geometry.transform.TransformPoint(Vector3.left*.5f));
                Check(Mathf.Abs(length-2f*k)<.001f && Mathf.Abs(width-.9f*k)<.001f,
                    "bed yaw="+yaw+" parentScale="+parentScale+" length="+length+" width="+width);
                Object.DestroyImmediate(frame.gameObject);
            }
            var ground=new GameObject("test floor");ground.transform.position=new Vector3(9100,-.1f,9100);
            var floor=ground.AddComponent<BoxCollider>();floor.size=new Vector3(8,.2f,8);
            var placed=new GameObject("test complete model").transform;
            var child=GameObject.CreatePrimitive(PrimitiveType.Cube);child.transform.SetParent(placed,false);
            child.transform.localScale=new Vector3(1,.7f,2);
            var geometryBounds=child.GetComponentsInChildren<Renderer>();
            var ownCollider=placed.gameObject.AddComponent<BoxCollider>();ownCollider.size=new Vector3(1,.7f,2);
            bool supported=BraxObozPlacement.OnGround(placed,geometryBounds,
                new Vector3(9100,.2f,9100),"regression complete object collider");
            Physics.SyncTransforms();
            Check(supported && Mathf.Abs(child.GetComponent<Renderer>().bounds.min.y)<.001f,
                "ground contact excludes both model parent and mesh child colliders");
            Object.DestroyImmediate(placed.gameObject);Object.DestroyImmediate(ground);
            foreach(var finding in BraxObozFootContactRegression.Run())
                Check(finding.StartsWith("PASS ",StringComparison.Ordinal),finding.Substring(5));
            foreach(var finding in BraxObozRuntimeRegression.Run())
                Check(finding.StartsWith("PASS ",StringComparison.Ordinal),finding.Substring(5));
            foreach(var finding in BraxObozDiningRegression.Run())
                Check(finding.StartsWith("PASS ",StringComparison.Ordinal),finding.Substring(5));
        }
        catch(Exception ex) {failed++;checks.Add("FAIL exception: "+ex);}
        finally
        {
            EditorSceneManager.CloseScene(scene,true);
            if(previous.IsValid())SceneManager.SetActiveScene(previous);
        }
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../audit"));
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder,"oboz-regresje-jednostek.json"),JsonUtility.ToJson(new Result {
            revision=Environment.GetEnvironmentVariable("GITHUB_SHA")??"local",
            passed=passed,failed=failed,checks=checks.ToArray()},true));
        Debug.Log("BRAXHORN_OBOZ_PLACEMENT_REGRESSION passed="+passed+" failed="+failed);
        if(failed>0)BraxBuildQuality.Warn("OBÓZ REGRESJA STAWIANIA — "+string.Join("; ",checks));
    }
}
