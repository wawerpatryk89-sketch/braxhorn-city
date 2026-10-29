using System;

// This file deliberately has no Unity dependency: it is also compiled with
// BraxTrafficCycle.cs by the standalone test runner in unity-tools/city-systems.
public static class BraxTrafficCycleChecks
{
    private static void Require(bool value,string name)
    {
        if(!value) throw new InvalidOperationException("Signal test failed: "+name);
    }

    public static int Run()
    {
        int checks=0;
        var c=new BraxTrafficCycle();
        Require(c.VehicleRed && c.PedestrianRed && !c.VehicleAmber,"safe startup");checks++;
        c.Advance(1.999);
        Require(c.Phase==BraxTrafficPhase.StartupAllRed,"startup clearance must finish");checks++;
        c.Reset();c.Advance(2);
        Require(c.Phase==BraxTrafficPhase.VehiclesRedAmber && c.VehicleRed && c.VehicleAmber && c.PedestrianRed,"red amber start");checks++;
        c.Advance(1);
        Require(c.VehiclesMayProceed && c.PedestrianRed && !c.VehicleRed,"vehicles green");checks++;
        c.Advance(16);
        Require(c.VehicleAmber && !c.VehicleRed && c.PedestrianRed,"vehicles amber");checks++;
        c.Advance(3);
        Require(c.VehicleRed && c.PedestrianRed && c.Phase==BraxTrafficPhase.AllRedBeforePedestrians,"conflict clearance");checks++;
        c.Advance(2);
        Require(c.VehicleRed && c.PedestriansMayProceed,"pedestrians green");checks++;
        c.Advance(9);
        Require(c.VehicleRed && c.PedestrianRed && c.Phase==BraxTrafficPhase.PedestrianClearance,"pedestrian clearance starts");checks++;
        c.Advance(6.999);
        Require(!c.VehiclesMayProceed && !c.VehicleAmber,"pedestrian clearance cannot be shortened");checks++;
        c.Reset();c.Advance(40);
        Require(c.Phase==BraxTrafficPhase.AllRedBeforeVehicles,"final all red");checks++;
        c.Advance(2);
        Require(c.Phase==BraxTrafficPhase.VehiclesRedAmber && c.CompletedCycles==1,"one complete cycle");checks++;
        c.Reset();
        for(int i=0;i<100000;i++)
        {
            c.Advance(.01);
            Require(!(c.VehiclesMayProceed && c.PedestriansMayProceed),"no conflicting greens");
            Require(c.PedestrianRed != c.PedestriansMayProceed,"pedestrian signal is unambiguous");
            Require(c.VehicleRed || c.VehicleAmber || c.VehiclesMayProceed,"vehicles never have an unlit head");
            Require(c.SecondsRemaining>0 && c.SecondsRemaining<=BraxTrafficCycle.Duration(c.Phase),"phase time is bounded");
        }
        checks+=4;
        var random=new Random(7319);
        for(int run=0;run<64;run++)
        {
            var whole=new BraxTrafficCycle();var sliced=new BraxTrafficCycle();double elapsed=0;
            for(int step=0;step<200;step++){double delta=.001+random.NextDouble()*.8;elapsed+=delta;sliced.Advance(delta);}
            whole.Advance(elapsed);
            Require(whole.Phase==sliced.Phase && whole.CompletedCycles==sliced.CompletedCycles && Math.Abs(whole.PhaseElapsed-sliced.PhaseElapsed)<.000001,"frame independent timing");
        }
        checks++;
        foreach(double bad in new[]{-1,double.NaN,double.PositiveInfinity,double.NegativeInfinity})
        {
            c.Reset();bool rejected=false;
            try{c.Advance(bad);}catch(ArgumentOutOfRangeException){rejected=true;}
            Require(rejected && c.Phase==BraxTrafficPhase.StartupAllRed && c.Elapsed==0,"invalid delta cannot mutate signal state");
        }
        checks++;
        c.Advance(25);c.Reset();
        Require(c.VehicleRed && c.PedestrianRed && c.Elapsed==0 && c.CompletedCycles==0,"resume reset returns to all red");checks++;
        return checks;
    }
}
