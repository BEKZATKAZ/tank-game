using NUnit.Framework;
using Tanks.Shared.Adapters;
using Tanks.Shared.Tests.Adapters;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Shared.Tests.Weapons.Projectiles
{
    public class ProjectileTests
    {
        private readonly ProjectileMover _mover = new ProjectileMover();
        private readonly float2 _initialProjectilePosition = new float2(-10, 51.5f);

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

                static void FailIfRicocheted(float2 hitPoint)
                {
                    Assert.Fail($"Ricocheted too early at {hitPoint}");
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
                    OnRicochet = _ => ricocheted = true
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
