using System;
using Tanks.Core.Helpers;
using Tanks.Shared.Weapons.Data;

namespace Tanks.Shared.Weapons
{
    public interface IReadOnlyWeapon
    {
        event Action<ShotId> Used;

        IReadOnlyAmmo Ammo { get; }
        IReadOnlyCooldown Cooldown { get; }
        IReadOnlyUseMode UseMode { get; }
        IRecoilConfig Recoil { get; }
    }

    public class Weapon : IReadOnlyWeapon
    {
        public event Action<ShotId> Used;

        public IReadOnlyAmmo Ammo => _ammo;
        public IReadOnlyCooldown Cooldown => _cooldown;
        public IReadOnlyUseMode UseMode => _useMode;
        public IRecoilConfig Recoil { get; private set; }

        private IAmmo _ammo;
        private ICooldown _cooldown;
        private IUseMode _useMode;
        private IUseAction _useAction;

        public void Initialize(
            Required<IAmmo> ammo,
            Required<ICooldown> cooldown,
            Required<IUseMode> useMode,
            Required<IUseAction> useAction,
            Required<IRecoilConfig> recoil)
        {
            _ammo = ammo.Value;
            _cooldown = cooldown.Value;
            _useMode = useMode.Value;
            _useAction = useAction.Value;
            Recoil = recoil.Value;
        }

        public bool Tick(IWeaponOwner owner, bool isHoldingDown, float deltaTime, ShotId id)
        {
            _useMode.Tick(isHoldingDown);
            _cooldown.Tick(deltaTime);
            _ammo.Tick(deltaTime);

            if (_useMode.IsUsing == false) return false;
            if (_cooldown.CanUse() == false) return false;
            if (_ammo.CanConsume() == false) return false;

            _ammo.Consume();
            _cooldown.Use();
            _useAction.Use(owner, id);
            Used?.Invoke(id);

            return true;
        }
    }
}
