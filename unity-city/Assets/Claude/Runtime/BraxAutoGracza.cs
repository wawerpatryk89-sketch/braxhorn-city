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
    public Vector3 przesuniecieWejscia;
    public Quaternion obrotWejscia = Quaternion.identity;
    public BraxRekaKierowcy lewaReka = new BraxRekaKierowcy(), prawaReka = new BraxRekaKierowcy();
    public bool pozaZweryfikowana;
    // Build 45 (E9): drzwi kierowcy na zawiasie (BraxClaudeAutoPatryka) — otwierają się przy wsiadaniu/wysiadaniu, zamykają za graczem.
    public Transform drzwi; public Vector3 osDrzwi = Vector3.up; public float katDrzwi = 65f;
    // Build 45 (E6/E11): poza Patryka za kierownicą — odchylenie tułowia i pochylenie głowy dobrane w buildzie (zapas nad głową ≥ 0).
    public Transform kregoslupGracza, glowaGracza; public float odchylenie, pochylenieGlowy;
    float wagaPozy; Quaternion drzwiZamk; bool drzwiZap; Quaternion poKorekcie; bool maKorekte;
    IEnumerator Drzwi(float od, float doK, float czas)
    {
        if (!drzwi) yield break;
        if (!drzwiZap) { drzwiZamk = drzwi.localRotation; drzwiZap = true; }
        for (float t = 0; t < 1f; t += Time.deltaTime / czas)
        { float u = Mathf.SmoothStep(od, doK, t); drzwi.localRotation = drzwiZamk * Quaternion.AngleAxis(katDrzwi * u, osDrzwi); yield return null; }
        drzwi.localRotation = drzwiZamk * Quaternion.AngleAxis(katDrzwi * doK, osDrzwi);
    }
    void LateUpdate()
    {
        if (!gracz || wagaPozy <= 0f) return;
        if (kregoslupGracza && !(maKorekte && Quaternion.Dot(kregoslupGracza.localRotation, poKorekcie) > .9999999f))
        {
            BraxMieszkaniec.PozaWAucie(kregoslupGracza, glowaGracza, transform.right, odchylenie * wagaPozy, pochylenieGlowy * wagaPozy);
            poKorekcie = kregoslupGracza.localRotation; maKorekte = true;
        }
        lewaReka.Ustaw(transform, wagaPozy);
        prawaReka.Ustaw(transform, wagaPozy);
    }
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
    AudioSource audioSilnika, audioEfekty;
    // runda 4 (dźwięki pojazdów): 0 zgaszony, 1 odpalanie, 2 pracuje, 3 gaszenie — ton i głośność od prędkości tylko w stanie 2
    int silnikStan; float silnikGlos;
    Rigidbody cialo;
    readonly RaycastHit[] hits = new RaycastHit[32];
    readonly Collider[] overlaps = new Collider[32];
    const int Mask = ~(1<<29);

    void OnEnable() { Wszystkie.Add(this); BraxInterakcje.Dodaj(this); }
    void OnDisable() { Wszystkie.Remove(this); BraxInterakcje.Usun(this); if (gracz && !BraxSceneTransit.Busy) Odlacz(startWejscia); }
    void Start()
    {
        if (!obrys) obrys = GetComponent<BoxCollider>();
        cialo=GetComponent<Rigidbody>();
        if (silnik) { audioSilnika=gameObject.AddComponent<AudioSource>(); audioSilnika.clip=silnik; audioSilnika.loop=true; audioSilnika.spatialBlend=1; audioSilnika.minDistance=4; audioSilnika.maxDistance=45; audioSilnika.playOnAwake=false;
            audioSilnika.rolloffMode=AudioRolloffMode.Custom; audioSilnika.SetCustomCurve(AudioSourceCurveType.CustomRolloff,BraxDzwiekiAuta.KrzywaZaniku()); }
        audioEfekty=gameObject.AddComponent<AudioSource>(); audioEfekty.spatialBlend=1; audioEfekty.minDistance=2; audioEfekty.maxDistance=30; audioEfekty.playOnAwake=false;
        audioEfekty.rolloffMode=AudioRolloffMode.Custom; audioEfekty.SetCustomCurve(AudioSourceCurveType.CustomRolloff,BraxDzwiekiAuta.KrzywaZaniku());
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
        if (!player || !player.clips || !player.clips.GetClip("CarIdle") || !player.clips.GetClip("EnteringCar") ||
            !lewaReka.Gotowa || !prawaReka.Gotowa || player.autoGracza || player.vessel || player.seat || !fotel || !obrys || (Drzwi(-1)-p).sqrMagnitude>6.25f) return false;
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
        p.clips.CrossFade("Idle",.2f);
        Efekt(BraxDzwiekiAuta.DrzwiOtwKlip,.7f);                                 // runda 4: klik zamka
        yield return Drzwi(0f,1f,.55f);                                         // E9: drzwi otwierają się przed wsiadaniem
        // The owner's full entry clip, timed to its actual length. Keep the door
        // open until the clip and the seated-pose blend have both finished.
        var entry = p.clips["EnteringCar"];
        entry.wrapMode = WrapMode.ClampForever; entry.speed = 1f; entry.time = 0f;
        p.clips.CrossFade("EnteringCar", .15f);
        var entryRot = transform.rotation * obrotWejscia;
        var entryEnd = transform.TransformPoint(przesuniecieWejscia);
        var doorRot = p.transform.rotation;
        Vector3 end=transform.TransformPoint(przesuniecieGracza);
        for(float t=0;t<1f;t+=Time.deltaTime/Mathf.Max(.1f,entry.length))
        {
            if(!p)yield break;
            float u=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.96f,t));
            p.transform.SetPositionAndRotation(Vector3.Lerp(door,entryEnd,u),Quaternion.Slerp(doorRot,entryRot,Mathf.Clamp01(t*5f)));
            yield return null;
        }
        p.clips.CrossFade("CarIdle",.3f);
        for(float t=0;t<1f;t+=Time.deltaTime/.3f)
        {
            float u=Mathf.SmoothStep(0,1,t); wagaPozy=u;
            p.transform.SetPositionAndRotation(Vector3.Lerp(entryEnd,end,u),Quaternion.Slerp(entryRot,transform.rotation,u));
            yield return null;
        }
        wagaPozy=1f;
        p.transform.SetParent(transform,true); p.transform.SetPositionAndRotation(end,transform.rotation);
        yield return Drzwi(1f,0f,.5f);                                          // i zamykają się za graczem
        Efekt(BraxDzwiekiAuta.DrzwiTrzaskKlip,.9f);                              // runda 4: trzask drzwi → odpalanie → jałowy
        yield return Odpal();
        przejscie=false;
    }
    const float Skora=.04f;
    // Maks. skręt kół (°): 38 do 2 m/s, liniowo do 12 przy prędkości maksymalnej (stabilnie na prostej, ciasno na parkingu).
    public static float MaksSkret(float v, float vmax) => Mathf.Lerp(38f, 12f, Mathf.Clamp01((Mathf.Abs(v) - 2f) / Mathf.Max(1f, vmax - 2f)));
    // Poślizg po przeszkodzie: ruch rzutowany na płaszczyznę styku (bez składowej pionowej), utrata prędkości 15–85 %
    // zależnie od kąta (czołowo — prawie stop, po styczności — mało). Czysta funkcja — test BraxTestNaprawOsiedla.
    public static Vector3 Slizg(Vector3 ruch, Vector3 normalna, out float utrata)
    {
        normalna.y = 0f;
        if (normalna.sqrMagnitude < 1e-6f || ruch.sqrMagnitude < 1e-10f) { utrata = ruch.sqrMagnitude < 1e-10f ? 0f : .85f; return Vector3.zero; }
        normalna.Normalize();
        float czolowo = Mathf.Clamp01(-Vector3.Dot(ruch.normalized, normalna));
        utrata = Mathf.Lerp(.15f, .85f, czolowo);
        var w = Vector3.ProjectOnPlane(ruch, normalna); w.y = 0f;
        return w * (1f - utrata);
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
        // build 45 (właściciel: „zakręty ciężko się wchodzi”): skręt kół do 38° przy małej prędkości, malejący z prędkością
        kierownica=Mathf.MoveTowards(kierownica,wejscie.x*MaksSkret(predkosc,maxPredkosc),dt*95f);
        float yaw=predkosc/Mathf.Max(1,rozstaw)*Mathf.Tan(kierownica*Mathf.Deg2Rad)*Mathf.Rad2Deg*dt;
        var rot=rotation*Quaternion.Euler(0,yaw,0);
        var scale=transform.lossyScale;
        var half=Vector3.Scale(obrys.size,new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z)))*.5f; half.y=Mathf.Max(.2f,half.y-.16f);
        var centerOffset=Vector3.Scale(obrys.center,scale);
        // Check the swept rotation before translating: a free end pose alone
        // can miss a thin bollard crossed by a corner of the body.
        bool Free(Vector3 at, Quaternion facing)
        {
            int count=Physics.OverlapBoxNonAlloc(at+facing*centerOffset+Vector3.up*.16f,half,overlaps,facing,Mask,QueryTriggerInteraction.Ignore);
            if(count==overlaps.Length)return false;
            for(int i=0;i<count;i++)if(Obcy(overlaps[i]))return false;
            return true;
        }
        var rotNext=rot;
        int turns=Mathf.Max(1,Mathf.CeilToInt(Mathf.Abs(yaw)/1f));
        for(int i=1;i<=turns;i++)if(!Free(position,Quaternion.Slerp(rotation,rot,(float)i/turns))) { rotNext=rotation;break; }
        Vector3 next=position, remaining=(rotNext*Vector3.forward)*predkosc*dt;
        float utrataSuma=0f;
        for(int attempt=0;attempt<3 && remaining.sqrMagnitude>1e-8f;attempt++)
        {
            var center=next+rotNext*centerOffset+Vector3.up*.16f;
            int n=Physics.BoxCastNonAlloc(center,half,remaining.normalized,hits,rotNext,remaining.magnitude+Skora,Mask,QueryTriggerInteraction.Ignore);
            float distance=float.PositiveInfinity;Vector3 normal=Vector3.zero;
            for(int i=0;i<n;i++)if(Obcy(hits[i].collider) && hits[i].distance<distance){distance=hits[i].distance;normal=hits[i].normal;}
            if(n==hits.Length){distance=0;normal=-remaining.normalized;}
            if(float.IsPositiveInfinity(distance)){next+=remaining;remaining=Vector3.zero;break;}
            var travelled=remaining.normalized*Mathf.Clamp(distance-Skora,0,remaining.magnitude);
            next+=travelled;
            remaining=Slizg(remaining-travelled,normal,out float loss);
            utrataSuma=Mathf.Max(utrataSuma,loss);
        }
        predkosc*=1f-utrataSuma;
        bool blocked=!Free(next,rotNext);
        if(!Grunt(next,out float ground) || Mathf.Abs(ground-position.y)>.35f) blocked=true;
        if(blocked) predkosc*=.5f;                                                          // zaklinowane: hamuje, nie staje w miejscu od razu
        else {next.y=ground;cialo.MovePosition(next);cialo.MoveRotation(rotNext);}
        obrotKol=Mathf.Repeat(obrotKol+predkosc*dt/Mathf.Max(.1f,promienKola)*Mathf.Rad2Deg,360);
        foreach(var wheel in kola)if(wheel)wheel.localRotation=Quaternion.Euler(obrotKol,0,0);
        foreach(var pivot in skret)if(pivot)pivot.localRotation=Quaternion.Euler(0,BraxAuto.KatKola(kierownica,rozstaw,transform.InverseTransformPoint(pivot.position).x*Mathf.Abs(transform.lossyScale.x)),0);
        if(audioSilnika&&silnikStan==2)   // runda 4: ton i głośność od prędkości (przy przyspieszaniu ciut wyżej)
        {
            float u=Mathf.Clamp01(Mathf.Abs(predkosc)/Mathf.Max(1,maxPredkosc)), gaz=Mathf.Abs(wejscie.y)>.1f?.06f:0f;
            audioSilnika.pitch=Mathf.Lerp(audioSilnika.pitch,.8f+u*.75f+gaz,1f-Mathf.Exp(-dt*4f));
            audioSilnika.volume=Mathf.Lerp(audioSilnika.volume,silnikGlos+u*.3f+gaz,1f-Mathf.Exp(-dt*3f));
        }
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
        przejscie=true; var p=gracz; var from=p.transform.position;
        yield return Zgas();                                                    // runda 4: zgaszenie (opadający ton), potem drzwi
        Efekt(BraxDzwiekiAuta.DrzwiOtwKlip,.7f);
        yield return Drzwi(0f,1f,.5f);                                          // E9: drzwi otwierają się przed wyjściem
        p.transform.SetParent(null,true);
        p.clips.CrossFade("Idle",.65f);
        for(float t=0;t<1;t+=Time.deltaTime/.7f){float u=Mathf.SmoothStep(0,1,t);wagaPozy=1f-u;p.transform.position=Vector3.Lerp(from,point,u);yield return null;}
        wagaPozy=0f;
        Odlacz(point);
        yield return Drzwi(1f,0f,.5f);                                          // i zamykają się za graczem
        Efekt(BraxDzwiekiAuta.DrzwiTrzaskKlip,.9f);
    }
    public void PrzedTeleportem() { StopAllCoroutines(); if (drzwi && drzwiZap) drzwi.localRotation = drzwiZamk; Odlacz(startWejscia); }
    void Odlacz(Vector3 point)
    {
        if(!gracz)return;var p=gracz;gracz=null;przejscie=false;predkosc=0;wejscie=Vector2.zero;wagaPozy=0f;maKorekte=false;
        p.transform.SetParent(null,true);
        if(scenaGracza.IsValid()&&scenaGracza.isLoaded&&p.gameObject.scene!=scenaGracza)SceneManager.MoveGameObjectToScene(p.gameObject,scenaGracza);
        BraxPose.Get(p).Set(BraxPose.Mode.None,null);p.clips.Play("Idle");p.transform.position=point;p.autoGracza=null;p.body.enabled=true;
        if(audioSilnika)audioSilnika.Stop(); silnikStan=0;
    }
    // ---------------- runda 4: dźwięki auta gracza (synteza BraxDzwiekiAuta) ----------------
    void Efekt(AudioClip c, float g) { if (audioEfekty && c) audioEfekty.PlayOneShot(c, g); }
    IEnumerator Odpal()
    {
        if (!audioSilnika) yield break;
        silnikStan = 1; silnikGlos = .42f;
        int w = Random.Range(0, 5); Efekt(BraxDzwiekiAuta.RozrusznikKlip(w), .85f);
        yield return new WaitForSeconds(BraxDzwiekiAuta.CzasRozrusznika(w) + .22f);   // rozrusznik, zapłon „łapie”
        audioSilnika.volume = 0f; audioSilnika.pitch = 1.05f; audioSilnika.Play();         // pętla jałowego z wyskoku obrotów
        for (float t = 0; t < 1f; t += Time.deltaTime / .6f)
        { audioSilnika.volume = Mathf.Lerp(0f, silnikGlos, Mathf.Clamp01(t * 2f)); audioSilnika.pitch = Mathf.Lerp(1.05f, .8f, t); yield return null; }
        audioSilnika.volume = silnikGlos; audioSilnika.pitch = .8f; silnikStan = 2;
    }
    IEnumerator Zgas()
    {
        if (!audioSilnika || silnikStan == 0) yield break;
        silnikStan = 3; Efekt(BraxDzwiekiAuta.ZgaszenieKlip, .7f);
        float v0 = audioSilnika.volume, p0 = audioSilnika.pitch;
        for (float t = 0; t < 1f; t += Time.deltaTime / .55f)
        { audioSilnika.volume = Mathf.Lerp(v0, 0f, t); audioSilnika.pitch = Mathf.Lerp(p0, .45f, t); yield return null; }
        audioSilnika.Stop(); silnikStan = 0;
        yield return new WaitForSeconds(.45f);                                   // dogasa (klip zgaszenia ~1 s)
    }
}
