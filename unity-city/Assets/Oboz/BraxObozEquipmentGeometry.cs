using UnityEngine;

// Exact original parts retained as one supported assembly. No gameplay loop.
public sealed class BraxObozEquipmentGeometry : MonoBehaviour
{
    public string sourcePath,sourceSha256;
    public string[] sourceParts;
    public Collider floorSupport;
    public MeshFilter[] geometry;

    public bool TryMeasureBounds(out Bounds bounds)
    {
        bounds=default;bool measured=false;
        foreach(var filter in geometry??System.Array.Empty<MeshFilter>())
        {
            if(!filter || !filter.sharedMesh ||
                (Application.isPlaying && !filter.sharedMesh.isReadable))return false;
            var matrix=filter.transform.localToWorldMatrix;
            foreach(var vertex in filter.sharedMesh.vertices)
            {
                var point=matrix.MultiplyPoint3x4(vertex);
                if(!measured){bounds=new Bounds(point,Vector3.zero);measured=true;}
                else bounds.Encapsulate(point);
            }
        }
        return measured;
    }
}
