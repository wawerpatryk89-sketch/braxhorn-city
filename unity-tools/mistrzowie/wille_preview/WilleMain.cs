// Runs the real villa generator (BraxChampionsWille) on the stubs and dumps its meshes in pad-local nominal metres.
using System;
using System.Linq;
using UnityEngine;
public static class WilleProgram
{
    public static void Main(string[] args)
    {
        int nr = args.Length > 0 ? int.Parse(args[0]) : 1;
        var parent = new GameObject("preview root").transform;
        var g = BraxChampionsWille.Willa(parent, nr, Vector3.zero, 0f);
        g.SetParent(null, false); g.localPosition = Vector3.zero; g.localRotation = Quaternion.identity; g.localScale = Vector3.one;
        MeshDump.Write("wille_geometry.json", g, 1f);
        Console.WriteLine("DUMP villa " + nr + " colliders=" + BraxChampionsWille.Kolizje + " seats=" + BraxChampionsWille.Siedziska + " quads=" + Rec.Quads.Count);
    }
}
