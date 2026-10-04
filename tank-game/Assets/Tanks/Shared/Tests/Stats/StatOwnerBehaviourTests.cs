using NUnit.Framework;
using System;
using Tanks.Shared.Stats;
using UnityEngine;

namespace Tanks.Shared.Tests.Stats
{
    public class StatOwnerBehaviourTests
    {
        private StatOwnerBehaviour _owner;

        [SetUp]
        public void Setup()
        {
            _owner = new GameObject().AddComponent<StatOwnerBehaviour>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_owner) UnityEngine.Object.DestroyImmediate(_owner.gameObject);
        }

        [Test]
        public void OverrideStat_DuplicateId_DoesNotThrow()
        {
            StatId id1 = new StatId(1);
            StatId id2 = new StatId(2);

            _owner.OverrideStat(id1, StatType.Damage, StatValueType.Flat, value: 5);
            _owner.OverrideStat(id2, StatType.Damage, StatValueType.Flat, value: 3);

            Assert.That(
                _owner.GetStat(StatType.Damage, baseValue: 0),
                Is.EqualTo(8));

            _owner.OverrideStat(id1, StatType.Damage, StatValueType.Flat, value: 1);

            Assert.That(
                _owner.GetStat(StatType.Damage, baseValue: 0),
                Is.EqualTo(4));
        }

        [TestCase(StatType.Damage, 2, 1)]
        [TestCase(StatType.Speed, 5, 0.5f)]
        [TestCase(StatType.Defense, 3.5f, 0.25f)]
        public void AddStat_Addition_AddsNumber(StatType type, float baseValue, float value)
        {
            _owner.AddStat(type, StatValueType.Flat, value);
            float expected = baseValue + value;
            Assert.AreEqual(expected, _owner.GetStat(type, baseValue));
        }

        [TestCase(StatType.Damage, 2, 250)]
        [TestCase(StatType.Speed, 6, 125)]
        [TestCase(StatType.Defense, 3.5f, 50)]
        public void AddStat_Percent_AddsMultiplier(StatType type, float baseValue, float percent)
        {
            _owner.AddStat(type, StatValueType.Percent, percent);

            float normalized = percent * 0.01f;
            float expected = baseValue + baseValue * normalized;

            Assert.AreEqual(expected, _owner.GetStat(type, baseValue));
        }

        [TestCase(StatType.Resistance)]
        [TestCase(StatType.RateOfFire)]
        public void Get_NoStat_ReturnsBase(StatType type)
        {
            Assert.AreEqual(2, _owner.GetStat(type, baseValue: 2));
        }

        [TestCase(StatType.Resistance, 30)]
        [TestCase(StatType.RateOfFire, 0)]
        public void Tick_OneStat_ShouldRunOut(StatType type, float duration)
        {
            _owner.AddStat(type, StatValueType.Flat, value: 5, duration: duration);

            float baseValue = 1;
            float expected = 6;

            float delta = 0.05f;
            int maxTicks = (int)(duration / delta);

            for (int i = 1; i <= maxTicks; i++)
            {
                _owner.OnTick(i, delta);

                Assert.AreEqual(expected, _owner.GetStat(type, baseValue),
                    "Stat ran out or was not properly registered");
            }

            _owner.OnTick(maxTicks + 1, delta);
            Assert.AreEqual(1, _owner.GetStat(type, baseValue), "Stat should already run out");
        }

        [Test]
        public void Tick_MultipleStats_ShouldAddUp()
        {
            float baseValue = 1;

            // 1 + 1000% = 11
            _owner.AddStat(StatType.Damage, StatValueType.Percent, value: 1000, duration: 1);

            // 1 + 200% = 3
            _owner.AddStat(StatType.RateOfFire, StatValueType.Percent, value: 200, duration: 1);
            _owner.OnTick(1, 0.25f);

            Assert.AreEqual(11, _owner.GetStat(StatType.Damage, baseValue), "Incorrect damage");
            Assert.AreEqual(3, _owner.GetStat(StatType.RateOfFire, baseValue), "Incorrect rate of fire");

            _owner.AddStat(StatType.RateOfFire, StatValueType.Flat, value: 0.5f, duration: 0.4f);
            _owner.OnTick(2, 0.25f);

            Assert.AreEqual(3.5f, _owner.GetStat(StatType.RateOfFire, baseValue),
                "New stat should be added to the previous");

            _owner.OnTick(3, 0.25f);

            Assert.AreEqual(3, _owner.GetStat(StatType.RateOfFire, baseValue),
                "New stat should run out");

            _owner.OnTick(4, 0.25f);

            Assert.AreEqual(1, _owner.GetStat(StatType.RateOfFire, baseValue),
                "All stats should run out");
        }

        [Test]
        public void Tick_InfiniteDuration_NeverRunsOut()
        {
            _owner.AddStat(StatType.Damage, StatValueType.Percent, value: 10, duration: -1);

            for (int i = 0; i < 10000; i += 1000)
            {
                _owner.OnTick(i, tickDelta: 1000);

                Assert.Greater(_owner.GetStat(StatType.Damage, 1), 1,
                    "Infinite durations should not run out");
            }
        }
    }
}
