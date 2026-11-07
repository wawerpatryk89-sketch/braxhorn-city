using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

// Real Unity meshes and support graphs in temporary fixtures. No scene save,
// PlayMode, APK or device-performance result is claimed by these checks.
public static class BraxObozSupportMeasurementRegression
{
    const float K=1.294f;
    static GameObject Cube(Transform parent,Vector3 position,Vector3 size)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.transform.SetParent(parent,false);
        g.transform.localPosition=position*K;g.transform.localScale=size*K;return g;
    }
    static GameObject Draw(Transform parent,Mesh mesh)
    {
        var g=new GameObject("temporary measured source mesh");g.transform.SetParent(parent,false);
        g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>();return g;
    }
    static BraxObozStructureRecord Record(Transform owner,params GameObject[] parts)
    {
        return BraxObozStructureAuthoring.Capture(owner,parts.Select(g=>g.GetComponent<MeshFilter>()).ToArray(),
            parts.SelectMany(g=>g.GetComponents<Collider>()).ToArray());
    }
    static Mesh Grid(int n)
    {
        var mesh=new Mesh {name="temporary connected grid",indexFormat=IndexFormat.UInt32};
        var vertices=new Vector3[(n+1)*(n+1)];var indices=new int[n*n*6];
        for(int z=0;z<=n;z++)for(int x=0;x<=n;x++)
        {
            int i=z*(n+1)+x;vertices[i]=new Vector3((x-n*.5f)*.1f,0,(z-n*.5f)*.1f);
            if(x==n || z==n)continue;
            int t=(z*n+x)*6;indices[t]=i;indices[t+1]=i+n+1;indices[t+2]=i+1;
            indices[t+3]=i+1;indices[t+4]=i+n+1;indices[t+5]=i+n+2;
        }
        mesh.vertices=vertices;mesh.triangles=indices;mesh.RecalculateBounds();return mesh;
    }
    static void Construction(List<string> checks)
    {
        void Check(bool value,string label)=>checks.Add((value?"PASS ":"FAIL ")+label);
        GameObject fixture=null;var meshes=new List<Mesh>();
        try
        {
            fixture=new GameObject("temporary raised support and batch fixture");
            fixture.transform.position=new Vector3(13400,1.25f*K,13400);
            var terrain=Grid(4);meshes.Add(terrain);
            var terrainObject=Draw(fixture.transform,terrain);terrainObject.transform.localScale=Vector3.one*100;
            var terrainCollider=terrainObject.AddComponent<MeshCollider>();terrainCollider.sharedMesh=terrain;
            BraxObozStructureAuthoring.RegisterGround(terrainCollider);
            var ground=terrainObject.GetComponent<BraxObozStructureGround>();
            var pavilion=Cube(fixture.transform,new Vector3(0,.055f,8),new Vector3(4,.11f,4));
            var pavilionRecord=Record(pavilion.transform,pavilion);
            var ring=new GameObject("temporary ring on 0.11 metre pavilion").transform;
            ring.SetParent(fixture.transform,false);ring.localPosition=new Vector3(0,.11f,8)*K;
            var foot=Cube(ring,new Vector3(0,.45f,0),new Vector3(.2f,.9f,.2f));
            var deck=Cube(ring,new Vector3(0,.95f,0),new Vector3(2,.1f,2));
            var ringRecord=Record(ring,foot,deck);ringRecord.expectedTopMetres=1;
            ringRecord.heightMembers=new[]{deck.GetComponent<Renderer>()};
            var report=BraxObozStructureAudit.Inspect(new[]{pavilionRecord,ringRecord},new[]{ground});
            Check(report.heights.Length==1 && report.heights[0].measured && report.heights[0].supportVerified &&
                Mathf.Abs(report.heights[0].actualMetres-1)<.0001f && report.heightWarnings==0,
                "one-metre deck on a 0.11 metre pavilion is measured above its own captured base");
            var late=Cube(ring,new Vector3(4,-4,0),Vector3.one);
            report=BraxObozStructureAudit.Inspect(new[]{pavilionRecord,ringRecord},new[]{ground});
            Check(report.heights[0].measured && Mathf.Abs(report.heights[0].actualMetres-1)<.0001f &&
                !ringRecord.Contains(late.GetComponent<Renderer>()),
                "later low child cannot change the sparse captured construction base");
            ring.position+=Vector3.up*(.4f*K);
            report=BraxObozStructureAudit.Inspect(new[]{pavilionRecord,ringRecord},new[]{ground});
            Check(report.heights[0].measured && Mathf.Abs(report.heights[0].actualMetres-1)<.0001f &&
                !report.heights[0].supportVerified && report.unsupportedPieces>0 && report.heightWarnings>0,
                "correct relative height never certifies a deck whose independent support graph is broken");
            ringRecord.heightMembers=new[]{late.GetComponent<Renderer>()};
            report=BraxObozStructureAudit.Inspect(new[]{pavilionRecord,ringRecord},new[]{ground});
            Check(!report.heights[0].measured && report.heightWarnings>0,
                "height reference to an uncaptured renderer remains unmeasured");

            // 1,400 separate cubes -> 16,800 triangles in ONE source range.
            // Each island is only 12 triangles and must remain measurable.
            var sample=Cube(fixture.transform,Vector3.zero,Vector3.one);
            var source=sample.GetComponent<MeshFilter>().sharedMesh;
            const int islandCount=1400;var combine=new CombineInstance[islandCount];
            for(int i=0;i<combine.Length;i++)combine[i]=new CombineInstance {mesh=source,
                transform=Matrix4x4.TRS(new Vector3((i%40-20)*.3f,.04f,(i/40-18)*.3f)*K,
                    Quaternion.identity,Vector3.one*(.08f*K))};
            var batch=new Mesh {name="temporary 16800 triangle board batch",indexFormat=IndexFormat.UInt32};
            batch.CombineMeshes(combine);meshes.Add(batch);Object.DestroyImmediate(sample);
            var batchObject=Draw(fixture.transform,batch);var batchRecord=Record(batchObject.transform,batchObject);
            report=BraxObozStructureAudit.Inspect(new[]{batchRecord},new[]{ground});
            Check(batchRecord.parts.Length==1 && batch.triangles.Length/3==16800 &&
                report.pieces.Length==islandCount && report.measuredPieces==islandCount &&
                report.unmeasuredPieces==0 && report.batchesOverBudget==0 && report.islandsOverBudget==0,
                "batch larger than 16000 triangles is split first and all small islands are measured");
            Check(report.renderers.Length==1 && report.renderers[0].completeTriangleCoverage && report.renderers[0].verified,
                "large grounded batch passes only after every island and its complete index coverage pass");
            var grid=Grid(90);meshes.Add(grid);var gridObject=Draw(fixture.transform,grid);
            var gridRecord=Record(gridObject.transform,gridObject);
            report=BraxObozStructureAudit.Inspect(new[]{gridRecord},new[]{ground});
            Check(grid.triangles.Length/3==16200 && report.islandsOverBudget==1 && report.batchesOverBudget==0 &&
                report.unmeasuredPieces==1 && !report.renderers[0].verified,
                "one connected island above 16000 triangles stays explicitly unmeasured");
            var tooLarge=new Mesh {name="temporary over-budget batch"};meshes.Add(tooLarge);
            tooLarge.vertices=new[]{Vector3.zero,Vector3.right,Vector3.forward};
            var repeated=new int[250001*3];for(int i=0;i<repeated.Length;i++)repeated[i]=i%3;
            tooLarge.triangles=repeated;tooLarge.RecalculateBounds();
            var bigObject=Draw(fixture.transform,tooLarge);var bigRecord=Record(bigObject.transform,bigObject);
            report=BraxObozStructureAudit.Inspect(new[]{bigRecord},new[]{ground});
            Check(report.batchesOverBudget==1 && report.unmeasuredPieces==1 && !report.renderers[0].verified &&
                report.maximumTrianglesPerBatch==250000 && report.maximumTrianglesPerIsland==16000 &&
                report.maximumTrianglePairs==2000000,
                "batch above 250000 triangles retains an explicit unmeasured result and all budgets");
        }
        catch(Exception ex){checks.Add("FAIL support height/batch regression: "+ex);}
        finally
        {
            if(fixture)Object.DestroyImmediate(fixture);
            foreach(var mesh in meshes)if(mesh)Object.DestroyImmediate(mesh);
        }
    }
    static void Surfaces(List<string> checks)
    {
        void Check(bool value,string label)=>checks.Add((value?"PASS ":"FAIL ")+label);
        GameObject fixture=null;Mesh mesh=null;
        try
        {
            fixture=new GameObject("temporary nonconvex support fixture");
            fixture.transform.position=new Vector3(13600,3,13600);
            var surfaceObject=new GameObject("actual nonconvex triangle");surfaceObject.transform.SetParent(fixture.transform,false);
            mesh=new Mesh {name="temporary support triangle"};
            mesh.vertices=new[]{Vector3.zero,Vector3.right*2,Vector3.forward*2};mesh.triangles=new[]{0,2,1};mesh.RecalculateBounds();
            var surface=surfaceObject.AddComponent<MeshCollider>();surface.sharedMesh=mesh;surface.convex=false;
            surfaceObject.transform.localRotation=Quaternion.Euler(20,40,15);
            surfaceObject.transform.localScale=new Vector3(2,.7f,1.1f);
            var va=surfaceObject.transform.TransformPoint(mesh.vertices[0]);
            var vb=surfaceObject.transform.TransformPoint(mesh.vertices[1]);
            var vc=surfaceObject.transform.TransformPoint(mesh.vertices[2]);
            var normal=Vector3.Cross(vb-va,vc-va).normalized;var onFace=(va+vb+vc)/3;
            var query=onFace+normal*(.17f*K);
            bool measured=BraxObozSupportSurface.TryMeasure(surface,query,out var actual);
            Check(measured && !surface.convex && actual.trianglesTested==1 && actual.triangleIndex==0 &&
                Mathf.Abs(actual.distanceWorld-.17f*K)<.002f && Vector3.Distance(actual.closestWorld,onFace)<.002f,
                "nonconvex support distance uses actual transformed triangles under rotation and nonuniform scale");
            surfaceObject.transform.localRotation=Quaternion.identity;surfaceObject.transform.localScale=Vector3.one;
            query=surfaceObject.transform.TransformPoint(new Vector3(1.5f,0,1.5f));Physics.SyncTransforms();
            measured=BraxObozSupportSurface.TryMeasure(surface,query,out actual);
            Check(surface.bounds.Contains(query) && measured && Mathf.Abs(actual.distanceWorld-Mathf.Sqrt(.5f))<.002f &&
                Vector3.Distance(actual.closestWorld,surfaceObject.transform.TransformPoint(new Vector3(1,0,1)))<.002f,
                "a point inside mesh bounds but outside its triangle never receives a false zero distance");
            query=surfaceObject.transform.TransformPoint(new Vector3(.5f,0,.5f));
            measured=BraxObozSupportSurface.TryMeasure(surface,query,out actual);
            Check(measured && actual.distanceWorld<.0001f,"point on a real nonconvex triangle has measured zero separation");
            surface.enabled=false;
            Check(!BraxObozSupportSurface.TryMeasure(surface,query,out actual) && !actual.measured && actual.distanceWorld==-1,
                "disabled support returns unmeasured minus one instead of a fake contact");
            surface.enabled=true;surface.sharedMesh=null;
            Check(!BraxObozSupportSurface.TryMeasure(surface,query,out actual) && !actual.measured && actual.distanceWorld==-1,
                "missing mesh remains explicitly unmeasured");

            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.transform.SetParent(fixture.transform,false);
            cube.transform.localPosition=Vector3.right*5;
            measured=BraxObozSupportSurface.TryMeasure(cube.GetComponent<BoxCollider>(),cube.transform.position,out actual);
            Check(measured && Mathf.Abs(actual.distanceWorld-.5f)<.001f,
                "inside a box the helper measures distance to its surface rather than reporting volume distance zero");
            Object.DestroyImmediate(cube.GetComponent<BoxCollider>());
            var cubeMesh=cube.AddComponent<MeshCollider>();cubeMesh.sharedMesh=cube.GetComponent<MeshFilter>().sharedMesh;cubeMesh.convex=false;
            measured=BraxObozSupportSurface.TryMeasure(cubeMesh,cube.transform.position,out actual);
            Check(measured && Mathf.Abs(actual.distanceWorld-.5f)<.001f && actual.trianglesTested==12,
                "closed nonconvex mesh measures its actual surface even when the query lies inside");
            var capsuleObject=new GameObject("temporary scaled capsule");capsuleObject.transform.SetParent(fixture.transform,false);
            capsuleObject.transform.localScale=new Vector3(2,3,1);
            var capsule=capsuleObject.AddComponent<CapsuleCollider>();capsule.radius=.5f;capsule.height=2;capsule.direction=1;
            measured=BraxObozSupportSurface.TryMeasure(capsule,capsuleObject.transform.position+Vector3.right*1.2f,out actual);
            Check(measured && Mathf.Abs(actual.distanceWorld-.2f)<.002f,
                "existing primitive capsule supports retain their actual scaled surface distance");
        }
        catch(Exception ex){checks.Add("FAIL exact support surface regression: "+ex);}
        finally
        {
            if(fixture)Object.DestroyImmediate(fixture);
            if(mesh)Object.DestroyImmediate(mesh);
        }
    }
    public static string[] Run()
    {
        var checks=new List<string>();Construction(checks);Surfaces(checks);return checks.ToArray();
    }
}
