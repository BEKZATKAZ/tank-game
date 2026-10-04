using NSubstitute;
using NUnit.Framework;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Projectiles;
using UnityEngine;
using VContainer;

namespace Tanks.Shared.Tests.Runtime.Weapons
{
    public sealed class ProjectilePoolTests
    {
        private SharedProjectilePoolService _pool;
        private ProjectileBehaviour _prefab;

        [SetUp]
        public void Setup()
        {
            _pool = new GameObject("projectile-pool")
                .AddComponent<SharedProjectilePoolService>();

            _pool.Construct(Substitute.For<IObjectResolver>());

            _prefab = new GameObject("projectile").AddComponent<ProjectileBehaviour>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_prefab) Object.DestroyImmediate(_prefab.gameObject);
            if (_pool) Object.DestroyImmediate(_pool.gameObject);
        }

        [Test]
        public void TryInitializePool()
        {
            Assert.True(_pool.TryInitializePool(_prefab), "Should initialize pool");
            Assert.False(_pool.TryInitializePool(_prefab), "Should initialize the same pool");
        }

        [Test]
        public void TryRent_DifferentProjectileIds_ReturnsTrue()
        {
            _pool.TryInitializePool(_prefab);

            Assert.True(_pool.TryRent(_prefab.Name, new ProjectileId(new(1), 1), out var i1),
                "Failed to rent (1)");

            Assert.True(_pool.TryRent(_prefab.Name, new ProjectileId(new(2), 1), out var i2),
                "Failed to rent (2)");

            Assert.AreNotEqual(i1, i2, "Projectile instances should not be duplicated");


            i1.Launch(null, new ProjectileId(new(1), 1));
            i2.Launch(null, new ProjectileId(new(2), 1));

            _pool.Return(i1.Id);
            _pool.Return(i2.Id);

            Assert.True(_pool.TryRent(_prefab.Name, new ProjectileId(new(1), 1), out _),
                "Failed to rent after returning");
        }

        [Test]
        public void TryRent_SameProjectileIds_ReturnsFalse()
        {
            _pool.TryInitializePool(_prefab);

            ProjectileId id1 = new ProjectileId(new ShotId(1), 1);
            ProjectileId id2 = new ProjectileId(new ShotId(1), 2);
            ProjectileId id3 = new ProjectileId(new ShotId(1), 3);

            Assert.True(_pool.TryRent(_prefab.Name, id1, out var i1), "Failed to rent (1,1)");
            Assert.True(_pool.TryRent(_prefab.Name, id2, out _), "Failed to rent (1,2)");
            Assert.True(_pool.TryRent(_prefab.Name, id3, out _), "Failed to rent (1,3)");

            i1.Launch(null, id1);
            Assert.False(_pool.TryRent(_prefab.Name, id1, out _), "Should return false on duplicates");

            _pool.Return(i1.Id);
            Assert.True(_pool.TryRent(_prefab.Name, id1, out _), "Should no longer be a duplicate");
        }
    }
}
