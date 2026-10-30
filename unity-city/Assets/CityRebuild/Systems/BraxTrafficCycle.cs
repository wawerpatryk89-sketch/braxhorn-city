using System;

// Pure deterministic logic; the future traffic AI and the visible heads read
// this single authority. Times are design timings, not a road certification.
public enum BraxTrafficPhase
{
    StartupAllRed,
    VehiclesRedAmber,
    VehiclesGreen,
    VehiclesAmber,
    AllRedBeforePedestrians,
    PedestriansGreen,
    PedestrianClearance,
    AllRedBeforeVehicles
}

public sealed class BraxTrafficCycle
{
    public BraxTrafficPhase Phase { get; private set; }
    public double PhaseElapsed { get; private set; }
    public double Elapsed { get; private set; }
    public int CompletedCycles { get; private set; }
    public bool VehiclesMayProceed { get { return Phase == BraxTrafficPhase.VehiclesGreen; } }
    public bool PedestriansMayProceed { get { return Phase == BraxTrafficPhase.PedestriansGreen; } }
    public bool VehicleAmber { get { return Phase == BraxTrafficPhase.VehiclesAmber || Phase == BraxTrafficPhase.VehiclesRedAmber; } }
    public bool VehicleRed { get { return !VehiclesMayProceed && Phase != BraxTrafficPhase.VehiclesAmber; } }
    public bool PedestrianRed { get { return !PedestriansMayProceed; } }
    public double SecondsRemaining { get { return Duration(Phase) - PhaseElapsed; } }

    public BraxTrafficCycle() { Reset(); }

    public void Reset()
    {
        Phase = BraxTrafficPhase.StartupAllRed;
        PhaseElapsed = 0;
        Elapsed = 0;
        CompletedCycles = 0;
    }

    public static double Duration(BraxTrafficPhase phase)
    {
        switch (phase)
        {
            case BraxTrafficPhase.StartupAllRed: return 2;
            case BraxTrafficPhase.VehiclesRedAmber: return 1;
            case BraxTrafficPhase.VehiclesGreen: return 16;
            case BraxTrafficPhase.VehiclesAmber: return 3;
            case BraxTrafficPhase.AllRedBeforePedestrians: return 2;
            case BraxTrafficPhase.PedestriansGreen: return 9;
            case BraxTrafficPhase.PedestrianClearance: return 7;
            case BraxTrafficPhase.AllRedBeforeVehicles: return 2;
            default: throw new ArgumentOutOfRangeException("phase");
        }
    }

    public void Advance(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
            throw new ArgumentOutOfRangeException("seconds");
        Elapsed += seconds;
        PhaseElapsed += seconds;
        while (PhaseElapsed >= Duration(Phase))
        {
            PhaseElapsed -= Duration(Phase);
            switch (Phase)
            {
                case BraxTrafficPhase.StartupAllRed: Phase = BraxTrafficPhase.VehiclesRedAmber; break;
                case BraxTrafficPhase.VehiclesRedAmber: Phase = BraxTrafficPhase.VehiclesGreen; break;
                case BraxTrafficPhase.VehiclesGreen: Phase = BraxTrafficPhase.VehiclesAmber; break;
                case BraxTrafficPhase.VehiclesAmber: Phase = BraxTrafficPhase.AllRedBeforePedestrians; break;
                case BraxTrafficPhase.AllRedBeforePedestrians: Phase = BraxTrafficPhase.PedestriansGreen; break;
                case BraxTrafficPhase.PedestriansGreen: Phase = BraxTrafficPhase.PedestrianClearance; break;
                case BraxTrafficPhase.PedestrianClearance: Phase = BraxTrafficPhase.AllRedBeforeVehicles; break;
                case BraxTrafficPhase.AllRedBeforeVehicles:
                    Phase = BraxTrafficPhase.VehiclesRedAmber;
                    CompletedCycles++;
                    break;
                default: throw new InvalidOperationException("Invalid signal phase");
            }
        }
    }
}
