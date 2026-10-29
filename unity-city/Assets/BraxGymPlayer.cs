using System;
using System.Runtime.InteropServices;
using UnityEngine;

public sealed class BraxGymPlayer : MonoBehaviour
{
    public Transform visual;
    public Animation clips;
    public Camera view;
    public CharacterController body;
    public Transform door;
    public Transform[] bags;
    // Gym-only optional target. Original 4 heavy-bag pivots, rig, input and city stay intact.
    public BraxGymSpeedBag speedBag;
    // Optional second physical pear in this room; original heavy bag pivots unchanged.
    public BraxGymSpeedBag[] additionalSpeedBags = Array.Empty<BraxGymSpeedBag>();
    public Transform contactShadow;
    public string buildId;
    public bool ready;
    public BraxCityWorld cityWorld;
    public Vector3 worldSpawn=new Vector3(1.5f,.08f,2.8f);
    public int trainingHits;
    private Vector2 move;
    private float yaw, pitch = 19f, distance = 5.8f, vertical, speed;
    private float actionEnd, hitAt, stateAt;
    private string action = "Idle";
    private bool hitPending, doorOpen, paused, runEnabled = true, soundEnabled = true;
    private float doorAngle;
    private Vector2[] bagSwing, bagVelocity;
    private Quaternion[] bagRestRotation;
    private int lastHitBag = -1;
    private float lastHitTime, lastHitDistance, lastHitAlignment;
    private Vector2 lastHitImpulse;
    private float simulationTime;
    private bool reviewEnabled;
    private AudioSource audioSource;
    private AudioClip impact;
    private Transform hand, leftFoot, rightFoot;
    private Vector3 cameraTarget;
    private static readonly Vector3 Spawn = new Vector3(1.5f, .08f, 2.8f);
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void BXState(string data);
#endif
    [Serializable] private class Status
    {
        public bool ready, paused, doorOpen, grounded, runEnabled, handFound, leftFootFound, rightFootFound;
        public float x,y,z,yaw,pitch,speed,handX,handY,handZ;
        public float playerYaw,cameraYaw,rightX,rightZ,forwardX,forwardZ,verticalSpeed;
        public float lastHitTime,lastHitDistance,lastHitAlignment;
        public float gameTime,simulationTime,moveX,moveY;
        public Vector3 leftFootPosition,rightFootPosition;
        public Vector2 lastHitImpulse;
        public Vector2[] bagAngles,bagVelocities;
        public bool speedBagConnected;
        public int speedBagHits;
        public float speedBagLastHitTime, speedBagLastDistance, speedBagLastAlignment;
        public Vector2 speedBagAngles, speedBagVelocities;
        public string animation,context,build,area,activePoint;
        public bool loadingArea;
        public int cityMeshes,cityTriangles,cityColliders,openDoors;
        public BraxCityPoint[] points;
        public int hits,frames,lastHitBag;
    }
    private void Start()
    {
        if (!visual || !clips || !view || !body) throw new InvalidOperationException("Missing original player or controls");
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        clips.playAutomatically = false;
        clips.cullingType = AnimationCullingType.AlwaysAnimate;
        clips.Play("Idle");
        bagSwing = new Vector2[bags.Length]; bagVelocity = new Vector2[bags.Length];
        bagRestRotation = new Quaternion[bags.Length];
        for(int i=0;i<bags.Length;i++)bagRestRotation[i]=bags[i].localRotation;
        cameraTarget = transform.position + Vector3.up * 1.65f;
        foreach (Transform t in visual.GetComponentsInChildren<Transform>())
        {
            if(!hand&&t.name.Contains("RightHand"))hand=t;
            if(!leftFoot&&t.name.Contains("LeftFoot"))leftFoot=t;
            if(!rightFoot&&t.name.Contains("RightFoot"))rightFoot=t;
        }
        audioSource=gameObject.AddComponent<AudioSource>(); audioSource.spatialBlend=0f; audioSource.volume=.28f;
        const int rate=22050; var samples=new float[rate/6];
        for(int i=0;i<samples.Length;i++){float t=(float)i/rate; samples[i]=Mathf.Sin(2f*Mathf.PI*(100f-180f*t)*t)*Mathf.Exp(-t*34f)*.7f;}
        impact=AudioClip.Create("Training impact",samples.Length,1,rate,false); impact.SetData(samples,0);
        reviewEnabled=ReviewEnabled();ready=true;UpdateCamera(1f);Report();
    }
    public void MoveInput(string values)
    {
        if(paused){move=Vector2.zero;return;}
        var p=values.Split(',');
        if(p.Length>=2 && float.TryParse(p[0],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float x) && float.TryParse(p[1],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float y))
            move=Vector2.ClampMagnitude(new Vector2(x,y),1);
    }
    public void LookInput(string values)
    {
        if(paused)return;var p=values.Split(',');
        if(p.Length>=2 && float.TryParse(p[0],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float dx) && float.TryParse(p[1],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float dy))
        {yaw-=dx*.22f;pitch=Mathf.Clamp(pitch+dy*.16f,7f,57f);}
    }
    public void Command(string key)
    {
        if(!ready)return;
        if(key=="pause"){paused=!paused;move=Vector2.zero;foreach(AnimationState st in clips)st.speed=paused?0:1;Report();return;}
        if(key=="reset")
        {
            body.enabled=false;transform.position=cityWorld?worldSpawn:Spawn;transform.rotation=Quaternion.Euler(0,180,0);body.enabled=true;
            vertical=0;speed=0;move=Vector2.zero;actionEnd=0;hitPending=false;hitAt=0;
            yaw=0;pitch=19;distance=5.8f;cameraTarget=transform.position+Vector3.up*1.65f;
            action="Idle";clips.Play("Idle");UpdateCamera(1f);Report();return;
        }
        if(key=="run"){runEnabled=!runEnabled;Report();return;}
        if(key=="sound"){soundEnabled=!soundEnabled;return;}
        if(paused)return;
        if(key=="action")
        {
            if(cityWorld&&cityWorld.Interact())return;
            if(door && Vector3.Distance(transform.position,new Vector3(6,transform.position.y,11.4f))<2.6f){doorOpen=!doorOpen;Report();}
            return;
        }
        if(Time.time<actionEnd)return;
        if(key=="punch")StartAction("Punch",1.25f,.43f);
        else if(key=="kick")StartAction("Kick",1.2f,.60f);
        else if(key=="jump" && body.isGrounded){vertical=6.2f;StartAction("Jump",1.35f,-1);}
    }
    private void StartAction(string name,float rate,float contact)
    {
        if(clips[name]==null)return;
        action=name;clips[name].wrapMode=WrapMode.Once;clips[name].speed=rate;
        clips.CrossFade(name,.10f,PlayMode.StopAll);
        actionEnd=Time.time+Mathf.Min(clips[name].length/rate,name=="Punch"?1.25f:name=="Kick"?1.1f:1.2f);
        hitAt=Time.time+contact/rate;hitPending=contact>=0;Report();
    }
    private void Update()
    {
        if(!ready||paused||(cityWorld&&cityWorld.loading))return;
        float dt=Mathf.Min(Time.deltaTime,.12f);
        simulationTime+=dt;
        bool acting=Time.time<actionEnd;
        float target=!acting?(move.magnitude>.12f?(runEnabled&&move.magnitude>.76f?5f:2f)*Mathf.Clamp01(move.magnitude*1.3f):0):0;
        speed=Mathf.MoveTowards(speed,target,dt*15f);
        Vector3 right=Vector3.ProjectOnPlane(view.transform.right,Vector3.up).normalized;
        Vector3 forward=Vector3.ProjectOnPlane(view.transform.forward,Vector3.up).normalized;
        Vector3 direction=right*move.x+forward*move.y;
        if(direction.sqrMagnitude>.01f && !acting)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),1-Mathf.Exp(-dt*12f));
        if(body.isGrounded&&vertical<0)vertical=-2f;vertical-=18f*dt;
        Vector3 step=(direction.normalized*speed+Vector3.up*vertical)*dt;
        int n=Mathf.Max(1,Mathf.CeilToInt(step.magnitude/.16f));for(int i=0;i<n;i++)body.Move(step/n);
        if(transform.position.y<(cityWorld?-20f:-3f))Command("reset");
        if(!cityWorld&&transform.position.z>20.2f){body.enabled=false;var q=transform.position;q.z=20.2f;transform.position=q;body.enabled=true;}
        if(hitPending&&Time.time>=hitAt){hitPending=false;HitBag();}
        if(!acting)
        {
            string next=speed>3f?"Run":speed>.12f?"Walk":"Idle";
            if(action!=next){action=next;clips[next].wrapMode=WrapMode.Loop;clips[next].speed=1;clips.CrossFade(next,.16f,PlayMode.StopAll);}
            if(next=="Walk")clips[next].speed=Mathf.Max(.55f,speed/2f);
            if(next=="Run")clips[next].speed=Mathf.Max(.65f,speed/5f);
        }
        doorAngle=Mathf.Lerp(doorAngle,doorOpen?-98f:0f,1-Mathf.Exp(-dt*5f));
        if(door)door.localRotation=Quaternion.Euler(0,doorAngle,0);
        for(int i=0;i<bags.Length;i++)
        {
            bagVelocity[i]+=(-bagSwing[i]*15f-bagVelocity[i]*2.4f)*dt;
            bagSwing[i]+=bagVelocity[i]*dt;
            bags[i].localRotation=bagRestRotation[i]*Quaternion.Euler(bagSwing[i].x,0,bagSwing[i].y);
        }
    }
    private void HitBag()
    {
        bool originalBagHit=false;
        for(int i=0;i<bags.Length;i++)
        {
            Vector3 d=bags[i].position-transform.position;d.y=0;
            float alignment=Vector3.Dot(transform.forward,d.normalized);
            if(d.magnitude<1.85f && alignment>.15f)
            {
                trainingHits++;lastHitBag=i;lastHitTime=Time.time;lastHitDistance=d.magnitude;lastHitAlignment=alignment;
                Vector3 localForward=bags[i].parent?bags[i].parent.InverseTransformDirection(transform.forward):transform.forward;
                localForward=Quaternion.Inverse(bagRestRotation[i])*localForward;
                lastHitImpulse=new Vector2(-localForward.z,localForward.x)*55f;
                bagVelocity[i]+=lastHitImpulse;
                if(soundEnabled)audioSource.PlayOneShot(impact);
                originalBagHit=true;
                Report();
                break;
            }
        }
        // Same original punch contact frame, no fake proximity scoring in Update.
        // A speed pear is hit by FISTS, not by kicks, and only within its
        // measured 3D sphere/contact arc. Index -2 is reserved exclusively
        // for this new target; original bag indices 0..3 are unchanged.
        if (!originalBagHit && action == "Punch")
        {
            // Both separate steel stands have genuine timed punch contacts.
            // The primary pear retains the original diagnostics and hit index.
            int count=(speedBag ? 1 : 0)+(additionalSpeedBags == null ? 0 : additionalSpeedBags.Length);
            for (int i=0; i<count; i++)
            {
                BraxGymSpeedBag target;
                if (speedBag && i==0) target=speedBag;
                else target=additionalSpeedBags[speedBag ? i-1 : i];
                if (!target) continue;
                float distanceToPear,alignmentToPear;
                if (!target.TryReceivePunch(transform.position,transform.forward,
                    out distanceToPear,out alignmentToPear)) continue;
                trainingHits++;
                lastHitBag=-2;
                lastHitTime=Time.time;
                lastHitDistance=distanceToPear;
                lastHitAlignment=alignmentToPear;
                lastHitImpulse=target.LastImpulse;
                if (soundEnabled)audioSource.PlayOneShot(impact);
                Report();
                break; // One contact frame can only strike one physical target.
            }
        }
    }
    private void LateUpdate()
    {
        if(!ready)return;
        UpdateCamera(Mathf.Min(Time.deltaTime,.1f));
        if(reviewEnabled||Time.unscaledTime>=stateAt){stateAt=Time.unscaledTime+.16f;Report();}
    }
    private void UpdateCamera(float dt)
    {
        Vector3 want=transform.position+Vector3.up*1.65f;
        cameraTarget=Vector3.Lerp(cameraTarget,want,1-Mathf.Exp(-dt*12f));
        float ya=yaw*Mathf.Deg2Rad,pi=pitch*Mathf.Deg2Rad;
        Vector3 offset=new Vector3(Mathf.Sin(ya)*Mathf.Cos(pi),Mathf.Sin(pi),Mathf.Cos(ya)*Mathf.Cos(pi))*distance;
        Vector3 position=cameraTarget+offset;
        if(Physics.SphereCast(cameraTarget,.18f,offset.normalized,out RaycastHit hit,distance,~(1<<8),QueryTriggerInteraction.Ignore))position=cameraTarget+offset.normalized*Mathf.Max(.8f,hit.distance-.15f);
        view.transform.position=position;view.transform.LookAt(cameraTarget);
        if(contactShadow){var p=transform.position;
            if(cityWorld&&Physics.Raycast(p+Vector3.up*.25f,Vector3.down,out RaycastHit floor,30,~(1<<8),QueryTriggerInteraction.Ignore))p.y=floor.point.y+.018f;
            else p.y=body.isGrounded?transform.position.y+.018f:.018f;
            contactShadow.position=p;}
    }
    public void ReviewView(string text)
    {
        if(!reviewEnabled)return;
        var a=text.Split(',');if(a.Length!=6)return;var v=new float[6];
        for(int i=0;i<6;i++)if(!float.TryParse(a[i],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out v[i])||float.IsNaN(v[i])||float.IsInfinity(v[i]))return;
        move=Vector2.zero;speed=0;vertical=0;actionEnd=0;hitPending=false;action="Idle";clips.Play("Idle");
        body.enabled=false;transform.position=new Vector3(v[0],v[1],v[2]);body.enabled=true;
        yaw=v[3];pitch=Mathf.Clamp(v[4],7f,57f);distance=Mathf.Clamp(v[5],.8f,12f);
        cameraTarget=transform.position+Vector3.up*1.65f;UpdateCamera(1f);Report();
    }
    public void TravelTo(Vector3 position)
    {
        move=Vector2.zero;speed=0;vertical=0;actionEnd=0;hitPending=false;action="Idle";
        body.enabled=false;transform.position=position;body.enabled=true;clips.Play("Idle");
        cameraTarget=position+Vector3.up*1.65f;UpdateCamera(1f);Report();
    }
    public void TravelPoint(string id){if(cityWorld&&!paused)cityWorld.GoTo(id);}
    private static bool ReviewEnabled()
    {
        if(!Uri.TryCreate(Application.absoluteURL,UriKind.Absolute,out var url))return false;
        foreach(var value in url.Query.TrimStart('?').Split('&'))if(value=="qa=1")return true;
        return false;
    }
    private void OnApplicationFocus(bool focus){if(!focus)move=Vector2.zero;}
    private void Report()
    {
        var h=hand?hand.position:Vector3.zero;
        string context=door&&Vector3.Distance(transform.position,new Vector3(6,transform.position.y,11.4f))<2.6f?(doorOpen?"ZAMKNIJ DRZWI":"OTWÓRZ DRZWI"):transform.position.z>18?"ULICA — KOLEJNY ETAP":"";
        if(cityWorld){if(!string.IsNullOrEmpty(cityWorld.Context))context=cityWorld.Context;else if(context=="ULICA — KOLEJNY ETAP")context="";}
        var right=Vector3.ProjectOnPlane(view.transform.right,Vector3.up).normalized;
        var forward=Vector3.ProjectOnPlane(view.transform.forward,Vector3.up).normalized;
        var s=new Status{area=cityWorld?cityWorld.area:"gym",activePoint=cityWorld?cityWorld.activePoint:"",points=cityWorld?cityWorld.points:null,loadingArea=cityWorld&&cityWorld.loading,cityMeshes=cityWorld?cityWorld.originalMeshes:0,cityTriangles=cityWorld?cityWorld.originalTriangles:0,cityColliders=cityWorld?cityWorld.sourceColliders:0,openDoors=cityWorld?cityWorld.OpenDoors:0,ready=ready,paused=paused,x=transform.position.x,y=transform.position.y,z=transform.position.z,yaw=yaw,pitch=pitch,speed=speed,animation=action,context=context,hits=trainingHits,frames=Time.frameCount,handX=h.x,handY=h.y,handZ=h.z,doorOpen=doorOpen,build=buildId,
            grounded=body.isGrounded,runEnabled=runEnabled,playerYaw=transform.eulerAngles.y,cameraYaw=view.transform.eulerAngles.y,verticalSpeed=vertical,
            rightX=right.x,rightZ=right.z,forwardX=forward.x,forwardZ=forward.z,
            lastHitBag=lastHitBag,lastHitTime=lastHitTime,lastHitDistance=lastHitDistance,lastHitAlignment=lastHitAlignment,bagAngles=bagSwing,bagVelocities=bagVelocity,
            speedBagConnected=speedBag&&speedBag.HitBoxAssigned,
            speedBagHits=speedBag?speedBag.HitCount:0,
            speedBagLastHitTime=speedBag?speedBag.LastHitTime:0f,
            speedBagLastDistance=speedBag?speedBag.LastDistance:0f,
            speedBagLastAlignment=speedBag?speedBag.LastAlignment:0f,
            speedBagAngles=speedBag?speedBag.Angles:Vector2.zero,
            speedBagVelocities=speedBag?speedBag.AngularVelocity:Vector2.zero,
            gameTime=Time.time,simulationTime=simulationTime,moveX=move.x,moveY=move.y,lastHitImpulse=lastHitImpulse,
            handFound=hand!=null,leftFootFound=leftFoot!=null,rightFootFound=rightFoot!=null,
            leftFootPosition=leftFoot?leftFoot.position:Vector3.zero,rightFootPosition=rightFoot?rightFoot.position:Vector3.zero};
#if UNITY_WEBGL && !UNITY_EDITOR
        BXState(JsonUtility.ToJson(s));
#endif
    }
}
