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
    public int trainingHits;
    private Vector2 move;
    private float yaw, pitch = 19f, distance = 5.8f, vertical, speed;
    private float actionEnd, hitAt, stateAt;
    private string action = "Idle";
    private bool hitPending, doorOpen, paused, runEnabled = true, soundEnabled = true;
    private float doorAngle;
    private float[] bagSwing, bagVelocity;
    private AudioSource audioSource;
    private AudioClip impact;
    private Transform hand;
    private Vector3 cameraTarget;
    private static readonly Vector3 Spawn = new Vector3(1.5f, .08f, 2.8f);
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void BXState(string data);
#endif
    [Serializable] private class Status
    {
        public bool ready, paused, doorOpen;
        public float x,y,z,yaw,pitch,speed,handX,handY,handZ;
        public string animation,context,build;
        public int hits,frames;
    }
    private void Start()
    {
        if (!visual || !clips || !view || !body) throw new InvalidOperationException("Missing original player or controls");
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        clips.playAutomatically = false;
        clips.cullingType = AnimationCullingType.AlwaysAnimate;
        clips.Play("Idle");
        bagSwing = new float[bags.Length]; bagVelocity = new float[bags.Length];
        cameraTarget = transform.position + Vector3.up * 1.65f;
        foreach (Transform t in visual.GetComponentsInChildren<Transform>()) if(t.name.Contains("RightHand")){hand=t;break;}
        audioSource=gameObject.AddComponent<AudioSource>(); audioSource.spatialBlend=0f; audioSource.volume=.28f;
        const int rate=22050; var samples=new float[rate/6];
        for(int i=0;i<samples.Length;i++){float t=(float)i/rate; samples[i]=Mathf.Sin(2f*Mathf.PI*(100f-180f*t)*t)*Mathf.Exp(-t*34f)*.7f;}
        impact=AudioClip.Create("Training impact",samples.Length,1,rate,false); impact.SetData(samples,0);
        ready=true; UpdateCamera(1f); Report();
    }
    public void MoveInput(string values)
    {
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
        if(key=="reset"){body.enabled=false;transform.position=Spawn;body.enabled=true;vertical=0;speed=0;move=Vector2.zero;actionEnd=0;action="Idle";clips.Play("Idle");Report();return;}
        if(key=="run"){runEnabled=!runEnabled;return;}
        if(key=="sound"){soundEnabled=!soundEnabled;return;}
        if(paused)return;
        if(key=="action")
        {
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
        if(!ready||paused)return;
        float dt=Mathf.Min(Time.deltaTime,.12f);
        bool acting=Time.time<actionEnd;
        float target=!acting?(move.magnitude>.12f?(runEnabled&&move.magnitude>.76f?5f:2f)*Mathf.Clamp01(move.magnitude*1.3f):0):0;
        speed=Mathf.MoveTowards(speed,target,dt*15f);
        float a=yaw*Mathf.Deg2Rad;
        Vector3 direction=new Vector3(Mathf.Cos(a)*move.x-Mathf.Sin(a)*move.y,0,-Mathf.Sin(a)*move.x-Mathf.Cos(a)*move.y);
        if(direction.sqrMagnitude>.01f && !acting)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),1-Mathf.Exp(-dt*12f));
        if(body.isGrounded&&vertical<0)vertical=-2f;vertical-=18f*dt;
        Vector3 step=(direction.normalized*speed+Vector3.up*vertical)*dt;
        int n=Mathf.Max(1,Mathf.CeilToInt(step.magnitude/.16f));for(int i=0;i<n;i++)body.Move(step/n);
        if(transform.position.y< -3f)Command("reset");
        if(transform.position.z>20.2f){body.enabled=false;var q=transform.position;q.z=20.2f;transform.position=q;body.enabled=true;}
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
        {bagVelocity[i]+=(-bagSwing[i]*15f-bagVelocity[i]*2.4f)*dt;bagSwing[i]+=bagVelocity[i]*dt;bags[i].localRotation=Quaternion.Euler(0,0,bagSwing[i]);}
        if(Time.unscaledTime>=stateAt){stateAt=Time.unscaledTime+.16f;Report();}
    }
    private void HitBag()
    {
        for(int i=0;i<bags.Length;i++)
        {
            Vector3 d=bags[i].position-transform.position;d.y=0;
            if(d.magnitude<1.85f && Vector3.Dot(transform.forward,d.normalized)>.15f)
            {trainingHits++;bagVelocity[i]+=transform.forward.x>0? -55f:55f;if(soundEnabled)audioSource.PlayOneShot(impact);Report();break;}
        }
    }
    private void LateUpdate(){if(ready)UpdateCamera(Mathf.Min(Time.deltaTime,.1f));}
    private void UpdateCamera(float dt)
    {
        Vector3 want=transform.position+Vector3.up*1.65f;
        cameraTarget=Vector3.Lerp(cameraTarget,want,1-Mathf.Exp(-dt*12f));
        float ya=yaw*Mathf.Deg2Rad,pi=pitch*Mathf.Deg2Rad;
        Vector3 offset=new Vector3(Mathf.Sin(ya)*Mathf.Cos(pi),Mathf.Sin(pi),Mathf.Cos(ya)*Mathf.Cos(pi))*distance;
        Vector3 position=cameraTarget+offset;
        if(Physics.SphereCast(cameraTarget,.18f,offset.normalized,out RaycastHit hit,distance,~(1<<8),QueryTriggerInteraction.Ignore))position=cameraTarget+offset.normalized*Mathf.Max(.8f,hit.distance-.15f);
        view.transform.position=position;view.transform.LookAt(cameraTarget);
        if(contactShadow){var p=transform.position;p.y=body.isGrounded?transform.position.y+.018f:.018f;contactShadow.position=p;}
    }
    private void OnApplicationFocus(bool focus){if(!focus)move=Vector2.zero;}
    private void Report()
    {
        var h=hand?hand.position:Vector3.zero;
        string context=door&&Vector3.Distance(transform.position,new Vector3(6,transform.position.y,11.4f))<2.6f?(doorOpen?"ZAMKNIJ DRZWI":"OTWÓRZ DRZWI"):transform.position.z>18?"ULICA — KOLEJNY ETAP":"";
        var s=new Status{ready=ready,paused=paused,x=transform.position.x,y=transform.position.y,z=transform.position.z,yaw=yaw,pitch=pitch,speed=speed,animation=action,context=context,hits=trainingHits,frames=Time.frameCount,handX=h.x,handY=h.y,handZ=h.z,doorOpen=doorOpen,build=buildId};
#if UNITY_WEBGL && !UNITY_EDITOR
        BXState(JsonUtility.ToJson(s));
#endif
    }
}
