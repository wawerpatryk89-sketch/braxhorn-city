using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class BraxCitySystemsValidation
{
    public static void Validate(BraxCityWorld world,BraxMissionReservations catalogue,BraxCityTrafficController traffic,Transform root)
    {
        int signalChecks=BraxTrafficCycleChecks.Run();
        if(catalogue.reservations.Length!=11 || catalogue.reservations.Count(p=>p.type=="MAIN")!=5 || catalogue.reservations.Count(p=>p.type=="SIDE")!=6)
            throw new InvalidOperationException("The city requires five main and six side reservations");
        if(catalogue.reservations.Select(p=>p.id).Distinct().Count()!=11 || catalogue.reservations.Select(p=>p.anchorId).Distinct().Count()!=11)
            throw new InvalidOperationException("Reservations must represent eleven separate anchors");
        var points=new HashSet<string>(world.points.Select(p=>p.id));
        foreach(var reserve in catalogue.reservations)
        {
            if(!points.Contains(reserve.anchorId))throw new InvalidOperationException("Missing reserve anchor "+reserve.id);
            foreach(string id in (reserve.accessPointIds??Array.Empty<string>()).Concat(reserve.exitPointIds??Array.Empty<string>()).Concat(reserve.relatedPointIds??Array.Empty<string>()))
                if(!points.Contains(id))throw new InvalidOperationException("Missing linked point "+id+" for "+reserve.id);
        }
        foreach(var group in new[]{traffic.vehicleRed,traffic.vehicleAmber,traffic.vehicleGreen,traffic.pedestrianRed,traffic.pedestrianGreen})
        {
            if(group.Length!=2)throw new InvalidOperationException("Both sides of every crossing signal are required");
            foreach(var lamp in group)if(lamp==null||!lamp.surface||!lamp.illuminated||!lamp.extinguished||lamp.illuminated==lamp.extinguished)
                throw new InvalidOperationException("Signal lamp wiring is incomplete");
        }
        if(root.GetComponentsInChildren<Light>(true).Length!=0)throw new InvalidOperationException("City access systems must not add dynamic lights");
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if(!filter.sharedMesh||filter.sharedMesh.vertexCount<3)throw new InvalidOperationException("Empty generated detail mesh "+filter.name);
            foreach(var v in filter.sharedMesh.vertices)
                if(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.x)||float.IsInfinity(v.y)||float.IsInfinity(v.z))
                    throw new InvalidOperationException("Non finite detail geometry "+filter.name);
        }
        Physics.SyncTransforms();
        // These raycasts specifically catch an uncut original sidewalk/virtual
        // floor covering the newly built stairwell. They are not a playtest.
        for(int i=0;i<20;i++)
        {
            float z=35f+(i+.5f)*.44f, expected=.15f-(i+1)*.17f;
            if(!Physics.Raycast(new Vector3(78.9f,2.5f,z),Vector3.down,out var hit,8f,~(1<<8),QueryTriggerInteraction.Ignore) || Mathf.Abs(hit.point.y-expected)>.025f)
                throw new InvalidOperationException("Metro stair "+i+" obstructed or missing: old pavement/virtual floor must be cut");
        }
        var vestibule=new Vector3(78f,BraxCitySystemsBuild.MetroFloorY+.08f,46.5f);
        if(Physics.Raycast(vestibule,Vector3.up,out var ceiling,2.9f,~(1<<8),QueryTriggerInteraction.Ignore))
            throw new InvalidOperationException("Metro vestibule headroom blocked by "+ceiling.collider.name);
        Debug.Log("BRAX_CITY_SYSTEMS_SOURCE_CHECKS signalRules="+signalChecks+" reserves=11 stairRays=20; not a physical phone or visual acceptance");
    }
}
