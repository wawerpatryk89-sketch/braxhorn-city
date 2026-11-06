using System;
using System.Collections.Generic;
using UnityEngine;

// Camp-only visual foot contact. The capsule owns movement; source meshes,
// bones, animation clips and the normalized model pivot are never rewritten.
[DefaultExecutionOrder(400)]
public sealed class BraxObozIdleFootContact : MonoBehaviour
{
    // Keep the existing serialized player reference for previously saved scenes.
    public BraxGymPlayer player;
    public CharacterController body;
    public Transform visual;
    public Animation clips;
    [SerializeField] Vector3 baselineLocalPosition;
    [SerializeField] bool hasBaseline;

    const float K=1.294f;
    const float MaxCorrection=.24f*K;
    const float DesiredClearance=.006f*K;
    readonly List<SoleVertex> leftSole=new List<SoleVertex>();
    readonly List<SoleVertex> rightSole=new List<SoleVertex>();
    bool initialized, warned;
    int leftFrame=-1,rightFrame=-1;
    Vector3 lastLeft,lastRight;
    Collider lastFloor;
    BraxObozDeformableMat floorMat;

    struct Influence
    {
        public Transform bone;
        public Vector3 localPoint;
        public float weight;
        public bool Valid => weight<=0f || bone;
        public Vector3 World => weight>0f && bone ? bone.TransformPoint(localPoint)*weight : Vector3.zero;
    }
    struct SoleVertex
    {
        public Influence a,b,c,d;
        public bool Valid => a.Valid && b.Valid && c.Valid && d.Valid;
        public Vector3 World => a.World+b.World+c.World+d.World;
    }

    public void Configure(CharacterController capsule,Transform model,Animation motion)
    {
        body=capsule;visual=model;clips=motion;
        if(visual){baselineLocalPosition=visual.localPosition;hasBaseline=true;}
        initialized=false;
        leftFrame=rightFrame=-1;
        leftSole.Clear();rightSole.Clear();
    }

    bool BindReferences()
    {
        if(player)
        {
            if(!body)body=player.body;
            if(!visual)visual=player.visual;
            if(!clips)clips=player.clips;
        }
        if(!body || !visual)return false;
        if(!hasBaseline){baselineLocalPosition=visual.localPosition;hasBaseline=true;}
        return true;
    }

    static string BoneName(string name)
    {
        int colon=name.LastIndexOf(':');
        if(colon>=0)return name.Substring(colon+1);
        return name.StartsWith("mixamorig",StringComparison.OrdinalIgnoreCase)
            ? name.Substring(9).TrimStart('0','1','2','3','4','5','6','7','8','9','_') : name;
    }

    static bool IsFootBone(Transform bone,bool left)
    {
        string foot=left?"LeftFoot":"RightFoot";
        // Only this foot and its toe descendants, never a similarly named hand.
        for(var at=bone;at;at=at.parent)
            if(string.Equals(BoneName(at.name),foot,StringComparison.OrdinalIgnoreCase))return true;
        return false;
    }

    static float FootWeight(BoneWeight weight,bool[] membership)
    {
        float result=0f;
        if(weight.weight0>0 && weight.boneIndex0>=0 && weight.boneIndex0<membership.Length && membership[weight.boneIndex0])result+=weight.weight0;
        if(weight.weight1>0 && weight.boneIndex1>=0 && weight.boneIndex1<membership.Length && membership[weight.boneIndex1])result+=weight.weight1;
        if(weight.weight2>0 && weight.boneIndex2>=0 && weight.boneIndex2<membership.Length && membership[weight.boneIndex2])result+=weight.weight2;
        if(weight.weight3>0 && weight.boneIndex3>=0 && weight.boneIndex3<membership.Length && membership[weight.boneIndex3])result+=weight.weight3;
        return result;
    }

    static Influence Point(Vector3 vertex,int index,float weight,Transform[] bones,Matrix4x4[] bindposes)
    {
        if(weight<=0)return default;
        if(index<0 || index>=bones.Length || index>=bindposes.Length || !bones[index])
            return new Influence{weight=weight};
        return new Influence{bone=bones[index],localPoint=bindposes[index].MultiplyPoint3x4(vertex),weight=weight};
    }

    bool InitializeSoles()
    {
        if(!BindReferences())return false;
        if(initialized)return leftSole.Count>0 && rightSole.Count>0;
        initialized=true;
        foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var mesh=skin.sharedMesh;
            if(!mesh || !mesh.isReadable)continue;
            var vertices=mesh.vertices;
            var weights=mesh.boneWeights;
            var bindposes=mesh.bindposes;
            var bones=skin.bones;
            if(weights.Length!=vertices.Length)continue;
            var leftBones=new bool[bones.Length];var rightBones=new bool[bones.Length];
            for(int i=0;i<bones.Length;i++)
            {
                leftBones[i]=IsFootBone(bones[i],true);
                rightBones[i]=IsFootBone(bones[i],false);
            }
            for(int i=0;i<vertices.Length;i++)
            {
                var weight=weights[i];
                bool left=FootWeight(weight,leftBones)>=.5f;
                bool right=FootWeight(weight,rightBones)>=.5f;
                if(!left && !right)continue;
                var point=new SoleVertex {
                    a=Point(vertices[i],weight.boneIndex0,weight.weight0,bones,bindposes),
                    b=Point(vertices[i],weight.boneIndex1,weight.weight1,bones,bindposes),
                    c=Point(vertices[i],weight.boneIndex2,weight.weight2,bones,bindposes),
                    d=Point(vertices[i],weight.boneIndex3,weight.weight3,bones,bindposes)
                };
                if(!point.Valid)continue;
                if(left)leftSole.Add(point);
                if(right)rightSole.Add(point);
            }
        }
        if(leftSole.Count>0 && rightSole.Count>0)return true;
        if(!warned)
        {
            Debug.LogWarning("BRAXHORN_OBOZ_FOOT_GEOMETRY_MISSING: "+name+
                " — brak czytelnej siatki ważonej do obu stóp; kontakt niepotwierdzony",this);
            warned=true;
        }
        return false;
    }

    // The same measured foot geometry is available to the editor scene audit,
    // footprint pool and mat deformation. No Renderer.bounds animation envelope.
    public bool TryGetSolePosition(bool left,out Vector3 world)
    {
        world=Vector3.zero;
        if(!InitializeSoles())return false;
        if(Application.isPlaying && (left?leftFrame:rightFrame)==Time.frameCount)
        {world=left?lastLeft:lastRight;return true;}
        var points=left?leftSole:rightSole;
        float minY=float.PositiveInfinity;
        for(int i=0;i<points.Count;i++)
        {
            var point=points[i];
            if(!point.Valid)continue;
            Vector3 at=point.World;
            if(at.y<minY){minY=at.y;world=at;}
        }
        bool measured=!float.IsInfinity(minY) && !float.IsNaN(minY);
        if(measured && Application.isPlaying)
        {
            if(left){leftFrame=Time.frameCount;lastLeft=world;}
            else{rightFrame=Time.frameCount;lastRight=world;}
        }
        return measured;
    }

    public bool TryGetSoleHeight(out float worldY)
    {
        worldY=0f;
        if(!TryGetSolePosition(true,out var left) || !TryGetSolePosition(false,out var right))return false;
        worldY=Mathf.Min(left.y,right.y);
        return true;
    }

    Vector3 LocalAtCorrection(float worldOffset)
    {
        Vector3 localDelta=visual.parent
            ? visual.parent.InverseTransformVector(Vector3.up*worldOffset) : Vector3.up*worldOffset;
        return baselineLocalPosition+localDelta;
    }

    float CurrentCorrection()
    {
        Vector3 baselineWorld=visual.parent
            ? visual.parent.TransformPoint(baselineLocalPosition) : baselineLocalPosition;
        return visual.position.y-baselineWorld.y;
    }

    float GroundedCorrection(float groundY,float soleY)
    {
        float requested=CurrentCorrection()+groundY+DesiredClearance-soleY;
        if(!warned && Mathf.Abs(requested)>MaxCorrection)
        {
            Debug.LogWarning("BRAXHORN_OBOZ_FOOT_CONTACT_LIMIT: "+name+
                " — wymagana korekta "+(requested/K).ToString("F3")+
                " m; sprawdzić rig i animacje w scenie",this);
            warned=true;
        }
        return Mathf.Clamp(requested,-MaxCorrection,MaxCorrection);
    }

    // Called only after the generator's downward ray hits the real surface.
    // Store the original pivot separately; this pose correction is reversible.
    public bool PlaceSolesOnSurface(float groundY)
    {
        if(!TryGetSoleHeight(out float soleY))return false;
        visual.localPosition=LocalAtCorrection(GroundedCorrection(groundY,soleY));
        leftFrame=rightFrame=-1;
        return true;
    }

    float RenderedGroundHeight(RaycastHit ground)
    {
        if(lastFloor!=ground.collider)
        {
            lastFloor=ground.collider;
            floorMat=lastFloor?lastFloor.GetComponent<BraxObozDeformableMat>():null;
        }
        if(!floorMat)return ground.point.y;
        if(TryGetSolePosition(true,out var left) && TryGetSolePosition(false,out var right) &&
            floorMat.TryGetSurfaceHeight(left.y<=right.y?left:right,out float atFoot))return atFoot;
        return floorMat.TryGetSurfaceHeight(ground.point,out float atCapsule)?atCapsule:ground.point.y;
    }

    void LateUpdate()
    {
        if(!BindReferences() || !clips)return;
        bool grounded=body.isGrounded;
        float targetCorrection=0f;
        if(grounded)
        {
            Vector3 origin=body.transform.position+Vector3.up*(.65f*K);
            if(Physics.Raycast(origin,Vector3.down,out var ground,3f*K,
                    ~(1<<8),QueryTriggerInteraction.Ignore) && ground.normal.y>.65f &&
                    TryGetSoleHeight(out float soleY))
                targetCorrection=GroundedCorrection(RenderedGroundHeight(ground),soleY);
        }
        // Airborne characters return to their authored normalization pivot,
        // not to (0,0,0). World-space correction remains correct under a parent scale.
        float next=Mathf.MoveTowards(CurrentCorrection(),targetCorrection,
            Mathf.Max(0f,Time.deltaTime)*(.52f*K));
        Vector3 target=LocalAtCorrection(next);
        if((visual.localPosition-target).sqrMagnitude>1e-12f)
        {
            visual.localPosition=target;
            leftFrame=rightFrame=-1;
        }
    }

    void OnDisable()
    {
        if(visual && hasBaseline)visual.localPosition=baselineLocalPosition;
        leftFrame=rightFrame=-1;
    }
}
