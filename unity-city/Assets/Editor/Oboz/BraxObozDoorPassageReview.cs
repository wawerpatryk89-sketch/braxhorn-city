using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// A static, measured capsule route through the closed-leaf aperture, with
// only explicitly declared moving-leaf colliders omitted. Not PlayMode.
public static class BraxObozDoorPassageReview
{
    const float K=1.294f;
    const float FloorClearance=.002f; // metres; radius and height are never reduced
    const float MaximumSampleSpacing=.02f;
    [Serializable] public sealed class Sample
    {
        public int index;
        public bool floorMeasured;
        public Vector3 axisAtBottom,floorPoint,centreRayFloorPoint;
        public string floorPath,centreRayFloorPath,status;
    }
    [Serializable] public sealed class Result
    {
        public string status="UNMEASURED",method="actual closed-leaf mesh + player capsule + floor sphere/ray + capsule sweeps";
        public bool measured,clearWithMovingLeavesOmitted,playModeTested,phoneTested;
        public bool geometryWithMovingLeavesOmitted=true;
        public string playerCollider;
        public Vector3 openingCentreAtBottom,openingRight,openingNormal,controllerCentre;
        public float openingWidthMetres,openingHeightMetres,openingDepthMetres;
        public float capsuleRadiusMetres,capsuleHeightMetres,stepOffsetMetres,skinWidthMetres;
        public float floorClearanceMetres=FloorClearance,maximumSampleSpacingMetres=MaximumSampleSpacing;
        public int playerLayer,layerCollisionMask,samplesMeasured,missingFloorSamples,sweeps,excessiveSteps;
        public string[] rawCapsuleCandidates=Array.Empty<string>(),omittedMovingLeafColliders=Array.Empty<string>();
        public string[] filteredOtherScene=Array.Empty<string>(),filteredLayersOrPairs=Array.Empty<string>();
        public string[] unsupportedLayerOverrides=Array.Empty<string>(),blockingColliders=Array.Empty<string>();
        public Sample[] samples=Array.Empty<Sample>();
    }
    static bool Overrides(Collider c)=>c.includeLayers.value!=0 || c.excludeLayers.value!=0 ||
        (c.attachedRigidbody && (c.attachedRigidbody.includeLayers.value!=0 || c.attachedRigidbody.excludeLayers.value!=0)) ||
        (c.attachedArticulationBody && (c.attachedArticulationBody.includeLayers.value!=0 || c.attachedArticulationBody.excludeLayers.value!=0));
    static string Label(Collider c)=>BraxObozDoorGeometry.PathOf(c.transform)+" ["+c.GetType().Name+"]";
    public static Result Inspect(BraxCityDoor door,CharacterController body)
    {
        var result=new Result();
        if(!door || !body || !body.enabled || !body.gameObject.activeInHierarchy ||
            body.gameObject.scene!=door.gameObject.scene)
        {result.status="UNMEASURED: brak rzeczywistej aktywnej kapsuły gracza w tej scenie";return result;}
        if(!BraxObozDoorGeometry.TryOpening(door,out var opening,out var problem))
        {result.status="UNMEASURED: "+problem;return result;}
        var scale=body.transform.lossyScale;
        float radius=body.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z));
        float height=Mathf.Max(body.height*Mathf.Abs(scale.y),2*radius);
        float step=body.stepOffset*Mathf.Abs(scale.y);
        if(radius<=0 || height<=0 || Vector3.Dot(body.transform.up,Vector3.up)<.999f)
        {result.status="UNMEASURED: nieprawidłowa lub pochylona kapsuła gracza";return result;}
        result.playerCollider=Label(body);result.playerLayer=body.gameObject.layer;
        result.controllerCentre=body.center;result.capsuleRadiusMetres=radius/K;
        result.capsuleHeightMetres=height/K;result.stepOffsetMetres=step/K;
        result.skinWidthMetres=body.skinWidth/K;
        result.openingCentreAtBottom=opening.centreAtBottom;result.openingRight=opening.right;
        result.openingNormal=opening.normal;result.openingWidthMetres=opening.width/K;
        result.openingHeightMetres=opening.height/K;result.openingDepthMetres=opening.depth/K;
        for(int layer=0;layer<32;layer++)if(!Physics.GetIgnoreLayerCollision(body.gameObject.layer,layer))
            result.layerCollisionMask|=1<<layer;
        var moving=new HashSet<Collider>(opening.movingColliders);
        var raw=new HashSet<string>();var omitted=new HashSet<string>();var foreign=new HashSet<string>();
        var ignored=new HashSet<string>();var overrides=new HashSet<string>();var blockers=new HashSet<string>();
        bool Eligible(Collider c,bool recordRaw=false)
        {
            if(!c || !c.enabled || !c.gameObject.activeInHierarchy || c.isTrigger)return false;
            string label=Label(c);if(recordRaw)raw.Add(label);
            if(c.gameObject.scene!=door.gameObject.scene){foreign.Add(label);return false;}
            if(c==body)return false; // the source capsule itself, never an actor/name category
            if(moving.Contains(c)){if(recordRaw)omitted.Add(label);return false;}
            if(Overrides(body)||Overrides(c))
            {
                // Do not invent precedence for an unsupported per-body mask.
                // Preserve the candidate and mark the whole proof unmeasured.
                overrides.Add(Overrides(body)?Label(body):label);return false;
            }
            if((result.layerCollisionMask&(1<<c.gameObject.layer))==0 || Physics.GetIgnoreCollision(body,c))
            {ignored.Add(label);return false;}
            return true;
        }
        void Capsule(Vector3 bottom,out Vector3 lower,out Vector3 upper)
        {
            lower=bottom+Vector3.up*radius;upper=bottom+Vector3.up*(height-radius);
        }
        void Overlap(Vector3 bottom)
        {
            Capsule(bottom,out var lower,out var upper);
            foreach(var c in Physics.OverlapCapsule(lower,upper,radius,~0,QueryTriggerInteraction.Ignore))
                if(Eligible(c,true))blockers.Add(Label(c));
        }
        void Sweep(Vector3 from,Vector3 to)
        {
            var delta=to-from;float distance=delta.magnitude;if(distance<.000001f)return;
            Capsule(from,out var lower,out var upper);result.sweeps++;
            foreach(var hit in Physics.CapsuleCastAll(lower,upper,radius,delta/distance,distance,~0,QueryTriggerInteraction.Ignore))
                if(Eligible(hit.collider,true))blockers.Add(Label(hit.collider));
        }
        Physics.SyncTransforms();
        float reach=Mathf.Max(.60f*K,opening.depth*.5f+radius+.15f*K);
        int intervals=Mathf.CeilToInt(reach*2/(MaximumSampleSpacing*K));
        if(intervals>512){result.status="UNMEASURED: przekroczony budżet próbek przejścia";return result;}
        float minimumUp=Mathf.Cos(body.slopeLimit*Mathf.Deg2Rad);
        var samples=new List<Sample>();Sample previous=null;
        for(int i=0;i<=intervals;i++)
        {
            var at=opening.centreAtBottom+opening.normal*Mathf.Lerp(-reach,reach,(float)i/intervals);
            var sample=new Sample {index=i,status="brak zmierzonego podłoża"};samples.Add(sample);
            // The centre ray verifies an actual floor at each route position.
            // The downward lower-hemisphere sweep additionally finds contact
            // at a real step edge before the capsule centre reaches that edge.
            var rayOrigin=new Vector3(at.x,opening.bottomY+step+.10f*K,at.z);
            var floor=Physics.RaycastAll(rayOrigin,Vector3.down,step+1.10f*K,~0,QueryTriggerInteraction.Ignore)
                .Where(h=>Eligible(h.collider) && h.normal.y>=minimumUp).OrderBy(h=>h.distance).FirstOrDefault();
            var sphereOrigin=rayOrigin+Vector3.up*radius;
            var contact=Physics.SphereCastAll(sphereOrigin,radius,Vector3.down,step+1.10f*K,~0,QueryTriggerInteraction.Ignore)
                .Where(h=>h.distance>.0001f && Eligible(h.collider) && h.normal.y>=minimumUp)
                .OrderBy(h=>h.distance).FirstOrDefault();
            if(!floor.collider || !contact.collider)
            {result.missingFloorSamples++;previous=null;continue;}
            var bottom=sphereOrigin-Vector3.up*(contact.distance+radius-FloorClearance*K);
            sample.floorMeasured=true;sample.axisAtBottom=bottom;sample.floorPoint=contact.point;
            sample.centreRayFloorPoint=floor.point;sample.floorPath=Label(contact.collider);
            sample.centreRayFloorPath=Label(floor.collider);sample.status="rzeczywiste podłoże i podparcie dolnej półkuli";
            result.samplesMeasured++;Overlap(bottom);
            if(previous!=null)
            {
                if(Mathf.Abs(floor.point.y-previous.centreRayFloorPoint.y)>step+.001f*K)result.excessiveSteps++;
                Sweep(previous.axisAtBottom,bottom);
            }
            previous=sample;
        }
        result.samples=samples.ToArray();result.rawCapsuleCandidates=raw.OrderBy(x=>x,StringComparer.Ordinal).ToArray();
        result.omittedMovingLeafColliders=omitted.OrderBy(x=>x,StringComparer.Ordinal).ToArray();
        result.filteredOtherScene=foreign.OrderBy(x=>x,StringComparer.Ordinal).ToArray();
        result.filteredLayersOrPairs=ignored.OrderBy(x=>x,StringComparer.Ordinal).ToArray();
        result.unsupportedLayerOverrides=overrides.OrderBy(x=>x,StringComparer.Ordinal).ToArray();
        result.blockingColliders=blockers.OrderBy(x=>x,StringComparer.Ordinal).ToArray();
        result.measured=result.missingFloorSamples==0 && overrides.Count==0;
        result.clearWithMovingLeavesOmitted=result.measured && blockers.Count==0 && result.excessiveSteps==0;
        result.status=!result.measured?"UNMEASURED: brak podłoża lub nieobsługiwany filtr warstw":
            result.clearWithMovingLeavesOmitted?"GEOMETRY_CLEAR_WITH_DECLARED_MOVING_LEAVES_OMITTED":
                "GEOMETRY_BLOCKED_OR_STEP_EXCEEDS_CONTROLLER_LIMIT";
        return result;
    }
}
