using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Measurements of the authored closed leaves. No assumed export-axis sign,
// name-based wall exemption, animation, collider disabling or scene movement.
public static class BraxObozDoorGeometry
{
    const float K=1.294f;
    public sealed class Opening
    {
        public Vector3 centreAtBottom,right,normal;
        public float width,height,depth,bottomY;
        public Vector3[] vertices;
        public Collider[] movingColliders;
    }
    public sealed class Hinge
    {
        public Vector3 vertexWorld;
        public Collider support;
        public float edgeFromPivotWorld,gapWorld;
        public string status;
    }
    public static string PathOf(Transform value)
    {
        var parts=new List<string>();
        for(var t=value;t;t=t.parent)parts.Add(t.name+"["+t.GetSiblingIndex()+"]");
        parts.Reverse();return value.gameObject.scene.name+"/"+string.Join("/",parts);
    }
    static bool Vertices(Transform[] leaves,HashSet<Renderer> selected,
        out Vector3[] vertices,out string problem)
    {
        vertices=Array.Empty<Vector3>();problem="brak jawnego skrzydła lub czytelnej geometrii";
        if(leaves==null || leaves.Length==0 || leaves.Any(t=>!t))return false;
        var collected=new List<Vector3>();var filters=new HashSet<MeshFilter>();
        foreach(var leaf in leaves)
        {
            if(!leaf.gameObject.activeInHierarchy)return false;
            foreach(var renderer in leaf.GetComponentsInChildren<Renderer>(true))
            {
                if(!renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                    (selected!=null && !selected.Contains(renderer)))continue;
                var filter=renderer.GetComponent<MeshFilter>();
                if(!filter || !filter.sharedMesh || !filter.sharedMesh.isReadable)
                {problem="nieczytelna albo nieobsługiwana geometria skrzydła: "+PathOf(renderer.transform);return false;}
                if(!filters.Add(filter))continue;
                foreach(var vertex in filter.sharedMesh.vertices)
                    collected.Add(filter.transform.TransformPoint(vertex));
            }
        }
        if(collected.Count==0)return false;
        vertices=collected.ToArray();problem=null;return true;
    }
    public static bool TryOpening(BraxCityDoor door,out Opening opening,out string problem)
    {
        opening=null;problem="brak drzwi w spoczynkowej scenie edytora";
        if(!door || Application.isPlaying)return false;
        if(!Vertices(door.leaves,null,out var vertices,out problem))return false;
        var first=door.leaves[0];var right=Vector3.ProjectOnPlane(first.right,Vector3.up).normalized;
        if(right.sqrMagnitude<.99f || Mathf.Abs(Vector3.Dot(first.up,Vector3.up))<.999f)
        {problem="pochylona płaszczyzna skrzydła — brak pomiaru pionowego otworu";return false;}
        var normal=Vector3.Cross(right,Vector3.up).normalized;var origin=first.position;
        float min=vertices.Min(v=>Vector3.Dot(v-origin,right));
        float max=vertices.Max(v=>Vector3.Dot(v-origin,right));
        float bottom=vertices.Min(v=>v.y),top=vertices.Max(v=>v.y);
        float near=vertices.Min(v=>Vector3.Dot(v-origin,normal));
        float far=vertices.Max(v=>Vector3.Dot(v-origin,normal));
        var centre=origin+right*((min+max)*.5f)+normal*((near+far)*.5f);centre.y=bottom;
        var moving=door.leaves.SelectMany(t=>t.GetComponentsInChildren<Collider>(true)).Distinct().ToArray();
        if(max<=min || top<=bottom){problem="pusta rozpiętość skrzydeł";return false;}
        opening=new Opening {centreAtBottom=centre,right=right,normal=normal,
            width=max-min,height=top-bottom,depth=far-near,bottomY=bottom,
            vertices=vertices,movingColliders=moving};
        return true;
    }
    public static bool TryHinge(Transform leaf,Renderer[] visible,Transform building,
        out Hinge hinge,out string problem)
    {
        hinge=null;problem="brak budynku lub skrzydła";
        if(!leaf || !building || visible==null || leaf.gameObject.scene!=building.gameObject.scene)return false;
        if(!Vertices(new[]{leaf},new HashSet<Renderer>(visible),out var vertices,out problem))return false;
        var axis=Vector3.ProjectOnPlane(leaf.right,Vector3.up).normalized;
        if(axis.sqrMagnitude<.99f){problem="brak poziomej osi szerokości skrzydła";return false;}
        float min=vertices.Min(v=>Vector3.Dot(v-leaf.position,axis));
        float max=vertices.Max(v=>Vector3.Dot(v-leaf.position,axis));
        // The imported leaf can extend in either direction. The hinge edge
        // is the extreme closest to its actual rotation axis, never min(X)
        // solely because the recipe used a left-hand pivot before export.
        bool atMinimum=Mathf.Abs(min)<=Mathf.Abs(max);
        float edge=atMinimum?min:max;var outward=axis*(atMinimum?-1f:1f);
        if(Mathf.Abs(edge)>.03f*K)
        {problem="oś obrotu oddalona od obu skrajnych krawędzi o ponad 3 cm";return false;}
        float middle=vertices.Average(v=>v.y);
        var candidates=vertices.Where(v=>Mathf.Abs(Vector3.Dot(v-leaf.position,axis)-edge)<.001f*K)
            .OrderBy(v=>Mathf.Abs(v.y-middle)).ToArray();
        Physics.SyncTransforms();
        foreach(var vertex in candidates)
        {
            var hits=Physics.RaycastAll(vertex-outward*(.04f*K),outward,.14f*K,~0,QueryTriggerInteraction.Ignore)
                .Where(h=>h.collider && h.collider.enabled && h.collider.gameObject.activeInHierarchy &&
                    h.collider.gameObject.scene==leaf.gameObject.scene &&
                    h.collider.transform.IsChildOf(building) && !h.collider.transform.IsChildOf(leaf) &&
                    Vector3.Dot(h.normal,-outward)>.7f).OrderBy(h=>h.distance);
            foreach(var hit in hits)
            {
                if(!BraxObozSupportSurface.TryMeasure(hit.collider,vertex,out var surface))continue;
                hinge=new Hinge {vertexWorld=vertex,support=hit.collider,
                    edgeFromPivotWorld=Mathf.Abs(edge),gapWorld=surface.distanceWorld,
                    status=surface.distanceWorld<=.03f*K?"zmierzona krawędź zawiasowa przy ościeżu":
                        "zmierzona krawędź zawiasowa — odstęp od podpory ponad 3 cm"};
                return true;
            }
        }
        problem="brak poziomego trafienia z rzeczywistej krawędzi przy osi zawiasu w ościeże";
        return false;
    }
}
