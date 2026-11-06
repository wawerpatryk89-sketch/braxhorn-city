using System;
using System.Collections.Generic;
using UnityEngine;

// One shared pool for the player and all NPCs. Scene references survive save/load;
// all changing tracking state is rebuilt in Start, after scene deserialization.
[DefaultExecutionOrder(500)]
public sealed class BraxObozFootprintTrail : MonoBehaviour
{
    public const int PoolSize=40;
    public const float LifetimeSeconds=20f;
    const float K=1.294f;
    const float Stride=.70f*K;
    const float FootprintWidth=.18f*K;
    const float FootprintLength=.32f*K;
    const float MaxContactGap=.12f*K;
    static readonly int TintId=Shader.PropertyToID("_Color");
    static readonly Color StampTint=new Color(.32f,.25f,.18f,.76f);
    [SerializeField] Transform[] actors=Array.Empty<Transform>();
    Vector3[] previous=Array.Empty<Vector3>();
    Vector3[] lastStep=Array.Empty<Vector3>();
    bool[] nextLeft=Array.Empty<bool>();
    CharacterController[] bodies=Array.Empty<CharacterController>();
    BraxObozIdleFootContact[] soles=Array.Empty<BraxObozIdleFootContact>();
    readonly RaycastHit[] rayHits=new RaycastHit[32];
    readonly Dictionary<Collider,Surface> surfaces=new Dictionary<Collider,Surface>();
    Mark[] pool;
    Texture2D footprint;
    Mesh stampMesh;
    Material stampMaterial;
    int cursor;

    sealed class Mark
    {
        public GameObject gameObject;
        public MeshRenderer renderer;
        public MaterialPropertyBlock properties;
        public float born;
    }
    sealed class Surface
    {
        public bool soft;
        public Color[] colors;
        public int[] triangles;
    }

    public void Configure(BraxGymPlayer player,BraxObozNpc[] npcs)
    {
        var targets=new List<Transform>();
        if(player)targets.Add(player.transform);
        foreach(var npc in npcs??Array.Empty<BraxObozNpc>())
            if(npc && !targets.Contains(npc.transform))targets.Add(npc.transform);
        actors=targets.ToArray();
        ResetTracking();
    }

    void ResetTracking()
    {
        int count=actors==null?0:actors.Length;
        previous=new Vector3[count];lastStep=new Vector3[count];nextLeft=new bool[count];
        bodies=new CharacterController[count];soles=new BraxObozIdleFootContact[count];
        for(int i=0;i<count;i++)
        {
            if(!actors[i])continue;
            previous[i]=lastStep[i]=actors[i].position;
            bodies[i]=actors[i].GetComponent<CharacterController>();
            soles[i]=actors[i].GetComponent<BraxObozIdleFootContact>();
        }
    }

    void Start()
    {
        if(actors==null || actors.Length==0)
        {
            // Backward compatibility for scenes saved before the field was serialized.
            var info=GetComponent<BraxObozSceneInfo>();
            if(info)Configure(info.player,info.npcs);
            else Configure(FindFirstObjectByType<BraxGymPlayer>(),
                FindObjectsByType<BraxObozNpc>(FindObjectsSortMode.None));
        }
        ResetTracking();
        var shader=Resources.Load<Shader>("BraxObozFootprint");
        if(!shader)
        {
            Debug.LogWarning("BRAXHORN_OBOZ_FOOTPRINT_SHADER_MISSING: ślady wyłączone",this);
            return;
        }
        footprint=BuildStamp();stampMesh=BuildQuad();
        stampMaterial=new Material(shader){name="OBÓZ ślad buta — detal mały",
            mainTexture=footprint,enableInstancing=true};
        stampMaterial.SetColor(TintId,StampTint);
        pool=new Mark[PoolSize];
        for(int i=0;i<PoolSize;i++)
        {
            var quad=new GameObject("Ślad na błocie lub piasku "+(i+1));
            quad.transform.SetParent(transform,false);
            quad.transform.localScale=new Vector3(FootprintWidth,1f,FootprintLength);
            quad.AddComponent<MeshFilter>().sharedMesh=stampMesh;
            var r=quad.AddComponent<MeshRenderer>();
            r.sharedMaterial=stampMaterial;
            r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows=false;
            quad.SetActive(false);
            pool[i]=new Mark{gameObject=quad,renderer=r,
                properties=new MaterialPropertyBlock()};
        }
    }

    static Mesh BuildQuad()
    {
        var mesh=new Mesh{name="OBÓZ wspólna siatka śladu"};
        // Explicit upward winding: a decal cannot face underneath the ground.
        mesh.vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(-.5f,0,.5f),
            new Vector3(.5f,0,.5f),new Vector3(.5f,0,-.5f)};
        mesh.normals=new[]{Vector3.up,Vector3.up,Vector3.up,Vector3.up};
        mesh.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};
        mesh.triangles=new[]{0,1,2,0,2,3};
        mesh.RecalculateBounds();
        return mesh;
    }

    static Texture2D BuildStamp()
    {
        const int w=64,h=128;
        var tex=new Texture2D(w,h,TextureFormat.RGBA32,false,true);
        tex.name="Ślad buta — procedura 64x128, bez zasobu 4K";
        var pixels=new Color32[w*h];
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)
        {
            float fx=(x+.5f)*2f/w-1f;
            float fy=(y+.5f)*2f/h-1f;
            float fore=(fx*fx/.68f/.68f)+
                ((fy-.40f)*(fy-.40f)/(.53f*.53f));
            float heel=(fx*fx/(.48f*.48f))+
                ((fy+.57f)*(fy+.57f)/(.31f*.31f));
            float inside=Mathf.Max(Mathf.Clamp01((1.05f-fore)*9f),
                Mathf.Clamp01((1.05f-heel)*9f));
            float bands=.74f+.26f*Mathf.Abs(Mathf.Sin((fy+.9f)*22f));
            byte a=(byte)Mathf.RoundToInt(170f*inside*bands);
            pixels[y*w+x]=new Color32(255,255,255,a);
        }
        tex.SetPixels32(pixels);tex.Apply(false,true);
        tex.wrapMode=TextureWrapMode.Clamp;tex.filterMode=FilterMode.Bilinear;
        return tex;
    }

    static bool IsSoftName(string name)
    {
        // No grass, slabs, rubber, timber or sandstone: only explicit loose soil.
        return name.IndexOf("mud_forest",StringComparison.OrdinalIgnoreCase)>=0 ||
            name.IndexOf("błoto",StringComparison.OrdinalIgnoreCase)>=0 ||
            name.IndexOf("piasek",StringComparison.OrdinalIgnoreCase)>=0 ||
            name.IndexOf("sand_",StringComparison.OrdinalIgnoreCase)>=0 ||
            string.Equals(name,"sand",StringComparison.OrdinalIgnoreCase);
    }

    Surface ReadSurface(Collider collider)
    {
        if(surfaces.TryGetValue(collider,out var cached))return cached;
        var surface=new Surface();
        var renderer=collider.GetComponent<Renderer>();
        bool blended=false;
        if(renderer)
        {
            foreach(var material in renderer.sharedMaterials)
            {
                if(!material)continue;
                blended|=material.HasProperty("_MudColor");
                surface.soft|=IsSoftName(material.name);
            }
        }
        if(blended)surface.soft=false;
        var meshCollider=collider as MeshCollider;
        if(blended && meshCollider && meshCollider.sharedMesh && meshCollider.sharedMesh.isReadable)
        {
            var mesh=meshCollider.sharedMesh;
            // Cache once, not mesh.colors/triangles array copies on every footstep.
            var colors=mesh.colors;
            if(colors.Length==mesh.vertexCount)
            {surface.colors=colors;surface.triangles=mesh.triangles;}
            surface.soft=false; // vertex mask decides which part of mixed ground is muddy.
        }
        else if(!blended)surface.soft|=IsSoftName(collider.name);
        surfaces.Add(collider,surface);
        return surface;
    }

    bool IsSoftSurface(RaycastHit hit)
    {
        var surface=ReadSurface(hit.collider);
        if(surface.colors==null)return surface.soft;
        int t=hit.triangleIndex;
        if(t<0 || t*3+2>=surface.triangles.Length)return false;
        var tri=surface.triangles;var colors=surface.colors;
        Vector3 bary=hit.barycentricCoordinate;
        float mud=colors[tri[t*3]].b*bary.x+
            colors[tri[t*3+1]].b*bary.y+colors[tri[t*3+2]].b*bary.z;
        return mud>=.28f;
    }

    bool FootHit(Vector3 sole,out RaycastHit ground)
    {
        ground=default;
        int count=Physics.RaycastNonAlloc(sole+Vector3.up*.35f*K,Vector3.down,
            rayHits,.60f*K,~(1<<8),QueryTriggerInteraction.Ignore);
        if(count==rayHits.Length)return false;
        float nearest=float.PositiveInfinity;
        for(int i=0;i<count;i++)
        {
            var hit=rayHits[i];
            if(!hit.collider || hit.normal.y<.65f || hit.distance>=nearest)continue;
            ground=hit;nearest=hit.distance;
        }
        // Never place through a solid path onto mud below it.
        return !float.IsInfinity(nearest) &&
            Mathf.Abs(sole.y-ground.point.y)<=MaxContactGap && IsSoftSurface(ground);
    }

    void LateUpdate()
    {
        if(pool==null)return;
        for(int i=0;i<actors.Length;i++)
        {
            var actor=actors[i];
            if(!actor || !actor.gameObject.activeInHierarchy)continue;
            Vector3 now=actor.position;
            Vector3 movement=now-previous[i];movement.y=0f;
            previous[i]=now;
            if(!bodies[i] || !bodies[i].isGrounded || movement.sqrMagnitude>16f*K*K)
            {lastStep[i]=now;continue;}
            if(movement.sqrMagnitude<.00004f)continue;
            Vector3 progress=now-lastStep[i];progress.y=0f;
            if(progress.sqrMagnitude<Stride*Stride)continue;
            lastStep[i]=now;
            Vector3 forward=movement.normalized;
            Vector3 side=Vector3.Cross(Vector3.up,forward);
            Vector3 at=now+side*(nextLeft[i]?-.095f*K:.095f*K);
            var contact=soles[i];
            if(contact && contact.TryGetSolePosition(nextLeft[i],out var measured))at=measured;
            if(!FootHit(at,out var hit))
            {
                // Prefer the other actual planted foot when the stride falls
                // during the selected foot's swing; do not stamp while jumping.
                if(!contact || !contact.TryGetSolePosition(!nextLeft[i],out at) ||
                    !FootHit(at,out hit))continue;
            }
            Spawn(hit.point,hit.normal,forward);
            nextLeft[i]=!nextLeft[i];
        }
        for(int i=0;i<pool.Length;i++)
        {
            var mark=pool[i];
            if(!mark.gameObject.activeSelf)continue;
            float age=Time.time-mark.born;
            if(age>=LifetimeSeconds){mark.gameObject.SetActive(false);continue;}
            Color tint=StampTint;tint.a*=1f-Mathf.Clamp01(age/LifetimeSeconds);
            mark.properties.SetColor(TintId,tint);
            mark.renderer.SetPropertyBlock(mark.properties);
        }
    }

    void Spawn(Vector3 where,Vector3 normal,Vector3 direction)
    {
        var mark=pool[cursor];cursor=(cursor+1)%PoolSize;
        mark.born=Time.time;
        mark.gameObject.transform.position=where+normal*(.003f*K);
        Vector3 forward=Vector3.ProjectOnPlane(direction,normal).normalized;
        mark.gameObject.transform.rotation=Quaternion.LookRotation(forward,normal);
        // Reset opacity before reusing a faded pool slot.
        mark.properties.SetColor(TintId,StampTint);
        mark.renderer.SetPropertyBlock(mark.properties);
        mark.gameObject.SetActive(true);
    }

    void OnDisable()
    {
        if(pool==null)return;
        foreach(var mark in pool)if(mark.gameObject)mark.gameObject.SetActive(false);
    }
    void OnEnable(){if(pool!=null)ResetTracking();}
    void OnDestroy()
    {
        if(pool!=null)foreach(var mark in pool)if(mark.gameObject)Destroy(mark.gameObject);
        if(stampMaterial)Destroy(stampMaterial);
        if(stampMesh)Destroy(stampMesh);
        if(footprint)Destroy(footprint);
    }
}
