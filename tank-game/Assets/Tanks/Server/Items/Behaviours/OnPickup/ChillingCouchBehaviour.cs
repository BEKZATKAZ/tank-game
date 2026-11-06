using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Server.Weapons.Projectiles;
using Tanks.Shared.Adapters;
using Tanks.Shared.Driving;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Stats;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Projectiles.Modules;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Server.Items.Behaviours.OnPickup
{
    [ItemBehaviour("chilling-couch")]
    public sealed class ChillingCouchBehaviour : ItemBehaviourBase
    {
        private sealed class PlayerEntry
        {
            public float2 Position;
            public Angle Rotation;
            public float Elapsed;
        }

        private readonly Dictionary<IItemOwnerBehaviour, PlayerEntry> _entries = new();
        private readonly StatType[] _types = (StatType[])Enum.GetValues(typeof(StatType));
        public override bool OncePerOwner => true;

        public override void OnPickup(IItemOwnerBehaviour owner)
        {
            if (_entries.ContainsKey(owner)) return;

            _entries.Add(owner, new PlayerEntry
            {
                Position = owner.Transform.WorldPosition,
                Rotation = owner.Transform.WorldRotation
            });
        }

        public override void OnTick(IItemOwnerBehaviour owner, int tick, float tickDelta)
        {
            if (_entries.TryGetValue(owner, out PlayerEntry entry) == false) return;

            float2 position = owner.Transform.WorldPosition;
            Angle rotation = owner.Transform.WorldRotation;

            bool moved = math.distancesq(entry.Position, position) > 0.000001f ||
                math.abs(Mathf.DeltaAngle(entry.Rotation.Degrees, rotation.Degrees)) > 0.01f;

            entry.Position = position;
            entry.Rotation = rotation;
            entry.Elapsed = moved ? 0 : entry.Elapsed + tickDelta;

            float interval = Config.GetStat("interval").Value;
            if (interval <= 0 || entry.Elapsed < interval) return;

            entry.Elapsed %= interval;

            IStatOwnerBehaviour stats = owner.Entity.GetBehaviour<IStatOwnerBehaviour>();
            if (stats == null) return;

            StatType? highest = null;
            float highestValue = float.NegativeInfinity;

            foreach (StatType type in _types)
            {
                float value = stats.GetStat(type, GetBaseValue(owner, type));
                if (value >= stats.GetMaximum(type) || value <= highestValue) continue;
                highest = type;
                highestValue = value;
            }

            if (highest.HasValue == false) return;

            float increase = Config.GetStat("increase").Value;
            int copies = owner.Items.Count(x => x == Config.Identifier);

            stats.AddStat(highest.Value, StatValueType.Flat, increase * copies);
        }

        private static float GetBaseValue(IItemOwnerBehaviour owner, StatType type)
        {
            if (type == StatType.Speed)
            {
                return owner.Entity.GetBehaviour<ITankDriveBehaviour>()?.DefaultSpeed ?? 0;
            }

            IReadOnlyWeapon weapon = owner.Entity
                .GetBehaviour<IWeaponHolderBehaviour>()?.CurrentWeapon;

            if (type == StatType.RateOfFire)
            {
                float cooldown = weapon?.Cooldown.DurationSeconds ?? 0;
                return cooldown > 0 ? 1 / cooldown : 0;
            }

            if (type != StatType.Damage || weapon?.UseAction is not ProjectileLaunchAction launch)
            {
                return 0;
            }

            DamageModuleBase[] modules = launch.Config.Prefab
                .GetComponentsInChildren<DamageModuleBase>();

            return modules.Length == 0 ? 0 : modules.Max(x => (float)x.MaxDamage);
        }

        public override void OnDestroy(IItemOwnerBehaviour owner) => _entries.Remove(owner);
    }
}
