using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BraxPodparcieObuwia
{
    sealed class Sole
    {
        public string side;
        public int[] heel, toes, inside, outside, all;
    }

    static string BoneName(Transform joint)
    {
        string name = joint.name.Substring(joint.name.LastIndexOf(':') + 1);
        return name.StartsWith("mixamorig", StringComparison.OrdinalIgnoreCase)
            ? name.Substring(9).TrimStart('0','1','2','3','4','5','6','7','8','9','_') : name;
    }
    static float Quantile(float[] sorted, float q) => sorted[Mathf.RoundToInt((sorted.Length - 1) * q)];

    static Sole Identify(SkinnedMeshRenderer skin, Vector3[] rest, string side, Vector3 front, Vector3 right)
    {
        var allowed = new HashSet<int>(Enumerable.Range(0, skin.bones.Length)
            .Where(i => BoneName(skin.bones[i]) == side + "Foot" || BoneName(skin.bones[i]) == side + "ToeBase"));
        var weights = skin.sharedMesh.boneWeights;
        var foot = Enumerable.Range(0, rest.Length).Where(i =>
        {
            var w = weights[i];
            float total = (allowed.Contains(w.boneIndex0) ? w.weight0 : 0f) + (allowed.Contains(w.boneIndex1) ? w.weight1 : 0f)
                + (allowed.Contains(w.boneIndex2) ? w.weight2 : 0f) + (allowed.Contains(w.boneIndex3) ? w.weight3 : 0f);
            return total > .5f;
        }).ToArray();
        if (foot.Length < 15) throw new InvalidDataException("Za mało punktów obuwia: " + side);
        float[] y = foot.Select(i => rest[i].y).OrderBy(v => v).ToArray();
        var sole = foot.Where(i => rest[i].y <= Quantile(y, .28f)).ToArray();
        Vector3 outward = side == "Right" ? right : -right;
        var length = sole.Select(i => Vector3.Dot(rest[i], front)).OrderBy(v => v).ToArray();
        var width = sole.Select(i => Vector3.Dot(rest[i], outward)).OrderBy(v => v).ToArray();
        return new Sole { side = side, all = sole,
            heel = sole.Where(i => Vector3.Dot(rest[i], front) <= Quantile(length, .24f)).ToArray(),
            toes = sole.Where(i => Vector3.Dot(rest[i], front) >= Quantile(length, .76f)).ToArray(),
            inside = sole.Where(i => Vector3.Dot(rest[i], outward) <= Quantile(width, .2f)).ToArray(),
            outside = sole.Where(i => Vector3.Dot(rest[i], outward) >= Quantile(width, .8f)).ToArray() };
    }

    static Vector3 Center(int[] indices, Vector3[] vertices)
    {
        Vector3 total = Vector3.zero;
        foreach (int i in indices) total += vertices[i];
        return total / indices.Length;
    }

    static float Bottom(int[] indices, Vector3[] vertices) => Quantile(indices.Select(i => vertices[i].y).OrderBy(v => v).ToArray(), .12f);

    [MenuItem("Braxhorn/Postacie/Podparcie stóp Patryka")]
    public static void Run()
    {
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../KontrolaPodparcia"));
        Directory.CreateDirectory(output);
        var holder = new GameObject("Pomiary gracza");
        var baked = new Mesh();
        try
        {
            var actor = BraxClaudeOsiedle.PatrykBohater(holder.transform, null);
            var skins = actor.GetComponentsInChildren<SkinnedMeshRenderer>();
            var skin = skins.OrderByDescending(s => s.sharedMesh.vertexCount).First();
            var animation = actor.GetComponent<Animation>();
            Transform Joint(string n) => skin.bones.Single(b => BoneName(b) == n);
            var root = Joint("Hips");
            // Bind matrices give rest coordinates even though the character is already in Idle.
            Vector3 RestJoint(string n)
            {
                int index = Array.IndexOf(skin.bones, Joint(n));
                return skin.transform.TransformPoint(skin.sharedMesh.bindposes[index].inverse.MultiplyPoint3x4(Vector3.zero));
            }
            var right = Vector3.ProjectOnPlane(RestJoint("RightUpLeg") - RestJoint("LeftUpLeg"), Vector3.up).normalized;
            var front = Vector3.Cross(right, Vector3.up);
            if (Vector3.Dot(front, RestJoint("LeftToeBase") - RestJoint("LeftFoot")) < 0) front = -front;
            var rest = skin.sharedMesh.vertices.Select(p => skin.transform.TransformPoint(p)).ToArray();
            var soles = new[] { Identify(skin, rest, "Left", front, right), Identify(skin, rest, "Right", front, right) };
            var rows = new List<string> {"stan,faza,strona,punkty_podeszwy,min_podeszwy,pieta_wysokosc,palce_wysokosc,wewnetrzna_wysokosc,zewnetrzna_wysokosc,azymut_buta,pochylenie_buta,azymut_spoczynku,wysokosc_bioder"};
            foreach (string action in new[] { "Idle", "Walk", "Run" })
            {
                var clip = animation.GetClip(action);
                for (int i = 0; i < 64; i++)
                {
                    clip.SampleAnimation(actor, clip.length * i / 64f);
                    skin.BakeMesh(baked);
                    var verts = baked.vertices.Select(p => skin.transform.TransformPoint(p)).ToArray();
                    foreach (var s in soles)
                    {
                        var axis = Center(s.toes, verts) - Center(s.heel, verts);
                        var restAxis = Center(s.toes, rest) - Center(s.heel, rest);
                        float azimuth = Vector3.SignedAngle(front, Vector3.ProjectOnPlane(axis, Vector3.up), Vector3.up);
                        float pitch = Mathf.Atan2(axis.y, Vector3.ProjectOnPlane(axis, Vector3.up).magnitude) * Mathf.Rad2Deg;
                        float restYaw = Vector3.SignedAngle(front, Vector3.ProjectOnPlane(restAxis, Vector3.up), Vector3.up);
                        float[] values = { Bottom(s.all, verts), Bottom(s.heel, verts), Bottom(s.toes, verts),
                            Bottom(s.inside, verts), Bottom(s.outside, verts), azimuth, pitch, restYaw, root.position.y };
                        rows.Add(action + "," + i + "," + s.side + "," + s.all.Length + "," +
                            string.Join(",", values.Select(v => v.ToString("F5", CultureInfo.InvariantCulture))));
                    }
                    if (action == "Idle" ? i == 0 : i % 4 == 0)
                    {
                        BraxKontrolaPostaciT.Capture(skins, (front + right * .1f).normalized, output, "Patryk", action + "_przod", i);
                        BraxKontrolaPostaciT.Capture(skins, (right - front * .1f).normalized, output, "Patryk", action + "_bok", i);
                    }
                }
            }
            File.WriteAllLines(Path.Combine(output, "patryk_podeszwy.csv"), rows);
            Debug.Log("PODPARCIE_PATRYK zapisane próbki=" + (rows.Count - 1));
        }
        finally { Object.DestroyImmediate(baked); Object.DestroyImmediate(holder); }
    }
}
