using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Small manufactured parts are combined per local object and material. This
// does not merge the district or remove spatial culling boundaries.
internal sealed class BraxCitySystemMesh
{
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<Vector3> normals = new List<Vector3>();
    private readonly List<Vector2> uv = new List<Vector2>();
    private readonly List<int> triangles = new List<int>();
    public int VertexCount { get { return vertices.Count; } }

    public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float textureMetres = 1)
    {
        Vector3 n = Vector3.Cross(b - a, c - a).normalized;
        Quad(a, b, c, d, n, n, n, n, textureMetres);
    }

    private void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
        Vector3 na, Vector3 nb, Vector3 nc, Vector3 nd, float textureMetres)
    {
        int start = vertices.Count;
        vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
        normals.Add(na); normals.Add(nb); normals.Add(nc); normals.Add(nd);
        float w = Vector3.Distance(a, b) / textureMetres, h = Vector3.Distance(b, c) / textureMetres;
        uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(w, 0)); uv.Add(new Vector2(w, h)); uv.Add(new Vector2(0, h));
        triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
        triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
    }

    public void Triangle(Vector3 a, Vector3 b, Vector3 c)
    {
        int start = vertices.Count; Vector3 n = Vector3.Cross(b - a, c - a).normalized;
        vertices.Add(a); vertices.Add(b); vertices.Add(c);
        normals.Add(n); normals.Add(n); normals.Add(n);
        uv.Add(Vector2.zero); uv.Add(Vector2.right); uv.Add(Vector2.up);
        triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
    }

    public void Box(Vector3 center, Vector3 size, float textureMetres = 1)
    {
        Vector3 a = center - size * .5f, b = center + size * .5f;
        Quad(new Vector3(a.x,a.y,b.z),new Vector3(b.x,a.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(a.x,b.y,b.z),textureMetres);
        Quad(new Vector3(b.x,a.y,a.z),new Vector3(a.x,a.y,a.z),new Vector3(a.x,b.y,a.z),new Vector3(b.x,b.y,a.z),textureMetres);
        Quad(new Vector3(a.x,a.y,a.z),new Vector3(a.x,a.y,b.z),new Vector3(a.x,b.y,b.z),new Vector3(a.x,b.y,a.z),textureMetres);
        Quad(new Vector3(b.x,a.y,b.z),new Vector3(b.x,a.y,a.z),new Vector3(b.x,b.y,a.z),new Vector3(b.x,b.y,b.z),textureMetres);
        Quad(new Vector3(a.x,b.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(b.x,b.y,a.z),new Vector3(a.x,b.y,a.z),textureMetres);
        Quad(new Vector3(a.x,a.y,a.z),new Vector3(b.x,a.y,a.z),new Vector3(b.x,a.y,b.z),new Vector3(a.x,a.y,b.z),textureMetres);
    }

    public void Tube(Vector3 start, Vector3 end, float radius, int sides = 16, float endRadius = -1)
    {
        if (endRadius < 0) endRadius = radius;
        Vector3 axis = (end - start).normalized;
        Vector3 u = Vector3.Cross(axis, Mathf.Abs(axis.y) > .95f ? Vector3.forward : Vector3.up).normalized;
        Vector3 v = Vector3.Cross(axis, u);
        for (int i = 0; i < sides; i++)
        {
            float a = 2 * Mathf.PI * i / sides, b = 2 * Mathf.PI * (i + 1) / sides;
            Vector3 n0 = u * Mathf.Cos(a) + v * Mathf.Sin(a), n1 = u * Mathf.Cos(b) + v * Mathf.Sin(b);
            Vector3 p0 = start + n0 * radius, p1 = start + n1 * radius;
            Vector3 p2 = end + n1 * endRadius, p3 = end + n0 * endRadius;
            Quad(p0,p1,p2,p3,n0,n1,n1,n0,1);
            Triangle(start,p1,p0); Triangle(end,p3,p2);
        }
    }

    public void RoundedPanel(Vector3 center, float width, float height, float depth, float radius)
    {
        var edge = new List<Vector2>();
        for (int corner = 0; corner < 4; corner++)
        {
            float angle = corner * Mathf.PI * .5f;
            Vector2 c = new Vector2((corner == 0 || corner == 3 ? 1 : -1) * (width * .5f - radius),
                (corner < 2 ? 1 : -1) * (height * .5f - radius));
            for (int s = 0; s <= 4; s++)
            {
                float a = angle + s * Mathf.PI / 8;
                edge.Add(c + new Vector2(Mathf.Cos(a),Mathf.Sin(a)) * radius);
            }
        }
        // Polygon runs anticlockwise in XY, viewed from the front (+Z).
        Vector3 front = center + Vector3.forward * depth * .5f, back = center - Vector3.forward * depth * .5f;
        for (int i = 0; i < edge.Count; i++)
        {
            Vector3 a = new Vector3(edge[i].x,edge[i].y,0), b = new Vector3(edge[(i+1)%edge.Count].x,edge[(i+1)%edge.Count].y,0);
            Triangle(front,front+a,front+b); Triangle(back,back+b,back+a);
            Quad(back+a,back+b,front+b,front+a);
        }
    }

    public void Ring(Vector3 center, float inner, float outer, float depth, float arcStart = 0, float arcEnd = 360, int sides = 24)
    {
        for (int i = 0; i < sides; i++)
        {
            float a = Mathf.Lerp(arcStart,arcEnd,(float)i/sides) * Mathf.Deg2Rad;
            float b = Mathf.Lerp(arcStart,arcEnd,(float)(i+1)/sides) * Mathf.Deg2Rad;
            Vector3 na = new Vector3(Mathf.Cos(a),Mathf.Sin(a),0), nb = new Vector3(Mathf.Cos(b),Mathf.Sin(b),0);
            Vector3 far = center + Vector3.forward * depth;
            Quad(center+na*outer,center+nb*outer,far+nb*outer,far+na*outer,na,nb,nb,na,1);
            Quad(center+nb*inner,center+na*inner,far+na*inner,far+nb*inner,-nb,-na,-na,-nb,1);
            Quad(far+na*inner,far+na*outer,far+nb*outer,far+nb*inner);
            Quad(center+nb*inner,center+nb*outer,center+na*outer,center+na*inner);
        }
    }

    public void Transform(Matrix4x4 matrix)
    {
        var normalMatrix = matrix.inverse.transpose;
        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] = matrix.MultiplyPoint3x4(vertices[i]);
            normals[i] = normalMatrix.MultiplyVector(normals[i]).normalized;
        }
    }

    public Mesh Build(string name)
    {
        var mesh = new Mesh { name = name, indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds(); mesh.RecalculateTangents();
        return mesh;
    }
}
