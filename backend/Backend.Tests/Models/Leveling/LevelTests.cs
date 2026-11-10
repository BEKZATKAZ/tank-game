using Backend.Models.Leveling;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Backend.Tests.Models.Leveling;

public sealed class LevelTests
{
    [Test]
    public void NewLevel_StartsAtOneWithoutXP()
    {
        Level level = new();

        Assert.Multiple(() =>
        {
            Assert.That(level.Value, Is.EqualTo(1));
            Assert.That(level.XP, Is.Zero);
            Assert.That(level.RequiredXP, Is.Positive);
            Assert.That(level.RequiredXP % 10, Is.Zero);
        });
    }

    [Test]
    public void NegativeXP_ThrowsWithoutChangingProgress()
    {
        Level level = new();
        level.AddXP(level.RequiredXP / 2);
        int previousValue = level.Value;
        int previousXP = level.XP;

        Assert.Throws<ArgumentOutOfRangeException>(() => level.AddXP(-1));

        Assert.Multiple(() =>
        {
            Assert.That(level.Value, Is.EqualTo(previousValue));
            Assert.That(level.XP, Is.EqualTo(previousXP));
        });
    }

    [Test]
    public void ZeroXP_PreservesProgress()
    {
        Level level = new();
        level.AddXP(level.RequiredXP / 2);
        int previousValue = level.Value;
        int previousXP = level.XP;
        level.AddXP(0);

        Assert.Multiple(() =>
        {
            Assert.That(level.Value, Is.EqualTo(previousValue));
            Assert.That(level.XP, Is.EqualTo(previousXP));
        });
    }

    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(1)]
    public void ThresholdBoundary_AdvancesAndRetainsExcess(int offset)
    {
        Level level = new();
        int initialValue = level.Value;
        int required = level.RequiredXP;
        level.AddXP(required + offset);

        Assert.Multiple(() =>
        {
            Assert.That(level.Value, Is.EqualTo(initialValue + (offset >= 0 ? 1 : 0)));
            Assert.That(level.XP, Is.EqualTo(offset >= 0 ? offset : required + offset));
        });
    }

    [Test]
    public void SeparateAwards_AccumulateUntilThreshold()
    {
        Level level = new();
        int initialValue = level.Value;
        int required = level.RequiredXP;
        int first = required / 3;
        level.AddXP(first);
        level.AddXP(required - first - 1);

        Assert.Multiple(() =>
        {
            Assert.That(level.Value, Is.EqualTo(initialValue));
            Assert.That(level.XP, Is.EqualTo(required - 1));
        });

        level.AddXP(1);

        Assert.Multiple(() =>
        {
            Assert.That(level.Value, Is.EqualTo(initialValue + 1));
            Assert.That(level.XP, Is.Zero);
        });
    }

    [Test]
    public void SingleAward_CrossesSeveralIncreasingThresholds()
    {
        Level expected = new();
        int award = 0;

        for (int i = 0; i < 4; i++)
        {
            int required = expected.RequiredXP;
            award += required;
            expected.AddXP(required);
            Assert.That(expected.RequiredXP, Is.GreaterThanOrEqualTo(required));
        }

        int excess = expected.RequiredXP / 2;
        Level actual = new();
        actual.AddXP(award + excess);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Value, Is.EqualTo(expected.Value));
            Assert.That(actual.XP, Is.EqualTo(excess));
            Assert.That(actual.RequiredXP, Is.EqualTo(expected.RequiredXP));
        });
    }

    [Test]
    public void Serialization_PreservesProgressAndExcludesComputedThreshold()
    {
        Level level = new();
        level.AddXP(level.RequiredXP);
        level.AddXP(level.RequiredXP / 2);
        string json = JsonConvert.SerializeObject(level);
        JObject stored = JObject.Parse(json);
        Level restored = JsonConvert.DeserializeObject<Level>(json)!;

        Assert.Multiple(() =>
        {
            Assert.That(stored.Properties().Select(property => property.Name),
                Is.EquivalentTo(new[] { nameof(Level.Value), nameof(Level.XP) }));

            Assert.That(restored.Value, Is.EqualTo(level.Value));
            Assert.That(restored.XP, Is.EqualTo(level.XP));
            Assert.That(restored.RequiredXP, Is.EqualTo(level.RequiredXP));
        });

        int remaining = restored.RequiredXP - restored.XP;
        restored.AddXP(remaining);

        Assert.Multiple(() =>
        {
            Assert.That(restored.Value, Is.EqualTo(level.Value + 1));
            Assert.That(restored.XP, Is.Zero);
        });
    }
}
