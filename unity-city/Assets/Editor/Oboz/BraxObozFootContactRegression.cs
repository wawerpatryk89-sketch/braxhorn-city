using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

// Real Unity transforms, skin bindposes and serialized pivot state. These tiny
// test meshes are destroyed; they never replace any object in the camp scene.
public static class BraxObozFootContactRegression
{
    public static string[] Run()
    {
        const float k=1.294f;
        var results=new List<string>();
        void Check(bool ok,string label)=>results.Add((ok?"PASS ":"FAIL ")+label);
        foreach(float parentScale in new[]{1f,k})
        {
            GameObject frame=null;Mesh mesh=null;
            string label="foot contact parentScale="+parentScale;
            try
            {
                frame=new GameObject("temporary foot contact regression");
                frame.transform.position=new Vector3(9200,0,9200);
                frame.transform.rotation=Quaternion.Euler(0,37f,0);
                frame.transform.localScale=Vector3.one*parentScale;
                var actor=new GameObject("temporary actor");actor.transform.SetParent(frame.transform,false);
                var body=actor.AddComponent<CharacterController>();body.height=2f;body.center=Vector3.up;
                var visual=new GameObject("temporary normalized visual").transform;
                visual.SetParent(actor.transform,false);visual.localPosition=Vector3.up*.31f;
                var left=new GameObject("mixamorig:LeftFoot").transform;left.SetParent(visual,false);
                var right=new GameObject("mixamorig:RightFoot").transform;right.SetParent(visual,false);
                left.localPosition=new Vector3(-.15f,.12f,0);right.localPosition=new Vector3(.15f,.12f,0);
                var skin=visual.gameObject.AddComponent<SkinnedMeshRenderer>();
                mesh=new Mesh {name="temporary two sole triangles"};
                mesh.vertices=new[]{new Vector3(-.25f,-.1f,-.1f),new Vector3(-.05f,-.1f,-.1f),new Vector3(-.15f,-.1f,.2f),
                    new Vector3(.05f,-.1f,-.1f),new Vector3(.25f,-.1f,-.1f),new Vector3(.15f,-.1f,.2f)};
                mesh.triangles=new[]{0,2,1,3,5,4};
                mesh.bindposes=new[]{left.worldToLocalMatrix*visual.localToWorldMatrix,right.worldToLocalMatrix*visual.localToWorldMatrix};
                var weights=new BoneWeight[6];
                for(int i=0;i<weights.Length;i++)weights[i]=new BoneWeight{boneIndex0=i<3?0:1,weight0=1f};
                mesh.boneWeights=weights;mesh.RecalculateBounds();
                skin.sharedMesh=mesh;skin.bones=new[]{left,right};skin.rootBone=visual;
                var clips=visual.gameObject.AddComponent<Animation>();
                var contact=actor.AddComponent<BraxObozIdleFootContact>();contact.Configure(body,visual,clips);
                bool measured=contact.TryGetSoleHeight(out float originalY);
                Check(measured && Mathf.Abs(originalY-(.31f-.10f)*parentScale)<.0001f,
                    label+" reads weighted sole geometry instead of renderer bounds");
                const float ground=.24f*k;
                bool placed=contact.PlaceSolesOnSurface(ground);
                measured=contact.TryGetSoleHeight(out float placedY);
                Check(placed && measured && Mathf.Abs(placedY-(ground+.006f*k))<.0001f,
                    label+" world floor correction under rotated scaled parent");
                left.localPosition+=Vector3.up*.05f;right.localPosition+=Vector3.up*.05f;
                measured=contact.TryGetSoleHeight(out float posedY);
                Check(measured && Mathf.Abs(posedY-placedY-.05f*parentScale)<.0001f,
                    label+" changed bone pose is measured, not cached renderer envelope");
                string serialized=EditorJsonUtility.ToJson(contact);
                Object.DestroyImmediate(contact);
                var restored=actor.AddComponent<BraxObozIdleFootContact>();
                EditorJsonUtility.FromJsonOverwrite(serialized,restored);
                restored.PlaceSolesOnSurface(ground);
                // This is an isolated EditMode method check, not a simulated
                // Unity lifecycle transition. SendMessage is forbidden by the
                // engine for a behaviour outside the play/render loop here.
                typeof(BraxObozIdleFootContact).GetMethod("OnDisable",
                    BindingFlags.Instance|BindingFlags.NonPublic).Invoke(restored,null);
                Check(Vector3.Distance(visual.localPosition,Vector3.up*.31f)<.0001f,
                    label+" serialized normalization pivot restored by explicit EditMode callback check");
            }
            catch(Exception ex){results.Add("FAIL "+label+": "+ex);}
            finally
            {
                if(frame)Object.DestroyImmediate(frame);
                if(mesh)Object.DestroyImmediate(mesh);
            }
        }
        return results.ToArray();
    }
}
