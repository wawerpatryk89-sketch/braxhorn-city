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
    // Read-only pause state for region-specific activities using these controls.
    public bool Paused => paused;
    public BraxCityWorld cityWorld;
    public Vector3 worldSpawn=new Vector3(1.5f,.08f,2.8f);
    public int trainingHits;
    public BraxVessel vessel;   // pojazd wodny, na którym siedzi gracz (BraxVessel.Board)
    public BraxAutoGracza autoGracza;
    public BraxSeat seat;       // sofa / ławka / hoker / łóżko (BraxSeat.Use)
    // Chwila trafienia CIOSU / KOPU (ta sama co trening na workach siłowni): pozycja gracza, kierunek, true = kopnięcie.
    // Słuchają worki, liny ringu i piłki osiedla (BraxWorek, BraxLina, BraxPilka) — bez zależności gracza od nich.
    public static event Action<Vector3,Vector3,bool> OnCios;
    private Vector2 move;
    private float yaw, pitch = 19f, distance = 5.8f, vertical, speed;
    private float actionEnd, hitAt, stateAt;
    private float regionReactionLeft;
    private string action = "Idle";
    private float combatUntil;
    private bool hitPending, doorOpen, paused, runEnabled = true, soundEnabled = true;
    // SKOK (właściciel 1.11, 18:23: „skok zjebany, jakiś podwójny”): klip Jump sam podnosił biodra, a fizyka od razu wybijała
    // Braxa w górę — wychodziły dwa skoki naraz (z kucnięciem w powietrzu). Teraz: wybicie dopiero w chwili odbicia w klipie
    // (biodra ruszają w górę po przysiadzie, najpóźniej po 0.3 s), wysokość daje tylko fizyka (klip bez własnego unoszenia bioder),
    // a w skoku Brax leci dalej z prędkością biegu i da się nim lekko sterować.
    private Transform hips; private float hipsRestY = float.NaN, hipsPrevY = float.NaN, jumpDeadline; private bool jumpPending;
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
        public string animation,context,build,area,activePoint;
        public bool loadingArea;
        public bool loadingParts;   // obszar doczytywany w tle (BraxObszary)
        public bool inCar, carTransition;
        public float carSpeed;
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
        Physics.IgnoreLayerCollision(8, 29, true);   // warstwa 29 = przeszkody tylko dla kamery
        if (!view.GetComponent<BraxClaudeKamera>()) view.gameObject.AddComponent<BraxClaudeKamera>();   // bliska płaszczyzna 0.25 / 0.1 m przy ścianie — też w mieście
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
        {yaw-=dx*.22f;pitch=Mathf.Clamp(pitch+dy*.16f,-32f,70f);lastLook=Time.unscaledTime;}   // 1.11: kamera wyżej w górę (patrzenie na budynki, niebo) i bardziej z góry
    }
    // Obrót kamery palcem bez napisów (sterowanie dotykowe APK, build 14): stopnie w poziomie i w pionie, bez alokacji.
    public void LookDelta(float dYaw,float dPitch)
    {
        if(paused)return;
        yaw+=dYaw;pitch=Mathf.Clamp(pitch+dPitch,-32f,70f);lastLook=Time.unscaledTime;
    }
    // Kamera wraca za plecy sama (MENU może kiedyś wyłączyć); do odczytu w dzienniku UI.
    public static bool AutoCamera=true;
    public const float AutoCameraDelay=1.5f;
    public void Command(string key)
    {
        if(!ready)return;
        if(key=="pause"){paused=!paused;move=Vector2.zero;foreach(AnimationState st in clips)st.speed=paused?0:1;Report();return;}
        if(key=="reset")
        {
            if(autoGracza)autoGracza.PrzedTeleportem();
            if(vessel){transform.SetParent(null,true);vessel=null;}
            if(seat){seat.Leave();}
            body.enabled=false;transform.position=cityWorld?worldSpawn:Spawn;transform.rotation=Quaternion.Euler(0,180,0);body.enabled=true;
            vertical=0;speed=0;move=Vector2.zero;actionEnd=0;regionReactionLeft=0;hitPending=false;hitAt=0;
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
        if(Time.time<actionEnd||regionReactionLeft>0||vessel||seat||autoGracza)return;
        if(key=="punch")StartAction("Punch",1.25f,.43f);
        else if(key=="kick")StartAction("Kick",1.2f,.60f);
        else if(key=="jump" && body.isGrounded && !jumpPending){StartAction("Jump",1.35f,-1);jumpPending=true;jumpDeadline=Time.time+.30f;hipsPrevY=float.NaN;}
    }
    private void StartAction(string name,float rate,float contact)
    {
        if(clips[name]==null)return;
        if ((name=="Punch" || name=="Kick") && clips["CombatIdle"]!=null && !BraxSport.Aktywne) combatUntil=Time.time+8f;
        action=name;clips[name].wrapMode=WrapMode.Once;clips[name].speed=rate;
        clips.CrossFade(name,.10f,PlayMode.StopAll);
        actionEnd=Time.time+Mathf.Min(clips[name].length/rate,name=="Punch"?1.25f:name=="Kick"?1.1f:1.2f);
        hitAt=Time.time+contact/rate;hitPending=contact>=0;Report();
    }
    // Optional regional reaction: existing city/estate callers are unaffected.
    // The same action timer owns movement and animation, so a reaction cannot
    // fight a second controller or leave a pending punch/jump behind it.
    public bool PlayReaction(string name)
    {
        if(!ready||paused||!clips||clips[name]==null||seat||vessel||autoGracza)return false;
        hitPending=false;jumpPending=false;hitAt=0;move=Vector2.zero;speed=0;
        action=name;var state=clips[name];state.wrapMode=WrapMode.Once;state.speed=1;
        regionReactionLeft=state.length;actionEnd=Time.time+state.length;
        clips.CrossFade(name,.10f,PlayMode.StopAll);Report();return true;
    }
    private void Update()
    {
        if(!ready||paused||(cityWorld&&cityWorld.loading))return;
        float dt=Mathf.Min(Time.deltaTime,.12f);
        if(regionReactionLeft>0){regionReactionLeft=Mathf.Max(0,regionReactionLeft-dt);actionEnd=Time.time+regionReactionLeft;}
        simulationTime+=dt;
        if(autoGracza)
        {
            autoGracza.Steruj(move);speed=0;vertical=0;hitPending=false;jumpPending=false;
            action="Idle";actionEnd=0;
            return;
        }
        if(seat)
        {
            if(move.magnitude>.6f){seat.Leave();}
            else{speed=0;vertical=0;if(action!="Idle"){action="Idle";clips["Idle"].wrapMode=WrapMode.Loop;clips["Idle"].speed=1;clips.CrossFade("Idle",.16f,PlayMode.StopAll);}return;}
        }
        if(vessel)
        {
            vessel.Drive(move,dt);speed=0;vertical=0;
            if(action!="Idle"){action="Idle";clips["Idle"].wrapMode=WrapMode.Loop;clips["Idle"].speed=1;clips.CrossFade("Idle",.16f,PlayMode.StopAll);}
            return;
        }
        bool acting=Time.time<actionEnd;
        bool jumping=acting&&action=="Jump";   // w skoku: pęd zostaje, sterowanie lekkie
        float mag=move.magnitude;   // 1.11: chód przy lekkim wychyleniu (0.9–2.1 m/s), płynnie w bieg przy mocnym (do 5.2 m/s), bez przycisku BIEG
        float naJoy=mag<.12f?0f:mag<.7f?Mathf.Lerp(.9f,2.1f,(mag-.12f)/.58f):(runEnabled?Mathf.Lerp(2.1f,5.2f,Mathf.SmoothStep(0f,1f,(mag-.7f)/.22f)):2.1f);
        float target=jumping?speed:!acting?naJoy:0;
        speed=Mathf.MoveTowards(speed,target,dt*15f);
        Vector3 right=Vector3.ProjectOnPlane(view.transform.right,Vector3.up).normalized;
        Vector3 forward=Vector3.ProjectOnPlane(view.transform.forward,Vector3.up).normalized;
        // E22 (build 45): kamera za plecami — kierunek z joysticka liczony od kamery z CHWILI wciśnięcia (gdy kamera obraca się
        // za postacią, trzymany „w bok” nie kręci postaci w kółko); przeciąganie palcem w trakcie — od bieżącej kamery.
        bool joy=mag>=.12f;
        if(joy&&(!joyHeld||Time.unscaledTime-lastLook<.15f)){joyRefYaw=Mathf.Atan2(forward.x,forward.z)*Mathf.Rad2Deg;if(!joyHeld)joyStart=Time.unscaledTime;}
        joyHeld=joy;
        if(AutoCamera&&joy){var q=Quaternion.Euler(0,joyRefYaw,0);right=q*Vector3.right;forward=q*Vector3.forward;}
        Vector3 direction=right*move.x+forward*move.y;
        if(direction.sqrMagnitude>.01f && (!acting||jumping))transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),1-Mathf.Exp(-dt*12f));
        if(jumpPending)
        {
            bool odbicie=Time.time>=jumpDeadline;
            if(hips&&!float.IsNaN(hipsPrevY)&&Time.time>jumpDeadline-.2f&&visual.InverseTransformPoint(hips.position).y>hipsPrevY+.0015f)odbicie=true;   // koniec przysiadu
            if(hips)hipsPrevY=visual.InverseTransformPoint(hips.position).y;
            if(odbicie){jumpPending=false;if(body.isGrounded)vertical=6.2f;}
        }
        if(body.isGrounded&&vertical<0)vertical=-2f;vertical-=18f*dt;
        Vector3 moveDir=jumping&&direction.sqrMagnitude<.01f?transform.forward:direction.normalized;
        Vector3 step=(moveDir*speed+Vector3.up*vertical)*dt;
        int n=Mathf.Max(1,Mathf.CeilToInt(step.magnitude/.16f));for(int i=0;i<n;i++)body.Move(step/n);
        if(transform.position.y<(cityWorld?-20f:-3f))Command("reset");
        if(!cityWorld&&transform.position.z>20.2f){body.enabled=false;var q=transform.position;q.z=20.2f;transform.position=q;body.enabled=true;}
        if(hitPending&&Time.time>=hitAt){hitPending=false;HitBag();OnCios?.Invoke(transform.position,transform.forward,action=="Kick");}
        if(!acting)
        {
            string next=speed>3f?"Run":speed>.12f?"Walk":Time.time<combatUntil && clips["CombatIdle"]!=null && !BraxSport.Aktywne?"CombatIdle":"Idle";
            if(action!=next){action=next;clips[next].wrapMode=WrapMode.Loop;clips[next].speed=1;clips.CrossFade(next,.16f,PlayMode.StopAll);}
            if(next=="Walk")clips[next].speed=Mathf.Max(.55f,speed/2f);
            if(next=="Run")clips[next].speed=Mathf.Max(.65f,speed/5f);
        }
        if(door&&!doorOpen&&move.sqrMagnitude>.01f&&Vector3.Distance(transform.position,new Vector3(6,transform.position.y,11.4f))<1.9f){doorOpen=true;Report();}   // drzwi siłowni otwierają się same przy podejściu
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
    // Klip Jump bez własnego unoszenia bioder (wysokość daje fizyka); przysiad przy odbiciu i lądowaniu zostaje.
    private void JumpHips()
    {
        if(!hips&&visual){foreach(var t in visual.GetComponentsInChildren<Transform>(true))if(t.name.EndsWith("Hips")){hips=t;break;}}
        if(!hips)return;
        if(action!="Jump"&&!jumpPending){if(action=="Idle")hipsRestY=float.IsNaN(hipsRestY)?visual.InverseTransformPoint(hips.position).y:Mathf.Lerp(hipsRestY,visual.InverseTransformPoint(hips.position).y,.05f);return;}
        if(float.IsNaN(hipsRestY))return;
        var p=visual.InverseTransformPoint(hips.position);if(p.y>hipsRestY){p.y=hipsRestY;hips.position=visual.TransformPoint(p);}
    }
    private void LateUpdate()
    {
        JumpHips();
        if(!ready)return;
        UpdateCamera(Mathf.Min(Time.deltaTime,.1f));
        if(reviewEnabled||Time.unscaledTime>=stateAt){stateAt=Time.unscaledTime+.16f;Report();}
    }
    private bool hiddenClose;
    // KAMERA ZA PLECAMI (build 14; właściciel 20:45, zmiana zasady „kamerę obraca tylko gracz”): kamera sama, płynnie
    // obraca się za postacią w ruchu (pieszo, łodzią); gracz może ją obracać palcem, a 1.5 s po ostatnim obrocie ręcznym
    // dociąganie za plecy wraca stopniowo (0.8 s narastania). Przy staniu kamera się nie kręci (właściciel). Pieszo siła
    // dociągania zależy od kąta: postać biegnąca w bok / w stronę kamery jej nie zawraca (sterowanie jest względem kamery —
    // inaczej postać kręciłaby się w kółko), biegnąca do przodu ustawia kamerę za plecami. Na łodzi zawsze za rufą.
    private float lastLook=-99f,camSpeed,camNear=-1f;private Vector3 camPrevPos;private bool camPrevOk;
    private float joyRefYaw,joyStart=-99f;private bool joyHeld;
    // E22: wygładzenie powrotu za plecy — stała czasu 0.32 s, martwa strefa 4°, maks. 220°/s (czysta funkcja — test regresji)
    public const float ZaPlecamiTau=.32f,ZaPlecamiMartwa=4f,ZaPlecamiMaks=220f;
    public static float KrokZaPlecy(float yaw,float cel,float dt)
    {
        float d=Mathf.DeltaAngle(yaw,cel); if(Mathf.Abs(d)<ZaPlecamiMartwa)return yaw;
        float krok=d*(1f-Mathf.Exp(-dt/ZaPlecamiTau)); float max=ZaPlecamiMaks*dt;
        return yaw+Mathf.Clamp(krok,-max,max);
    }
    readonly RaycastHit[] cameraHits=new RaycastHit[32];
    readonly Collider[] cameraOverlaps=new Collider[32];
    bool CameraObstacle(Collider c) => c && !(autoGracza && c.transform.IsChildOf(autoGracza.transform));
    bool CameraBlocked(Vector3 position,int mask)
    {
        if(!autoGracza)return Physics.CheckSphere(position,.22f,mask,QueryTriggerInteraction.Ignore);
        int n=Physics.OverlapSphereNonAlloc(position,.22f,cameraOverlaps,mask,QueryTriggerInteraction.Ignore);
        if(n==cameraOverlaps.Length)return true;
        for(int i=0;i<n;i++)if(CameraObstacle(cameraOverlaps[i]))return true;
        return false;
    }
    private void AutoYaw(float dt,bool snap)
    {
        Vector3 pos=transform.position;
        if(snap||!camPrevOk||dt<1e-4f){camPrevPos=pos;camPrevOk=true;camSpeed=0;return;}
        Vector3 dv=pos-camPrevPos;dv.y=0;camPrevPos=pos;
        float v=dv.magnitude/dt;if(v<25f)camSpeed=Mathf.Lerp(camSpeed,v,1-Mathf.Exp(-dt*6f));   // teleport (> 25 m/s) pomijany
        if(!AutoCamera||reviewEnabled||paused||seat)return;
        float since=Time.unscaledTime-lastLook;
        bool water=vessel||autoGracza;
        if(!water)
        {   // E22: pieszo ZAWSZE za plecami — w ruchu yaw podąża za kierunkiem postaci (0.32 s, martwa strefa 4°); po rozejrzeniu
            // palcem powrót po 1.5 s bez ruchu kamery albo od razu, gdy postać rusza (joystick wciśnięty po rozejrzeniu)
            if(since<AutoCameraDelay&&!(joyHeld&&joyStart>lastLook))return;
            if(camSpeed<.35f)return;                                              // przy staniu kamera się nie kręci
            Vector3 fw=transform.forward;fw.y=0;if(fw.sqrMagnitude<.01f)return;
            yaw=KrokZaPlecy(yaw,Mathf.Atan2(-fw.x,-fw.z)*Mathf.Rad2Deg,dt);
            pitch=Mathf.Lerp(pitch,Mathf.Clamp(pitch,8f,32f),1-Mathf.Exp(-dt*1.5f));
            return;
        }
        if(since<AutoCameraDelay)return;
        if(camSpeed<(water?.8f:.6f))return;
        Vector3 f=autoGracza?autoGracza.transform.forward:vessel?vessel.transform.forward:transform.forward;f.y=0;if(f.sqrMagnitude<.01f)return;
        float wantYaw=Mathf.Atan2(-f.x,-f.z)*Mathf.Rad2Deg;   // kamera po przeciwnej stronie niż przód postaci
        float delta=Mathf.DeltaAngle(yaw,wantYaw);
        float w=water?1f:Mathf.Clamp01(Mathf.Cos(delta*Mathf.Deg2Rad));
        float ramp=Mathf.Clamp01((since-AutoCameraDelay)/.8f);
        float v01=Mathf.Clamp01(camSpeed/(water?7f:5.2f));
        float rate=(water?1.8f:Mathf.Lerp(.3f,1.2f,v01*v01))*w*ramp;   // 1/s: chód łagodnie, bieg szybciej
        float max=(water?100f:85f)*dt;
        yaw+=Mathf.Clamp(delta*(1-Mathf.Exp(-dt*rate)),-max,max);
        // pochylenie: skrajne (z dołu / z góry) wraca do wygodnego zakresu 8–32°
        pitch=Mathf.Lerp(pitch,Mathf.Clamp(pitch,8f,32f),1-Mathf.Exp(-dt*.9f*ramp*w));
    }
    private void UpdateCamera(float dt)
    {
        bool snap=dt>=.5f;   // Start, reset, teleport: kamera od razu na miejscu
        AutoYaw(dt,snap);
        Vector3 want=autoGracza?autoGracza.CelKamery:transform.position+Vector3.up*1.65f;
        cameraTarget=Vector3.Lerp(cameraTarget,want,1-Mathf.Exp(-dt*10f));   // build 14: lekkie opóźnienie za postacią (10/s)
        float ya=yaw*Mathf.Deg2Rad,pi=pitch*Mathf.Deg2Rad;
        float viewDistance=autoGracza?Mathf.Lerp(7f,9.5f,Mathf.Clamp01(Mathf.Abs(autoGracza.Predkosc)/12f)):distance;
        Vector3 offset=new Vector3(Mathf.Sin(ya)*Mathf.Cos(pi),Mathf.Sin(pi),Mathf.Cos(ya)*Mathf.Cos(pi))*viewDistance;
        Vector3 position=cameraTarget+offset;
        // Kamera (audyt filmów właściciela 1.11): nie wchodzi w ściany, szyby, korony drzew i lampy — kolidują z nią też
        // przeszkody „tylko dla kamery” (warstwa 29: korony, klosze, szkło balustrad; gracz przez nie przechodzi).
        // Gdy ściana podsuwa kamerę bliżej niż 1.1 m, Brax znika z obrazu (zostaje jego cień) — pokój widać, a nie plecy.
        // Build 14: kula 0.26 m (róg bliskiej płaszczyzny 0.15 m przy FOV 55° i 20:9 = 0.24 m od kamery) + dosunięcie, gdy
        // ściana boczna jest tuż przy kamerze (SphereCast nie widzi przeszkody, w której startuje — wąskie przejścia).
        const int mask=~(1<<8);Vector3 dir=offset.normalized;
        float near=viewDistance;
        if(autoGracza)
        {
            int n=Physics.SphereCastNonAlloc(cameraTarget,.26f,dir,cameraHits,viewDistance,mask,QueryTriggerInteraction.Ignore);
            if(n==cameraHits.Length)near=.35f;
            for(int i=0;i<n;i++)if(CameraObstacle(cameraHits[i].collider))near=Mathf.Min(near,Mathf.Max(.35f,cameraHits[i].distance-.12f));
        }
        else if(Physics.SphereCast(cameraTarget,.26f,dir,out RaycastHit hit,viewDistance,mask,QueryTriggerInteraction.Ignore))near=Mathf.Max(.35f,hit.distance-.12f);
        position=cameraTarget+dir*near;
        // build 45: cienka ściana, w której kula startuje (drzwi, ościeżnica) — promień od celu do kamery też ją widzi
        if(Physics.Linecast(cameraTarget,position,out RaycastHit line,mask,QueryTriggerInteraction.Ignore)&&CameraObstacle(line.collider)){near=Mathf.Min(near,Mathf.Max(.35f,line.distance-.2f));position=cameraTarget+dir*near;}
        for(int k=0;k<4&&near>.36f&&CameraBlocked(position,mask);k++){near=Mathf.Max(.35f,near*.72f);position=cameraTarget+dir*near;}
        // przeszkoda znika (minięty pień, róg) — kamera odjeżdża płynnie (3.5 m/s), a nie skokiem; do przeszkody — od razu
        if(snap||camNear<0f||near<camNear)camNear=near;else{camNear=Mathf.MoveTowards(camNear,near,dt*3.5f);near=camNear;position=cameraTarget+dir*near;}
        view.transform.position=position;view.transform.LookAt(cameraTarget);
        // build 45 (E8, nagranie: kamera „w głowie” Patryka przy drzwiach): postać chowana też wtedy, gdy kamera jest blisko CIAŁA
        // (kapsuła od stóp do czubka głowy), a nie tylko blisko celu na 1.65 m — Patryk 2.27 m ma głowę wyżej niż cel kamery
        bool close=near<1.1f||(!autoGracza&&OdlegloscOdCiala(view.transform.position)<.55f);
        if(close!=hiddenClose){hiddenClose=close;foreach(var r in visual.GetComponentsInChildren<Renderer>(true))r.shadowCastingMode=close?UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly:UnityEngine.Rendering.ShadowCastingMode.On;}
        if(contactShadow){var p=transform.position;
            if(cityWorld&&Physics.Raycast(p+Vector3.up*.25f,Vector3.down,out RaycastHit floor,30,~((1<<8)|(1<<29)),QueryTriggerInteraction.Ignore))p.y=floor.point.y+.018f;
            else p.y=body.isGrounded?transform.position.y+.018f:.018f;
            contactShadow.position=p;}
    }
    // Odległość punktu od powierzchni kapsuły ciała gracza (CharacterController; bez niego — pion 0–2.3 m, r 0.3).
    public float OdlegloscOdCiala(Vector3 p)
    {
        float r=body?body.radius:.3f, h=body?body.height:2.3f; var c=transform.position+(body?body.center-Vector3.up*h*.5f:Vector3.zero);
        Vector3 a=c+Vector3.up*r, b=c+Vector3.up*Mathf.Max(r,h-r), ab=b-a;
        float t=Mathf.Clamp01(Vector3.Dot(p-a,ab)/Mathf.Max(1e-6f,ab.sqrMagnitude));
        return Vector3.Distance(p,a+ab*t)-r;
    }
    public void ReviewView(string text)
    {
        if(!reviewEnabled)return;
        if(autoGracza)autoGracza.PrzedTeleportem();
        var a=text.Split(',');if(a.Length!=6)return;var v=new float[6];
        for(int i=0;i<6;i++)if(!float.TryParse(a[i],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out v[i])||float.IsNaN(v[i])||float.IsInfinity(v[i]))return;
        move=Vector2.zero;speed=0;vertical=0;actionEnd=0;regionReactionLeft=0;hitPending=false;action="Idle";clips.Play("Idle");
        body.enabled=false;transform.position=new Vector3(v[0],v[1],v[2]);body.enabled=true;
        yaw=v[3];pitch=Mathf.Clamp(v[4],-32f,70f);distance=Mathf.Clamp(v[5],.8f,12f);
        cameraTarget=transform.position+Vector3.up*1.65f;UpdateCamera(1f);Report();
    }
    public void TravelTo(Vector3 position)
    {
        if(autoGracza)autoGracza.PrzedTeleportem();
        move=Vector2.zero;speed=0;vertical=0;actionEnd=0;regionReactionLeft=0;hitPending=false;action="Idle";
        body.enabled=false;transform.position=position;body.enabled=true;clips.Play("Idle");
        cameraTarget=position+Vector3.up*1.65f;UpdateCamera(1f);Report();
    }
    // Przyjazd z drugiego rejonu (tunel): pozycja + kierunek kamery (0 = kamera na +z, patrzy na −z).
    public void TravelTo(Vector3 position,float cameraYaw){yaw=cameraYaw;pitch=19f;transform.rotation=Quaternion.Euler(0,cameraYaw+180f,0);TravelTo(position);}
    public void TravelPoint(string id){if(cityWorld&&!paused)cityWorld.GoTo(id);}
    // QA (tylko ?qa=1): przejście do drugiego rejonu bez chodzenia tunelem — zdjęcia osiedla z playera miasta.
    public void ReviewScene(string targetArea){if(reviewEnabled&&cityWorld)cityWorld.ReviewScene(targetArea);}
    public void ReviewCar(string command)
    {
        if(!reviewEnabled)return;
        var car=autoGracza?autoGracza:BraxAutoGracza.Wszystkie.Find(c=>c&&c.isActiveAndEnabled);
        if(!car)return;
        if(command=="approach")TravelTo(car.Podejscie+Vector3.up*.06f,car.transform.eulerAngles.y+180f);
        else if(command=="interact")car.Wykonaj(transform.position);
    }
    private static bool ReviewEnabled()
    {
        if(!Uri.TryCreate(Application.absoluteURL,UriKind.Absolute,out var url))return false;
        foreach(var value in url.Query.TrimStart('?').Split('&'))if(value=="qa=1")return true;
        return false;
    }
    private void OnApplicationFocus(bool focus){if(!focus)move=Vector2.zero;}
    // Napis przycisku akcji (strona WWW i sterowanie dotykowe w APK): drzwi siłowni, potem kontekst miasta (pojazdy,
    // siedziska, drzwi lokali, windy, przejście na osiedle), na końcu pojazd / siedzisko gracza.
    public string Context
    {
        get
        {
            string context=door&&Vector3.Distance(transform.position,new Vector3(6,transform.position.y,11.4f))<2.6f?(doorOpen?"ZAMKNIJ DRZWI":"OTWÓRZ DRZWI"):transform.position.z>18?"ULICA — KOLEJNY ETAP":"";
            if(cityWorld){if(!string.IsNullOrEmpty(cityWorld.Context))context=cityWorld.Context;else if(context=="ULICA — KOLEJNY ETAP")context="";}
            if(vessel)context="ZEJDŹ · "+vessel.label;
            if(seat)context="WSTAŃ";
            return context;
        }
    }
    private void Report()
    {
        var h=hand?hand.position:Vector3.zero;
        string context=Context;
        var right=Vector3.ProjectOnPlane(view.transform.right,Vector3.up).normalized;
        var forward=Vector3.ProjectOnPlane(view.transform.forward,Vector3.up).normalized;
        var s=new Status{area=cityWorld?cityWorld.area:"gym",activePoint=cityWorld?cityWorld.activePoint:"",points=cityWorld?cityWorld.points:null,loadingArea=cityWorld&&cityWorld.loading,loadingParts=BraxObszary.Laduje>0,cityMeshes=cityWorld?cityWorld.originalMeshes:0,cityTriangles=cityWorld?cityWorld.originalTriangles:0,cityColliders=cityWorld?cityWorld.sourceColliders:0,openDoors=cityWorld?cityWorld.OpenDoors:0,ready=ready,paused=paused,x=transform.position.x,y=transform.position.y,z=transform.position.z,yaw=yaw,pitch=pitch,speed=speed,animation=action,context=context,hits=trainingHits,frames=Time.frameCount,handX=h.x,handY=h.y,handZ=h.z,doorOpen=doorOpen,build=buildId,
            grounded=body.isGrounded,runEnabled=runEnabled,playerYaw=transform.eulerAngles.y,cameraYaw=view.transform.eulerAngles.y,verticalSpeed=vertical,
            rightX=right.x,rightZ=right.z,forwardX=forward.x,forwardZ=forward.z,
            lastHitBag=lastHitBag,lastHitTime=lastHitTime,lastHitDistance=lastHitDistance,lastHitAlignment=lastHitAlignment,bagAngles=bagSwing,bagVelocities=bagVelocity,
            gameTime=Time.time,simulationTime=simulationTime,moveX=move.x,moveY=move.y,lastHitImpulse=lastHitImpulse,
            inCar=autoGracza,carTransition=autoGracza&&autoGracza.WTrakcie,carSpeed=autoGracza?autoGracza.Predkosc:0,
            handFound=hand!=null,leftFootFound=leftFoot!=null,rightFootFound=rightFoot!=null,
            leftFootPosition=leftFoot?leftFoot.position:Vector3.zero,rightFootPosition=rightFoot?rightFoot.position:Vector3.zero};
#if UNITY_WEBGL && !UNITY_EDITOR
        BXState(JsonUtility.ToJson(s));
#endif
    }
}
