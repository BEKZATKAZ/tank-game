using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Weapons.Projectiles.Modules;
using Tanks.Shared.Entities;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;
using Tanks.Shared.Weapons.Projectiles;
using UnityEngine;

namespace Tanks.Server.Tests.Weapons.Projectiles
{
    public class GiveStatModuleTests
    {
        private GiveStatModule _component;

        [SetUp]
        public void Setup()
        {
            _component = new GameObject().AddComponent<GiveStatModule>();
            _component.Projectile = Substitute.For<IProjectileBehaviour>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_component) Object.DestroyImmediate(_component.gameObject);
        }

        [Test]
        public void OnHitEntity_GivesStatsToTarget()
        {
            IEntityBehaviour target = Substitute.For<IEntityBehaviour>();
            IStatOwnerBehaviour statOwner = target.GetBehaviour<IStatOwnerBehaviour>();

            _component.Stats.Add(StatType.Speed, new PlayerStat
            {
                Duration = 30, Value = 10, ValueType = StatValueType.Percent
            });

            _component.Stats.Add(StatType.Defense, new PlayerStat
            {
                Duration = 10, Value = 20, ValueType = StatValueType.Flat
            });

            _component.HitEntity(target, _component.Projectile.Owner.Entity);

            foreach ((StatType type, PlayerStat stat) in _component.Stats)
            {
                statOwner.Received(1).AddStat(type, stat.ValueType, stat.Value, stat.Duration);
            }
        }
    }
}
