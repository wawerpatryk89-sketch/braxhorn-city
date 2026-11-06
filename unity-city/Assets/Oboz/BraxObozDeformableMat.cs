using System;
using System.Collections.Generic;
using UnityEngine;

// Camp rubber: a local runtime mesh, a fixed collision surface and measured soles.
// Originals and collider meshes remain unchanged; the visual contact component
// can query this same rendered surface after deformation.
[DefaultExecutionOrder(600)]
[RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
public sealed class BraxObozDeformableMat : MonoBehaviour
{
    const float K=1.294f;
    const float ContactRadius=.4f*K;
    const float MaxDepth=.03f*K;
    const float VisualTick=.06f;
    public float widthMetres=8f,depthMetres=8f;

    MeshFilter filter;
    Mesh originalMesh,dynamicMesh;
    Vector3[] baseVertices,liveVertices;
    BraxObozIdleFootContact[] contacts=Array.Empty<BraxObozIdleFootContact>();
    Vector2[] activeFeet=Array.Empty<Vector2>();
    int deformableCount,activeFootCount,nx,nz;
    bool wasDeformed;
    float nextTick;

    public void Configure(float width,float depth)
    {
        widthMetres=Mathf.Max(.1f,width);
        depthMetres=Mathf.Max(.1f,depth);
    }

    void Start()
    {
        filter=GetComponent<MeshFilter>();originalMesh=filter.sharedMesh;
        dynamicMesh=BuildMesh();filter.sharedMesh=dynamicMesh;
        liveVertices=dynamicMesh.vertices;
        baseVertices=(Vector3[])liveVertices.Clone();
        // One lookup after the saved scene is loaded. No global search or LINQ
        // allocation in the deformation loop; all seven actors use sole contacts.
        contacts=FindObjectsByType<BraxObozIdleFootContact>(FindObjectsSortMode.None);
        activeFeet=new Vector2[contacts.Length*2];
    }

    Mesh BuildMesh()
    {
        const float pitch=.20f;
        nx=Mathf.Clamp(Mathf.CeilToInt(widthMetres/pitch),6,160);
        nz=Mathf.Clamp(Mathf.CeilToInt(depthMetres/pitch),6,160);
        var vertices=new List<Vector3>((nx+1)*(nz+1)+20);
        var uv=new List<Vector2>((nx+1)*(nz+1)+20);
        var indices=new List<int>(nx*nz*6+30);
        // Generator scales the box. This grid keeps its physical dimensions.
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
            indices.Add(i);indices.Add(j);indices.Add(i+1);
            indices.Add(i+1);indices.Add(j);indices.Add(j+1);
        }
        void Face(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {
            int i=vertices.Count;
            vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);
            float u=transform.TransformVector(b-a).magnitude;
            float v=transform.TransformVector(d-a).magnitude;
            uv.Add(new Vector2(0,0));uv.Add(new Vector2(u,0));
            uv.Add(new Vector2(u,v));uv.Add(new Vector2(0,v));
            indices.Add(i);indices.Add(i+1);indices.Add(i+2);
            indices.Add(i);indices.Add(i+2);indices.Add(i+3);
        }
        // Outward winding on the sides and underside; the rim stays fixed.
        Face(new Vector3(-.5f,-.5f,-.5f),new Vector3(-.5f,.5f,-.5f),
            new Vector3(.5f,.5f,-.5f),new Vector3(.5f,-.5f,-.5f));
        Face(new Vector3(.5f,-.5f,.5f),new Vector3(.5f,.5f,.5f),
            new Vector3(-.5f,.5f,.5f),new Vector3(-.5f,-.5f,.5f));
        Face(new Vector3(-.5f,-.5f,.5f),new Vector3(-.5f,.5f,.5f),
            new Vector3(-.5f,.5f,-.5f),new Vector3(-.5f,-.5f,-.5f));
        Face(new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,.5f,-.5f),
            new Vector3(.5f,.5f,.5f),new Vector3(.5f,-.5f,.5f));
        Face(new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),
            new Vector3(.5f,-.5f,.5f),new Vector3(-.5f,-.5f,.5f));
        var mesh=new Mesh{name="OBÓZ — mata z ugięciem wierzchołków 0.4m/3cm"};
        if(vertices.Count>65000)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);
        mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        mesh.MarkDynamic();
        return mesh;
    }

    void AddContact(BraxObozIdleFootContact contact,bool left,float sizeX,float sizeY,float sizeZ)
    {
        if(!contact.TryGetSolePosition(left,out var world))return;
        Vector3 local=transform.InverseTransformPoint(world);
        float gap=(local.y-.5f)*sizeY;
        if(gap< -MaxDepth-.03f*K || gap>.08f*K ||
            Mathf.Abs(local.x)>.5f+ContactRadius/sizeX ||
            Mathf.Abs(local.z)>.5f+ContactRadius/sizeZ)return;
        activeFeet[activeFootCount++]=new Vector2(local.x,local.z);
    }

    float DepthAt(Vector3 local,float sizeX,float sizeZ)
    {
        float radius2=ContactRadius*ContactRadius;
        float rim=Mathf.Clamp01(Mathf.Min(
            (.5f-Mathf.Abs(local.x))*sizeX/(.08f*K),
            (.5f-Mathf.Abs(local.z))*sizeZ/(.08f*K)));
        float deepest=0f;
        for(int j=0;j<activeFootCount;j++)
        {
            float dx=(local.x-activeFeet[j].x)*sizeX;
            float dz=(local.z-activeFeet[j].y)*sizeZ;
            float d2=dx*dx+dz*dz;
            if(d2>=radius2)continue;
            float falloff=1f-d2/radius2;
            // Overlapping feet never add beyond the owner's 3 cm limit.
            deepest=Mathf.Max(deepest,falloff*falloff*rim);
        }
        return deepest*MaxDepth;
    }

    void LateUpdate()
    {
        if(!dynamicMesh || Time.time<nextTick)return;
        nextTick=Time.time+VisualTick;
        float sizeX=Mathf.Abs(transform.lossyScale.x);
        float sizeY=Mathf.Abs(transform.lossyScale.y);
        float sizeZ=Mathf.Abs(transform.lossyScale.z);
        if(sizeX<.0001f || sizeY<.0001f || sizeZ<.0001f)return;
        activeFootCount=0;
        foreach(var contact in contacts)
        {
            if(!contact || !contact.isActiveAndEnabled || !contact.body ||
                !contact.body.isGrounded)continue;
            AddContact(contact,true,sizeX,sizeY,sizeZ);
            AddContact(contact,false,sizeX,sizeY,sizeZ);
        }
        ApplyDeformation(sizeX,sizeY,sizeZ);
    }

    void ApplyDeformation(float sizeX,float sizeY,float sizeZ)
    {
        if(activeFootCount==0 && !wasDeformed)return;
        bool any=false;
        for(int i=0;i<deformableCount;i++)
        {
            Vector3 vertex=baseVertices[i];
            float depth=DepthAt(vertex,sizeX,sizeZ);
            liveVertices[i]=new Vector3(vertex.x,vertex.y-depth/sizeY,vertex.z);
            any|=depth>.00001f;
        }
        if(!any && !wasDeformed)return;
        dynamicMesh.vertices=liveVertices;
        dynamicMesh.RecalculateNormals();dynamicMesh.RecalculateTangents();
        // The undeformed box bounds already contain every depressed vertex.
        wasDeformed=any;
    }

    // Exact interpolation on the currently rendered grid triangles, not on an
    // unrendered analytic curve. This can keep soles on a depressed visual mat
    // while preserving CharacterController/NavMesh ownership of physical motion.
    public bool TryGetSurfaceHeight(Vector3 worldPosition,out float worldY)
    {
        worldY=0f;
        if(!dynamicMesh || liveVertices==null)return false;
        Vector3 local=transform.InverseTransformPoint(worldPosition);
        if(Mathf.Abs(local.x)>.5f || Mathf.Abs(local.z)>.5f)return false;
        float u=(local.x+.5f)*nx,v=(local.z+.5f)*nz;
        int x=Mathf.Min(nx-1,Mathf.FloorToInt(u)),z=Mathf.Min(nz-1,Mathf.FloorToInt(v));
        float fx=u-x,fz=v-z;int i=z*(nx+1)+x,j=i+nx+1;
        float y=fx+fz<=1f
            ? liveVertices[i].y*(1f-fx-fz)+liveVertices[i+1].y*fx+liveVertices[j].y*fz
            : liveVertices[j+1].y*(fx+fz-1f)+liveVertices[i+1].y*(1f-fz)+liveVertices[j].y*(1f-fx);
        worldY=transform.TransformPoint(new Vector3(local.x,y,local.z)).y;
        return true;
    }

    void OnDisable()
    {
        if(!dynamicMesh || baseVertices==null)return;
        Array.Copy(baseVertices,liveVertices,baseVertices.Length);
        dynamicMesh.vertices=liveVertices;
        dynamicMesh.RecalculateNormals();dynamicMesh.RecalculateTangents();
        activeFootCount=0;wasDeformed=false;
    }
    void OnDestroy()
    {
        if(filter && filter.sharedMesh==dynamicMesh)filter.sharedMesh=originalMesh;
        if(dynamicMesh)Destroy(dynamicMesh);
    }
}
