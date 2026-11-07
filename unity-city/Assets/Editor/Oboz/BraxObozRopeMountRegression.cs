using System;
using System.Collections.Generic;
using UnityEngine;
using Object=UnityEngine.Object;

// Uses the unchanged BraxLina.Zbuduj and actual cylinder/rope meshes. This
// measures the saved rest pose; it does not claim a PlayMode deformation run.
public static class BraxObozRopeMountRegression
{
    const float K=1.294f;
    public static string[] Run()
    {
        var checks=new List<string>();
        void Check(bool value,string label)=>checks.Add((value?"PASS ":"FAIL ")+label);
        GameObject fixture=null;Mesh ropeMesh=null;
        try
        {
            fixture=new GameObject("temporary rope surface intersection fixture");
            var ropeObject=new GameObject("actual BraxLina fixture");ropeObject.transform.SetParent(fixture.transform,false);
            ropeObject.AddComponent<MeshRenderer>();var line=ropeObject.AddComponent<BraxLina>();
            var initial=ropeObject.GetComponent<MeshFilter>().sharedMesh;
            line.a=new Vector3(318,0,318);line.b=line.a+Vector3.forward*(6.05f*K);
            line.wysokosci=new[]{1.35f*K,1.70f*K,2.05f*K,2.40f*K};
            line.promien=.025f*K;line.odcinki=18;line.boki=10;line.Zbuduj();
            ropeMesh=ropeObject.GetComponent<MeshFilter>().sharedMesh;
            if(initial && initial!=ropeMesh)Object.DestroyImmediate(initial);
            var ropeSurface=ropeObject.AddComponent<MeshCollider>();ropeSurface.sharedMesh=ropeMesh;ropeSurface.convex=false;
            var postObject=GameObject.CreatePrimitive(PrimitiveType.Cylinder);postObject.transform.SetParent(fixture.transform,false);
            postObject.transform.position=line.a+Vector3.up*(1.775f*K);
            postObject.transform.localScale=new Vector3(.18f,.775f,.18f)*K;
            Object.DestroyImmediate(postObject.GetComponent<Collider>());
            var post=postObject.AddComponent<MeshCollider>();post.sharedMesh=postObject.GetComponent<MeshFilter>().sharedMesh;
            foreach(bool convex in new[]{false,true})
            {
                post.convex=convex;Physics.SyncTransforms();string label="rope/post convex="+convex;
                var oldPoint=line.a+Vector3.up*line.wysokosci[0]+Vector3.right*line.promien;
                bool oldMeasured=BraxObozSupportSurface.TryMeasure(post,oldPoint,out var oldDistance);
                Check(oldMeasured && oldDistance.distanceWorld>.05f*K,
                    label+" old endpoint is measurably inside the post rather than on its surface");
                bool found=BraxObozEnvironment.TryRopePostSurfaceContact(line,post,out var contact,out int triangle);
                bool onPost=found && BraxObozSupportSurface.TryMeasure(post,contact,out var postDistance) &&
                    postDistance.distanceWorld<.001f*K;
                Check(onPost && triangle>=0 && triangle<line.odcinki*line.boki*2,
                    label+" measured rope triangle intersects the actual post surface");
                bool onRope=found && BraxObozSupportSurface.TryMeasure(ropeSurface,contact,out var ropeDistance) &&
                    ropeDistance.distanceWorld<.001f*K;
                Check(onRope,label+" the same attachment also lies on the actual generated rope surface");
                float along=Vector3.Dot(contact-line.a,Vector3.forward)/K;
                Check(found && along>.05f && along<.13f,
                    label+" attachment is the physical exit through the post, not the embedded endpoint");
            }
            var original=postObject.transform.position;
            postObject.transform.position=original+Vector3.forward*(3*K);
            Check(!BraxObozEnvironment.TryRopePostSurfaceContact(line,post,out _,out _),
                "a different post crossed halfway along the rope cannot be accepted as its endpoint support");
            postObject.transform.position=original+Vector3.right*(.3f*K);
            Check(!BraxObozEnvironment.TryRopePostSurfaceContact(line,post,out _,out _),
                "a detached post leaves the rope attachment unmeasured without a radius-based fallback");
            postObject.transform.position=original;
            ropeObject.GetComponent<MeshFilter>().sharedMesh=null;
            Check(!BraxObozEnvironment.TryRopePostSurfaceContact(line,post,out _,out _),
                "missing rope geometry cannot produce a synthetic contact point");
            ropeObject.GetComponent<MeshFilter>().sharedMesh=ropeMesh;post.enabled=false;
            Check(!BraxObozEnvironment.TryRopePostSurfaceContact(line,post,out _,out _),
                "disabled post cannot certify a rope mount");
        }
        catch(Exception ex){checks.Add("FAIL actual rope/post surface regression: "+ex);}
        finally
        {
            if(fixture)Object.DestroyImmediate(fixture);
            if(ropeMesh)Object.DestroyImmediate(ropeMesh);
        }
        return checks.ToArray();
    }
}
