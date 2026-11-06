using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

// OBÓZ: ugięcie istniejącego RENDERA maty, bez zmiany colliderów lub fizyki NPC.
// 3 cm głębokości, promień 0,4 m przed skalą K=1,294.
// Siatka wierzchołków budowana w Play Mode, nie edytuje źródła ani materiału PBR.
[RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
public sealed class BraxObozDeformableMat : MonoBehaviour
{
    const float K=1.294f;
    const float ContactRadius=.4f*K;
    const float MaxDepth=.03f*K;
    const float VisualTick=.06f;
    public float widthMetres=8f,depthMetres=8f;

    Mesh dynamicMesh;
    Vector3[] baseVertices;
    Vector3[] liveVertices;
    Transform[] feet=Array.Empty<Transform>();
    int deformableCount;
    bool wasDeformed;
    float nextTick;

    public void Configure(float width,float depth)
    {
        widthMetres=Mathf.Max(.1f,width);
        depthMetres=Mathf.Max(.1f,depth);
    }

    void Start()
    {
        // Budowa geometryczna tylko podczas gry. Brak nowych tekstur 4K
        // i brak podwójnego renderera pod deformowaną powierzchnią.
        var filter=GetComponent<MeshFilter>();
        dynamicMesh=BuildMesh();
        filter.mesh=dynamicMesh;
        liveVertices=dynamicMesh.vertices;
        baseVertices=(Vector3[])liveVertices.Clone();
        var p=FindObjectsByType<BraxGymPlayer>(FindObjectsSortMode.None)
            .Where(x=>x && x.body).Select(x=>x.transform);
        var n=FindObjectsByType<BraxObozNpc>(FindObjectsSortMode.None)
            .Where(x=>x).Select(x=>x.transform);
        feet=p.Concat(n).Distinct().ToArray();
    }

    Mesh BuildMesh()
    {
        const float pitch=.20f;
        int nx=Mathf.Clamp(Mathf.CeilToInt(widthMetres/pitch),6,160);
        int nz=Mathf.Clamp(Mathf.CeilToInt(depthMetres/pitch),6,160);
        var vertices=new List<Vector3>((nx+1)*(nz+1)+24);
        var uv=new List<Vector2>((nx+1)*(nz+1)+24);
        var indices=new List<int>(nx*nz*6+36);
        // Cube jest skalowany przez generator. Zapis w jednostkach [-.5,.5]
        // zachowuje rzeczywiste materiały i fizyczną szerokość powierzchni.
        for(int iz=0;iz<=nz;iz++)
        for(int ix=0;ix<=nx;ix++)
        {
            float u=ix/(float)nx,v=iz/(float)nz;
            vertices.Add(new Vector3(u-.5f,.5f,v-.5f));
            uv.Add(new Vector2(u*widthMetres*K,v*depthMetres*K));
        }
        deformableCount=vertices.Count;
        for(int z=0;z<nz;z++)
        for(int x=0;x<nx;x++)
        {
            int i=z*(nx+1)+x,j=i+nx+1;
            // Winding w górę — prawidłowa widoczność na Androidzie.
            indices.Add(i);indices.Add(j);indices.Add(i+1);
            indices.Add(i+1);indices.Add(j);indices.Add(j+1);
        }
        void Face(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {
            int i=vertices.Count;
            vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);
            uv.Add(new Vector2(0,0));uv.Add(new Vector2(1,0));
            uv.Add(new Vector2(1,1));uv.Add(new Vector2(0,1));
            indices.Add(i);indices.Add(i+1);indices.Add(i+2);
            indices.Add(i);indices.Add(i+2);indices.Add(i+3);
        }
        Face(new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),
            new Vector3(.5f,.5f,-.5f),new Vector3(-.5f,.5f,-.5f));
        Face(new Vector3(.5f,-.5f,.5f),new Vector3(-.5f,-.5f,.5f),
            new Vector3(-.5f,.5f,.5f),new Vector3(.5f,.5f,.5f));
        Face(new Vector3(-.5f,-.5f,.5f),new Vector3(-.5f,-.5f,-.5f),
            new Vector3(-.5f,.5f,-.5f),new Vector3(-.5f,.5f,.5f));
        Face(new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,-.5f,.5f),
            new Vector3(.5f,.5f,.5f),new Vector3(.5f,.5f,-.5f));
        Face(new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),
            new Vector3(.5f,-.5f,-.5f),new Vector3(-.5f,-.5f,-.5f));
        var mesh=new Mesh{name="OBÓZ — mata z ugięciem wierzchołków 0.4m/3cm"};
        if(vertices.Count>65000)
            mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);
        mesh.RecalculateNormals();mesh.RecalculateBounds();
        mesh.MarkDynamic();
        return mesh;
    }

    void LateUpdate()
    {
        if(!dynamicMesh||feet.Length==0 || Time.time<nextTick)return;
        nextTick=Time.time+VisualTick;
        Vector3 center=transform.position;
        float sizeX=Mathf.Abs(transform.lossyScale.x);
        float sizeY=Mathf.Abs(transform.lossyScale.y);
        float sizeZ=Mathf.Abs(transform.lossyScale.z);
        if(sizeY<.0001f)return;
        float top=transform.TransformPoint(new Vector3(0,.5f,0)).y;
        var active=feet.Where(t=>t && t.gameObject.activeInHierarchy &&
                Mathf.Abs(t.position.y-top)<.30f*K &&
                Mathf.Abs(t.position.x-center.x)<sizeX*.5f+ContactRadius &&
                Mathf.Abs(t.position.z-center.z)<sizeZ*.5f+ContactRadius)
            .ToArray();
        if(active.Length==0 && !wasDeformed)return;
        bool any=false;
        float radius2=ContactRadius*ContactRadius;
        for(int i=0;i<deformableCount;i++)
        {
            var vertex=baseVertices[i];
            var world=transform.TransformPoint(vertex);
            float falloff=0f;
            // 3cm zagłębienie tylko pod stopami, zero poza promieniem 0.4m.
            // Dodatkowa obwódka zapobiega szczelinom między bokiem maty a górą.
            float rim=Mathf.Min(1f,Mathf.Min(
                (.5f-Mathf.Abs(vertex.x))*sizeX/.08f/K,
                (.5f-Mathf.Abs(vertex.z))*sizeZ/.08f/K));
            for(int j=0;j<active.Length;j++)
            {
                var foot=active[j].position;
                float dx=world.x-foot.x,dz=world.z-foot.z;
                float d2=dx*dx+dz*dz;
                if(d2>=radius2)continue;
                float depth=(1f-d2/radius2);
                falloff=Mathf.Max(falloff,depth*depth*Mathf.Clamp01(rim));
            }
            liveVertices[i]=new Vector3(vertex.x,
                vertex.y-falloff*MaxDepth/sizeY,vertex.z);
            any|=falloff>.001f;
        }
        if(!any&&!wasDeformed)return;
        dynamicMesh.vertices=liveVertices;
        dynamicMesh.RecalculateNormals();
        dynamicMesh.RecalculateBounds();
        wasDeformed=any;
    }

    void OnDestroy()
    {
        if(dynamicMesh)Destroy(dynamicMesh);
    }
}
