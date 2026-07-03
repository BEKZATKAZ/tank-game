using System;
using Tanks.Shared.Weapons.Actions;

namespace Tanks.Shared.Weapons
{
    public interface IReadOnlyWeapon
    {
        event Action Used;

        IReadOnlyAmmo Ammo { get; }
        IReadOnlyCooldown Cooldown { get; }
        IReadOnlyUseMode UseMode { get; }
    }

    public class Weapon : IReadOnlyWeapon
    {
        public event Action Used;

        public IReadOnlyAmmo Ammo => _ammo;
        public IReadOnlyCooldown Cooldown => _cooldown;
        public IReadOnlyUseMode UseMode => _useMode;

        private IAmmo _ammo;
        private ICooldown _cooldown;
        private IUseMode _useMode;
        private IUseAction _useAction;

        public void Initialize(
            IAmmo ammo, ICooldown cooldown, IUseMode useMode, IUseAction useAction)
        {
            _ammo = ammo;
            _cooldown = cooldown;
            _useMode = useMode;
            _useAction = useAction;
        }

        public void StartUsing()
        {
            _useMode.StartUsing();
        }

        public void StopUsing()
        {
            _useMode.StopUsing();
        }

        public void Tick(float deltaTime)
        {
            if (_useMode.IsUsing && _ammo.CanConsume() && _cooldown.CanUse())
            {
                _ammo.Consume();
                _cooldown.Use();
                _useAction.Use();
                Used?.Invoke();
            }

            _cooldown.Tick(deltaTime);
            _ammo.Tick(deltaTime);
            _useMode.Tick();
        }
    }
}