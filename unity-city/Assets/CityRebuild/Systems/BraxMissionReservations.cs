using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class BraxReservationAnchor
{
    public string id, label;
    public Vector3 position;
    public float yaw;
    public bool requireExisting;
}

[Serializable]
public sealed class BraxMissionReservation
{
    public string id, type, anchorId, scene, module;
    public Vector3 areaSize;
    public float yaw;
    public string[] accessPointIds, exitPointIds, relatedPointIds;
    public Vector3[] futureCharacterPositions;
    public Vector3 cameraPosition;
    public string cameraConditions, status;
}

[Serializable]
public sealed class BraxCitySystemsDocument
{
    public string coordinateSystem, sourceCommit, scope;
    public BraxReservationAnchor[] anchors;
    public BraxMissionReservation[] reservations;
}

// Metadata references the existing world point registry. No mission, trigger,
// enemy, barrier or game-visible marker is instantiated by a reservation.
public sealed class BraxMissionReservations : MonoBehaviour
{
    public BraxCityWorld world;
    public BraxMissionReservation[] reservations = Array.Empty<BraxMissionReservation>();
    private readonly Dictionary<string, BraxCityPoint> anchors = new Dictionary<string, BraxCityPoint>();

    private void Awake()
    {
        if (!world) throw new InvalidOperationException("Missing city point registry");
        anchors.Clear();
        foreach (var point in world.points)
        {
            if (anchors.ContainsKey(point.id)) throw new InvalidOperationException("Duplicate city point: " + point.id);
            anchors.Add(point.id, point);
        }
        var ids = new HashSet<string>();
        foreach (var reserve in reservations)
        {
            if (!ids.Add(reserve.id) || !anchors.ContainsKey(reserve.anchorId))
                throw new InvalidOperationException("Invalid reservation link: " + reserve.id);
        }
    }

    public bool TryGetAnchor(string reservationId, out BraxCityPoint point)
    {
        foreach (var reserve in reservations)
            if (reserve.id == reservationId) return anchors.TryGetValue(reserve.anchorId, out point);
        point = null;
        return false;
    }
}
