using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

// Real Unity mesh/ray/serialization regression in the existing temporary test
// scene. This is not a PlayMode simulation or a phone-performance test.
public static class BraxObozStructureRegression
{
    const float K=1.294f;
    static GameObject Cube(Transform parent,string name,Vector3 position,Vector3 size)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;
        g.transform.SetParent(parent,false);g.transform.localPosition=position*K;
        g.transform.localScale=size*K;return g;
    }
    static BraxObozStructureRecord Record(Transform root,params GameObject[] pieces)
    {
        return BraxObozStructureAuthoring.Capture(root,pieces.Select(g=>g.GetComponent<MeshFilter>()).ToArray(),
            pieces.SelectMany(g=>g.GetComponents<Collider>()).ToArray());
    }
    public static string[] Run()
    {
        var checks=new List<string>();
        void Check(bool value,string label)=>checks.Add((value?"PASS ":"FAIL ")+label);
        GameObject fixture=null;var temporaryMeshes=new List<Mesh>();
        try
        {
            fixture=new GameObject("temporary structural support fixture");
            fixture.transform.position=new Vector3(13200,1.25f*K,13200);
            var terrain=new GameObject("actual elevated fixture terrain");terrain.transform.SetParent(fixture.transform,false);
            var mesh=new Mesh {name="temporary terrain plane"};temporaryMeshes.Add(mesh);
            mesh.vertices=new[]{new Vector3(-20,0,-20),new Vector3(-20,0,20),
                new Vector3(20,0,20),new Vector3(20,0,-20)};
            mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();
            terrain.AddComponent<MeshFilter>().sharedMesh=mesh;terrain.AddComponent<MeshRenderer>();
            var surface=terrain.AddComponent<MeshCollider>();surface.sharedMesh=mesh;
            BraxObozStructureAuthoring.RegisterGround(surface);
            var ground=terrain.GetComponent<BraxObozStructureGround>();
            var construction=new GameObject("explicit timber parts").transform;
            construction.SetParent(fixture.transform,false);
            var post=Cube(construction,"a",new Vector3(0,.5f,0),new Vector3(.2f,1,.2f));
            var beam=Cube(construction,"b",new Vector3(.5f,1.05f,0),new Vector3(1.2f,.1f,.2f));
            var record=Record(construction,post,beam);
            var result=BraxObozStructureAudit.Inspect(new[]{record},new[]{ground});
            Check(result.pieces.Length==2 && result.pieces.All(p=>p.verified),
                "beam is supported by a measured timber joint to a grounded post");
            Check(result.connections.Length==1 && result.pieces.Any(p=>p.parentPiece>=0),
                "support report contains the actual joint and the path to ground");
            var loose=Cube(construction,"late loose item",new Vector3(4,2,0),Vector3.one*.2f);
            Check(!record.Contains(loose.GetComponent<Renderer>()) &&
                !result.TryGet(loose.GetComponent<Renderer>(),out _),
                "later furniture under the same parent never inherits construction membership");
            record=Record(construction,post,beam,loose);
            result=BraxObozStructureAudit.Inspect(new[]{record},new[]{ground});
            Check(result.unsupportedPieces==1 && result.TryGet(loose.GetComponent<Renderer>(),out var looseEntry) &&
                !looseEntry.verified,"explicit membership alone does not certify a floating item");
            loose.name="belka słup dach wspornik łańcuch";
            var renamed=BraxObozStructureAudit.Inspect(new[]{record},new[]{ground});
            Check(renamed.unsupportedPieces==result.unsupportedPieces,
                "construction-like names cannot suppress unsupported geometry");
            // Same-session InstanceID roundtrip, not persistence across
            // reopening the scene or restarting Unity.
            string serialized=JsonUtility.ToJson(record);
            var reloaded=new GameObject("temporary serialized support record");
            reloaded.transform.SetParent(fixture.transform,false);
            var restored=reloaded.AddComponent<BraxObozStructureRecord>();
            JsonUtility.FromJsonOverwrite(serialized,restored);
            var roundtrip=BraxObozStructureAudit.Inspect(new[]{restored},new[]{ground});
            Check(restored.Contains(post.GetComponent<Renderer>()) &&
                roundtrip.unsupportedPieces==1 && roundtrip.measuredPieces==3,
                "explicit parts and renderer references survive same-session JsonUtility roundtrip");
            Object.DestroyImmediate(reloaded);

            // AABB overlap is deliberately insufficient: two oblique, parallel
            // bars overlap in world bounds while their actual surfaces do not.
            var diagonal=new GameObject("parallel separated bars").transform;diagonal.SetParent(fixture.transform,false);
            var a=Cube(diagonal,"first",new Vector3(-5,2,-4),new Vector3(2,.08f,.08f));
            var b=Cube(diagonal,"second",new Vector3(-4.7f,2,-3.7f),new Vector3(2,.08f,.08f));
            a.transform.localRotation=b.transform.localRotation=Quaternion.Euler(0,45,0);
            var diagonalRecord=Record(diagonal,a,b);
            result=BraxObozStructureAudit.Inspect(new[]{diagonalRecord},new[]{ground});
            Check(a.GetComponent<Renderer>().bounds.Intersects(b.GetComponent<Renderer>().bounds) &&
                result.connections.Length==0 && result.unsupportedPieces==2,
                "overlapping AABBs do not invent a triangle-to-triangle timber joint");

            var combinedRoot=new GameObject("combined but disconnected construction").transform;
            combinedRoot.SetParent(fixture.transform,false);
            var grounded=Cube(combinedRoot,"grounded source",new Vector3(0,.1f,4),Vector3.one*.2f);
            var floating=Cube(combinedRoot,"floating source",new Vector3(2,1,4),Vector3.one*.2f);
            var sources=new[]{grounded.GetComponent<MeshFilter>(),floating.GetComponent<MeshFilter>()};
            var combinedRecord=Record(combinedRoot,grounded,floating);
            var combinedObject=new GameObject("one actual batched renderer");combinedObject.transform.SetParent(combinedRoot,false);
            var combined=new Mesh {name="temporary construction batch"};temporaryMeshes.Add(combined);
            combined.CombineMeshes(sources.Select(f=>new CombineInstance {mesh=f.sharedMesh,
                transform=combinedObject.transform.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray());
            var finalFilter=combinedObject.AddComponent<MeshFilter>();finalFilter.sharedMesh=combined;
            var finalRenderer=combinedObject.AddComponent<MeshRenderer>();
            BraxObozStructureAuthoring.RemapCombined(combinedRecord,sources,finalFilter);
            Object.DestroyImmediate(grounded);Object.DestroyImmediate(floating);
            result=BraxObozStructureAudit.Inspect(new[]{combinedRecord},new[]{ground});
            Check(combinedRecord.parts.All(p=>p.filter==finalFilter && p.rangesVerified) &&
                result.pieces.Length==2 && result.unsupportedPieces==1,
                "mesh combination retains per-piece geometry and the detached source remains unsupported");
            Check(result.TryGet(finalRenderer,out var batchEntry) && !batchEntry.verified,
                "a single grounded piece never exempts its entire combined renderer");
            combinedRecord.parts=new[]{new BraxObozStructureRecord.Part {label="source with disconnected islands",
                filter=finalFilter,vertexCount=combined.vertexCount,indexCount=combined.triangles.Length,rangesVerified=true}};
            result=BraxObozStructureAudit.Inspect(new[]{combinedRecord},new[]{ground});
            Check(result.pieces.Length==2 && result.unsupportedPieces==1,
                "even a single source range is split at disconnected geometric islands");
            combinedRecord.parts[0].firstIndex=combined.triangles.Length+3;
            result=BraxObozStructureAudit.Inspect(new[]{combinedRecord},new[]{ground});
            Check(result.unmeasuredPieces==1 && result.renderers.All(r=>!r.verified),
                "invalid serialized ranges remain unmeasured instead of silently passing");

            var buriedRoot=new GameObject("actual ground penetration").transform;buriedRoot.SetParent(fixture.transform,false);
            var buried=Cube(buriedRoot,"penetrating part",new Vector3(-4,.4f,4),new Vector3(.2f,1,.2f));
            var buriedRecord=Record(buriedRoot,buried);
            result=BraxObozStructureAudit.Inspect(new[]{buriedRecord},new[]{ground});
            Check(result.embeddedPieces==1 && !result.renderers[0].verified &&
                result.pieces[0].minimumGroundGapMetres<-.09f,
                "real ten-centimetre ground penetration is reported independently of connectivity");
            var unregisteredFloor=Cube(construction,"unregistered floating floor",new Vector3(4,1.85f,0),new Vector3(1,.1f,1));
            result=BraxObozStructureAudit.Inspect(new[]{record},new[]{ground});
            Check(result.TryGet(loose.GetComponent<Renderer>(),out looseEntry) && !looseEntry.verified,
                "an unregistered floating collider cannot masquerade as the actual ground");
            ground.surface=null;
            result=BraxObozStructureAudit.Inspect(new[]{record},new[]{ground});
            Check(result.invalidGrounds==1 && result.renderers.All(r=>!r.verified) && result.unmeasuredPieces>0,
                "missing terrain evidence remains explicitly unmeasured");
            Object.DestroyImmediate(unregisteredFloor);
        }
        catch(Exception ex){checks.Add("FAIL structural support regression: "+ex);}
        finally
        {
            if(fixture)Object.DestroyImmediate(fixture);
            foreach(var mesh in temporaryMeshes)if(mesh)Object.DestroyImmediate(mesh);
        }
        checks.AddRange(BraxObozSupportMeasurementRegression.Run());
        checks.AddRange(BraxObozRopeMountRegression.Run());
        return checks.ToArray();
    }
}
