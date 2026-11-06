using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Measurements on the reopened, saved scene. These are geometry checks;
// neither a Play Mode test nor a claim about Android frame rate.
public static class BraxObozGroundAudit
{
    const float K=1.294f;
    [Serializable] public sealed class Approach
    {
        public string name;
        public int samples,hits;
        public float maximumHeightErrorMetres;
        public bool continuous;
    }
    [Serializable] public sealed class Mat
    {
        public string name;
        public bool supportMeasured,hasCollider,roomForFullDepression;
        public float thicknessMetres,clearanceAfterDepressionMetres;
    }
    [Serializable] public sealed class Result
    {
        public bool sceneGeometryMeasured,phoneVerified;
        public int warnings,terrainVertices,muddyVertices,grassVertices;
        public bool originalMudColor,originalMudNormal,originalMudRoughness;
        public Approach[] approaches;
        public Mat[] mats;
        public string[] findings;
    }

    static bool Surface(Scene scene,Vector3 at,float down,out RaycastHit surface,
        Func<RaycastHit,bool> accept=null)
    {
        surface=Physics.RaycastAll(at,Vector3.down,down,~0,QueryTriggerInteraction.Ignore)
            .Where(h=>h.collider && h.collider.gameObject.scene==scene && h.normal.y>.65f &&
                (accept==null||accept(h))).OrderBy(h=>h.distance).FirstOrDefault();
        return surface.collider;
    }

    public static Result Inspect(Scene scene)
    {
        Physics.SyncTransforms();
        var findings=new List<string>();
        var entries=new List<Approach>();
        foreach(var plan in BraxObozEnvironment.EntranceApproachPlan())
        {
            var entry=new Approach {name=plan.name};
            var forward=(plan.end-plan.start).normalized;
            var side=new Vector2(forward.y,-forward.x);
            // Three tracks across each approach; 11 samples on each track.
            // Origins are below door tops, furniture and roofs.
            foreach(float offset in new[]{-.35f,0f,.35f})
            for(int i=0;i<=10;i++)
            {
                float t=i/10f;
                var point=Vector2.Lerp(plan.start,plan.end,t)+side*(plan.width*offset);
                float expected=Mathf.Lerp(.016f,plan.height,t);
                var at=new Vector3(point.x,expected+.3f,point.y)*K;
                entry.samples++;
                if(!Surface(scene,at,.65f*K,out var hit))continue;
                entry.hits++;
                entry.maximumHeightErrorMetres=Mathf.Max(entry.maximumHeightErrorMetres,
                    Mathf.Abs(hit.point.y/K-expected));
            }
            entry.continuous=entry.hits==entry.samples && entry.maximumHeightErrorMetres<=.005f;
            if(!entry.continuous)findings.Add("Dojście "+entry.name+": trafienia="+
                entry.hits+"/"+entry.samples+", błąd wysokości="+entry.maximumHeightErrorMetres+" m");
            entries.Add(entry);
        }
        var mats=new List<Mat>();
        foreach(var component in scene.GetRootGameObjects()
            .SelectMany(g=>g.GetComponentsInChildren<BraxObozDeformableMat>(true)))
        {
            var renderer=component.GetComponent<Renderer>();
            var entry=new Mat {name=component.name,hasCollider=component.GetComponent<Collider>()};
            if(renderer)
            {
                var bounds=renderer.bounds;
                entry.thicknessMetres=bounds.size.y/K;
                var at=new Vector3(bounds.center.x,bounds.min.y+.005f*K,bounds.center.z);
                entry.supportMeasured=Surface(scene,at,5f*K,out var hit,
                    h=>!h.collider.transform.IsChildOf(component.transform));
                if(entry.supportMeasured)
                    entry.clearanceAfterDepressionMetres=(bounds.max.y-hit.point.y)/K-.03f;
                entry.roomForFullDepression=entry.supportMeasured && entry.hasCollider &&
                    entry.thicknessMetres>=.03f && entry.clearanceAfterDepressionMetres>=.004f;
            }
            if(!entry.roomForFullDepression)findings.Add("Mata "+entry.name+
                ": brak potwierdzenia miejsca na pełne ugięcie 3 cm, luz="+
                entry.clearanceAfterDepressionMetres+" m");
            mats.Add(entry);
        }
        var result=new Result {sceneGeometryMeasured=true,phoneVerified=false,
            approaches=entries.ToArray(),mats=mats.ToArray()};
        foreach(var filter in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true)))
        {
            var renderer=filter.GetComponent<Renderer>();
            if(!renderer || !filter.sharedMesh || !filter.sharedMesh.isReadable)continue;
            var material=renderer.sharedMaterial;
            if(!material || !material.IsKeywordEnabled("OBOZ_GROUND_BLEND"))continue;
            var colors=filter.sharedMesh.colors;
            result.terrainVertices+=colors.Length;
            result.muddyVertices+=colors.Count(c=>c.b>=.28f);
            result.grassVertices+=colors.Count(c=>c.g>=.28f);
            bool Original(string property)
            {
                var texture=material.GetTexture(property);
                return texture && UnityEditor.AssetDatabase.GetAssetPath(texture)
                    .Contains("mud_forest_2k/textures/");
            }
            result.originalMudColor|=Original("_MudColor");
            result.originalMudNormal|=Original("_MudNormal");
            result.originalMudRoughness|=Original("_MudRoughness");
        }
        if(result.muddyVertices==0 || !result.originalMudColor || !result.originalMudNormal ||
            !result.originalMudRoughness)findings.Add("Nie potwierdzono oryginalnego błota PBR i maski ruchu.");
        result.findings=findings.ToArray();result.warnings=findings.Count;
        return result;
    }
}
