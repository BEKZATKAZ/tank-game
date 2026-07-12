using NUnit.Framework;
using Tanks.Client.Weapons;
using Tanks.Shared.Adapters;
using Tanks.Shared.Weapons.Data;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Mathematics;

namespace Tanks.Client.Tests.Weapons.Projectiles
{
    public sealed class ProjectileReconciliationTests
    {
        private ProjectileReconciliation _reconciliation;
        private IProjectile _projectile;
        private ITransform _predictedTransform;

        [SetUp]
        public void Setup()
        {
            _predictedTransform = new ProjectileTransform(0.5f);

            IProjectileConfig config = new MockProjectileConfig
            {
                LifeTime = 5,
                MaxRicochetCount = 5,
                Speed = 5
            };

            _projectile = config.Create();
            _reconciliation = new ProjectileReconciliation(_projectile);
        }

        [TearDown]
        public void TearDown()
        {
            _reconciliation.Dispose();
        }

        [TestCase(0)]
        [TestCase(-0.0009f)]
        [TestCase(0.0009f)]
        [TestCase(0.0001f)]
        public void Reconcile_BelowThreshold_ReturnsFalse(float difference)
        {
            _reconciliation.Tick(100, _predictedTransform);

            float2 initialPosition = _predictedTransform.WorldPosition;

            ProjectileSnapshotFromServer snapshot = new ProjectileSnapshotFromServer
            {
                ServerPosition = new float2(difference, 0),
                ServerRotation = default,
                ServerTick = 95
            };

            Assert.False(_reconciliation.TryReconcile(
                _predictedTransform, snapshot, 0.05f, out _), "Too sensititve");

            Assert.AreEqual(initialPosition, _predictedTransform.WorldPosition,
                "Positions differ, but no reconciliation occured");
        }

        [TestCase(0.01f)]
        [TestCase(-0.01f)]
        [TestCase(0.1f)]
        public void Reconcile_WithinThreshold_ReturnsTrue(float difference)
        {
            _reconciliation.Tick(100, _predictedTransform);

            ProjectileSnapshotFromServer snapshot = new ProjectileSnapshotFromServer
            {
                ServerPosition = new float2(difference, 0),
                ServerRotation = default,
                ServerTick = 99
            };

            Assert.True(_reconciliation.TryReconcile(
                _predictedTransform, snapshot, 0.05f, out float2 error), "Too strict");

            float expectedErrorX = math.abs(difference) + _projectile.Config.Speed * 0.05f;

            Assert.AreEqual(expectedErrorX, math.abs(error.x), delta: 0.05f,
                "Reconciliation occured, but no error is out");
        }

        [TestCase(5)]
        [TestCase(1000)]
        [TestCase(-5)]
        [TestCase(-1000)]
        public void Reconcile_TooDistant_ReturnsTrueAndSnaps(float difference)
        {
            _reconciliation.Tick(100, _predictedTransform);

            ProjectileSnapshotFromServer snapshot = new ProjectileSnapshotFromServer
            {
                ServerPosition = new float2(difference, 0),
                ServerRotation = default,
                ServerTick = 95
            };

            Assert.True(_reconciliation.TryReconcile(
                _predictedTransform, snapshot, 0.05f, out float2 error), "Too strict");

            Assert.AreEqual(float2.zero, error, "Should snap when the distance is too much");
        }
    }
}
