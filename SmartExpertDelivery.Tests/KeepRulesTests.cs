using SmartExpertDelivery.Core;
using Xunit;

namespace SmartExpertDelivery.Tests;

public class KeepRulesTests
{
    private static readonly KeepOptions Off = new() { LowerJobs = false, Materia = false, Gearsets = false };

    private static Gear Make(int equipLevel, params JobLevel[] jobs) => new(equipLevel, jobs, false, false, false);

    [Fact]
    public void DeliversGearEveryJobHasOutgrown() =>
        Assert.Equal(KeepReason.None, KeepRules.Check(Make(47, new(51, 1), new(56, 1)), new KeepOptions()));

    [Fact]
    public void KeepsGearALowerJobCanGrowInto() =>
        Assert.Equal(KeepReason.LowerJob, KeepRules.Check(Make(50, new(100, 1), new(49, 1)), new KeepOptions()));

    [Theory]
    [InlineData(false, KeepReason.None)]
    [InlineData(true, KeepReason.LowerJob)]
    public void SameLevelGearFollowsTheOption(bool sameLevel, KeepReason expected) =>
        Assert.Equal(expected, KeepRules.Check(Make(100, new JobLevel(100, 1)), new KeepOptions { SameLevel = sameLevel }));

    [Fact]
    public void LockedJobsAreIgnoredByDefault() =>
        Assert.Equal(KeepReason.None, KeepRules.Check(Make(47, new(51, 1), new(0, 1)), new KeepOptions()));

    [Theory]
    [InlineData(50, KeepReason.None)]
    [InlineData(60, KeepReason.LockedJob)]
    public void LockedJobsCountFromTheirStartingLevel(int equipLevel, KeepReason expected) =>
        Assert.Equal(expected, KeepRules.Check(Make(equipLevel, new JobLevel(0, 60)), new KeepOptions { LockedJobs = true }));

    [Fact]
    public void TomeMateriaAndGearsetEachKeepOnTheirOwn()
    {
        var jobs = new[] { new JobLevel(100, 1) };
        Assert.Equal(KeepReason.Tome, KeepRules.Check(new Gear(50, jobs, true, false, false), new KeepOptions { TomeGear = true }));
        Assert.Equal(KeepReason.Materia, KeepRules.Check(new Gear(50, jobs, false, true, false), new KeepOptions()));
        Assert.Equal(KeepReason.Gearset, KeepRules.Check(new Gear(50, jobs, false, false, true), new KeepOptions()));
    }

    [Theory]
    [InlineData(true, KeepReason.None)]
    [InlineData(false, KeepReason.LowerJob)]
    public void OutclassedGearIsOnlyKeptForJobsWhenBestOnlyIsOff(bool bestOnly, KeepReason expected)
    {
        var jobs = new[] { new JobLevel(49, 1) };
        Assert.Equal(expected, KeepRules.Check(new Gear(100, jobs, false, false, false, Outclassed: true), new KeepOptions { BestOnly = bestOnly }));
        Assert.Equal(KeepReason.Gearset, KeepRules.Check(new Gear(100, jobs, false, false, true, Outclassed: true), new KeepOptions()));
    }

    [Fact]
    public void NothingIsKeptWithEveryOptionOff() =>
        Assert.Equal(KeepReason.None, KeepRules.Check(new Gear(50, [new(1, 1), new(0, 1)], true, true, true), Off));
}
