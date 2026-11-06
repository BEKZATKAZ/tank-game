using NUnit.Framework;
using System.Reflection;
using Tanks.Shared.Adapters;
using Tanks.Shared.Driving;
using Tanks.Shared.Effects;
using Tanks.Shared.Stats;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Shared.Tests.Driving
{
    public class FrozenDriveTests
    {
        private TankDriveBehaviour _drive;
        private EffectOwnerBehaviour _effects;
        private ArcadeDriver _driver;
        private StatTypeParametersConfig _config;

        [SetUp]
        public void Setup()
        {
            _drive = new GameObject().AddComponent<TankDriveBehaviour>();
            typeof(TankDriveBehaviour).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_drive, null);

            _effects = _drive.GetComponent<EffectOwnerBehaviour>();
            _driver = new ArcadeDriver(new ArcadeDriverSettings
            {
                AccelerationTime = 0.3f,
                MaxSpeed = 10,
                ReverseSpeed = 8,
                SteeringSpeed = Angle.FromDegrees(240)
            });

            typeof(TankDriveBehaviour).GetField("_driver", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_drive, _driver);

            _config = ScriptableObject.CreateInstance<StatTypeParametersConfig>();
            _config.Parameters[StatType.Speed] = new StatTypeParameters { MinValue = 0, MaxValue = 100 };
            _drive.GetComponent<StatOwnerBehaviour>().Config = _config;
        }

        [TearDown]
        public void TearDown()
        {
            if (_drive) Object.DestroyImmediate(_drive.gameObject);
            if (_config) Object.DestroyImmediate(_config);
        }

        [TestCase(1, 1)]
        [TestCase(-1, -1)]
        public void Tick_Frozen_ImmediatelyStopsMovementAndTurning(float throttle, float steering)
        {
            _driver.Drive(new float2(1, 0), throttle, 1);
            _drive.Velocity.Linear = new float2(10, 0);
            _drive.Velocity.Angular = Angle.FromDegrees(90);
            _effects.AddEffect(EffectType.Frozen, 5);

            _drive.Tick(throttle, steering, 0.02f);

            Assert.That(_drive.Velocity.Linear, Is.EqualTo(float2.zero));
            Assert.That(_drive.Velocity.Angular.Degrees, Is.Zero);
            Assert.That(_driver.Drive(new float2(1, 0), 0, 0), Is.EqualTo(float2.zero));
        }

        [Test]
        public void Tick_FrozenExpires_MovementResumes()
        {
            _effects.AddEffect(EffectType.Frozen, 1);
            _drive.Tick(1, 0, 0.3f);
            Assert.That(_drive.Velocity.Linear, Is.EqualTo(float2.zero));

            _effects.OnTick(4, 0.25f);
            _drive.Tick(1, 1, 0.3f);

            Assert.That(math.length(_drive.Velocity.Linear), Is.GreaterThan(0));
            Assert.That(_drive.Velocity.Angular.Degrees, Is.Not.Zero);
        }

        [Test]
        public void Tick_BurningReplacesFrozen_MovementResumes()
        {
            _effects.AddEffect(EffectType.Frozen, 5);
            _drive.Tick(1, 0, 0.3f);

            _effects.AddEffect(EffectType.Burning, 5);
            _drive.Tick(1, 0, 0.3f);

            Assert.That(math.length(_drive.Velocity.Linear), Is.GreaterThan(0));
        }

        [Test]
        public void Tick_NoclipPassesThroughWalls()
        {
            _drive.gameObject.layer = 3;
            _drive.gameObject.AddComponent<BoxCollider2D>();
            _drive.Rigidbody.position = new Vector2(6000, 6000);
            GameObject wall = new("noclip-drive-wall", typeof(BoxCollider2D));
            wall.transform.position = new Vector3(6002, 6000, 0);
            Physics2D.SyncTransforms();

            try
            {
                _drive.Tick(1, 0, 0.5f);
                Assert.That(_drive.Velocity.Linear.x, Is.Zero);

                _effects.AddEffect(EffectType.Noclip, 6);
                _drive.Tick(1, 0, 0.5f);

                Assert.That(_drive.Velocity.Linear.x, Is.GreaterThan(0));
            }
            finally { Object.DestroyImmediate(wall); }
        }
    }
}
