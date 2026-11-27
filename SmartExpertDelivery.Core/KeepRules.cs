namespace SmartExpertDelivery.Core;

public sealed class KeepOptions
{
    public bool LowerJobs { get; set; } = true;
    public bool SameLevel { get; set; }
    public bool BestOnly { get; set; } = true;
    public bool LockedJobs { get; set; }
    public bool TomeGear { get; set; }
    public bool Materia { get; set; } = true;
    public bool Spiritbond { get; set; } = true;
    public bool Gearsets { get; set; } = true;
}

public readonly record struct JobLevel(int Level, int StartingLevel)
{
    public bool Locked => this.Level == 0;
}

public sealed record Gear(int EquipLevel, IReadOnlyList<JobLevel> Jobs, bool IsTome, bool HasMateria, bool InGearset, bool Outclassed = false, bool Spiritbonded = false);

public enum KeepReason
{
    None,
    Gearset,
    Materia,
    Spiritbond,
    Tome,
    LowerJob,
    LockedJob,
}

public static class KeepRules
{
    public static KeepReason Check(Gear gear, KeepOptions options)
    {
        if (options.Gearsets && gear.InGearset)
            return KeepReason.Gearset;
        if (options.Materia && gear.HasMateria)
            return KeepReason.Materia;
        if (options.Spiritbond && gear.Spiritbonded)
            return KeepReason.Spiritbond;
        if (options.TomeGear && gear.IsTome)
            return KeepReason.Tome;
        if (options.BestOnly && gear.Outclassed)
            return KeepReason.None;
        if (options.LowerJobs && gear.Jobs.Any(j => !j.Locked && StillNeeds(j.Level, gear.EquipLevel, options.SameLevel)))
            return KeepReason.LowerJob;
        if (options.LockedJobs && gear.Jobs.Any(j => j.Locked && j.StartingLevel <= gear.EquipLevel))
            return KeepReason.LockedJob;
        return KeepReason.None;
    }

    public static HashSet<int> ListedCopiesGearsetsNeed(IReadOnlyDictionary<uint, int> needed, IReadOnlyList<uint> owned, IReadOnlyList<uint> listed)
    {
        var covered = new Dictionary<uint, int>();
        foreach (var id in owned)
            covered[id] = covered.GetValueOrDefault(id) + 1;
        foreach (var id in listed)
            covered[id] = covered.GetValueOrDefault(id) - 1;

        var kept = new HashSet<int>();
        for (var i = 0; i < listed.Count; i++)
        {
            var id = listed[i];
            if (covered.GetValueOrDefault(id) >= needed.GetValueOrDefault(id))
                continue;
            covered[id] = covered.GetValueOrDefault(id) + 1;
            kept.Add(i);
        }

        return kept;
    }

    private static bool StillNeeds(int jobLevel, int equipLevel, bool sameLevel) =>
        jobLevel < equipLevel || (sameLevel && jobLevel == equipLevel);
}
