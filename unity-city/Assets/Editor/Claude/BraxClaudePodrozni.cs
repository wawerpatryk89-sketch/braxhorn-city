using System;
using System.Linq;
using UnityEngine;

public static partial class BraxClaudeOsiedle
{
    static void PodrozniParkingow(System.Random rnd)
    {
        int made=0;
        foreach(var box in UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None)
            .Where(b=>b.name.StartsWith("Auto zaparkowane")))
        {
            if(made>=6)break;
            var seat=box.GetComponentsInChildren<BraxFotelAuta>().FirstOrDefault(f=>f.name==FotelKierowcy);
            if(!seat||seat.przeswit<.92f*K)continue;
            Vector3 door=box.transform.TransformPoint(new Vector3(-box.size.x*.5f-.65f,0,box.transform.InverseTransformPoint(seat.transform.position).z));
            if(!Wolne(door,.3f)||NaJezdni(door))continue;
            Vector3 home=door;bool found=false;
            foreach(var d in new[]{-box.transform.right,box.transform.forward,-box.transform.forward})
            {
                var candidate=door+d*3f;
                if(!Wolne(candidate,.3f)||PrzezJezdnie(door,candidate)||!WidacProsto(door,candidate))continue;
                if(!Physics.Raycast(candidate+Vector3.up,Vector3.down,out var floor,2,~(1<<29),QueryTriggerInteraction.Ignore))continue;
                if(Mathf.Abs(floor.point.y-door.y)>.2f)continue;
                home=new Vector3(candidate.x,floor.point.y+.03f,candidate.z);found=true;break;
            }
            if(!found)continue;
            var o=NowaOsoba(RolaKierowca,box.transform,"Mieszkaniec przy samochodzie",home,box.transform.eulerAngles.y,rnd);
            if(o==null)continue;
            var idle=Klip(o,"idle_neutral_01");var walk=Klip(o,"walk_neutral_01");var sit=Klip(o,"sit_table_breathe_01");
            if(idle==null||walk==null||sit==null)throw new InvalidOperationException("Brak klipów podróżnego samochodu");
            DodajKlipy(o,new[]{idle,walk,sit});
            var standing=o.model.transform.localPosition;
            o.root.transform.SetPositionAndRotation(seat.transform.position,seat.transform.rotation);Pozuj(o,sit,0);
            var hips=o.model.GetComponentsInChildren<Transform>().First(t=>t.name=="Bip01 Pelvis");
            o.model.transform.position+=seat.transform.position+Vector3.up*.08f*K-hips.position;
            var sitting=o.model.transform.localPosition;
            // Bake the entire seated body, not just the head joint, before admitting this resident.
            float top=0;var mesh=new Mesh();
            foreach(var r in o.model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {r.BakeMesh(mesh);foreach(var v in mesh.vertices)top=Mathf.Max(top,r.transform.TransformPoint(v).y-seat.transform.position.y);}
            UnityEngine.Object.DestroyImmediate(mesh);
            if(top>seat.przeswit-.04f*K){UnityEngine.Object.DestroyImmediate(o.root);rbLudzie--;continue;}
            o.root.transform.SetPositionAndRotation(home,box.transform.rotation);o.model.transform.localPosition=standing;Pozuj(o,idle,0);
            var m=o.root.AddComponent<BraxMieszkaniec>();m.anim=o.anim;m.klipy=new[]{idle};m.rola="przechodzień";
            m.wzrost=o.wzrost;m.glowa=o.glowa;m.szyja=o.szyja;m.kregoslup=o.kregoslup;m.ton=o.p.kobieta?1.35f:.95f;
            Zamknij(o,true);
            var travel=o.root.AddComponent<BraxPodroznyAuta>();travel.osoba=m;travel.samochod=box;travel.fotel=seat.transform;
            travel.dom=home;travel.drzwi=door;travel.siedzenieModel=sitting;travel.chod=walk;travel.spokoj=idle;travel.siedzi=sit;
            made++;Physics.SyncTransforms();
        }
        rep.notes.Add("Podróżni parkingów: "+made+"; podejście, wsiadanie, siedzenie, wysiadanie, powrót. Prywatne auto Patryka pominięte.");
        if(made==0)throw new InvalidOperationException("Nie znaleziono bezpiecznego miejsca do wsiadania NPC na parkingach");
    }
}
