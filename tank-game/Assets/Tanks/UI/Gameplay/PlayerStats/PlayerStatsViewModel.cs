using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Client.Entities;
using Tanks.Server.Weapons.Projectiles;
using Tanks.Shared.Driving;
using Tanks.Shared.Entities;
using Tanks.Shared.Stats;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Projectiles.Modules;
using Tanks.Utils;
using Unity.Mathematics;

namespace Tanks.UI.Gameplay.PlayerStats
{
    public sealed class StatChanges
    {
        public float CurrentValue { get; set; }
        public float Change { get; set; }
    }

    public sealed class PlayerStatsViewModel : IViewModel, IDisposable
    {
        public PlayerStatsViewModel(IClientEntityService clientEntityService)
        {
            _lastStats = new(new Dictionary<StatType, StatChanges>());

            foreach (StatType type in Enum.GetValues(typeof(StatType)))
            {
                _lastStats.Value[type] = new StatChanges();
            }

            _clientEntityService = clientEntityService;
            _clientEntityService.Changed += OnPossessed;

            IEntityBehaviour possessed = _clientEntityService.GetPossessedEntity();
            if (possessed is { IsValid: true }) OnPossessed(null, possessed);
        }

        public IReadOnlyBindableProperty<IDictionary<StatType, StatChanges>> Stats => _lastStats;

        private readonly IClientEntityService _clientEntityService;
        private readonly BindableProperty<IDictionary<StatType, StatChanges>> _lastStats;

        private IStatOwnerBehaviour _statOwner;
        private IWeaponHolderBehaviour _weaponHolder;
        private ITankDriveBehaviour _driveBehaviour;

        private IReadOnlyWeapon _currentWeapon;
        private float _cachedBaseDamage;

        public void Dispose()
        {
            _clientEntityService.Changed -= OnPossessed;
        }

        public void Update()
        {
            if (_statOwner is null) return;

            if (_currentWeapon != _weaponHolder.CurrentWeapon)
            {
                _currentWeapon = _weaponHolder.CurrentWeapon;
                if (_currentWeapon != null) _cachedBaseDamage = GetBaseDamage();
            }

            bool hasChanged = false;
            float weaponCooldown = _weaponHolder.CurrentWeapon.Cooldown.DurationSeconds;

            foreach ((StatType type, StatChanges changes) in _lastStats.Value)
            {
                float baseValue = type switch
                {
                    StatType.Damage => _cachedBaseDamage,
                    StatType.Speed => _driveBehaviour.DefaultSpeed,
                    StatType.RateOfFire => weaponCooldown <= math.EPSILON ? 0 : 1f / weaponCooldown,
                    _ => 0
                };

                float stat = _statOwner.GetStat(type, baseValue);
                float change = stat - changes.CurrentValue;

                if (math.abs(change) < math.EPSILON)
                {
                    _lastStats.Value[type].Change = 0;
                    continue;
                }

                _lastStats.Value[type].CurrentValue = stat;
                _lastStats.Value[type].Change = change;

                hasChanged = true;
            }

            if (hasChanged) _lastStats.ForceRefresh();
        }

        private float GetBaseDamage()
        {
            if (_currentWeapon.UseAction is not ProjectileLaunchAction launch) return 0;

            DamageModuleBase[] damageModules = launch.Config.Prefab
                .GetComponentsInChildren<DamageModuleBase>();

            if (damageModules is not { Length: > 0 }) return 0;

            return damageModules.Max(x => x.MaxDamage);
        }

        private void OnPossessed(IEntityBehaviour previous, IEntityBehaviour current)
        {
            _statOwner = current?.GetBehaviour<IStatOwnerBehaviour>();
            _weaponHolder = current?.GetBehaviour<IWeaponHolderBehaviour>();
            _driveBehaviour = current?.GetBehaviour<ITankDriveBehaviour>();
        }
    }
}
