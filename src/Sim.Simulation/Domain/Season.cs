namespace Sim.Simulation.Domain;

public enum Season { Winter, Spring, Summer, Autumn }

public static class Seasons
{
    public static Season Of(int month) => month switch
    {
        12 or 1 or 2 => Season.Winter,
        >= 3 and <= 5 => Season.Spring,
        >= 6 and <= 8 => Season.Summer,
        _ => Season.Autumn,
    };
}
