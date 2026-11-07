using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// T1 kurs po istniejącym dwutorowym odcinku C1-C2; tor JEST prosty,
// zatem CAF rewersuje na krańcówkach (bez skrętu całego 44-metrowego składu
// w poprzek jezdni). Oryginalny model, nie generujemy drugiego wagonu.
public sealed class BraxTramT1 : MonoBehaviour
{
    public float zachodX, wschodX, torZ, peronX0, peronX1, peronZ;
    public float predkosc = 6.2f, przyspieszenie = 1.35f;
    public float postojPeron = 12f, postojZachod = 6f;

    struct Door { public Transform t; public Vector3 home; public Vector3 delta; public Collider[] colliders; }
    readonly List<Door> doors = new List<Door>();
    float v, celX, postojDo, doorState; bool atStop, east=true, panelChecked;
    float floorY;

    void Start()
    {
        if (wschodX - zachodX < 3f)
        { Debug.LogWarning("BRAXHORN_TRAM_T1 DO POPRAWKI: za krotki tor dla CAF"); enabled=false; return; }
        floorY=transform.position.y;
        var p=transform.position; p.x=Mathf.Clamp(p.x,zachodX,wschodX); p.z=torZ; transform.position=p;
        east=Mathf.Abs(p.x-wschodX) <= Mathf.Abs(p.x-zachodX);
        celX=east?wschodX:zachodX;
        if (Mathf.Abs(transform.position.x-celX)>1f) celX=wschodX;
        IdentifyDoors();
        atStop=Mathf.Abs(transform.position.x-wschodX)<.30f;
        if(atStop)postojDo=Time.time+postojPeron;
        Debug.Log("BRAXHORN_TRAM_T1 szlak_x="+zachodX.ToString("F1")+".."+wschodX.ToString("F1")+
            " peron_z="+peronZ.ToString("F1")+" znalezione_skrzydla="+doors.Count);
    }

    void IdentifyDoors()
    {
        doors.Clear();
        var all=GetComponentsInChildren<Renderer>(true);
        var modelBounds=new Bounds();
        bool first=true;
        foreach(var r in all){if(!r)continue;if(first){modelBounds=r.bounds;first=false;}else modelBounds.Encapsulate(r.bounds);}
        if(first)return;
        foreach(var r in all)
        {
            if(!r || !r.GetComponent<MeshFilter>())continue;
            var name=r.name.ToLowerInvariant();
            bool labelled=name.Contains("drzwi") || name.Contains("door") || name.Contains("porta");
            var b=r.bounds;
            if(!labelled || b.size.y<1f || b.size.y>3.9f ||
               b.size.x>3f || b.size.z>1.25f) continue;
            // Na peronie południowym drzwi mają otwierać się w stronę -Z.
            if(b.center.z > modelBounds.center.z-.25f)continue;
            if(r.transform==transform)continue;
            doors.Add(new Door{t=r.transform,home=r.transform.localPosition,
                delta=Vector3.zero,colliders=r.GetComponents<Collider>()});
        }
        // Każdy otwór ma własną PARĘ skrzydeł. Nie wolno przesuwać
        // wszystkich drzwi w stronę głowicy wagonu — to właśnie wyglądało
        // jak otwieranie w nieodpowiednią stronę.
        doors.Sort((a,b)=>a.t.position.x.CompareTo(b.t.position.x));
        bool valid = doors.Count >= 2 && doors.Count <= 24 && doors.Count%2==0;
        if(valid)
        {
            for(int i=0;i<doors.Count;i+=2)
            {
                var a=doors[i]; var b=doors[i+1];
                float distance=Mathf.Abs(a.t.position.x-b.t.position.x);
                if(distance>.85f*1.294f+1.5f) {valid=false;break;}
                a.delta=a.t.parent.InverseTransformVector(Vector3.left*.52f);
                b.delta=b.t.parent.InverseTransformVector(Vector3.right*.52f);
                doors[i]=a;doors[i+1]=b;
            }
        }
        if(!valid)
        {
            doors.Clear();
            Debug.LogWarning("BRAXHORN_TRAM_T1 DO POPRAWKI: model CAF nie ma rozpoznanych PAR drzwi od strony peronu. " +
                "Nie przesuwam losowych brył: potrzebny krótki audyt siatek w Unity.");
        }
    }

    void Update()
    {
        if (!enabled)return;
        float dt=Mathf.Min(Time.deltaTime,.1f);
        if(atStop)
        {
            // Otwieranie tylko podczas postoju PRZY PERONIE;
            // po przeciwnej stronie brak peronu — drzwi pozostają zamknięte.
            bool beside = Mathf.Abs(transform.position.x-wschodX)<.30f;
            float open = beside && Time.time<postojDo-1.6f ? 1f:0f;
            UpdateDoors(open,dt);
            if(Time.time<postojDo || doorState>.015f)return;
            atStop=false; east=!east;
            celX=east?wschodX:zachodX;
        }
        UpdateDoors(0f,dt);
        if(doorState>.015f)return;
        float remain=Mathf.Abs(transform.position.x-celX);
        if(remain<=.04f)
        {
            var dest=transform.position;dest.x=celX;dest.z=torZ;dest.y=floorY;
            transform.position=dest;
            v=0f;atStop=true;
            postojDo=Time.time+(east?postojPeron:postojZachod);
            Debug.Log("BRAXHORN_TRAM_T1 stop="+(east?"PERON":"ZACHOD")+
                " x="+transform.position.x.ToString("F2"));
            return;
        }
        float limited=Mathf.Min(predkosc,Mathf.Sqrt(Mathf.Max(0f,2f*przyspieszenie*remain)));
        v=Mathf.MoveTowards(v,limited,przyspieszenie*dt);
        var p=transform.position;p.x=Mathf.MoveTowards(p.x,celX,v*dt);p.y=floorY;p.z=torZ;
        transform.position=p;
        // Nie obracaj sztywnego tramwaju na zakręcie, którego nie ma
        // w torowisku; jazda rewersyjna przy obu czołach.
    }

    void UpdateDoors(float target,float dt)
    {
        doorState=Mathf.MoveTowards(doorState,target,dt/.95f);
        // Całobryłowy collider nie może blokować wejścia, gdy rzeczywiste
        // skrzydła są otwarte. Bez rozpoznanych drzwi pozostaje włączony.
        var hull=GetComponent<BoxCollider>();
        if(hull) hull.enabled=doors.Count==0 || doorState<.85f;
        float sm=doorState*doorState*(3f-2f*doorState);
        foreach(var d in doors)
        {
            if(!d.t)continue;
            d.t.localPosition=d.home+d.delta*sm;
            foreach(var c in d.colliders)if(c)c.enabled=doorState<.05f;
        }
    }
}
