using System;
using System.Linq;
using UnityEngine;

// OBÓZ: oryginalne podłoże mud_forest_2k, pula 40 śladów za chodzącym
// graczem i NPC, czas życia 20s. Bez nowego dużego atlasu i bez kostek 3D.
public sealed class BraxObozFootprintTrail : MonoBehaviour
{
    public const int PoolSize=40;
    public const float LifetimeSeconds=20f;
    const float K=1.294f;
    const float Stride=.70f*K;
    const float FootprintWidth=.18f*K;
    const float FootprintLength=.32f*K;
    Transform[] actors=Array.Empty<Transform>();
    Vector3[] previous=Array.Empty<Vector3>();
    Vector3[] lastStep=Array.Empty<Vector3>();
    bool[] nextLeft=Array.Empty<bool>();
    Mark[] pool;
    Texture2D footprint;
    Material stampMaterial;
    int cursor;

    sealed class Mark
    {
        public GameObject gameObject;
        public MeshRenderer renderer;
        public MaterialPropertyBlock properties;
        public float born;
    }

    public void Configure(BraxGymPlayer player,BraxObozNpc[] npcs)
    {
        actors=(new[]{player?player.transform:null}).Concat(
            (npcs??Array.Empty<BraxObozNpc>()).Where(n=>n).Select(n=>n.transform))
            .Where(t=>t).ToArray();
        previous=actors.Select(x=>x.position).ToArray();
        lastStep=(Vector3[])previous.Clone();
        nextLeft=new bool[actors.Length];
    }

    void Start()
    {
        footprint=BuildStamp();
        var shader=Shader.Find("Unlit/Transparent");
        if(!shader)
        {
            Debug.LogWarning("BRAXHORN_OBOZ_FOOTPRINT_SHADER_MISSING: ślady wyłączone");
            return;
        }
        stampMaterial=new Material(shader){name="OBÓZ ślad buta — detal mały",
            mainTexture=footprint,enableInstancing=true};
        stampMaterial.SetColor("_Color",new Color(.32f,.25f,.18f,.76f));
        pool=new Mark[PoolSize];
        for(int i=0;i<PoolSize;i++)
        {
            var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name="Ślad na błocie "+(i+1);
            quad.transform.SetParent(transform,false);
            quad.transform.localScale=new Vector3(FootprintWidth,FootprintLength,1f);
            var c=quad.GetComponent<Collider>();
            if(c)Destroy(c);
            var r=quad.GetComponent<MeshRenderer>();
            r.sharedMaterial=stampMaterial;
            r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows=false;
            quad.SetActive(false);
            pool[i]=new Mark{gameObject=quad,renderer=r,
                properties=new MaterialPropertyBlock()};
        }
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
            // Prosty bieżnik, a nie jednolita ciemna prostokątna naklejka.
            float bands=.74f+.26f*Mathf.Abs(Mathf.Sin((fy+.9f)*22f));
            byte a=(byte)Mathf.RoundToInt(170f*inside*bands);
            pixels[y*w+x]=new Color32(255,255,255,a);
        }
        tex.SetPixels32(pixels);tex.Apply(false,true);
        tex.wrapMode=TextureWrapMode.Clamp;tex.filterMode=FilterMode.Bilinear;
        return tex;
    }

    static float MudAt(RaycastHit hit)
    {
        var col=hit.collider as MeshCollider;
        if(!col||!col.sharedMesh||col.gameObject.name.IndexOf("Podłoże",
            StringComparison.OrdinalIgnoreCase)<0)return 0f;
        var mesh=col.sharedMesh;
        var colors=mesh.colors;
        int t=hit.triangleIndex;
        if(t<0||colors==null||colors.Length!=mesh.vertexCount)return 0f;
        var tri=mesh.triangles;
        if(t*3+2>=tri.Length)return 0f;
        Vector3 bary=hit.barycentricCoordinate;
        return colors[tri[t*3]].b*bary.x+
            colors[tri[t*3+1]].b*bary.y+
            colors[tri[t*3+2]].b*bary.z;
    }

    void Update()
    {
        if(pool==null||actors==null)return;
        for(int i=0;i<actors.Length;i++)
        {
            var a=actors[i];
            if(!a||!a.gameObject.activeInHierarchy)continue;
            var now=a.position;
            var movement=now-previous[i];movement.y=0;
            previous[i]=now;
            if(movement.sqrMagnitude<.00004f)continue;
            var progress=now-lastStep[i];progress.y=0f;
            if(progress.sqrMagnitude<Stride*Stride)continue;
            lastStep[i]=now;
            Vector3 forward=movement.normalized;
            Vector3 side=Vector3.Cross(Vector3.up,forward).normalized;
            Vector3 at=now+side*(nextLeft[i]?-.095f*K:.095f*K);
            nextLeft[i]=!nextLeft[i];
            var hits=Physics.RaycastAll(at+Vector3.up*.75f*K,
                Vector3.down,3f*K,~(1<<8),QueryTriggerInteraction.Ignore)
                .Where(h=>h.collider)
                .OrderBy(h=>h.distance).ToArray();
            if(hits.Length==0 || MudAt(hits[0])<.28f)continue;
            Spawn(hits[0].point,hits[0].normal,forward);
        }
        for(int i=0;i<pool.Length;i++)
        {
            var m=pool[i];
            if(!m.gameObject.activeSelf)continue;
            float age=Time.time-m.born;
            if(age>=LifetimeSeconds){m.gameObject.SetActive(false);continue;}
            // Bez tworzenia materialu dla każdego śladu (MaterialPropertyBlock).
            float fade=1f-Mathf.Clamp01(age/LifetimeSeconds);
            m.properties.SetColor("_Color",new Color(.32f,.25f,.18f,.76f*fade));
            m.renderer.SetPropertyBlock(m.properties);
        }
    }

    void Spawn(Vector3 where,Vector3 normal,Vector3 direction)
    {
        if(pool==null)return;
        var m=pool[cursor];cursor=(cursor+1)%PoolSize;
        m.gameObject.SetActive(true);
        m.born=Time.time;
        m.gameObject.transform.position=where+normal*(.008f*K);
        var forward=Vector3.ProjectOnPlane(direction,normal).normalized;
        var rotation=Quaternion.LookRotation(forward,normal);
        m.gameObject.transform.rotation=rotation*Quaternion.Euler(-90f,0,0);
    }

    void OnDestroy()
    {
        if(stampMaterial)Destroy(stampMaterial);
        if(footprint)Destroy(footprint);
    }
}
