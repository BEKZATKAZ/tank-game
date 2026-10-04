using System;
using Tanks.Core.Helpers;
using Tanks.Shared.Adapters;
using Tanks.Shared.Weapons.Data;

namespace Tanks.Shared.Weapons
{
    public interface IWeaponFactory
    {
        Weapon Create(IWeaponConfig config, ITransform muzzlePoint);
    }

    public sealed class WeaponFactory : IWeaponFactory
    {
        private IUseMode CreateUseMode(WeaponUseMode useMode) => useMode switch
        {
            WeaponUseMode.SemiAutomatic => new SemiAutomaticUseMode(),
            WeaponUseMode.FullAutomatic => new FullAutomaticUseMode(),
            _ => throw new NotImplementedException()
        };

        public Weapon Create(IWeaponConfig config, ITransform muzzlePoint)
        {
            Weapon weapon = new Weapon();

            var ammo = config.Ammo.Create().AsRequired();
            var cooldown = config.Cooldown?.Create().AsRequired() ?? new NoCooldown();
            var useMode = CreateUseMode(config.UseMode).AsRequired();
            var useAction = config.UseAction?.Create(muzzlePoint).AsRequired() ?? new NoUseAction();
            var recoil = config.Recoil.AsRequired();

            weapon.Initialize(config.Name, ammo, cooldown, useMode, useAction, recoil);
            return weapon;
        }
    }
}
