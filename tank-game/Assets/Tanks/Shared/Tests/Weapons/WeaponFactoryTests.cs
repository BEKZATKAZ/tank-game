using NUnit.Framework;
using Tanks.Shared.Adapters;
using Tanks.Shared.Tests.Adapters;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Data;
using Tanks.Shared.Weapons.Data.Actions;
using UnityEngine;

namespace Tanks.Shared.Tests.Weapons
{
    class MockWeaponConfig : IWeaponConfig, System.IDisposable
    {
        public WeaponUseMode UseMode { get; set; }
        public IAmmoConfig Ammo { get; } = ScriptableObject.CreateInstance<AmmoConfig>();
        public ICooldownConfig Cooldown { get; } = ScriptableObject.CreateInstance<CooldownConfig>();
        public IUseActionConfig UseAction { get; } = new MockUseActionConfig();

        public void Dispose()
        {
            Object.DestroyImmediate((AmmoConfig)Ammo);
            Object.DestroyImmediate((CooldownConfig)Cooldown);
        }
    }

    class MockUseActionConfig : IUseActionConfig
    {
        public IUseAction Create(ITransform muzzlePoint) => new NoUseAction();
    }

    public class WeaponFactoryTests
    {
        private readonly IWeaponFactory _factory = new WeaponFactory();

        [Test]
        public void Create_ReturnsWeapon()
        {
            using MockWeaponConfig config = new MockWeaponConfig();

            Weapon weapon = _factory.Create(config, new MockTransform());
            Assert.NotNull(weapon, "No weapon created");

            Assert.NotNull(weapon.Ammo, "No ammo");
            Assert.NotNull(weapon.Cooldown, "No cooldown");
            Assert.NotNull(weapon.UseMode, "No use mode");
        }
    }
}
