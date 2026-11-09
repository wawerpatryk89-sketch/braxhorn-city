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
    }
}
