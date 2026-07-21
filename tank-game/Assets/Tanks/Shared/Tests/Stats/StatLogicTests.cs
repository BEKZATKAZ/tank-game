using NUnit.Framework;
using Tanks.Shared.Stats;

namespace Tanks.Shared.Tests.Stats
{
    public class StatLogicTests
    {
        private StatLogic _owner;

        [SetUp]
        public void Setup()
        {
            _owner = new StatLogic();
        }

        [TestCase(StatType.Damage, 2, 1)]
        [TestCase(StatType.Speed, 1, 0.5f)]
        [TestCase(StatType.Speed, 3.5f, 0.25f)]
        public void AddStat_Addition_AddsNumber(StatType type, float baseValue, float value)
        {
            _owner.AddAddition(type, value);
            float expected = baseValue + value;
            Assert.AreEqual(expected, _owner.GetStat(type, baseValue));
        }

        [TestCase(StatType.Damage, 2, 250)]
        [TestCase(StatType.Speed, 1, 125)]
        [TestCase(StatType.Speed, 3.5f, 50)]
        public void AddStat_Percent_AddsMultiplier(StatType type, float baseValue, float percent)
        {
            _owner.AddPercent(type, percent);

            float normalized = percent * 0.01f;
            float expected = baseValue + baseValue * normalized;

            Assert.AreEqual(expected, _owner.GetStat(type, baseValue));
        }

        [TestCase(StatType.Damage)]
        [TestCase(StatType.Speed)]
        public void Get_NoStat_ReturnsBase(StatType type)
        {
            Assert.AreEqual(2, _owner.GetStat(type, baseValue: 2));
        }

        [TestCase(StatType.Damage, 0.5f)]
        [TestCase(StatType.Speed, 10)]
        public void Tick_OneStat_ShouldRunOut(StatType type, float duration)
        {
            _owner.AddAddition(type, value: 5, duration);

            float baseValue = 1;
            float expected = 6;

            float delta = 0.05f;
            int maxTicks = (int)(duration / delta);

            for (int i = 1; i <= maxTicks; i++)
            {
                _owner.Tick(i, delta);

                Assert.AreEqual(expected, _owner.GetStat(type, baseValue),
                    "Stat ran out or was not properly registered");
            }

            _owner.Tick(maxTicks + 1, delta);
            Assert.AreEqual(1, _owner.GetStat(type, baseValue), "Stat should already run out");
        }

        [Test]
        public void Tick_MultipleStats_ShouldAddUp()
        {
            float baseValue = 1;

            // 1 + 1000% = 11
            _owner.AddPercent(StatType.Damage, percent: 1000, duration: 1);

            // 1 + 200% = 3
            _owner.AddPercent(StatType.Speed, percent: 200, duration: 1);
            _owner.Tick(1, 0.25f);

            Assert.AreEqual(11, _owner.GetStat(StatType.Damage, baseValue), "Incorrect damage");
            Assert.AreEqual(3, _owner.GetStat(StatType.Speed, baseValue), "Incorrect speed");

            _owner.AddAddition(StatType.Speed, value: 0.5f, duration: 0.4f);
            _owner.Tick(2, 0.25f);

            Assert.AreEqual(3.5f, _owner.GetStat(StatType.Speed, baseValue),
                "New stat should be added to the previous");

            _owner.Tick(3, 0.25f);

            Assert.AreEqual(3, _owner.GetStat(StatType.Speed, baseValue),
                "New stat should run out");

            _owner.Tick(4, 0.25f);

            Assert.AreEqual(1, _owner.GetStat(StatType.Speed, baseValue),
                "All stats should run out");
        }

        [Test]
        public void Tick_InfiniteDuration_NeverRunsOut()
        {
            _owner.AddPercent(StatType.Damage, percent: 10);

            for (int i = 0; i < 10000; i += 1000)
            {
                _owner.Tick(i, tickDelta: 1000);

                Assert.Greater(_owner.GetStat(StatType.Damage, 1), 1,
                    "Infinite durations should not run out");
            }
        }
    }
}
