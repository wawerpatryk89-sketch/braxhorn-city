// Runs the real casino T11 generator on the stubs and dumps its meshes (root-local nominal metres).
using System;
using System.Linq;
using UnityEngine;
public static class CasinoProgram
{
    public static void Main(string[] args)
    {
        BraxChampionsCasinoT11.Generate("out");
        var root = GameObject.Registry.First(g => g.transform.parent == null && g.name.StartsWith("T11 | KASYNO WAWER")).transform;
        MeshDump.Write("casino_geometry.json", root, BraxClaudeOsiedle.K);
        // the games' places (root-local nominal metres): the close-up views of the seat-play screen are worked out from these
        // (BraxChampionsStanowisko: the machine's face, the felt over a chair) - casino_stanowiska.json
        var sb = new System.Text.StringBuilder("[");
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        foreach (var g in GameObject.Registry)
        {
            string n = g.name ?? "";
            if (!(n.StartsWith("automat ") || n.StartsWith("blackjack ") || n.StartsWith("bakarat ") || n.StartsWith("casino hold'em ") || n.StartsWith("ruletka ")
                  || n.StartsWith("SIC BO |") || n.StartsWith("KOŁO FORTUNY |") || n.StartsWith("Siedzisko: ") || n == "kopuła | dno" || n == "obrót")) continue;
            if (!g.transform.IsChildOf(root)) continue;
            var p = root.InverseTransformPoint(g.transform.position) / BraxClaudeOsiedle.K; var f = g.transform.rotation * Vector3.forward;      // nominal, as the mesh dump
            if (sb.Length > 1) sb.Append(",\n");
            sb.Append(string.Format(ci, "{{\"name\":\"{0}\",\"x\":{1:F4},\"y\":{2:F4},\"z\":{3:F4},\"fx\":{4:F4},\"fy\":{5:F4},\"fz\":{6:F4}}}", n.Replace("\"", "'"), p.x, p.y, p.z, f.x, f.y, f.z));
        }
        System.IO.File.WriteAllText("casino_stanowiska.json", sb.Append("]").ToString());
    }
}
