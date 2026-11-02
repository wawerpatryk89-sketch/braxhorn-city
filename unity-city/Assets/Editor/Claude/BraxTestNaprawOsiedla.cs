using System;
using System.Collections.Generic;
using UnityEngine;

// Regression checks run in the real Unity editor before estate generation.
// A failed check aborts generation; these do not replace gameplay/device validation.
public static class BraxTestNaprawOsiedla
{
    static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException("Regresja osiedla: " + message); }

    static float Area(Mesh m)
    {
        var p = m.vertices; float area = 0;
        for (int sub = 0; sub < m.subMeshCount; sub++)
        {
            var t = m.GetTriangles(sub);
            for (int i = 0; i < t.Length; i += 3) area += Vector3.Cross(p[t[i+1]]-p[t[i]], p[t[i+2]]-p[t[i]]).magnitude * .5f;
        }
        return area;
    }
    public static void Wykonaj()
    {
        Require(BraxAuto.KatKola(0,3,.8f)==0, "koła prosto");
        Require(BraxAuto.KatKola(25,3,.8f)>BraxAuto.KatKola(25,3,-.8f), "wewnętrzne koło skręca mocniej");
        Require(Mathf.Abs(BraxAuto.KatKola(25,3,.8f)+BraxAuto.KatKola(-25,3,-.8f))<.0001f, "symetria skrętu");
        var traffic = new BraxRuchAutSym(null,null,null,null,1);
        traffic.zewnetrzne.Add(new BraxRuchAutSym.ObrysZewnetrzny { srodek=Vector3.zero, przod=Vector3.forward,polSzer=1.2f,polDl=3f });
        Require(traffic.ZajeteZewnetrznie(new Vector3(0,0,4),Vector3.forward,1,2),"zderzak auta gracza");
        Require(traffic.ZajeteZewnetrznie(new Vector3(2,0,0),Vector3.right,1,2),"auto gracza stojące w poprzek");
        Require(!traffic.ZajeteZewnetrznie(new Vector3(6,0,0),Vector3.forward,1,2),"wolny sąsiedni pas");
        var mesh = new Mesh { name = "test starej nawierzchni", subMeshCount = 2 };
        Mesh cut = null, empty = null;
        try
        {
            mesh.vertices = new[] { new Vector3(-2,0,-2), new Vector3(-2,0,2), new Vector3(2,0,2), new Vector3(2,0,-2) };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.uv = new[] { new Vector2(-2,-2), new Vector2(-2,2), new Vector2(2,2), new Vector2(2,-2) };
            mesh.SetTriangles(new[] {0,1,2}, 0); mesh.SetTriangles(new[] {0,2,3}, 1);
            // All original vertices are outside this strip. The old full-containment test removed nothing.
            var strip = new Bounds(Vector3.zero, new Vector3(2,2,6));
            cut = BraxWycinanieSiatki.Wytnij(mesh, Matrix4x4.identity, new[] { strip }, out int changed);
            Require(cut && changed == 2, "trójkąty przecinające drogę");
            Require(Mathf.Abs(Area(cut)-8) < .0001f && Mathf.Abs(Area(mesh)-16) < .0001f, "pole wycięcia / nienaruszone źródło");
            Require(cut.subMeshCount == 2, "materiały po wycięciu");
            var vertices = cut.vertices; var uv = cut.uv;
            Require(uv.Length == vertices.Length && cut.normals.Length == vertices.Length, "kanały wierzchołków");
            for (int i = 0; i < vertices.Length; i++)
                Require((uv[i]-new Vector2(vertices[i].x,vertices[i].z)).sqrMagnitude < 1e-8f, "interpolacja UV");
            empty = BraxWycinanieSiatki.Wytnij(mesh, Matrix4x4.identity, new[] { new Bounds(Vector3.zero, Vector3.one * 10) }, out changed);
            Require(empty && empty.vertexCount == 0 && empty.subMeshCount == 2, "całkowite wycięcie / puste materiały");
            var unchanged = BraxWycinanieSiatki.Wytnij(mesh, Matrix4x4.Translate(Vector3.right*100), new[] {strip}, out changed);
            Require(!unchanged && changed == 0, "wycięcie w współrzędnych świata");
            var parts = BraxWycinanieSiatki.Odejmij(new Bounds(Vector3.zero,new Vector3(4,2,1)), new[] {strip});
            float volume = 0; foreach (var b in parts) volume += b.size.x*b.size.y*b.size.z;
            Require(parts.Count == 2 && Mathf.Abs(volume-4) < .0001f, "kolizja długiego płotu poza jezdnią");
            Debug.Log("BRAXHORN_REGRESJA wycinanie: PASS (granice, pole, UV, submeshes, pusty wynik, transform, kolizje)");
        }
        finally { UnityEngine.Object.DestroyImmediate(mesh); if (cut) UnityEngine.Object.DestroyImmediate(cut); if (empty) UnityEngine.Object.DestroyImmediate(empty); }
    }
}
