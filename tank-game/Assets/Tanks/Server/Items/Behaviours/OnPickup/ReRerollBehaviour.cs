using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Server.Weapons.Projectiles;
using Tanks.Shared.Driving;
using Tanks.Shared.Entities;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Stats;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Projectiles.Modules;
using Unity.Mathematics;

namespace Tanks.Server.Items.Behaviours.OnPickup
{
    public sealed class PlayerReRerollEntry
    {
        public IWeaponHolderBehaviour WeaponOwner;
        public ITankDriveBehaviour DriveBehaviour;
        public StatId[] StatIds;
        public int? LastTick;
    }

    [ItemBehaviour("re-reroll")]
    public sealed class ReRerollBehaviour : ItemBehaviourBase
    {
        private readonly StatType[] _types = (StatType[])Enum.GetValues(typeof(StatType));
        private readonly Dictionary<EntityId, PlayerReRerollEntry> _playerEntries = new();

        public override void OnPickup(IItemOwnerBehaviour owner)
        {
            if (_playerEntries.ContainsKey(owner.Entity.Id)) return;

            _playerEntries.Add(owner.Entity.Id, new PlayerReRerollEntry
            {
                DriveBehaviour = owner.GetBehaviour<ITankDriveBehaviour>(),
                WeaponOwner = owner.GetBehaviour<IWeaponHolderBehaviour>(),
                StatIds = _types.Select(_ => StatIdGenerator.GenerateNext()).ToArray()
            });
        }

        public override void OnDestroy(IItemOwnerBehaviour owner)
        {
            if (_playerEntries.TryGetValue(owner.Entity.Id, out var entry))
            {
                IStatOwnerBehaviour statOwner = owner.GetBehaviour<IStatOwnerBehaviour>();
                foreach (StatId id in entry.StatIds) statOwner?.RemoveStat(id);
            }
            _playerEntries.Remove(owner.Entity.Id);
        }

        public override void Reset() => _playerEntries.Clear();

        public override void OnTick(IItemOwnerBehaviour owner, int tick, float tickDelta)
        {
            if (_playerEntries.TryGetValue(owner.Entity.Id, out var entry) == false) return;

            float elapsed = entry.LastTick.HasValue
                ? (tick - entry.LastTick.Value) * tickDelta
                : float.PositiveInfinity;
            float intervalSeconds = Config.GetStat("interval").Value;

            if (elapsed < intervalSeconds) return;

            IStatOwnerBehaviour statOwner = owner.GetBehaviour<IStatOwnerBehaviour>();
            if (statOwner is null) return;

            IReadOnlyWeapon weapon = entry.WeaponOwner?.CurrentWeapon;
            if (weapon?.Cooldown is null || entry.DriveBehaviour is null) return;
            float weaponCooldown = weapon.Cooldown.DurationSeconds;

            Span<float> newStats = stackalloc float[_types.Length];
            Span<float> currentStats = stackalloc float[_types.Length];
            Span<int> shuffledIndices = stackalloc int[_types.Length];
            int count = 0;
            entry.LastTick = tick;

            for (int i = 0; i < entry.StatIds.Length; i++)
            {
                StatId id = entry.StatIds[i];
                statOwner.RemoveStat(id);

                float baseValue = _types[i] switch
                {
                    StatType.Damage => GetBaseDamage(weapon),
                    StatType.Speed => entry.DriveBehaviour.DefaultSpeed,
                    StatType.RateOfFire => weaponCooldown <= math.EPSILON ? 0 : 1f / weaponCooldown,
                    _ => 0
                };

                currentStats[i] = statOwner.GetStat(_types[i], baseValue);
                if (currentStats[i] == 0) continue;

                shuffledIndices[count] = i;
                newStats[count++] = currentStats[i];
            }

            ShuffleSpan(newStats[..count]);

            for (int i = 0; i < count; i++)
            {
                int index = shuffledIndices[i];
                StatId id = entry.StatIds[index];
                statOwner.OverrideStat(id, _types[index], StatValueType.Flat, newStats[i] - currentStats[index]);
            }
        }

        private void ShuffleSpan<T>(Span<T> span)
        {
            int n = span.Length;

            while (n > 1)
            {
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                (span[n], span[k]) = (span[k], span[n]);
            }
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
