using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

public static partial class BraxObozEnvironment
{
    static bool CampfireFloor(Transform camp,Vector3 local,out RaycastHit floorHit)
    {
        Physics.SyncTransforms();
        var at=camp.TransformPoint(local*K);
        floorHit=Physics.RaycastAll(at+Vector3.up*(2*K),Vector3.down,5*K,
                ~0,QueryTriggerInteraction.Ignore)
            .Where(h=>h.collider && h.collider.gameObject.scene==camp.gameObject.scene &&
                !h.collider.transform.IsChildOf(camp) && h.normal.y>.65f &&
                h.point.y<=at.y+.5f*K)
            .OrderBy(h=>h.distance).FirstOrDefault();
        if(floorHit.collider)return true;
        BraxBuildQuality.Warn("OBÓZ CAMPFIRE_NO_FLOOR — nie utworzono niepodpartego bala/ławy przy "+at);
        return false;
    }
    static MeshCollider CampfireSolidLog(GameObject log)
    {
        // The built-in cylinder's capsule rounds off its end caps. Support
        // rays use exactly the rendered wooden cylinder instead.
        foreach(var collision in log.GetComponents<Collider>())Object.DestroyImmediate(collision);
        var solid=log.AddComponent<MeshCollider>();
        solid.sharedMesh=log.GetComponent<MeshFilter>().sharedMesh;solid.convex=false;
        return solid;
    }
    static void CampfireWoodStack(Transform camp)
    {
        var stack=Group("Drewno ogniska — trzy dolne i dwa poprzeczne bale",camp,Vector3.zero);
        var lower=new List<MeshCollider>();
        foreach(float z in new[]{-.34f,0f,.34f})
            Optional("dolny bal ogniska "+z,()=>{
                if(!CampfireFloor(camp,new Vector3(-.65f,0,z),out var a) ||
                    !CampfireFloor(camp,new Vector3(.65f,0,z),out var b))return;
                float floorY=Mathf.Max(a.point.y,b.point.y);
                float y=(floorY-stack.position.y)/K+.11f;
                var log=Cylinder(stack,"Bal opałowy na rzeczywistym gruncie",
                    new Vector3(-.65f,y,z),new Vector3(.65f,y,z),.11f,wood);
                // Set the actual lowest rendered point on the measured floor.
                log.transform.position+=Vector3.up*(floorY-log.GetComponent<Renderer>().bounds.min.y);
                lower.Add(CampfireSolidLog(log));
            });
        foreach(float x in new[]{-.34f,.34f})
            Optional("górny bal ogniska "+x,()=>{
                Physics.SyncTransforms();float support=float.NegativeInfinity;int contacts=0;
                foreach(float z in new[]{-.34f,0f,.34f})
                {
                    var ray=new Ray(stack.TransformPoint(new Vector3(x,2,z)*K),Vector3.down);
                    float highest=float.NegativeInfinity;
                    foreach(var log in lower)
                        if(log && log.Raycast(ray,out var hit,4*K) && hit.normal.y>.65f)
                            highest=Mathf.Max(highest,hit.point.y);
                    if(float.IsNegativeInfinity(highest))continue;
                    contacts++;support=Mathf.Max(support,highest);
                }
                if(contacts<2)
                {
                    BraxBuildQuality.Warn("OBÓZ CAMPFIRE_UPPER_LOG_UNSUPPORTED — brak dwóch rzeczywistych podpór");return;
                }
                float y=(support-stack.position.y)/K+.11f;
                var top=Cylinder(stack,"Poprzeczny bal opałowy oparty na dolnym stosie",
                    new Vector3(x,y,-.65f),new Vector3(x,y,.65f),.11f,wood);
                top.transform.position+=Vector3.up*(support-top.GetComponent<Renderer>().bounds.min.y);
                CampfireSolidLog(top);
            });
        BraxObozStructureAuthoring.Capture(stack,stack.GetComponentsInChildren<MeshFilter>(),
            stack.GetComponentsInChildren<Collider>());
    }
    static void CampfireLogBenches(Transform camp)
    {
        for(int i=0;i<8;i++)
        {
            int number=i;
            Optional("ława ogniska "+number,()=>{
                float a=number*Mathf.PI/4f;
                var radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                var tangent=new Vector3(-radial.z,0,radial.x);var centre=radial*5f;
                var supports=new[]{centre-tangent*.7f,centre+tangent*.7f};
                if(!CampfireFloor(camp,supports[0],out var first) ||
                    !CampfireFloor(camp,supports[1],out var second) ||
                    !CampfireFloor(camp,centre,out var centreFloor))return;
                var groundHits=new[]{first,second};
                var bench=Group("Ława ogniska "+number+" — siedzisko 0,45 m",camp,Vector3.zero);
                float targetTop=centreFloor.point.y+.45f*K;
                float centreY=(centreFloor.point.y-bench.position.y)/K+.29f;
                var seat=Cylinder(bench,"Siedzisko ławy z bala",
                    centre-tangent*1.05f+Vector3.up*centreY,
                    centre+tangent*1.05f+Vector3.up*centreY,.16f,wood);
                Physics.SyncTransforms();
                // Renderer bounds can lag one transform update in batch mode.
                // Correct twice against the measured terrain point and verify
                // the real rendered top, never an assumed primitive height.
                for(int pass=0;pass<2;pass++)
                {
                    var seatBounds=seat.GetComponent<Renderer>().bounds;
                    seat.transform.position+=Vector3.up*(targetTop-seatBounds.max.y);
                    Physics.SyncTransforms();
                }
                var seatRenderer=seat.GetComponent<Renderer>();
                var finalSeatBounds=seatRenderer.bounds;
                CampfireSolidLog(seat);
                var supportColliders=new List<MeshCollider>();
                for(int end=0;end<supports.Length;end++)
                {
                    var basePoint=supports[end];
                    float startY=(groundHits[end].point.y-bench.position.y)/K;
                    // Two centimetres of real timber overlap joins the vertical
                    // support to the round seat without a visible/floating gap.
                    float topY=(finalSeatBounds.min.y-bench.position.y)/K+.02f;
                    var support=Cylinder(bench,"Pień pod ławą — od gruntu do siedziska",
                        basePoint+Vector3.up*startY,basePoint+Vector3.up*topY,.16f,wood);
                    supportColliders.Add(CampfireSolidLog(support));
                }
                var structure=BraxObozStructureAuthoring.Capture(bench,
                    bench.GetComponentsInChildren<MeshFilter>(),bench.GetComponentsInChildren<Collider>());
                // Height is measured from the actual terrain below the seat
                // centre. Structural support is independently proven by mesh contact.
                var placement=BraxObozPlacementRecord.For(seat.transform);
                placement.SetMembers(new[]{seatRenderer});
                placement.support=BraxObozPlacementRecord.SupportKind.Suspended;
                placement.label="ława ogniska — siedzisko 0,45 m";
                placement.heightAboveFloorMetres=.45f;
                placement.heightReference=(int)BraxObozPlacement.HeightReference.Top;
                placement.heightRenderers=new[]{seatRenderer};
                placement.verticalSupport=centreFloor.collider;
                var centreWorld=new Vector3(finalSeatBounds.center.x,targetTop,finalSeatBounds.center.z);
                placement.contactPoint=Group("Pomiar wysokości siedziska",seat.transform,
                    seat.transform.InverseTransformPoint(centreWorld));
                var supportWorld=bench.TransformPoint(supports[0]*K);
                var suspensionWorld=new Vector3(supportWorld.x,finalSeatBounds.min.y+.005f*K,supportWorld.z);
                placement.suspensionPoint=Group("Zmierzony styk siedziska z podporą",seat.transform,
                    seat.transform.InverseTransformPoint(suspensionWorld));
                placement.suspensionSupport=supportColliders[0];
            });
        }
    }
}
