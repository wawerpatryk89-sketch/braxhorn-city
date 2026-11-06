using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Fast NON-BLOCKING collision inventory before the oboz APK.
// Does not disable colliders, change transforms, build NavMesh or claim play
// testing. Bounds overlaps are CANDIDATES for later device review, not defects.
public static class BraxObozCollisionReview
{
    [Serializable] public sealed class DoorwayReview
    {
        public string name, position;
        public int possibleBlockers;
        public string[] colliderCandidates;
    }

    [Serializable] public sealed class Report
    {
        public string status = "ADVISORY_ONLY__DEVICE_REVIEW_REQUIRED";
        public bool approved = false, playModeTested = false, phoneTested = false;
        public string sourceCommit, generatedUtc;
        public int enabledColliders, boxColliders, capsuleColliders, meshColliders, doorways;
        public int possibleDoorwayBlockers;
        public string[] warnings;
        public DoorwayReview[] doors;
    }

    public static void Write(BraxObozSceneInfo scene, string destination)
    {
        if (!scene) throw new ArgumentNullException(nameof(scene));
        const float K = 1.294f;
        var colliders = scene.GetComponentsInChildren<Collider>(true)
            .Where(c => c && c.enabled && c.gameObject.activeInHierarchy &&
                c.gameObject.layer != 8).ToArray();
        var report = new Report {
            sourceCommit = Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "local",
            generatedUtc = DateTime.UtcNow.ToString("o"),
            enabledColliders = colliders.Length,
            boxColliders = colliders.Count(c => c is BoxCollider),
            capsuleColliders = colliders.Count(c => c is CapsuleCollider),
            meshColliders = colliders.Count(c => c is MeshCollider)
        };
        var warnings = new List<string> {
            "Kolizje nie zostały wyłączone. Raport nie zatrzymuje kompilacji APK.",
            "Bounds to tylko wskazówki: przejścia, drzwi, bieżnia, autokar i NavMesh wymagają testu Play Mode/Android."
        };
        var doors = scene.GetComponentsInChildren<BraxCityDoor>(true);
        report.doorways = doors.Length;
        var results = new List<DoorwayReview>();
        foreach (var door in doors)
        {
            if (!door) continue;
            var pivot = door.transform;
            var access = new Bounds(pivot.position + Vector3.up * (.93f*K),
                new Vector3(.80f*K,1.42f*K,.80f*K));
            // The door's own moving leaf is expected to overlap at rest.
            // Ground, pavement, rings and road are intentionally underfoot:
            // disregard low/very large colliders in doorway candidates.
            var candidates = colliders.Where(c =>
                !c.transform.IsChildOf(pivot) &&
                !pivot.IsChildOf(c.transform) &&
                c.bounds.size.sqrMagnitude > 0 &&
                c.bounds.max.y > access.min.y + .12f*K &&
                c.bounds.size.x < 32f*K && c.bounds.size.z < 32f*K &&
                c.bounds.Intersects(access))
                .Select(c=>c.name+" ["+c.GetType().Name+"]")
                .Distinct().Take(12).ToArray();
            results.Add(new DoorwayReview {
                name=door.name,
                position=pivot.position.ToString("F2"),
                possibleBlockers=candidates.Length,
                colliderCandidates=candidates
            });
        }
        report.doors = results.ToArray();
        report.possibleDoorwayBlockers = results.Sum(x=>x.possibleBlockers);
        if (results.Any(x=>x.possibleBlockers>0))
            warnings.Add("Możliwe przeszkody przy drzwiach: "+
                string.Join("; ",results.Where(x=>x.possibleBlockers>0)
                    .Select(x=>x.name+"="+x.possibleBlockers).ToArray()));
        report.warnings=warnings.ToArray();
        var folder=Path.GetDirectoryName(destination);
        if (string.IsNullOrWhiteSpace(folder))
            throw new ArgumentException("Collision report output directory missing",nameof(destination));
        Directory.CreateDirectory(folder);
        File.WriteAllText(destination,JsonUtility.ToJson(report,true));
        Debug.Log("BRAXHORN_OBOZ_COLLIDER_REVIEW: colliders="+report.enabledColliders+
            " doors="+report.doorways+" possibleDoorwayBlockers="+report.possibleDoorwayBlockers+
            " report="+destination+" APK_BUILD_UNBLOCKED=true phoneTested=false");
    }
}
