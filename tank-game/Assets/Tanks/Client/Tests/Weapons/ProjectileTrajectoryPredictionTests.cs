using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Weapons.Projectiles;
using Tanks.Core.Helpers;
using Tanks.Editor.TestHelpers;
using Tanks.Server.Weapons.Projectiles;
using Tanks.Shared.Adapters;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Data;
using Tanks.Shared.Weapons.Data.Actions;
using Tanks.Shared.Weapons.Projectiles;
using UnityEngine;

namespace Tanks.Client.Tests.Weapons
{
    public sealed class ProjectileTrajectoryPredictionTests
    {
        private GameObject _player;
        private GameObject _wall;
        private ProjectileBehaviour _prefab;
        private ProjectileLaunchActionConfig _config;
        private ProjectileTrajectoryPrediction _preview;

        [SetUp]
        public void Setup()
        {
            _player = new GameObject("trajectory-player", typeof(WeaponHolderBehaviour), typeof(BoxCollider2D));
            _player.layer = 3;
            _player.transform.position = new Vector3(20000, 20000, 0);
            _player.GetComponent<BoxCollider2D>().size = new Vector2(3.815f, 2.5f);
            WeaponHolderBehaviour holder = _player.GetComponent<WeaponHolderBehaviour>();
            holder.InvokePrivateMethod("Awake");
            holder.Entity.MarkAsPossessed();

            GameObject muzzle = new("muzzle");
            muzzle.transform.SetParent(_player.transform, false);
            muzzle.transform.localPosition = new Vector3(1.5f, 0, 0);
            holder.SetPrivateProperty("MuzzlePoint", new UnityTransform(muzzle.transform));

            _prefab = new GameObject("test-projectile").AddComponent<ProjectileBehaviour>();
            _prefab.SetPrivateField("_radius", 0.85f);
            _config = ScriptableObject.CreateInstance<ProjectileLaunchActionConfig>();
            _config.SetPrivateField("_prefab", _prefab);
            ProjectileLaunchAction action = new(Substitute.For<IProjectilePoolService>().AsRequired(), _config, holder.MuzzlePoint);
            Weapon weapon = new();
            weapon.Initialize("test", new FixedAmmo(10), new NoCooldown(), new FullAutomaticUseMode(),
                action, Substitute.For<IRecoilConfig>().AsRequired());
            holder.SetDefault(weapon);
            holder.SwitchToDefault();

            GameObject view = new("trajectory");
            view.transform.SetParent(_player.transform, false);
            GameObject line = new("line", typeof(LineRenderer));
            line.transform.SetParent(view.transform, false);
            _preview = view.AddComponent<ProjectileTrajectoryPrediction>();
            _preview.InvokePrivateMethod("Start");
            _wall = new GameObject("trajectory-wall", typeof(BoxCollider2D));
            _wall.transform.position = new Vector3(20010, 20000, 0);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_player);
            Object.DestroyImmediate(_wall);
            Object.DestroyImmediate(_prefab.gameObject);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void MovingFireball_ReachesWallSurfaceInsteadOfHittingItsOwner()
        {
            for (int i = 0; i < 5; i++)
            {
                _player.transform.position += new Vector3(0.05f, 0, 0);
                Physics2D.SyncTransforms();
                _preview.InvokePrivateMethod("LateUpdate");
                LineRenderer line = _preview.GetPrivateField<List<LineRenderer>>("_lines")[0];

                Assert.That(line.enabled, Is.True);
                Assert.That(line.GetPosition(1).x, Is.EqualTo(20009.5f).Within(0.002f));
            }
        }

        [Test]
        public void Spread_AllocatesEveryLineAndKeepsRangesIndependent()
        {
            _config.SetPrivateField("_projectileCount", (ushort)6);
            _config.SetPrivateField("_spreadDegrees", 45f);
            _wall.transform.position = new Vector3(20010, 19994, 0);
            Physics2D.SyncTransforms();

            _preview.InvokePrivateMethod("LateUpdate");
            List<LineRenderer> lines = _preview.GetPrivateField<List<LineRenderer>>("_lines");

            Assert.That(lines.Count, Is.EqualTo(6));
            Assert.That(Vector3.Distance(lines[0].GetPosition(0), lines[0].GetPosition(1)), Is.LessThan(20));

            for (int i = 1; i < lines.Count; i++)
            {
                Assert.That(Vector3.Distance(lines[i].GetPosition(0), lines[i].GetPosition(1)),
                    Is.EqualTo(_prefab.LifeTime * _prefab.Speed).Within(0.01f));
            }
        }

        [Test]
        public void OtherEntity_StopsPreviewBeforeWall()
        {
            GameObject other = new("other-player", typeof(BoxCollider2D));
            other.layer = 3;
            other.transform.position = new Vector3(20005, 20000, 0);

            try
            {
                Physics2D.SyncTransforms();
                _preview.InvokePrivateMethod("LateUpdate");
                LineRenderer line = _preview.GetPrivateField<List<LineRenderer>>("_lines")[0];

                Assert.That(line.GetPosition(1).x, Is.EqualTo(20004.5f).Within(0.002f));
            }
            finally { Object.DestroyImmediate(other); }
        }
    }
}
