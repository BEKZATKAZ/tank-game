using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Weapons.Projectiles.Modules;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Items.Pickups;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Server.Tests.Weapons.Projectiles
{
    public sealed class RandomItemDropModuleTests
    {
        private RandomItemDropModule _module;
        private IPickupSpawnBehaviour _pickups;
        private readonly List<GameObject> _walls = new();
        private readonly List<PickupSpawned> _spawned = new();
        private static readonly float2 Origin = new(8000, 8000);

        [SetUp]
        public void Setup()
        {
            _module = new GameObject().AddComponent<RandomItemDropModule>();
            _module.Projectile = Substitute.For<IProjectileBehaviour>();
            _module.Projectile.Owner.IsServer.Returns(true);
            _module.Projectile.Transform.WorldPosition.Returns(new float2(10, 20));

            _pickups = Substitute.For<IPickupSpawnBehaviour>();
            _spawned.Clear();
            _pickups.When(x => x.SpawnEveryoneRpc(Arg.Any<PickupSpawned>()))
                .Do(x => _spawned.Add(x.Arg<PickupSpawned>()));
            IItemDatabase items = Substitute.For<IItemDatabase>();
            IItemConfig item = Substitute.For<IItemConfig>();

            item.Tier.Returns(ItemTier.Rare);
            item.Identifier.Returns(new ItemIdentifier("drop-test"));
            items.Items.Returns(new[] { item });

            _module.Construct(items, _pickups);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_module.gameObject);
            foreach (GameObject wall in _walls) Object.DestroyImmediate(wall);
            _walls.Clear();
        }

        [Test]
        public void ProjectileDone_SpawnsAtCurrentPositionOnceAndResetsForNextLaunch()
        {
            float2 point = new(123, 234);
            _module.Handle(default);
            _module.Projectile.Transform.WorldPosition.Returns(point);
            _module.HitWall(point, new float2(1, 0));
            _module.HitEntity(null, null);

            _pickups.DidNotReceiveWithAnyArgs().SpawnEveryoneRpc(default);

            _module.ProjectileDone();
            _module.ProjectileDone();

            _pickups.Received(1).SpawnEveryoneRpc(Arg.Is<PickupSpawned>(x => x.WorldPosition.Equals(point)));
            _pickups.ReceivedWithAnyArgs(1).SpawnEveryoneRpc(default);

            _module.Handle(default);
            _module.Projectile.Transform.WorldPosition.Returns(new float2(10, 20));
            _module.ProjectileDone();

            _pickups.Received(1).SpawnEveryoneRpc(Arg.Is<PickupSpawned>(x => x.WorldPosition.Equals(new float2(10, 20))));
            _pickups.ReceivedWithAnyArgs(2).SpawnEveryoneRpc(default);
        }

        [Test]
        public void ClientProjectileDone_DoesNotSpawnItems()
        {
            _module.Projectile.Owner.IsServer.Returns(false);
            _module.HitWall(default, new float2(1, 0));
            _module.ProjectileDone();
            _pickups.DidNotReceiveWithAnyArgs().SpawnEveryoneRpc(default);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WallImpact_PlacesClearDropOnFiringSide(bool insideWall)
        {
            AddWall(new float2(0.25f, 0), new float2(0.2f, 10));
            LaunchFrom(new float2(-4, 0));
            FinishAt(new float2(insideWall ? 0.25f : 0, 0));

            Assert.That(_spawned.Count, Is.EqualTo(1));
            Assert.That(_spawned[0].WorldPosition.x, Is.LessThan(Origin.x - 0.35f));
            AssertClear(_spawned[0].WorldPosition);
        }

        [Test]
        public void Corner_PlacesDropClearOfBothWalls()
        {
            AddWall(new float2(0.25f, 0), new float2(0.2f, 10));
            AddWall(new float2(0, 0.25f), new float2(10, 0.2f));
            LaunchFrom(new float2(-4, -4));
            FinishAt(float2.zero);

            Assert.That(_spawned.Count, Is.EqualTo(1));
            AssertClear(_spawned[0].WorldPosition);
        }

        [Test]
        public void NarrowPassage_FallsBackTowardLaunchBeyondLocalSearch()
        {
            AddWall(new float2(1, 0.6f), new float2(8, 0.4f));
            AddWall(new float2(1, -0.6f), new float2(8, 0.4f));
            LaunchFrom(new float2(-4, 0));
            FinishAt(float2.zero);

            Assert.That(_spawned.Count, Is.EqualTo(1));
            Assert.That(_spawned[0].WorldPosition.x, Is.LessThan(Origin.x - 3));
            Assert.That(_spawned[0].WorldPosition.y, Is.EqualTo(Origin.y));
            AssertClear(_spawned[0].WorldPosition);
        }

        [Test]
        public void NoClearPosition_SkipsDropAndResetsForReuse()
        {
            AddWall(new float2(0, 0.6f), new float2(20, 0.4f));
            AddWall(new float2(0, -0.6f), new float2(20, 0.4f));
            LaunchFrom(new float2(-4, 0));
            FinishAt(float2.zero);
            _module.ProjectileDone();

            Assert.That(_spawned, Is.Empty);

            LaunchFrom(new float2(-14, 0));
            FinishAt(new float2(-13, 0));

            Assert.That(_spawned.Count, Is.EqualTo(1));
            AssertClear(_spawned[0].WorldPosition);
        }

        [Test]
        public void Placement_IgnoresTriggersAndEntities()
        {
            AddWall(float2.zero, new float2(2, 2)).isTrigger = true;
            AddWall(float2.zero, new float2(2, 2)).gameObject.layer = 3;
            LaunchFrom(new float2(-4, 0));
            FinishAt(float2.zero);

            Assert.That(_spawned.Count, Is.EqualTo(1));
            Assert.That(_spawned[0].WorldPosition, Is.EqualTo(Origin));
        }

        [Test]
        public void ConfiguredClearance_IsRespected()
        {
            _module.WallClearance = 1;
            AddWall(new float2(0.25f, 0), new float2(0.2f, 10));
            LaunchFrom(new float2(-4, 0));
            FinishAt(float2.zero);

            Assert.That(_spawned.Count, Is.EqualTo(1));
            AssertClear(_spawned[0].WorldPosition);
        }

        private BoxCollider2D AddWall(float2 offset, float2 size)
        {
            GameObject wall = new("freebie-test-wall", typeof(BoxCollider2D));
            _walls.Add(wall);
            wall.transform.position = new Vector3(Origin.x + offset.x, Origin.y + offset.y, 0);
            BoxCollider2D collider = wall.GetComponent<BoxCollider2D>();
            collider.size = size;
            return collider;
        }

        private void LaunchFrom(float2 offset)
        {
            _module.Projectile.Transform.WorldPosition.Returns(Origin + offset);
            _module.Handle(default);
        }

        private void FinishAt(float2 offset)
        {
            Physics2D.SyncTransforms();
            _module.Projectile.Transform.WorldPosition.Returns(Origin + offset);
            _module.ProjectileDone();
        }

        private void AssertClear(float2 point)
        {
            foreach (GameObject wall in _walls)
            {
                Collider2D collider = wall.GetComponent<Collider2D>();
                float distance = math.distance(point, collider.ClosestPoint(point));
                Assert.That(distance, Is.GreaterThanOrEqualTo(_module.WallClearance));
            }
        }
    }
}
