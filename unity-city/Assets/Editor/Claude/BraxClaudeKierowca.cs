using System;
using System.Linq;
using UnityEngine;

public static partial class BraxClaudeOsiedle
{
    // Measure a ring in the actual imported cabin, never a guessed dashboard point.
    static bool KierownicaPunkty(GameObject auto, Transform fotel, out Vector3 lewy, out Vector3 prawy)
    {
        lewy = prawy = default;
        var a = AnalizujAuto(auto.transform); if (a == null) return false;
        var seat = auto.transform.InverseTransformPoint(fotel.position);
        float best = float.PositiveInfinity;
        bool Named(CzescAuta part)
        {
            for(var t=part.mf.transform;t && t!=auto.transform;t=t.parent)
                if(t.name.IndexOf("steer",StringComparison.OrdinalIgnoreCase)>=0 || t.name.IndexOf("volant",StringComparison.OrdinalIgnoreCase)>=0)return true;
            return false;
        }
        // Some source wheels are split into upper/lower leather, stitching and spokes.
        // Examine the complete named assembly before individual connected components.
        var namedPoints=a.cz.Where(Named).SelectMany(part=>part.w).ToArray();
        var candidates=a.skl.Select(part=>(verts:part.wierz.Select(i=>a.cz[part.cz].w[i]).ToArray(),named:Named(a.cz[part.cz]))).ToList();
        if(namedPoints.Length>0)candidates.Insert(0,(namedPoints,true));
        foreach (var candidate in candidates)
        {
            var verts=candidate.verts;
            if(verts.Length<32)continue;
            var mn=verts.Aggregate(Vector3.Min);var mx=verts.Aggregate(Vector3.Max);
            var center=(mn+mx)*.5f;var size=mx-mn;var d=center-seat;
            if (d.z < .10f*K || d.z > 1.25f*K || d.y < .20f*K || d.y > .95f*K ||
                Mathf.Abs(d.x) > .22f*K || size.x < .25f*K || size.x > .58f*K) continue;
            // The rim's vertical axis can tilt into z; derive it from its yz covariance.
            float yy=0, zz=0, yz=0;
            foreach(var v in verts) { var q=v-center; yy+=q.y*q.y; zz+=q.z*q.z; yz+=q.y*q.z; }
            float angle=.5f*Mathf.Atan2(2f*yz,yy-zz);
            var up=new Vector3(0,Mathf.Cos(angle),Mathf.Sin(angle));
            var normal=Vector3.Cross(Vector3.right,up);
            float ry=verts.Max(v=>Mathf.Abs(Vector3.Dot(v-center,up))), rx=size.x*.5f;
            float depth=verts.Max(v=>Mathf.Abs(Vector3.Dot(v-center,normal)));
            if(ry<rx*.65f || ry>rx*1.4f || depth>rx*.55f) continue;
            int mask=0, rim=0;
            foreach(var v in verts)
            {
                var q=v-center; float x=q.x/rx, y=Vector3.Dot(q,up)/ry, r=Mathf.Sqrt(x*x+y*y);
                if(r<.72f || r>1.25f)continue;
                rim++; int bin=Mathf.Clamp((int)((Mathf.Atan2(y,x)+Mathf.PI)/(2f*Mathf.PI)*8f),0,7); mask|=1<<bin;
            }
            int sectors=0; for(int i=0;i<8;i++)if((mask&(1<<i))!=0)sectors++;
            // Named assemblies include the central airbag and spokes: the original
            // 918 rim is 32% of vertices, R8 39%, Revuelto 43%. Keep full-circle
            // coverage and geometry limits, but do not reject a real multipart wheel.
            if(sectors<7 || rim<verts.Length*(candidate.named ? .25f : .4f))continue;
            float score=(candidate.named?0f:10f)+Mathf.Abs(d.x)*5f+Mathf.Abs(d.z-.65f*K)+Mathf.Abs(rx-.19f*K);
            if(ReferenceEquals(verts,namedPoints))score-=1f;
            if(score>=best)continue;
            Vector3 Grip(bool left)
            {
                var vs=verts.Where(v=>left?v.x<mn.x+.025f*K:v.x>mx.x-.025f*K).ToArray();
                return vs.Aggregate(Vector3.zero,(sum,v)=>sum+v)/vs.Length;
            }
            best=score; lewy=Grip(true); prawy=Grip(false);
        }
        return !float.IsPositiveInfinity(best);
    }

    static BraxRekaKierowcy RekaKierowcy(Transform[] bones, bool left, Transform grip)
    {
        string side=left?"Left":"Right";
        Transform Bone(string name) => bones.FirstOrDefault(t=>PatrykKosc(t.name)==side+name);
        var hand=Bone("Hand"); var middle=Bone("HandMiddle1"); var index=Bone("HandIndex1"); var pinky=Bone("HandPinky1");
        if(!hand || !middle || !index || !pinky)throw new InvalidOperationException("Patryk: brak kości dłoni "+side);
        var forward=(middle.position-hand.position).normalized;
        var normal=Vector3.Cross(forward,index.position-pinky.position).normalized*(left?1f:-1f);
        var frame=Quaternion.LookRotation(forward,normal);
        // Fingers down the rim, palm facing the dashboard. Preserve anatomical hand axes.
        var localFrame=Quaternion.Inverse(hand.rotation)*frame;
        var result=new BraxRekaKierowcy { ramie=Bone("Arm"), lokiec=Bone("ForeArm"), dlon=hand, uchwyt=grip,
            strona=left?-1f:1f, srodekDloni=hand.InverseTransformPoint(Vector3.Lerp(hand.position,middle.position,.65f)),
            orientacjaDloni=Quaternion.LookRotation(Vector3.down,Vector3.forward)*Quaternion.Inverse(localFrame) };
        if(grip)
        {
            var fingers=new System.Collections.Generic.List<Transform>();var curls=new System.Collections.Generic.List<Quaternion>();
            foreach(string finger in new[]{"Index","Middle","Ring","Pinky"})
                for(int joint=1;joint<=3;joint++)
                {
                    var bone=Bone("Hand"+finger+joint);if(!bone)continue;
                    var axis=bone.parent.InverseTransformDirection(Vector3.Cross(forward,normal).normalized);
                    fingers.Add(bone);curls.Add(Quaternion.AngleAxis(joint==1?25f:joint==2?50f:35f,axis)*bone.localRotation);
                }
            result.palce=fingers.ToArray();result.chwyty=curls.ToArray();
        }
        return result;
    }

    static bool ReceSiegaKierownicy(Transform auto,Transform[] bones,Vector3 lp,Vector3 rp)
    {
        foreach(bool left in new[]{true,false})
        {
            var hand=RekaKierowcy(bones,left,null);
            if(!hand.ramie || !hand.lokiec)return false;
            var wrist=auto.TransformPoint(left?lp:rp)-auto.rotation*hand.orientacjaDloni*Vector3.Scale(hand.srodekDloni,hand.dlon.lossyScale);
            float distance=Vector3.Distance(hand.ramie.position,wrist);
            float upper=Vector3.Distance(hand.ramie.position,hand.lokiec.position),lower=Vector3.Distance(hand.lokiec.position,hand.dlon.position);
            if(distance>upper+lower-.015f || distance<Mathf.Abs(upper-lower)+.015f)return false;
        }
        return true;
    }

    static void DopasujKierowce(BraxAutoGracza car, BraxGymPlayer player, Animation anim, Transform hips, Transform[] bones)
    {
        if(!anim.GetClip("EnteringCar"))throw new InvalidOperationException("Patryk: brak wymaganego EnteringCar");
        if(!KierownicaPunkty(car.gameObject,car.fotel,out var lp,out var rp))
            throw new InvalidOperationException("Auto Patryka: nie odnaleziono obręczy kierownicy w geometrii modelu");
        var oldPos=player.transform.position;var oldRot=player.transform.rotation;
        try
        {
            player.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            anim.GetClip("CarIdle").SampleAnimation(anim.gameObject,0);
            var seatedHips=hips.position; var seatedForward=Vector3.ProjectOnPlane(hips.forward,Vector3.up);
            Transform Grip(string name,Vector3 p) { var t=new GameObject(name).transform;t.SetParent(car.transform,false);t.localPosition=p;return t; }
            car.lewaReka=RekaKierowcy(bones,true,Grip("kierownica: chwyt lewy",lp));
            car.prawaReka=RekaKierowcy(bones,false,Grip("kierownica: chwyt prawy",rp));
            var entry=anim.GetClip("EnteringCar");entry.SampleAnimation(anim.gameObject,entry.length);
            car.obrotWejscia=Quaternion.AngleAxis(Vector3.SignedAngle(Vector3.ProjectOnPlane(hips.forward,Vector3.up),seatedForward,Vector3.up),Vector3.up);
            car.przesuniecieWejscia=car.przesuniecieGracza+seatedHips-car.obrotWejscia*hips.position;
            player.transform.SetPositionAndRotation(car.transform.TransformPoint(car.przesuniecieGracza),car.transform.rotation);
            anim.GetClip("CarIdle").SampleAnimation(anim.gameObject,0);
            BraxMieszkaniec.PozaWAucie(car.kregoslupGracza,car.glowaGracza,car.transform.right,car.odchylenie,car.pochylenieGlowy);
            bool left=car.lewaReka.Ustaw(car.transform,1f),right=car.prawaReka.Ustaw(car.transform,1f);
            float Error(BraxRekaKierowcy r)=>Vector3.Distance(r.dlon.TransformPoint(r.srodekDloni),r.uchwyt.position);
            float error=Mathf.Max(Error(car.lewaReka),Error(car.prawaReka));
            if(!left || !right || error>.025f)throw new InvalidOperationException("Patryk: kierownica poza zasięgiem dłoni, błąd="+error.ToString("F3"));
            car.pozaZweryfikowana=true;
            Debug.Log("BRAXHORN_KIEROWCA_OK clip=EnteringCar czas="+entry.length.ToString("F2")+" chwyty=geometria_kierownicy blad="+error.ToString("F4")+" skala_postaci=bez_zmian");
        }
        finally { anim.GetClip("Idle").SampleAnimation(anim.gameObject,0);player.transform.SetPositionAndRotation(oldPos,oldRot); }
    }
}
