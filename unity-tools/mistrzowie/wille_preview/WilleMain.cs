// Runs the real villa generator (BraxChampionsWille) on the stubs and dumps its meshes in pad-local nominal metres.
using System;
using System.Linq;
using UnityEngine;
public static class WilleProgram
{
    public static void Main(string[] args)
    {
        if (args.Length > 2 && args[0] == "grunt")
        {
            var d = BraxChampionsWilleDzialki.Dzialki[int.Parse(args[1]) - 1]; float x = float.Parse(args[2]);
            for (float z = float.Parse(args[3]); z <= float.Parse(args[4]); z += 1f) Console.WriteLine("GRUNT x=" + x + " z=" + z + " h=" + BraxChampionsWilleDzialki.Grunt(d, x, z).ToString("F2"));
            return;
        }
        if (args.Length > 1 && args[0] == "dzialka")
        {
            // a villa's site in world nominal metres: its ground, road, walls, fence or jetty and the villa (BraxChampionsWilleDzialki)
            var d = BraxChampionsWilleDzialki.Dzialki[int.Parse(args[1]) - 1];
            var root = new GameObject("preview sites").transform;
            var s = BraxChampionsWilleDzialki.Jedna(root, d);
            if (d.woda)
            {
                // the sea round the site (the region's sea or the far sea in the game), for the picture only
                float y = BraxChampionsWilleDzialki.SeaY * BraxClaudeOsiedle.K, k = BraxClaudeOsiedle.K;
                var sea = new GameObject("preview sea").transform; sea.SetParent(root, false);
                var b = new BraxChampionsMeshBatch();
                b.Quad(BraxChampionsHarbour.Surface("morze", new Color(.16f, .40f, .48f), .08f, 0f),
                    new Vector3(d.x0 - 400f, BraxChampionsWilleDzialki.SeaY, d.z0 - 400f), new Vector3(d.x0 - 400f, BraxChampionsWilleDzialki.SeaY, d.z1 + 400f),
                    new Vector3(d.x1 + 400f, BraxChampionsWilleDzialki.SeaY, d.z1 + 400f), new Vector3(d.x1 + 400f, BraxChampionsWilleDzialki.SeaY, d.z0 - 400f));
                b.Build(sea, "preview sea", "Assets/PlayableGenerated/Mistrzowie/Meshes", false, k);
            }
            MeshDump.Write("wille_geometry.json", root, BraxClaudeOsiedle.K);   // world (x K) back to nominal metres
            Console.WriteLine("DUMP site " + d.nr + " " + d.nazwa + " colliders=" + BraxChampionsWilleDzialki.Kolizje + " fence=" + BraxChampionsWilleDzialki.Ogrodzenie + " quads=" + Rec.Quads.Count);
            return;
        }
        int nr = args.Length > 0 ? int.Parse(args[0]) : 1;
        var parent = new GameObject("preview root").transform;
        var g = BraxChampionsWille.Willa(parent, nr, Vector3.zero, 0f);
        g.SetParent(null, false); g.localPosition = Vector3.zero; g.localRotation = Quaternion.identity; g.localScale = Vector3.one;
        MeshDump.Write("wille_geometry.json", g, 1f);
        Console.WriteLine("DUMP villa " + nr + " colliders=" + BraxChampionsWille.Kolizje + " seats=" + BraxChampionsWille.Siedziska + " quads=" + Rec.Quads.Count);
    }
}
