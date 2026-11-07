using UnityEngine;

// Measurement metadata only; no Update, animation or source-asset mutation.
// The body is a verified vertex range in the actual combined rendered mesh,
// independent of the collider and BraxWorek's physical parameters.
public sealed class BraxObozBagGeometry : MonoBehaviour
{
    [SerializeField] MeshFilter geometry;
    [SerializeField] int bodyVertexStart,bodyVertexCount,attachmentVertex;
    public string sourcePath,sourceSha256,sourceBodyNode;
    public Collider groundSupport,beamSupport;
    public Mesh SharedMesh => geometry?geometry.sharedMesh:null;
    public int BodyVertexCount => bodyVertexCount;
    public bool Contains(Renderer renderer)
    {
        return renderer && geometry && renderer.GetComponent<MeshFilter>()==geometry;
    }

    public void Configure(MeshFilter rendered,int start,int count,int attachment,
        string path,string sha,string bodyName,Collider ground,Collider beam)
    {
        geometry=rendered;bodyVertexStart=start;bodyVertexCount=count;
        attachmentVertex=attachment;sourcePath=path;sourceSha256=sha;
        sourceBodyNode=bodyName;groundSupport=ground;beamSupport=beam;
    }

    public bool TryMeasureBodyBounds(out Bounds bounds)
    {
        bounds=default;var mesh=SharedMesh;
        if(!mesh || bodyVertexCount<1 || bodyVertexStart<0 ||
            bodyVertexStart+bodyVertexCount>mesh.vertexCount ||
            (Application.isPlaying && !mesh.isReadable))return false;
        var vertices=mesh.vertices;
        Matrix4x4 world=geometry.transform.localToWorldMatrix;
        bounds=new Bounds(world.MultiplyPoint3x4(vertices[bodyVertexStart]),Vector3.zero);
        for(int i=1;i<bodyVertexCount;i++)
            bounds.Encapsulate(world.MultiplyPoint3x4(vertices[bodyVertexStart+i]));
        return true;
    }

    public bool TryMeasureAttachment(out Vector3 worldPoint)
    {
        worldPoint=default;var mesh=SharedMesh;
        if(!mesh || attachmentVertex<0 || attachmentVertex>=mesh.vertexCount ||
            (Application.isPlaying && !mesh.isReadable))return false;
        worldPoint=geometry.transform.TransformPoint(mesh.vertices[attachmentVertex]);
        return true;
    }
}
