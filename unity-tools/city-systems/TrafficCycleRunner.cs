using System;

public static class TrafficCycleRunner
{
    public static int Main()
    {
        try { Console.WriteLine("PASS traffic cycle contract checks: " + BraxTrafficCycleChecks.Run()); return 0; }
        catch(Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
