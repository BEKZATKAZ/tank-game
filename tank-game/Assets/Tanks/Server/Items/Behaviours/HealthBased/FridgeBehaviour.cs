using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Server.Weapons.Projectiles;
using Tanks.Shared.Driving;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Projectiles.Modules;
using Unity.Mathematics;

namespace Tanks.Server.Items.Behaviours.HealthBased
{
    public sealed class PlayerFridgeEntry
    {
        public IWeaponHolderBehaviour WeaponOwner;
        public ITankDriveBehaviour DriveBehaviour;
    }

    [ItemBehaviour("supplies-in-the-fridge")]
    public sealed class FridgeBehaviour : ItemBehaviourBase
    {
        private readonly StatType[] _types = (StatType[])Enum.GetValues(typeof(StatType));
        private readonly Dictionary<EntityId, PlayerFridgeEntry> _playerEntries = new();

        public override void OnPickup(IItemOwnerBehaviour owner)
        {
            if (_playerEntries.ContainsKey(owner.Entity.Id)) return;

            _playerEntries.Add(owner.Entity.Id, new PlayerFridgeEntry
            {
                DriveBehaviour = owner.GetBehaviour<ITankDriveBehaviour>(),
                WeaponOwner = owner.GetBehaviour<IWeaponHolderBehaviour>()
            });
        }

        public override void OnDestroy(IItemOwnerBehaviour owner)
        {
            _playerEntries.Remove(owner.Entity.Id);
        }

        public override void OnHealthChanged(
            IItemOwnerBehaviour owner, IEntityBehaviour from,
            HealthState oldState, HealthState newState)
        {
            // Discard damage
            if (oldState.Current >= newState.Current) return;

            if (_playerEntries.TryGetValue(owner.Entity.Id, out var entry) == false) return;

            IStatOwnerBehaviour statOwner = owner.GetBehaviour<IStatOwnerBehaviour>();
            if (statOwner is null) return;

            IReadOnlyWeapon weapon = entry.WeaponOwner?.CurrentWeapon;
            if (weapon?.Cooldown is null || entry.DriveBehaviour is null) return;

            float weaponCooldown = weapon.Cooldown.DurationSeconds;
            float lowestValue = float.MaxValue;
            float highestValue = float.MinValue;

            StatType lowestType = 0;
            StatType highestType = 0;

            foreach (StatType type in _types)
            {
                float stat = statOwner.GetStat(type, type switch
                {
                    StatType.Damage => GetBaseDamage(weapon),
                    StatType.Speed => entry.DriveBehaviour.DefaultSpeed,
                    StatType.RateOfFire => weaponCooldown <= math.EPSILON ? 0 : 1f / weaponCooldown,
                    _ => 0
                });

                if (lowestValue > stat)
                {
                    lowestValue = stat;
                    lowestType = type;
                }

                if (highestValue < stat)
                {
                    highestValue = stat;
                    highestType = type;
                }
            }

            if (highestType == lowestType) return;

            ItemStat increase = Config.GetStat("increase");
            ItemStat decrease = Config.GetStat("decrease");

            statOwner.AddStat(lowestType, increase.ValueType, increase.Value);
            statOwner.AddStat(highestType, decrease.ValueType, decrease.Value);
        }

        private float GetBaseDamage(IReadOnlyWeapon weapon)
        {
            if (weapon.UseAction is not ProjectileLaunchAction launch) return 0;

            DamageModuleBase[] damageModules = launch.Config.Prefab
                .GetComponentsInChildren<DamageModuleBase>();

            if (damageModules is not { Length: > 0 }) return 0;

            return damageModules.Max(x => x.MaxDamage);
        }
    }
}
