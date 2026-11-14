using NUnit.Framework;
using System.Collections;
using System.Reflection;
using Tanks.Shared.Adapters;
using Tanks.Shared.Driving;
using Tanks.Shared.Effects;
using Tanks.Shared.Stats;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tanks.Shared.Tests.Runtime.Driving
{
    public sealed class CollisionDriveTests
    {
        private Scene _scene;
        private TankDriveBehaviour _drive;
        private StatTypeParametersConfig _config;
        private ArcadeDriverSettings _settings;

        [SetUp]
        public void Setup()
        {
            _scene = SceneManager.CreateScene(
                nameof(CollisionDriveTests),
                new CreateSceneParameters(LocalPhysicsMode.Physics2D));

            GameObject tank = new GameObject("tank", typeof(BoxCollider2D));

            SceneManager.MoveGameObjectToScene(tank, _scene);
            tank.layer = LayerMask.NameToLayer("Entity");
            _drive = tank.AddComponent<TankDriveBehaviour>();

            typeof(TankDriveBehaviour)
                .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_drive, null);

            _drive.Rigidbody.gravityScale = 0;
            _settings = new ArcadeDriverSettings
            {
                AccelerationTime = 0.2f,
                MaxSpeed = 8,
                ReverseSpeed = 4,
                SteeringSpeed = Angle.FromDegrees(180)
            };

            typeof(TankDriveBehaviour)
                .GetField("_driver", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_drive, new ArcadeDriver(_settings));

            _config = ScriptableObject.CreateInstance<StatTypeParametersConfig>();
            _config.Parameters[StatType.Speed] = new StatTypeParameters
            {
                MinValue = 0,
                MaxValue = _settings.MaxSpeed
            };

            _drive.GetComponent<StatOwnerBehaviour>().Config = _config;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
                Object.DestroyImmediate(root);

            Object.DestroyImmediate(_config);

            yield return SceneManager.UnloadSceneAsync(_scene);
        }

        [TestCase("Default", false)]
        [TestCase("Entity", false)]
        [TestCase("Entity", true)]
        public void Tick_HeadOn_BlocksForwardAndRetainsSteering(
            string layer, bool isDynamic)
        {
            BoxCollider2D obstacle = CreateObstacle(layer);

            if (isDynamic) obstacle.gameObject.AddComponent<Rigidbody2D>();

            Physics2D.SyncTransforms();
            _drive.Tick(1, 1, _settings.AccelerationTime);

            Assert.That(math.length(_drive.Velocity.Linear), Is.Zero);
            Assert.That(_drive.Velocity.Angular.Degrees, Is.LessThan(0));
        }

        [TestCase("Default")]
        [TestCase("Entity")]
        public void Tick_Glancing_SlidesAlongNearestObstacle(string layer)
        {
            CreateObstacle(layer);
            BoxCollider2D farther = CreateObstacle(layer);
            farther.size = new Vector2(20, 0.4f);
            farther.transform.position = new Vector2(0, 1.6f);
            _drive.Transform.WorldRotation = Angle.FromDegrees(45);

            Physics2D.SyncTransforms();
            _drive.Tick(1, 0, _settings.AccelerationTime);

            Assert.That(_drive.Velocity.Linear.x, Is.EqualTo(0).Within(0.001f));
            Assert.That(_drive.Velocity.Linear.y, Is.GreaterThan(0));
        }

        [TestCase("Default")]
        [TestCase("Entity")]
        public void Tick_TurningFromContact_EscapesWithoutReversing(string layer)
        {
            CreateObstacle(layer);
            _drive.Rigidbody.position = new Vector2(0.28f, 0);

            Physics2D.SyncTransforms();

            PhysicsScene2D physics = _scene.GetPhysicsScene2D();
            Vector2 start = _drive.Rigidbody.position;
            float step = Time.fixedDeltaTime;
            int steps = Mathf.CeilToInt(90 / _drive.TurnRate / step);

            for (int i = 0; i < steps; i++)
            {
                _drive.Tick(1, -1, step);
                physics.Simulate(step);
            }

            Assert.That(_drive.Rigidbody.position.y, Is.GreaterThan(start.y));
            Assert.That(_drive.Rigidbody.rotation, Is.GreaterThan(45));
        }

        [TestCase("Default", -1, 0)]
        [TestCase("Entity", -1, 0)]
        [TestCase("Default", 1, 180)]
        [TestCase("Entity", 1, 180)]
        public void Tick_DrivingAway_IsUnblocked(
            string layer, float throttle, float heading)
        {
            CreateObstacle(layer);
            _drive.Tick(1, 0, _settings.AccelerationTime);
            _drive.Transform.WorldRotation = Angle.FromDegrees(heading);

            Physics2D.SyncTransforms();
            _drive.Tick(throttle, 0, _settings.AccelerationTime);

            Assert.That(_drive.Velocity.Linear.x, Is.LessThan(0));
        }

        [TestCase("Default", true)]
        [TestCase("Entity", false)]
        public void Tick_Noclip_OnlyIgnoresWalls(string layer, bool passes)
        {
            CreateObstacle(layer);
            _drive.GetComponent<EffectOwnerBehaviour>()
                .AddEffect(EffectType.Noclip, 1);

            _drive.Tick(1, 0, _settings.AccelerationTime);

            Assert.That(_drive.Velocity.Linear.x > 0, Is.EqualTo(passes));
        }

        [TestCase("Default")]
        [TestCase("Entity")]
        public void Tick_Trigger_DoesNotBlock(string layer)
        {
            CreateObstacle(layer).isTrigger = true;
            _drive.Tick(1, 0, _settings.AccelerationTime);

            Assert.That(_drive.Velocity.Linear.x, Is.GreaterThan(0));
        }

        private BoxCollider2D CreateObstacle(string layer)
        {
            GameObject obstacle = new GameObject(layer, typeof(BoxCollider2D));

            SceneManager.MoveGameObjectToScene(obstacle, _scene);
            obstacle.layer = LayerMask.NameToLayer(layer);
            obstacle.transform.position = new Vector2(1, 0);

            BoxCollider2D collider = obstacle.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(0.4f, 20);

            Physics2D.SyncTransforms();
            return collider;
        }
    }
}
