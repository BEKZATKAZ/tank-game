using NUnit.Framework;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Data;
using Tanks.Shared.Weapons.Projectiles;
using UnityEngine;

namespace Tanks.Shared.Tests.Weapons.Projectiles
{
    public sealed class ProjectilePoolTests
    {
        private SharedProjectilePoolService _pool;

        [SetUp]
        public void Setup()
        {
            _pool = new GameObject("projectile-pool")
                .AddComponent<SharedProjectilePoolService>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_pool) Object.DestroyImmediate(_pool.gameObject);
        }

        [Test]
        public void TryInitializePool()
        {
            IProjectileConfig config = new MockProjectileConfig();
            Assert.True(_pool.TryInitializePool(config), "Should initialize pool");
            Assert.False(_pool.TryInitializePool(config), "Should initialize the same pool");
        }

        [Test]
        public void TryRent_DifferentProjectileIds_ReturnsTrue()
        {
            IProjectileConfig config = new MockProjectileConfig();
            _pool.TryInitializePool(config);

            Assert.True(_pool.TryRent(config, new ProjectileId(new(1), 1), out var i1),
                "Failed to rent (1)");

            Assert.True(_pool.TryRent(config, new ProjectileId(new(2), 1), out var i2),
                "Failed to rent (2)");

            Assert.AreNotEqual(i1, i2, "Projectile instances should not be duplicated");

            _pool.Return(i1);
            _pool.Return(i2);

            Assert.True(_pool.TryRent(config, new ProjectileId(new(1), 1), out _),
                "Failed to rent after returning");
        }

        [Test]
        public void TryRent_SameProjectileIds_ReturnsFalse()
        {
            IProjectileConfig config = new MockProjectileConfig();
            _pool.TryInitializePool(config);

            ProjectileId id1 = new ProjectileId(new ShotId(1), 1);
            ProjectileId id2 = new ProjectileId(new ShotId(1), 2);
            ProjectileId id3 = new ProjectileId(new ShotId(1), 3);

            Assert.True(_pool.TryRent(config, id1, out var i1), "Failed to rent (1,1)");
            Assert.True(_pool.TryRent(config, id2, out _), "Failed to rent (1,2)");
            Assert.True(_pool.TryRent(config, id3, out _), "Failed to rent (1,3)");
            Assert.False(_pool.TryRent(config, id1, out _), "Should return false on duplicates");

            _pool.Return(i1);

            Assert.True(_pool.TryRent(config, id1, out _), "Should no longer be a duplicate");
        }
    }
}
