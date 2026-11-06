using System.Collections;
using UnityEngine;

// A resident walks to a parked car, sits, leaves and walks back. The seat is reserved
// for this resident; the private player car is never assigned to this component.
public sealed class BraxPodroznyAuta : MonoBehaviour
{
    public BraxMieszkaniec osoba;
    public Transform fotel;
    public BoxCollider samochod;
    public Vector3 dom, drzwi, siedzenieModel;
    public string chod, spokoj, siedzi;
    // Build 45: poza w kabinie dobrana w buildzie (BraxClaudeKabiny): odchylenie tułowia, pochylenie głowy — skala 1.0.
    public Transform kregoslupDol;
    public float odchylenie, pochylenieGlowy;
    Quaternion poKorekcie; bool maKorekte;
    void LateUpdate()
    {
        if (!osoba || !osoba.wAucie || !kregoslupDol || (odchylenie == 0f && pochylenieGlowy == 0f) || !osoba.anim || !osoba.anim.enabled) return;
        if (maKorekte && Quaternion.Dot(kregoslupDol.localRotation, poKorekcie) > .9999999f) return;   // animacja nie nadpisała (culling)
        BraxMieszkaniec.PozaWAucie(kregoslupDol, osoba.glowa, transform.right, odchylenie, pochylenieGlowy);
        poKorekcie = kregoslupDol.localRotation; maKorekte = true;
    }
    Vector3 modelStoi;
    Quaternion rotStoi;
    CapsuleCollider kapsula;
    readonly Collider[] kontakty=new Collider[24];
    const int Mask=~(1<<29);
    bool Wolne(Vector3 p)
    {
        int n=Physics.OverlapCapsuleNonAlloc(p+Vector3.up*.35f,p+Vector3.up*(osoba.wzrost-.3f),.28f,kontakty,Mask,QueryTriggerInteraction.Ignore);
        if(n==kontakty.Length)return false;
        for(int i=0;i<n;i++)if(kontakty[i]&&!kontakty[i].transform.IsChildOf(transform))return false;
        return true;
    }
    bool doszedl;
    IEnumerator Idz(Vector3 cel)
    {
        doszedl = false;
        osoba.enabled = false;
        osoba.anim.enabled = true;
        osoba.anim.CrossFade(chod, .2f);
        float bezPostepu = 0f;
        while ((transform.position - cel).sqrMagnitude > .01f)
        {
            Vector3 d = cel - transform.position; d.y = 0f;
            var next = Vector3.MoveTowards(transform.position, cel, Time.deltaTime * 1.2f);
            if (Wolne(next))
            {
                bezPostepu = 0f;
                osoba.anim.CrossFade(chod, .2f);
                if (d.sqrMagnitude > .001f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 180f);
                transform.position = next;
            }
            else
            {
                osoba.anim.CrossFade(spokoj, .2f);
                bezPostepu += Time.deltaTime;
                // A blocked parking door cannot leave the NPC walking in place forever.
                // Abort the attempt; never teleport a resident through a parked car.
                if (bezPostepu >= 4f)
                {
                    Debug.LogWarning("BRAXHORN_PODROZNI: dojście zablokowane, próba wycofana: " + name);
                    yield break;
                }
            }
            yield return null;
        }
        doszedl = true;
    }
    IEnumerator Start()
    {
        if(!osoba||!fotel||!samochod||samochod.GetComponent<BraxAutoGracza>())yield break;
        kapsula=GetComponent<CapsuleCollider>();
        if (!kapsula || !osoba.anim) { Debug.LogWarning("BRAXHORN_PODROZNI: brak kapsuły lub animacji: " + name); yield break; }
        modelStoi=osoba.anim.transform.localPosition;rotStoi=transform.rotation;
        while(fotel&&samochod)
        {
            osoba.enabled=true;
            yield return new WaitForSeconds(Random.Range(18f,45f));
            // Do not start a transition while the player or another car blocks the door.
            if(!Wolne(drzwi))continue;
            yield return Idz(drzwi);
            if (!doszedl)
            {
                // A blocked door may leave a resident halfway across a parking lane.
                // Walk back safely rather than abandoning the character on the road.
                yield return Idz(dom);
                if (!doszedl)
                    Debug.LogWarning("BRAXHORN_PODROZNI: powrót do domu zablokowany: " + name);
                osoba.enabled = true;
                osoba.anim.CrossFade(spokoj, .2f);
                continue;
            }
            kapsula.enabled=false;osoba.anim.CrossFade(siedzi,.65f);
            Vector3 start=transform.position;Quaternion rotation=transform.rotation;
            for(float t=0;t<1;t+=Time.deltaTime/1.1f)
            {
                float u=Mathf.SmoothStep(0,1,t);
                transform.SetPositionAndRotation(Vector3.Lerp(start,fotel.position,u),Quaternion.Slerp(rotation,fotel.rotation,u));
                osoba.anim.transform.localPosition=Vector3.Lerp(modelStoi,siedzenieModel,u);
                yield return null;
            }
            transform.SetPositionAndRotation(fotel.position,fotel.rotation);osoba.anim.transform.localPosition=siedzenieModel;
            osoba.wAucie=true;
            yield return new WaitForSeconds(Random.Range(10f,22f));
            while(!Wolne(drzwi))yield return new WaitForSeconds(.5f);
            start=transform.position;osoba.anim.CrossFade(spokoj,.7f);
            for(float t=0;t<1;t+=Time.deltaTime/1.1f)
            {
                float u=Mathf.SmoothStep(0,1,t);transform.position=Vector3.Lerp(start,drzwi,u);
                osoba.anim.transform.localPosition=Vector3.Lerp(siedzenieModel,modelStoi,u);yield return null;
            }
            transform.position=drzwi;osoba.anim.transform.localPosition=modelStoi;kapsula.enabled=true;osoba.wAucie=false;
            yield return Idz(dom);transform.rotation=rotStoi;osoba.anim.CrossFade(spokoj,.2f);
        }
    }
}
