using System;
using UnityEngine;

[Serializable]
public sealed class BraxSignalLamp
{
    public Renderer surface;
    public Material illuminated;
    public Material extinguished;

    public void Apply(bool on)
    {
        if (surface) surface.sharedMaterial = on ? illuminated : extinguished;
    }
}

public sealed class BraxCityTrafficController : MonoBehaviour
{
    public BraxCityWorld world;
    public BraxSignalLamp[] vehicleRed = Array.Empty<BraxSignalLamp>();
    public BraxSignalLamp[] vehicleAmber = Array.Empty<BraxSignalLamp>();
    public BraxSignalLamp[] vehicleGreen = Array.Empty<BraxSignalLamp>();
    public BraxSignalLamp[] pedestrianRed = Array.Empty<BraxSignalLamp>();
    public BraxSignalLamp[] pedestrianGreen = Array.Empty<BraxSignalLamp>();
    private readonly BraxTrafficCycle cycle = new BraxTrafficCycle();
    private bool suspended;

    public BraxTrafficCycle Cycle { get { return cycle; } }

    private void OnEnable()
    {
        suspended = false;
        cycle.Reset();
        RenderSignals();
    }

    private void Update()
    {
        if (suspended || (world && world.loading)) return;
        var previous = cycle.Phase;
        // Returning from a suspended browser must never skip the clearance.
        cycle.Advance(Math.Min(Time.deltaTime, .25f));
        if (previous != cycle.Phase) RenderSignals();
    }

    private void OnApplicationPause(bool paused) { Suspend(paused); }
    private void OnApplicationFocus(bool focused) { Suspend(!focused); }

    private void Suspend(bool value)
    {
        suspended = value;
        cycle.Reset();
        RenderSignals();
    }

    private void OnDisable()
    {
        cycle.Reset();
        RenderSignals();
    }

    private static void Apply(BraxSignalLamp[] lamps, bool on)
    {
        foreach (var lamp in lamps) if (lamp != null) lamp.Apply(on);
    }

    private void RenderSignals()
    {
        Apply(vehicleRed, cycle.VehicleRed);
        Apply(vehicleAmber, cycle.VehicleAmber);
        Apply(vehicleGreen, cycle.VehiclesMayProceed);
        Apply(pedestrianRed, cycle.PedestrianRed);
        Apply(pedestrianGreen, cycle.PedestriansMayProceed);
    }
}
