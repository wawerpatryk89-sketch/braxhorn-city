using System;
using System.Runtime.InteropServices;
using UnityEngine;

public sealed class BraxCitySystemsTelemetry : MonoBehaviour
{
    public BraxCityWorld world;
    public BraxCityTrafficController traffic;
    public BraxMissionReservations catalogue;
    private float nextSample;
    private int sampleStartFrame;
    private float sampleStartTime;
    private readonly Snapshot snapshot = new Snapshot();

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void BXCitySystemsState(string json);
#endif

    [Serializable]
    private sealed class Snapshot
    {
        public string build, area, trafficPhase;
        public bool vehicleRed, vehicleAmber, vehicleGreen, pedestrianRed, pedestrianGreen;
        public double secondsRemaining, signalElapsed;
        public int completedSignalCycles, reservationCount;
        public float observedFramesPerSecond;
        public long managedMemoryBytes;
        public Vector3 player;
        public string[] reservationIds;
        public Vector3[] reservationPositions;
    }

    private void Start()
    {
        snapshot.reservationIds = new string[catalogue.reservations.Length];
        snapshot.reservationPositions = new Vector3[catalogue.reservations.Length];
        for (int i = 0; i < catalogue.reservations.Length; i++)
        {
            var reserve = catalogue.reservations[i];
            snapshot.reservationIds[i] = reserve.id;
            if (!catalogue.TryGetAnchor(reserve.id, out var point)) throw new InvalidOperationException("Missing reserve " + reserve.id);
            snapshot.reservationPositions[i] = point.position;
        }
        snapshot.reservationCount = catalogue.reservations.Length;
        sampleStartFrame = Time.frameCount;
        sampleStartTime = Time.unscaledTime;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextSample) return;
        nextSample = Time.unscaledTime + .2f;
        var state = traffic.Cycle;
        snapshot.build = world.player.buildId;
        snapshot.area = world.area;
        snapshot.player = world.player.transform.position;
        snapshot.trafficPhase = state.Phase.ToString();
        snapshot.vehicleRed = state.VehicleRed;
        snapshot.vehicleAmber = state.VehicleAmber;
        snapshot.vehicleGreen = state.VehiclesMayProceed;
        snapshot.pedestrianRed = state.PedestrianRed;
        snapshot.pedestrianGreen = state.PedestriansMayProceed;
        snapshot.secondsRemaining = state.SecondsRemaining;
        snapshot.signalElapsed = state.Elapsed;
        snapshot.completedSignalCycles = state.CompletedCycles;
        float interval = Time.unscaledTime - sampleStartTime;
        snapshot.observedFramesPerSecond = interval > .001f ? (Time.frameCount - sampleStartFrame) / interval : 0;
        snapshot.managedMemoryBytes = GC.GetTotalMemory(false);
        sampleStartFrame = Time.frameCount;
        sampleStartTime = Time.unscaledTime;
#if UNITY_WEBGL && !UNITY_EDITOR
        BXCitySystemsState(JsonUtility.ToJson(snapshot));
#endif
    }
}
