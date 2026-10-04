using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Weapons.Projectiles;
using Tanks.Shared.Adapters;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Mathematics;

namespace Tanks.Client.Tests.Weapons.Projectiles
{
    public class ProjectileReconciliationTests
    {
        private ProjectileReconciliation _reconciliation;
        private IProjectileBehaviour _projectile;
        private ITransform _predictedTransform;

        [SetUp]
        public void Setup()
        {
            _predictedTransform = new ProjectileTransform(0.5f);
            _projectile = Substitute.For<IProjectileBehaviour>();
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
                _predictedTransform, snapshot, default, 0.05f, out _), "Too sensititve");

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
                _predictedTransform, snapshot, default, 0.05f, out float2 error), "Too strict");

            float expectedErrorX = math.abs(difference) + _projectile.Speed * 0.05f;

            Assert.AreEqual(expectedErrorX, math.abs(error.x), delta: 0.05f,
                "Reconciliation occured, but no error is out");
        }
    }
}
