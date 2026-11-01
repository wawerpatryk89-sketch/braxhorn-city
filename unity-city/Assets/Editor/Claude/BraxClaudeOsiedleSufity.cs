using System.Collections.Generic;
using UnityEngine;

// Sufity dla kamery na osiedlu (właściciel 1.11, 18:19: „blokada do sufitu, żeby kamera nie wychodziła nad pomieszczenie”).
// Generator: każdy czworokąt zwrócony w dół (spód stropu, daszku, balkonu) ≥ 2.2 m nad ziemią i ≥ 4 m² → płyta kolizji
// tylko dla kamery (BraxClaudeSufity, warstwa 29). Do tego siatki gotowe w scenie (glTF Codexa, siłownia A z .glb).
public static partial class BraxClaudeOsiedle
{
    static readonly List<Bounds> spodyStropow = new List<Bounds>();

    static void SpodStropu(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        var b = new Bounds(p0, Vector3.zero); b.Encapsulate(p1); b.Encapsulate(p2); b.Encapsulate(p3);
        if (b.min.y < 2.2f || b.size.x * b.size.z < 4f) return;
        spodyStropow.Add(b);
    }

    static void SufityKamery()
    {
        var root = new GameObject("BRAXHORN sufity dla kamery (osiedle)").transform;
        int a = BraxClaudeSufity.Plyty(spodyStropow, root);
        int b = BraxClaudeSufity.Renderery(root.gameObject.scene, root);
        rep.notes.Add("sufity dla kamery: " + a + " płyt ze stropów generatora, " + b + " z gotowych siatek");
    }
}
