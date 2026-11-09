// Runs the real building generator (A1 arena: exterior shell from Exteriors.bytes, hall bowl, lobby, signs, murals)
// on the stubs and dumps the whole A1 building in building-local nominal metres (hall centre 0,0; public front on -Z).
using System;
using System.Linq;
using UnityEngine;
public static class ArenaProgram
{
    public static void Main(string[] args)
    {
        Rec.Clear();
        Physics.PreviewGroundY = .245f * BraxClaudeOsiedle.K;      // the arena forecourt / lobby floor every grounding ray finds
        BraxChampionsBuildings.Generate();
        var root = GameObject.Registry.First(g => g.transform.parent == null && g.name.StartsWith("A1 | ")).transform;
        // LOD1 repeats the shell in low detail on the same spot: the preview draws LOD0 only
        bool Lod1(GameObject g) { for (var t = g.transform; t != null; t = t.parent) if (t.gameObject.name == "LOD1") return true; return false; }
        MeshDump.Write("arena_geometry.json", root, BraxClaudeOsiedle.K, Lod1);
    }
}
