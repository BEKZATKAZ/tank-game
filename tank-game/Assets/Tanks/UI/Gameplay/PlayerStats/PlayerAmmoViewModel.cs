using System;
using Tanks.Client.Entities;
using Tanks.Shared.Entities;
using Tanks.Shared.Weapons;
using Tanks.Utils;

namespace Tanks.UI.Gameplay.PlayerStats
{
    public sealed class PlayerAmmoViewModel : IViewModel, IDisposable
    {
        public PlayerAmmoViewModel(IClientEntityService clientEntityService)
        {
            _clientEntityService = clientEntityService;
            _clientEntityService.Changed += OnPossessed;

            OnPossessed(null, _clientEntityService.GetPossessedEntity());
        }

        public IReadOnlyBindableProperty<(ushort Current, ushort Max)> Ammo => _ammo;

        private readonly IClientEntityService _clientEntityService;
        private readonly BindableProperty<(ushort Current, ushort Max)> _ammo = new();
        private IEntityBehaviour _player;
        private IWeaponHolderBehaviour _weaponHolder;

        public void Dispose()
        {
            _clientEntityService.Changed -= OnPossessed;
        }

        public void Update()
        {
            IReadOnlyAmmo ammo = _player is { IsValid: true }
                ? _weaponHolder?.CurrentWeapon?.Ammo
                : null;

            _ammo.Value = ammo is null ? default : (ammo.Current, ammo.Max);
        }

        private void OnPossessed(IEntityBehaviour previous, IEntityBehaviour current)
        {
            _player = current;
            _weaponHolder = current is { IsValid: true }
                ? current.GetBehaviour<IWeaponHolderBehaviour>()
                : null;

            Update();
        }
    }
}
