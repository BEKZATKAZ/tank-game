using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Server.Items.Behaviours.OnPickup;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;
using Tanks.Shared.Tests.Items;
using UnityEngine;
using EntityId = Tanks.Shared.Entities.EntityId;

namespace Tanks.Server.Tests.Items.Behaviours
{
    public class DNAReconstructionTests : ItemBehaviourTestBase<DNAReconstructionBehaviour>
    {
        private UnityEngine.Random.State _randomState;
        private IStatOwnerBehaviour _stats;
        private List<(StatId Id, StatType Type, StatValueType ValueType, float Value)> _added;

        protected override void InitializeConfig()
        {
            AddItemStat("damage-boost", new ItemStat { Value = 60, ValueType = StatValueType.Percent });
            AddItemStat("damage-penalty", new ItemStat { Value = -40, ValueType = StatValueType.Percent });
            AddItemStat("speed-boost", new ItemStat { Value = 60, ValueType = StatValueType.Percent });
            AddItemStat("speed-penalty", new ItemStat { Value = -40, ValueType = StatValueType.Percent });
            AddItemStat("defense-boost", new ItemStat { Value = 25 });
            AddItemStat("defense-penalty", new ItemStat { Value = -20 });
            AddItemStat("resistance-boost", new ItemStat { Value = 10 });
            AddItemStat("resistance-penalty", new ItemStat { Value = -10 });
            AddItemStat("rate-of-fire-boost", new ItemStat { Value = 60, ValueType = StatValueType.Percent });
            AddItemStat("rate-of-fire-penalty", new ItemStat { Value = -40, ValueType = StatValueType.Percent });
        }

        public override void Setup()
        {
            base.Setup();
            _randomState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(42);
            _stats = MockOwner.GetBehaviour<IStatOwnerBehaviour>();
            _added = new List<(StatId, StatType, StatValueType, float)>();

            _stats.AddStat(Arg.Any<StatType>(), Arg.Any<StatValueType>(), Arg.Any<float>(), -1)
                .Returns(args =>
                {
                    StatId id = StatIdGenerator.GenerateNext();
                    _added.Add((id, (StatType)args[0], (StatValueType)args[1], (float)args[2]));
                    return id;
                });
        }

        [TearDown]
        public void TearDown() => UnityEngine.Random.state = _randomState;

        [Test]
        public void Pickup_AppliesOneBoostAndOnePenaltyToDifferentStats()
        {
            for (int i = 0; i < 100; i++)
            {
                Behaviour.OnPickup(MockOwner);

                var boost = _added[i * 2];
                var penalty = _added[i * 2 + 1];
                Assert.That(boost.Type, Is.Not.EqualTo(penalty.Type));
                Assert.That(boost.Value, Is.GreaterThan(0));
                Assert.That(penalty.Value, Is.LessThan(0));
            }

            Assert.That(_added.Count, Is.EqualTo(200));
            Assert.That(_added.Select(x => x.Id).Distinct().Count(), Is.EqualTo(200));
        }

        [Test]
        public void Pickup_AllStatsUseConfiguredValuesAndTypes()
        {
            for (int i = 0; i < 100; i++) Behaviour.OnPickup(MockOwner);

            foreach (StatType type in Enum.GetValues(typeof(StatType)))
            {
                var boost = _added.First(x => x.Type == type && x.Value > 0);
                var penalty = _added.First(x => x.Type == type && x.Value < 0);
                bool flat = type is StatType.Defense or StatType.Resistance;
                StatValueType expectedType = flat ? StatValueType.Flat : StatValueType.Percent;
                float expectedBoost = type switch
                {
                    StatType.Defense => 25,
                    StatType.Resistance => 10,
                    _ => 60
                };
                float expectedPenalty = type switch
                {
                    StatType.Defense => -20,
                    StatType.Resistance => -10,
                    _ => -40
                };

                Assert.That(boost.ValueType, Is.EqualTo(expectedType));
                Assert.That(penalty.ValueType, Is.EqualTo(expectedType));
                Assert.That(boost.Value, Is.EqualTo(expectedBoost));
                Assert.That(penalty.Value, Is.EqualTo(expectedPenalty));
            }
        }

        [Test]
        public void Pickup_UsesConfigRatherThanHardcodedAmounts()
        {
            Config.GetStat(Arg.Any<string>()).Returns(args => new ItemStat
            {
                Value = ((string)args[0]).EndsWith("-boost") ? 123 : -77,
                ValueType = StatValueType.Flat
            });

            Behaviour.OnPickup(MockOwner);

            Assert.That(_added.Select(x => x.Value), Is.EqualTo(new float[] { 123, -77 }));
            Assert.That(_added.All(x => x.ValueType == StatValueType.Flat), Is.True);
        }

        [Test]
        public void Pickup_AllOrderedPairsCanBeSelected()
        {
            var pairs = new HashSet<(StatType, StatType)>();

            for (int i = 0; i < 500; i++)
            {
                Behaviour.OnPickup(MockOwner);
                pairs.Add((_added[i * 2].Type, _added[i * 2 + 1].Type));
            }

            Assert.That(pairs.Count, Is.EqualTo(20));
        }

        [Test]
        public void Pickup_Repeated_AddsModifiersWithoutReplacingPreviousOnes()
        {
            Behaviour.OnPickup(MockOwner);
            Behaviour.OnPickup(MockOwner);
            Behaviour.OnPickup(MockOwner);

            Assert.That(_added.Count, Is.EqualTo(6));
            _stats.DidNotReceiveWithAnyArgs().RemoveStat(default);
            _stats.DidNotReceiveWithAnyArgs().OverrideStat(default, default, default, default, default);
        }

        [Test]
        public void Destroy_RemovesOnlyThisOwnersMutationsAndIsIdempotent()
        {
            IItemOwnerBehaviour other = Substitute.For<IItemOwnerBehaviour>();
            other.Entity.Id.Returns(new EntityId(2));
            Behaviour.OnPickup(MockOwner);
            Behaviour.OnPickup(MockOwner);
            Behaviour.OnPickup(other);

            Behaviour.OnDestroy(MockOwner);
            Behaviour.OnDestroy(MockOwner);

            foreach (var stat in _added) _stats.Received(1).RemoveStat(stat.Id);
            other.GetBehaviour<IStatOwnerBehaviour>().DidNotReceiveWithAnyArgs().RemoveStat(default);
            Assert.That(_stats.ReceivedCalls().Count(x => x.GetMethodInfo().Name == "RemoveStat"), Is.EqualTo(4));
        }

        [Test]
        public void Destroy_ThenPickup_StartsFresh()
        {
            Behaviour.OnPickup(MockOwner);
            Behaviour.OnDestroy(MockOwner);
            _stats.ClearReceivedCalls();

            Behaviour.OnPickup(MockOwner);
            Behaviour.OnDestroy(MockOwner);

            _stats.DidNotReceive().RemoveStat(_added[0].Id);
            _stats.DidNotReceive().RemoveStat(_added[1].Id);
            _stats.Received(1).RemoveStat(_added[2].Id);
            _stats.Received(1).RemoveStat(_added[3].Id);
        }

        [Test]
        public void Pickup_NoStatOwner_DoesNothing()
        {
            MockOwner.GetBehaviour<IStatOwnerBehaviour>().Returns((IStatOwnerBehaviour)null);

            Assert.DoesNotThrow(() => Behaviour.OnPickup(MockOwner));
            Assert.DoesNotThrow(() => Behaviour.OnDestroy(MockOwner));
            Assert.That(_added, Is.Empty);
        }

        [Test]
        public void Pickup_RealStats_StacksAndCombinesWithLaterItemsAndBaseChanges()
        {
            GameObject target = new GameObject();
            StatTypeParametersConfig parameters = ScriptableObject.CreateInstance<StatTypeParametersConfig>();

            try
            {
                StatOwnerBehaviour stats = target.AddComponent<StatOwnerBehaviour>();
                stats.Config = parameters;
                MockOwner.GetBehaviour<IStatOwnerBehaviour>().Returns(stats);

                UnityEngine.Random.InitState(42);
                Behaviour.OnPickup(MockOwner);
                UnityEngine.Random.InitState(42);
                Behaviour.OnPickup(MockOwner);

                UnityEngine.Random.InitState(42);
                int boostIndex = UnityEngine.Random.Range(0, 5);
                int penaltyIndex = UnityEngine.Random.Range(0, 4);
                if (penaltyIndex >= boostIndex) penaltyIndex++;
                StatType[] types = (StatType[])Enum.GetValues(typeof(StatType));

                foreach (StatType type in types)
                {
                    float modifier = type == types[boostIndex] ? BoostValue(type) * 2 :
                        type == types[penaltyIndex] ? PenaltyValue(type) * 2 : 0;
                    bool flat = type is StatType.Defense or StatType.Resistance;
                    float expected = 10 + (flat ? modifier : 10 * modifier * 0.01f);
                    Assert.That(stats.GetStat(type, 10), Is.EqualTo(expected).Within(0.001f));

                    stats.AddStat(type, StatValueType.Flat, 3);
                    Assert.That(stats.GetStat(type, 10), Is.EqualTo(expected + 3).Within(0.001f));
                    expected = 20 + (flat ? modifier : 20 * modifier * 0.01f) + 3;
                    Assert.That(stats.GetStat(type, 20), Is.EqualTo(expected).Within(0.001f));
                }

                stats.OnTick(100000, 1);
                Assert.That(stats.GetStat(types[boostIndex], 10), Is.GreaterThan(13));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(parameters);
            }
        }

        [Test]
        public void Pickup_RealStats_RespectsCaps()
        {
            GameObject target = new GameObject();
            StatTypeParametersConfig parameters = ScriptableObject.CreateInstance<StatTypeParametersConfig>();

            try
            {
                StatOwnerBehaviour stats = target.AddComponent<StatOwnerBehaviour>();
                stats.Config = parameters;
                MockOwner.GetBehaviour<IStatOwnerBehaviour>().Returns(stats);
                StatType[] types = (StatType[])Enum.GetValues(typeof(StatType));

                foreach (StatType type in types)
                {
                    parameters.Parameters.Add(type, new StatTypeParameters { MinValue = 2, MaxValue = 15 });
                }

                for (int i = 0; i < 3; i++)
                {
                    UnityEngine.Random.InitState(42);
                    Behaviour.OnPickup(MockOwner);
                }

                UnityEngine.Random.InitState(42);
                int boostIndex = UnityEngine.Random.Range(0, 5);
                int penaltyIndex = UnityEngine.Random.Range(0, 4);
                if (penaltyIndex >= boostIndex) penaltyIndex++;

                Assert.That(stats.GetStat(types[boostIndex], 10), Is.EqualTo(15));
                Assert.That(stats.GetStat(types[penaltyIndex], 10), Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(parameters);
            }
        }

        private float BoostValue(StatType type) => type switch
        {
            StatType.Defense => 25,
            StatType.Resistance => 10,
            _ => 60
        };

        private float PenaltyValue(StatType type) => type switch
        {
            StatType.Defense => -20,
            StatType.Resistance => -10,
            _ => -40
        };
    }
}
