using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Private road vehicle. Only the player interaction registry can board it; traffic never owns it.
[DefaultExecutionOrder(50)]
public sealed class BraxAutoGracza : MonoBehaviour, IBraxInterakcja
{
    public static readonly List<BraxAutoGracza> Wszystkie = new List<BraxAutoGracza>();
    public Transform fotel;
    public BoxCollider obrys;
    public Transform[] kola = new Transform[0], skret = new Transform[0];
    public float rozstaw = 3.3f, promienKola = .42f, maxPredkosc = 12f;
    public AudioClip silnik;
    public float Predkosc => predkosc;
    public Vector3 przesuniecieGracza; // authored from the actual CarIdle hips, in car space
    public bool Zajety => gracz;
    public bool WTrakcie => przejscie;
    public Vector3 Podejscie => Drzwi(-1);
    public Vector3 CelKamery => transform.TransformPoint(obrys.center + Vector3.up * (obrys.size.y*.5f+.35f));
    BraxGymPlayer gracz;
    Scene scenaGracza;
    Vector2 wejscie;
    float predkosc, kierownica, obrotKol, ostatnieSterowanie;
    bool przejscie;
    Vector3 startWejscia;
    AudioSource audioSilnika;
    Rigidbody cialo;
    readonly RaycastHit[] hits = new RaycastHit[32];
    readonly Collider[] overlaps = new Collider[32];
    const int Mask = ~((1<<8)|(1<<29));

    void OnEnable() { Wszystkie.Add(this); BraxInterakcje.Dodaj(this); }
    void OnDisable() { Wszystkie.Remove(this); BraxInterakcje.Usun(this); if (gracz && !BraxSceneTransit.Busy) Odlacz(startWejscia); }
    void Start()
    {
        if (!obrys) obrys = GetComponent<BoxCollider>();
        cialo=GetComponent<Rigidbody>();
        if (silnik) { audioSilnika=gameObject.AddComponent<AudioSource>(); audioSilnika.clip=silnik; audioSilnika.loop=true; audioSilnika.spatialBlend=1; audioSilnika.minDistance=4; audioSilnika.maxDistance=45; audioSilnika.playOnAwake=false; }
    }
    bool Obcy(Collider c) => c && !c.transform.IsChildOf(transform) && !(gracz && c.transform.IsChildOf(gracz.transform));
    Vector3 Drzwi(int strona) => transform.TransformPoint(new Vector3(strona*(obrys.size.x*.5f+.65f),0,fotel ? transform.InverseTransformPoint(fotel.position).z : 0));
    public string Kontekst(Vector3 p)
    {
        if (gracz) return przejscie ? "WSIADANIE / WYSIADANIE" : Mathf.Abs(predkosc)>.25f ? "ZATRZYMAJ, ABY WYSIĄŚĆ" : "WYSIĄDŹ · AUTO PATRYKA";
        return fotel && obrys && (Drzwi(-1)-p).sqrMagnitude < 2.5f*2.5f ? "WSIĄDŹ · AUTO PATRYKA" : "";
    }
    public bool Wykonaj(Vector3 p)
    {
        if (przejscie) return true;
        if (gracz) { if (Mathf.Abs(predkosc)<.25f && MiejsceWyjscia(out var point)) StartCoroutine(Wyjdz(point)); return true; }
        var player=FindAnyObjectByType<BraxGymPlayer>();
        if (!player || !player.clips || !player.clips.GetClip("CarIdle") || player.autoGracza || player.vessel || player.seat || !fotel || !obrys || (Drzwi(-1)-p).sqrMagnitude>6.25f) return false;
        var approach=Drzwi(-1)-player.transform.position;approach.y=0;
        if(approach.sqrMagnitude>.01f)
        {
            float radius=player.body.radius,height=player.body.height;
            int count=Physics.CapsuleCastNonAlloc(player.transform.position+Vector3.up*(radius+.08f),
                player.transform.position+Vector3.up*(height-radius),radius,approach.normalized,hits,approach.magnitude,Mask,QueryTriggerInteraction.Ignore);
            if(count==hits.Length)return false;
            for(int i=0;i<count;i++)if(hits[i].collider&&!hits[i].collider.transform.IsChildOf(player.transform))return false;
        }
        gracz=player; scenaGracza=player.gameObject.scene; startWejscia=player.transform.position;
        BraxPose.Get(player).Set(BraxPose.Mode.None,null);
        player.autoGracza=this; player.body.enabled=false;
        player.clips.Play("Idle"); player.clips.Sample();
        StartCoroutine(Wejdz()); return true;
    }
    IEnumerator Wejdz()
    {
        przejscie=true;
        var p=gracz; Vector3 start=p.transform.position; Quaternion rot=p.transform.rotation;
        var door=Drzwi(-1); if(Grunt(door,out float floor))door.y=floor;
        p.clips.CrossFade("Walk",.15f);
        float seconds=Mathf.Max(.15f,Vector3.Distance(start,door)/1.5f);
        for(float t=0;t<1;t+=Time.deltaTime/seconds)
        {
            if(!p)yield break;
            p.transform.SetPositionAndRotation(Vector3.Lerp(start,door,t),Quaternion.Slerp(rot,transform.rotation,t));
            yield return null;
        }
        p.transform.position=door;
        p.clips.CrossFade("CarIdle",.55f);
        Vector3 end=transform.TransformPoint(przesuniecieGracza);
        for(float t=0;t<1f;t+=Time.deltaTime/.9f)
        {
            if(!p)yield break;
            float u=Mathf.SmoothStep(0,1,t);
            p.transform.SetPositionAndRotation(Vector3.Lerp(door,end,u),transform.rotation);
            yield return null;
        }
        p.transform.SetParent(transform,true); p.transform.SetPositionAndRotation(end,transform.rotation);
        przejscie=false; if(audioSilnika){audioSilnika.volume=.45f;audioSilnika.Play();}
    }
    public void Steruj(Vector2 move) { wejscie=Vector2.ClampMagnitude(move,1); ostatnieSterowanie=Time.unscaledTime; }
    bool Grunt(Vector3 pos, out float y)
    {
        y=0; int n=Physics.RaycastNonAlloc(pos+Vector3.up*2,Vector3.down,hits,5,Mask,QueryTriggerInteraction.Ignore);
        float nearest=float.PositiveInfinity; bool found=false;
        for(int i=0;i<n;i++) if(Obcy(hits[i].collider) && hits[i].normal.y>.75f && hits[i].distance<nearest)
        { nearest=hits[i].distance; y=hits[i].point.y; found=true; }
        return found;
    }
    void FixedUpdate()
    {
        if(!gracz || przejscie || !obrys || !cialo) return;
        float dt=Time.fixedDeltaTime;
        var position=cialo.position; var rotation=cialo.rotation;
        if(Time.unscaledTime-ostatnieSterowanie>.15f) wejscie=Vector2.zero;
        float desired=wejscie.y*(wejscie.y>=0?maxPredkosc:maxPredkosc*.3f);
        predkosc=Mathf.MoveTowards(predkosc,desired,dt*(Mathf.Abs(desired)<Mathf.Abs(predkosc)?9f:4f));
        kierownica=Mathf.MoveTowards(kierownica,wejscie.x*32f,dt*85f);
        float yaw=predkosc/Mathf.Max(1,rozstaw)*Mathf.Tan(kierownica*Mathf.Deg2Rad)*Mathf.Rad2Deg*dt;
        var rot=rotation*Quaternion.Euler(0,yaw,0);
        Vector3 delta=(rot*Vector3.forward)*predkosc*dt, next=position+delta;
        var half=obrys.size*.5f; half.y=Mathf.Max(.2f,half.y-.16f);
        var center=position+rotation*obrys.center+Vector3.up*.16f;
        int n=Physics.BoxCastNonAlloc(center,half,delta.sqrMagnitude>1e-8f?delta.normalized:transform.forward,hits,rot,delta.magnitude+.08f,Mask,QueryTriggerInteraction.Ignore);
        bool blocked=n==hits.Length;
        for(int i=0;i<n;i++) if(Obcy(hits[i].collider)) blocked=true;
        n=Physics.OverlapBoxNonAlloc(next+rot*obrys.center+Vector3.up*.16f,half,overlaps,rot,Mask,QueryTriggerInteraction.Ignore);
        if(n==overlaps.Length)blocked=true;
        for(int i=0;i<n;i++)if(Obcy(overlaps[i]))blocked=true;
        if(!Grunt(next,out float ground) || Mathf.Abs(ground-position.y)>.35f) blocked=true;
        if(blocked) predkosc=0;
        else {next.y=ground;cialo.MovePosition(next);cialo.MoveRotation(rot);}
        obrotKol=Mathf.Repeat(obrotKol+predkosc*dt/Mathf.Max(.1f,promienKola)*Mathf.Rad2Deg,360);
        foreach(var wheel in kola)if(wheel)wheel.localRotation=Quaternion.Euler(obrotKol,0,0);
        foreach(var pivot in skret)if(pivot)pivot.localRotation=Quaternion.Euler(0,BraxAuto.KatKola(kierownica,rozstaw,transform.InverseTransformPoint(pivot.position).x),0);
        if(audioSilnika)audioSilnika.pitch=.8f+Mathf.Abs(predkosc)/Mathf.Max(1,maxPredkosc)*.7f;
    }
    bool MiejsceWyjscia(out Vector3 point)
    {
        foreach(int side in new[]{-1,1})
        {
            var p=Drzwi(side); if(!Grunt(p,out float y))continue; p.y=y+.05f;
            float radius=gracz.body.radius, height=gracz.body.height;
            int n=Physics.OverlapCapsuleNonAlloc(p+Vector3.up*(radius+.04f),p+Vector3.up*(height-radius),radius,overlaps,Mask,QueryTriggerInteraction.Ignore);
            bool free=n<overlaps.Length;for(int i=0;i<n;i++)if(overlaps[i] && !overlaps[i].transform.IsChildOf(gracz.transform))free=false;
            if(free){point=p;return true;}
        }
        point=default; return false;
    }
    IEnumerator Wyjdz(Vector3 point)
    {
        przejscie=true; var p=gracz; var from=p.transform.position; p.transform.SetParent(null,true);
        p.clips.CrossFade("Idle",.65f);
        for(float t=0;t<1;t+=Time.deltaTime/.7f){p.transform.position=Vector3.Lerp(from,point,Mathf.SmoothStep(0,1,t));yield return null;}
        Odlacz(point);
    }
    public void PrzedTeleportem() { StopAllCoroutines(); Odlacz(startWejscia); }
    void Odlacz(Vector3 point)
    {
        if(!gracz)return;var p=gracz;gracz=null;przejscie=false;predkosc=0;wejscie=Vector2.zero;
        p.transform.SetParent(null,true);
        if(scenaGracza.IsValid()&&scenaGracza.isLoaded&&p.gameObject.scene!=scenaGracza)SceneManager.MoveGameObjectToScene(p.gameObject,scenaGracza);
        BraxPose.Get(p).Set(BraxPose.Mode.None,null);p.clips.Play("Idle");p.transform.position=point;p.autoGracza=null;p.body.enabled=true;
        if(audioSilnika)audioSilnika.Stop();
    }
}
