using NUnit.Framework;
using System.Collections.Generic;
using Tanks.Shared.Adapters;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Tests.Adapters;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Shared.Tests.Weapons.Projectiles
{
    public class ProjectileTests
    {
        private readonly ProjectileMover _mover = new ProjectileMover(0.5f);
        private readonly float2 _initialProjectilePosition = new float2(-10, 51.5f);
        private readonly List<GameObject> _objects = new();

        private ITransform _projectileTransform;

        [SetUp]
        public void Setup()
        {
            _projectileTransform = new MockTransform
            {
                WorldPosition = _initialProjectilePosition,
                LocalScale = 0.5f
            };
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject instance in _objects) Object.DestroyImmediate(instance);
            _objects.Clear();
        }

        [Test]
        public void ResolveLaunch_WithClearPosition_PreservesTransform()
        {
            ITransform transform = CreateLaunchTransform(_initialProjectilePosition, new float2(1, 1));
            transform.WorldRotation = Angle.FromDegrees(37);

            IHealthBehaviour target = _mover.ResolveLaunch(transform);

            Assert.That(target, Is.Null);
            Assert.That(transform.WorldPosition, Is.EqualTo(_initialProjectilePosition));
            Assert.That(transform.WorldRotation, Is.EqualTo(Angle.FromDegrees(37)));
        }

        [TestCase(4, -2.25f, 0)]
        [TestCase(4, -1.9f, 0)]
        [TestCase(4, 0, 0)]
        [TestCase(4, 1.9f, 0)]
        [TestCase(40, 0, 0)]
        [TestCase(4, 0, 90)]
        [TestCase(4, 0, 215)]
        public void ResolveLaunch_InsideWall_RetreatsUntilEntireRadiusIsClear(
            float wallSize, float offset, float angle)
        {
            BoxCollider2D wall = CreateCollider(_initialProjectilePosition, new float2(wallSize));
            wall.transform.rotation = Quaternion.Euler(0, 0, angle);

            ITransform transform = CreateLaunchTransform(_initialProjectilePosition, new float2(1, 1));
            transform.WorldRotation = Angle.FromDegrees(angle);
            float2 direction = transform.Right;
            transform.WorldPosition += direction * offset;
            Physics2D.SyncTransforms();

            IHealthBehaviour target = _mover.ResolveLaunch(transform);

            float2 displacement = transform.WorldPosition - _initialProjectilePosition;
            float along = math.dot(displacement, direction);

            Assert.That(target, Is.Null);
            Assert.That(Physics2D.OverlapCircle(transform.WorldPosition, 0.5f, 1 << 0), Is.Null);
            Assert.That(along, Is.LessThanOrEqualTo(-wallSize * 0.5f - 0.5f));
            Assert.That(along, Is.GreaterThan(-wallSize * 0.5f - 0.53f));
            Assert.That(math.length(displacement - direction * along), Is.LessThan(0.0001f));
            Assert.That(transform.WorldRotation, Is.EqualTo(Angle.FromDegrees(angle)));
        }

        [TestCase(2, 0.5f)]
        [TestCase(-2, -0.5f)]
        public void ResolveLaunch_WithScaledProjectile_UsesLargestAbsoluteScale(float x, float y)
        {
            CreateCollider(_initialProjectilePosition, new float2(4, 4));
            ITransform transform = CreateLaunchTransform(_initialProjectilePosition, new float2(x, y));
            Physics2D.SyncTransforms();

            _mover.ResolveLaunch(transform);

            Assert.That(Physics2D.OverlapCircle(transform.WorldPosition, 1, 1 << 0), Is.Null);
            Assert.That(transform.WorldPosition.x, Is.LessThanOrEqualTo(_initialProjectilePosition.x - 3));
            Assert.That(transform.WorldPosition.x, Is.GreaterThan(_initialProjectilePosition.x - 3.03f));
        }

        [Test]
        public void ResolveLaunch_WithOverlappingWalls_ClearsEveryWall()
        {
            CreateCollider(_initialProjectilePosition, new float2(2, 4));
            CreateCollider(_initialProjectilePosition - new float2(1.5f, 0), new float2(2, 4));
            ITransform transform = CreateLaunchTransform(_initialProjectilePosition, new float2(1, 1));
            Physics2D.SyncTransforms();

            _mover.ResolveLaunch(transform);

            Assert.That(Physics2D.OverlapCircle(transform.WorldPosition, 0.5f, 1 << 0), Is.Null);
            Assert.That(transform.WorldPosition.x, Is.LessThanOrEqualTo(_initialProjectilePosition.x - 3));
        }

        [TestCase(0, false)]
        [TestCase(-3, true)]
        [TestCase(-10, false)]
        public void ResolveLaunch_ChecksTankOverlapOnlyAfterClearingWalls(float tankOffset, bool expectedHit)
        {
            CreateCollider(_initialProjectilePosition, new float2(4, 4));
            BoxCollider2D tank = CreateCollider(
                _initialProjectilePosition + new float2(tankOffset, 0), new float2(1, 1), layer: 3);
            IHealthBehaviour health = tank.gameObject.AddComponent<HealthBehaviour>();

            ITransform transform = CreateLaunchTransform(_initialProjectilePosition, new float2(1, 1));
            Physics2D.SyncTransforms();

            IHealthBehaviour target = _mover.ResolveLaunch(transform);

            Assert.That(target, expectedHit ? Is.SameAs(health) : Is.Null);
            Assert.That(Physics2D.OverlapCircle(transform.WorldPosition, 0.5f, 1 << 0), Is.Null);

            transform.WorldPosition = _initialProjectilePosition + new float2(10, 0);

            Assert.That(_mover.ResolveLaunch(transform), Is.Null);
            Assert.That(transform.WorldPosition, Is.EqualTo(_initialProjectilePosition + new float2(10, 0)));
        }

        [TestCase(0)]
        [TestCase(3)]
        public void ResolveLaunch_IgnoresTriggersEvenWhenGlobalQueriesIncludeThem(int layer)
        {
            BoxCollider2D trigger = CreateCollider(_initialProjectilePosition, new float2(4, 4), layer);
            trigger.isTrigger = true;
            trigger.gameObject.AddComponent<HealthBehaviour>();

            ITransform transform = CreateLaunchTransform(_initialProjectilePosition, new float2(1, 1));
            Physics2D.SyncTransforms();
            bool queriesHitTriggers = Physics2D.queriesHitTriggers;

            try
            {
                Physics2D.queriesHitTriggers = true;

                Assert.That(_mover.ResolveLaunch(transform), Is.Null);
                Assert.That(transform.WorldPosition, Is.EqualTo(_initialProjectilePosition));
            }
            finally
            {
                Physics2D.queriesHitTriggers = queriesHitTriggers;
            }
        }

        [Test]
        public void ResolveLaunch_AfterRetreat_UsesNormalRicochetSimulation()
        {
            CreateCollider(_initialProjectilePosition, new float2(4, 4));
            ITransform transform = CreateLaunchTransform(_initialProjectilePosition, new float2(1, 1));
            Physics2D.SyncTransforms();

            _mover.ResolveLaunch(transform);
            int ricochets = 0;

            _mover.Simulate(new LaunchCallbacks
            {
                OnRicochet = (_, _) => ricochets++
            }, transform, 0.1f);

            Assert.That(ricochets, Is.EqualTo(1));
            Assert.That(transform.Right.x, Is.LessThan(-0.99f));
        }

        private ITransform CreateLaunchTransform(float2 position, float2 scale)
        {
            GameObject instance = new GameObject("launch-projectile");
            _objects.Add(instance);

            return new UnityTransform(instance.transform)
            {
                WorldPosition = position,
                LocalScale = scale
            };
        }

        private BoxCollider2D CreateCollider(float2 position, float2 size, int layer = 0)
        {
            GameObject instance = new GameObject("launch-collider", typeof(BoxCollider2D))
            {
                layer = layer
            };

            instance.transform.position = new float3(position, 0);
            _objects.Add(instance);

            BoxCollider2D collider = instance.GetComponent<BoxCollider2D>();
            collider.size = size;
            return collider;
        }

        [TestCase(0)]
        [TestCase(90)]
        [TestCase(170)]
        [TestCase(215)]
        public void Simulate_MovesAlongForward(float angleDegrees)
        {
            _projectileTransform.WorldRotation = Angle.FromDegrees(angleDegrees);

            float deltaTime = 0.05f;
            float distance = 10 * deltaTime;

            float2 currentPosition = _initialProjectilePosition;
            float2 forward = _projectileTransform.Right;

            for (int i = 0; i < 10; i++)
            {
                _mover.Simulate(default, _projectileTransform, distance);
                float2 expected = currentPosition + forward * distance;

                Assert.AreEqual(expected, _projectileTransform.WorldPosition);
                currentPosition = _projectileTransform.WorldPosition;
            }
        }

        [Test]
        public void Simulate_Ricochet_MovesFromNormal()
        {
            float2 currentPosition = _initialProjectilePosition;
            float2 forward = _projectileTransform.Right;

            GameObject wall = new GameObject("ricochet-wall", typeof(BoxCollider2D));
            float2 wallNormal = new float2(-1, 0);

            try
            {
                // 1 unit to the right
                wall.transform.position = new float3(currentPosition + forward, 0);

                BoxCollider2D box = wall.GetComponent<BoxCollider2D>();
                box.offset = new float2(0.5f, 0);
                box.size = new float2(1, 5);

                static void FailIfRicocheted(float2 hitPoint, float2 normal)
                {
                    Assert.Fail($"Ricocheted too early at {hitPoint}, normal {normal}");
                }

                for (int i = 0; i < 9; i++)
                {
                    // Stop right before the wall
                    _mover.Simulate(new LaunchCallbacks
                    {
                        OnRicochet = FailIfRicocheted
                    }, _projectileTransform, 0.1f);
                }

                bool ricocheted = false;
                float2 expectedForward = math.reflect(forward, wallNormal);

                _mover.Simulate(new LaunchCallbacks
                {
                    OnRicochet = (_, _) => ricocheted = true
                }, _projectileTransform, 0.5f);

                float difference = math.distance(expectedForward, _projectileTransform.Right);

                Assert.LessOrEqual(difference, math.EPSILON, "Did not properly ricochet");
                Assert.True(ricocheted, "Ricocheted but did not call 'OnRicochet'");
            }
            finally
            {
                Object.DestroyImmediate(wall);
            }
        }
    }
}
