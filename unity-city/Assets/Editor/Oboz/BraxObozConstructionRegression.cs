using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

// Run by the existing isolated Unity regression. The expected clip areas were
// independently calculated with SciPy halfspace intersections, not this clipper.
public static class BraxObozConstructionRegression
{
    public static string[] Run()
    {
        const float k=1.294f;
        var checks=new List<string>();
        void Check(bool ok,string name)=>checks.Add((ok?"PASS ":"FAIL ")+name);
        var frame=new GameObject("Construction regression — disposable");
        frame.transform.position=new Vector3(20000,0,20000);
        var material=new Material(Shader.Find("Standard"));
        try
        {
            var cases=new[]{
                ("road",-3.5f,3.5f,-139f,-109f,176.397313f),
                ("bus path",24.6f,27.4f,-122f,-73f,122.838998f),
                ("sprint crossing",60f,76f,-5f,-3f,20f),
                ("inside",0f,1f,0f,1f,1f),
                ("outside",100f,101f,100f,101f,1f),
                ("oval east",80f,87f,-23f,-21f,4.400164f),
                ("oval north",-1f,1f,70f,79f,8.399781f),
                ("track only",25f,25.25f,-115f,-114.75f,0f),
                ("whole loop",-90f,90f,-125f,81f,33775.261852f),
                ("shared start corner",68f,71f,-74f,-70f,0f)
            };
            foreach(var sample in cases)
            {
                var polygon=new List<Vector2>{new Vector2(sample.Item2,sample.Item4),
                    new Vector2(sample.Item3,sample.Item4),new Vector2(sample.Item3,sample.Item5),
                    new Vector2(sample.Item2,sample.Item5)};
                float area=BraxObozEnvironment.OutsideRunningTracks(polygon).Sum(BraxObozEnvironment.PolygonArea);
                Check(Mathf.Abs(area-sample.Item6)<.02f,
                    "exact clipping "+sample.Item1+" area="+area+" independent="+sample.Item6);
            }
            foreach(var point in new[]{new Vector2(0,-121f),new Vector2(0,-116.2f),
                new Vector2(0,72.2f),new Vector2(0,77f),new Vector2(71f,-24f)})
                Check(Mathf.Abs(BraxObozEnvironment.WalkwayHeightAt(point)-.09f)<.0001f,
                    "paving meets exact .09 m track boundary at "+point);
            Check(Mathf.Abs(BraxObozEnvironment.WalkwayHeightAt(new Vector2(0,-115.2f))-.016f)<.0001f,
                "approach returns to original .016 m walkway after one metre");

            float a=116*Mathf.PI*2f/BraxObozEnvironment.TrackSegments;
            float b=117*Mathf.PI*2f/BraxObozEnvironment.TrackSegments;
            var meetingSector=new List<Vector2>{
                new Vector2(Mathf.Cos(a)*81.2f,Mathf.Sin(a)*94.2f),
                new Vector2(Mathf.Cos(a)*82.4f,Mathf.Sin(a)*95.4f),
                new Vector2(Mathf.Cos(b)*82.4f,Mathf.Sin(b)*95.4f),
                new Vector2(Mathf.Cos(b)*81.2f,Mathf.Sin(b)*94.2f)};
            float retainedArea=BraxObozEnvironment.ClipOvalAtSprint(meetingSector)
                .Sum(BraxObozEnvironment.PolygonArea);
            Check(Mathf.Abs(retainedArea-.91022917f)<.002f,
                "oval physical layer removes sprint intersection: area="+retainedArea+" independent=.91022917");
            foreach(float x in new[]{65f,71f})foreach(float z in new[]{-74f,26f})
                Check(BraxObozCampusLayout.InsidePlot(x,z),
                    "100 source metre sprint corner stays inside retained fence: "+x+", "+z);

            var sprintFrame=new GameObject("temporary physical sprint measurement").transform;
            sprintFrame.SetParent(frame.transform,false);sprintFrame.localPosition=new Vector3(100,0,0);
            var sprint=BraxObozEnvironment.CreateSprintSurface(sprintFrame,material);
            var sprintBounds=sprint.GetComponent<Renderer>().bounds;
            // The isolated test is at world coordinate20000. The small .005
            // tolerance allows float rounding there, not a different scale.
            Check(Mathf.Abs(sprintBounds.size.z-100f*k)<.005f &&
                Mathf.Abs(sprintBounds.size.x-6f*k)<.005f,
                "actual sprint mesh measures100*K=129.4 world units, width6*K=7.764; actual="+sprintBounds.size);

            float previous=0;
            for(int i=0;i<BraxObozEnvironment.RingStairCount;i++)
            {
                float top=BraxObozEnvironment.RingStairTop(i);
                Check((top-previous)*k<.30f && (top-previous)*k>.20f,
                    "real ring riser "+i+" height="+((top-previous)*k)+" below NavMesh climb .30m");
                if(i>0)
                    Check(Mathf.Abs(BraxObozEnvironment.RingStairCentreX(i)-
                        BraxObozEnvironment.RingStairCentreX(i-1)-BraxObozEnvironment.RingStairRun)<1e-5f,
                        "successive ring treads meet without a .125 m gap");
                previous=top;
            }
            Check(Mathf.Abs(previous-1.04f)<1e-6f,"last ring tread meets mat top, including .04 m mat");
            Check(Mathf.Abs((2*BraxObozEnvironment.RingRopeAxis-2*BraxObozEnvironment.RingRopeRadius)-6f)<1e-6f,
                "ring clear width between inner rope faces is six source metres");
            Check((2*BraxObozEnvironment.ArenaEntryHalf-.13f)*k>2*.43f+.2f,
                "same-side rope entry clears the real baked agent diameter");

            // Exercise the actual volume builder and actual saved mesh. Every
            // edge of this prism is closed; a single elevated face must fail.
            var square=new List<Vector2>{new Vector2(-1,-1),new Vector2(1,-1),new Vector2(1,1),new Vector2(-1,1)};
            var volume=BraxObozEnvironment.PolygonVolume(frame.transform,"Regression supported volume",square,
                0,p=>.09f,material,true);
            var mesh=volume.GetComponent<MeshFilter>().sharedMesh;var bounds=mesh.bounds;
            Check(Mathf.Abs(bounds.min.y)<1e-7f && Mathf.Abs(bounds.max.y-.09f*k)<1e-6f &&
                mesh.triangles.Length==36 && volume.GetComponent<MeshCollider>().sharedMesh==mesh,
                "actual foundation mesh has top, underside, four sides and the same physical collider");
            var vertices=mesh.vertices;var indices=mesh.triangles;
            var topNormal=Vector3.Cross(vertices[indices[1]]-vertices[indices[0]],vertices[indices[2]]-vertices[indices[0]]).normalized;
            var bottomNormal=Vector3.Cross(vertices[indices[4]]-vertices[indices[3]],vertices[indices[5]]-vertices[indices[3]]).normalized;
            Check(topNormal.y>.99f && bottomNormal.y<-.99f,"actual mesh top/bottom winding is outward");

            var floor=new GameObject("temporary construction-test floor");floor.transform.SetParent(frame.transform,false);
            floor.transform.localPosition=new Vector3(0,-.1f,0);
            floor.AddComponent<BoxCollider>().size=new Vector3(30,.2f,30);
            var asymmetric=new GameObject("temporary asymmetric ring footprint").transform;
            asymmetric.SetParent(frame.transform,false);asymmetric.localPosition=new Vector3(4,.17f,0);
            var body=GameObject.CreatePrimitive(PrimitiveType.Cube);body.transform.SetParent(asymmetric,false);
            body.transform.localPosition=new Vector3(0,.5f,0);body.transform.localScale=new Vector3(8,1,8);
            var stair=GameObject.CreatePrimitive(PrimitiveType.Cube);stair.transform.SetParent(asymmetric,false);
            stair.transform.localPosition=new Vector3(-5.5f,.1f,0);stair.transform.localScale=new Vector3(1,.2f,1);
            var before=asymmetric.position;
            bool placed=BraxObozEnvironment.GroundBuiltStructure(asymmetric,asymmetric.GetComponentsInChildren<Renderer>(),0,
                "regression asymmetrical footprint");
            var after=asymmetric.position;
            Check(placed && Mathf.Abs(after.x-before.x)<.0001f && Mathf.Abs(after.z-before.z)<.0001f &&
                Mathf.Abs(after.y)<.0001f,"actual physics placement keeps the ring centre with off-centre stairs");
        }
        catch(Exception ex){checks.Add("FAIL construction regression exception: "+ex);}
        finally{Object.DestroyImmediate(frame);Object.DestroyImmediate(material);}
        return checks.ToArray();
    }
}
