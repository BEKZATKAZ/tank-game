using NSubstitute;
using NUnit.Framework;
using Tanks.Shared.Adapters;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Data.Actions;
using Tanks.Shared.Weapons.Projectiles;
using Tanks.Shared.Weapons.Projectiles.Modules;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Shared.Tests.Weapons.Projectiles
{
    public class WeaponActionModuleTests
    {
        private WeaponActionModule _component;
        private IUseActionConfig _config;
        private IUseAction _action;

        [SetUp]
        public void Setup()
        {
            _component = new GameObject().AddComponent<WeaponActionModule>();
            _component.Projectile = Substitute.For<IProjectileBehaviour>();
            _component.Projectile.Id.Returns(new ProjectileId(new ShotId(123), 3));

            _action = Substitute.For<IUseAction>();
            _config = Substitute.For<IUseActionConfig>();
            _config.Create(_component.Transform).Returns(_action);
        }

        [TearDown]
        public void TearDown()
        {
            if (_component) Object.DestroyImmediate(_component.gameObject);
        }

        [TestCase(ProjectileModuleCallbackFlags.HitWall, 1)]
        [TestCase(ProjectileModuleCallbackFlags.HitEntity, 1)]
        [TestCase(ProjectileModuleCallbackFlags.ProjectileDone, 1)]
        [TestCase(ProjectileModuleCallbackFlags.HitEntity | ProjectileModuleCallbackFlags.HitWall, 2)]
        public void Launch_BasedOnParameter(ProjectileModuleCallbackFlags flags, int flagCount)
        {
            LaunchOn(flags, flagCount);
        }

        private void LaunchOn(ProjectileModuleCallbackFlags flags, int flagCount)
        {
            _action.ClearReceivedCalls();

            _component.Setup(flags, _config);
            _component.HitWall(default, new float2(1, 0));
            _component.HitEntity(null, null);
            _component.ProjectileDone();

            ShotId shotId = _component.Projectile.Id.ShotId;

            _action.Received(flagCount)
                .Use(_component.Projectile.Owner, new ShotId(shotId.Value + 1));
        }
    }
}
