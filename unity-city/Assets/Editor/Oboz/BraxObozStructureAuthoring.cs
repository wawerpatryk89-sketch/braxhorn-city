using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BraxObozStructureAuthoring
{
    public static void RegisterGround(MeshCollider terrainCollider)
    {
        if (!terrainCollider) return;
        var record = terrainCollider.GetComponent<BraxObozStructureGround>();
        if (!record) record = terrainCollider.gameObject.AddComponent<BraxObozStructureGround>();
        record.surface = terrainCollider;
        record.geometry = terrainCollider.GetComponent<MeshFilter>();
        record.visibleSurface = terrainCollider.GetComponent<Renderer>();
        EditorUtility.SetDirty(record);
    }

    // Call with the concrete collection BEFORE adding furniture or effects.
    // Descendant lookup is never used later to infer ownership in the audit.
    public static BraxObozStructureRecord Capture(Transform owner,
        MeshFilter[] concreteParts, Collider[] ownColliders)
    {
        if (!owner) throw new ArgumentNullException(nameof(owner));
        var filters = (concreteParts ?? Array.Empty<MeshFilter>())
            .Where(f => f && f.sharedMesh && f.GetComponent<Renderer>()).Distinct().ToArray();
        var selected = new HashSet<MeshFilter>(filters);
        var previous = owner.GetComponentsInChildren<BraxObozStructureRecord>(true);
        var previousParts = previous.SelectMany(r => r.parts ?? Array.Empty<BraxObozStructureRecord.Part>())
            .Where(p => p != null && p.filter && selected.Contains(p.filter)).ToArray();
        var parts = new List<BraxObozStructureRecord.Part>();
        foreach (var filter in filters)
        {
            var existing = previousParts.Where(p => p.filter == filter).ToArray();
            if (existing.Length > 0)
            {
                foreach (var p in existing)
                    parts.Add(new BraxObozStructureRecord.Part {
                        label=p.label, filter=filter, firstVertex=p.firstVertex,
                        vertexCount=p.vertexCount, firstIndex=p.firstIndex,
                        indexCount=p.indexCount, rangesVerified=p.rangesVerified
                    });
                continue;
            }
            try
            {
                var mesh=filter.sharedMesh;
                bool triangles=true;
                for(int i=0;i<mesh.subMeshCount;i++)
                    triangles &= mesh.GetTopology(i)==MeshTopology.Triangles;
                parts.Add(new BraxObozStructureRecord.Part {
                    label=filter.name, filter=filter, vertexCount=mesh.vertexCount,
                    indexCount=triangles?mesh.triangles.Length:0, rangesVerified=triangles
                });
            }
            catch(Exception ex)
            {
                parts.Add(new BraxObozStructureRecord.Part {label=filter.name,filter=filter});
                BraxBuildQuality.Warn("OBÓZ CONSTRUCTION_CAPTURE_UNMEASURED "+filter.name+": "+ex);
            }
        }
        var result=owner.GetComponent<BraxObozStructureRecord>();
        if(!result)result=owner.gameObject.AddComponent<BraxObozStructureRecord>();
        // Capture all physics belonging to these parts while still in the
        // authoring scope; no future hierarchy expansion is allowed.
        result.ownColliders=(ownColliders ?? Array.Empty<Collider>())
            .Concat(previous.SelectMany(r=>r.ownColliders ?? Array.Empty<Collider>()))
            .Where(c=>c).Distinct().ToArray();
        result.parts=parts.ToArray();
        result.members=filters.Select(f=>f.GetComponent<Renderer>()).Distinct().ToArray();
        // Nested batches are subsumed only for the explicitly selected mesh.
        // Unselected members remain in their earlier independent record.
        foreach(var old in previous)
        {
            if(old==result)continue;
            old.parts=(old.parts ?? Array.Empty<BraxObozStructureRecord.Part>())
                .Where(p=>p!=null && (!p.filter || !selected.Contains(p.filter))).ToArray();
            old.members=old.parts.Where(p=>p.filter).Select(p=>p.filter.GetComponent<Renderer>())
                .Where(r=>r).Distinct().ToArray();
            if(old.parts.Length==0)Object.DestroyImmediate(old);
            else EditorUtility.SetDirty(old);
        }
        EditorUtility.SetDirty(result);
        return result;
    }

    // Invoke immediately after CombineMeshes, before removing the sources.
    // Exact channel/order checks stop guessed ranges from becoming evidence.
    public static void RemapCombined(BraxObozStructureRecord record,
        MeshFilter[] sources, MeshFilter destination)
    {
        if(!record || !destination || !destination.sharedMesh)return;
        var target=destination.sharedMesh;
        var targetVertices=target.vertices;var targetIndices=target.triangles;
        int vertexOffset=0,indexOffset=0;
        foreach(var source in sources)
        {
            var mesh=source.sharedMesh;
            var vertices=mesh.vertices;var indices=mesh.triangles;
            var matrix=destination.transform.worldToLocalMatrix*source.transform.localToWorldMatrix;
            bool matches=mesh.subMeshCount==1 &&
                vertexOffset+vertices.Length<=targetVertices.Length &&
                indexOffset+indices.Length<=targetIndices.Length;
            if(matches)
            {
                for(int v=0;v<vertices.Length;v++)
                    if((matrix.MultiplyPoint3x4(vertices[v])-targetVertices[vertexOffset+v]).sqrMagnitude>1e-10f)
                    {matches=false;break;}
                for(int i=0;matches && i<indices.Length;i++)
                    if(indices[i]+vertexOffset!=targetIndices[indexOffset+i])matches=false;
            }
            foreach(var part in record.parts)
                if(part.filter==source)
                {
                    part.filter=destination;part.firstVertex+=vertexOffset;part.firstIndex+=indexOffset;
                    part.rangesVerified &= matches;
                }
            if(!matches)BraxBuildQuality.Warn("OBÓZ CONSTRUCTION_BATCH_UNMEASURED "+source.name+
                " — combined vertices/indices do not match; no support exemption");
            vertexOffset+=vertices.Length;indexOffset+=indices.Length;
        }
        record.members=record.parts.Where(p=>p!=null && p.filter)
            .Select(p=>p.filter.GetComponent<Renderer>()).Where(r=>r).Distinct().ToArray();
        EditorUtility.SetDirty(record);
    }
}
