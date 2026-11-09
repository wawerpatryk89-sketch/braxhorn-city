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
        // 9.11: metra w mieście nie ma (decyzja właściciela) — bez sond schodów i bez kontroli stacji.
        Debug.Log("BRAX_CITY_SYSTEMS_SOURCE_CHECKS signalRules="+signalChecks+
            " reserves=11 metro=brak; nie jest to test Androida");
    }
}
