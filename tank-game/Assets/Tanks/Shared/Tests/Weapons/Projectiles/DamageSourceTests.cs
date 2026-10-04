using NSubstitute;
using NUnit.Framework;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Projectiles;
using Tanks.Shared.Weapons.Projectiles.Modules;
using UnityEngine;

namespace Tanks.Shared.Tests.Weapons.Projectiles
{
    public sealed class DamageSourceTests
    {
        private DirectDamageModule _component;
        private IEntityBehaviour _target;
        private IEntityBehaviour _from;
        private IDamageService _service;

        [SetUp]
        public void Setup()
        {
            _target = Substitute.For<IEntityBehaviour>();
            _from = Substitute.For<IEntityBehaviour>();
            _service = Substitute.For<IDamageService>();

            _component = new GameObject().AddComponent<DirectDamageModule>();
            _component.Projectile = Substitute.For<IProjectileBehaviour>();
            _component.Projectile.Id.Returns(new ProjectileId(new ShotId(123), 3));
            _component.Construct(_service);
        }

        [TearDown]
        public void TearDown()
        {
            if (_component) Object.DestroyImmediate(_component.gameObject);
        }

        [TestCase(50, 10)]
        [TestCase(2, 1)]
        [TestCase(100, 200)]
        public void NearFar_EvaluatesCorrectly(int near, int far)
        {
            _component.NearDamage = (ushort)near;
            _component.FarDamage = (ushort)far;

            _component.MinTime = 2;
            _component.MaxTime = 10;

            _component.Projectile.ElapsedSeconds.Returns(0.5f);
            AssertDamage(near);

            _component.Projectile.ElapsedSeconds.Returns(2);
            AssertDamage(near);

            _component.Projectile.ElapsedSeconds.Returns(10);
            AssertDamage(far);

            _component.Projectile.ElapsedSeconds.Returns(20);
            AssertDamage(far);

            _component.Projectile.ElapsedSeconds.Returns(6);
            AssertDamage((far + near) * 0.5f);
        }

        private void AssertDamage(float expectedAmount)
        {
            HealthEventId eventId = HealthEventId.Create(_component.GetType(), _component.Id);
            _service.ClearReceivedCalls();

            _component.HitEntity(_target, _from);
            _service.Received(1).Damage(expectedAmount, _target, _from, eventId);

            _component.HitEntity(_target, null);
            _service.Received(1).Damage(expectedAmount, _target, null, eventId);
        }
    }
}
