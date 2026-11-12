using NUnit.Framework;
using Tanks.Core.Players;

namespace Tanks.Core.Tests.Debriefing
{
    public sealed class PlayerLevelServiceTests
    {
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        public void Boundary_AdvancesAtThresholdAndRetainsRemainder(int offset)
        {
            PlayerLevel initial = new PlayerLevel { Value = 4 };
            int required = new PlayerLevelService().RequiredXP(initial.Value);

            PlayerLevel actual = new PlayerLevelService().AddXP(initial,
                required + offset);

            Assert.That(actual.Value,
                Is.EqualTo(initial.Value + (offset >= 0 ? 1 : 0)));

            Assert.That(actual.XP,
                Is.EqualTo(offset >= 0 ? offset : required + offset));
        }

        [Test]
        public void MultipleLevels_UsesEachNewThreshold()
        {
            PlayerLevel initial = new PlayerLevel { Value = 1 };

            int award = 0;

            for (int level = initial.Value; level < initial.Value + 4; level++)
            {
                award += new PlayerLevelService().RequiredXP(level);
            }

            int excess = new PlayerLevelService().RequiredXP(initial.Value + 4) / 3;

            PlayerLevel actual = new PlayerLevelService().AddXP(initial,
                award + excess);

            Assert.That(actual.Value, Is.EqualTo(initial.Value + 4));
            Assert.That(actual.XP, Is.EqualTo(excess));
        }
    }
}
