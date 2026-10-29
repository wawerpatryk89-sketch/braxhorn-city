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
    // City-only camera tracking. The original gym/estate camera, animation,
    // touch joystick and player controller remain exactly as before.
    private float cityManualLookUntil,cityAutoYawVelocity;
    private bool cityAutomaticFollow;
    // Diagnostic-only external QA camera. Never enabled in normal gameplay.
    // Lets us send honest aerial/rooftop screenshots from the actual Unity
    // render without moving or teleporting the playable Brax.
    private bool reviewAerial;
    private Vector3 reviewAerialPos;
    private Quaternion reviewAerialRotation;
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
        public string animation,context,build,area,activePoint;
        public bool loadingArea,cityAutomaticCameraFollow;
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
        {
            move=Vector2.ClampMagnitude(new Vector2(x,y),1);
            if(move.sqrMagnitude>.0001f)reviewAerial=false;
        }
    }
    public void LookInput(string values)
    {
        if(paused)return;var p=values.Split(',');
        if(p.Length>=2 && float.TryParse(p[0],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float dx) && float.TryParse(p[1],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float dy))
        {
            yaw=Mathf.Repeat(yaw-dx*.22f,360f);
            // Full -90..+90 pitch is preserved, so Brax can look straight at
            // the zenith, sun and cloud textures without a "ceiling" clamp.
            pitch=Mathf.Clamp(pitch+dy*.16f,-90f,90f);
            if(cityWorld && cityWorld.area=="city" && (Mathf.Abs(dx)+Mathf.Abs(dy)>.05f))
            {
                // Touch input has priority. No camera snap during a user orbit.
                cityManualLookUntil=Time.time+3.5f;
                cityAutoYawVelocity=0;
                cityAutomaticFollow=false;
            }
        }
    }
    private void FollowCityCamera(float dt)
    {
        cityAutomaticFollow=false;
        if(!cityWorld||cityWorld.area!="city"||Time.time<cityManualLookUntil||paused)return;
        // Look around while stopped or walking sideways: the player's chosen
        // angle remains stable. Follow only normal forward locomotion; changing
        // yaw during pure sidesteps would continuously rotate the joystick
        // direction and make Brax run in circles.
        if(move.y<=.45f || Mathf.Abs(move.x)>.28f || speed<.4f)return;
        float behindYaw=Mathf.Repeat(transform.eulerAngles.y-180f,360f);
        yaw=Mathf.Repeat(Mathf.SmoothDampAngle(yaw,behindYaw,
            ref cityAutoYawVelocity,.42f,135f,dt),360f);
        cityAutomaticFollow=true;
        // Do NOT recenter pitch: manual full vertical look stays at the
        // height selected by the user, including -90° into the sky.
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
        // The player's horizontal controls remain well defined while looking
        // exactly at the zenith/nadir, where projection of camera.forward is zero.
        var heading=Quaternion.Euler(0,yaw+180f,0);
        Vector3 right=heading*Vector3.right;
        Vector3 forward=heading*Vector3.forward;
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
                if(soundEnabled)audioSource.PlayOneShot(impact);Report();break;
            }
        }
    }
    private void LateUpdate()
    {
        if(!ready)return;
        float cameraDt=Mathf.Min(Time.deltaTime,.1f);
        FollowCityCamera(cameraDt);
        UpdateCamera(cameraDt);
        if(reviewEnabled&&reviewAerial)
        {
            view.transform.position=reviewAerialPos;
            view.transform.rotation=reviewAerialRotation;
        }
        if(reviewEnabled||Time.unscaledTime>=stateAt){stateAt=Time.unscaledTime+.16f;Report();}
    }
    private void UpdateCamera(float dt)
    {
        Vector3 want=transform.position+Vector3.up*1.65f;
        cameraTarget=Vector3.Lerp(cameraTarget,want,1-Mathf.Exp(-dt*12f));
        float ya=yaw*Mathf.Deg2Rad,pi=pitch*Mathf.Deg2Rad;
        // At the vertical poles retain a small horizontal stand-off, so a
        // straight-up view comes from beside the player instead of underneath
        // their mesh. Pitch remains entirely controlled by the user's input.
        float horizontal=Mathf.Max(distance*Mathf.Cos(pi),Mathf.Min(1.3f,distance));
        Vector3 offset=new Vector3(Mathf.Sin(ya)*horizontal,Mathf.Sin(pi)*distance,Mathf.Cos(ya)*horizontal);
        offset.y=Mathf.Max(offset.y,transform.position.y+.45f-cameraTarget.y);
        Vector3 position=cameraTarget+offset;
        if(Physics.SphereCast(cameraTarget,.18f,offset.normalized,out RaycastHit hit,offset.magnitude,~(1<<8),QueryTriggerInteraction.Ignore))position=cameraTarget+offset.normalized*Mathf.Max(.05f,hit.distance-.15f);
        view.transform.position=position;
        // Explicit orientation avoids LookAt's ambiguous up vector at +/-90.
        view.transform.rotation=Quaternion.Euler(pitch,yaw+180f,0);
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
        reviewAerial=false;
        move=Vector2.zero;speed=0;vertical=0;actionEnd=0;hitPending=false;action="Idle";clips.Play("Idle");
        body.enabled=false;transform.position=new Vector3(v[0],v[1],v[2]);body.enabled=true;
        yaw=Mathf.Repeat(v[3],360f);pitch=Mathf.Clamp(v[4],-90f,90f);distance=Mathf.Clamp(v[5],.8f,12f);
        cameraTarget=transform.position+Vector3.up*1.65f;UpdateCamera(1f);Report();
    }
    public void ReviewAerialCamera(string values)
    {
        if(!reviewEnabled || !cityWorld || cityWorld.area!="city")return;
        // Only the camera moves for QA: Brax remains on the street, physics
        // and game interactions continue to use his true physical position.
        // Coordinates are real Unity metres, not an illustration transform.
        var parts=values.Split(',');
        if(parts.Length!=5)return;
        var v=new float[5];
        for(int i=0;i<v.Length;i++)
            if(!float.TryParse(parts[i],System.Globalization.NumberStyles.Float,
                 System.Globalization.CultureInfo.InvariantCulture,out v[i])||
               float.IsNaN(v[i])||float.IsInfinity(v[i]))return;
        if(Mathf.Abs(v[0])>150f||v[1]<3f||v[1]>160f||v[2]<-60f||v[2]>140f)return;
        reviewAerialPos=new Vector3(v[0],v[1],v[2]);
        reviewAerialRotation=Quaternion.Euler(Mathf.Clamp(v[4],-89f,89f),
                                             Mathf.Repeat(v[3],360f),0);
        reviewAerial=true;
        view.transform.position=reviewAerialPos;
        view.transform.rotation=reviewAerialRotation;
        Debug.Log("CITY_REAL_UNITY_AERIAL_QA cameraXYZ="+reviewAerialPos+
            " BraxRemainsAt="+transform.position+
            " notAGameplayTeleport=true");
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
        var heading=Quaternion.Euler(0,yaw+180f,0);
        var right=heading*Vector3.right;
        var forward=heading*Vector3.forward;
        var s=new Status{area=cityWorld?cityWorld.area:"gym",activePoint=cityWorld?cityWorld.activePoint:"",points=cityWorld?cityWorld.points:null,loadingArea=cityWorld&&cityWorld.loading,cityMeshes=cityWorld?cityWorld.originalMeshes:0,cityTriangles=cityWorld?cityWorld.originalTriangles:0,cityColliders=cityWorld?cityWorld.sourceColliders:0,openDoors=cityWorld?cityWorld.OpenDoors:0,ready=ready,paused=paused,x=transform.position.x,y=transform.position.y,z=transform.position.z,yaw=yaw,pitch=pitch,speed=speed,animation=action,context=context,hits=trainingHits,frames=Time.frameCount,handX=h.x,handY=h.y,handZ=h.z,doorOpen=doorOpen,build=buildId,
            grounded=body.isGrounded,runEnabled=runEnabled,cityAutomaticCameraFollow=cityAutomaticFollow,playerYaw=transform.eulerAngles.y,cameraYaw=Mathf.Repeat(yaw+180f,360f),verticalSpeed=vertical,
            rightX=right.x,rightZ=right.z,forwardX=forward.x,forwardZ=forward.z,
            lastHitBag=lastHitBag,lastHitTime=lastHitTime,lastHitDistance=lastHitDistance,lastHitAlignment=lastHitAlignment,bagAngles=bagSwing,bagVelocities=bagVelocity,
            gameTime=Time.time,simulationTime=simulationTime,moveX=move.x,moveY=move.y,lastHitImpulse=lastHitImpulse,
            handFound=hand!=null,leftFootFound=leftFoot!=null,rightFootFound=rightFoot!=null,
            leftFootPosition=leftFoot?leftFoot.position:Vector3.zero,rightFootPosition=rightFoot?rightFoot.position:Vector3.zero};
#if UNITY_WEBGL && !UNITY_EDITOR
        BXState(JsonUtility.ToJson(s));
#endif
    }
}
