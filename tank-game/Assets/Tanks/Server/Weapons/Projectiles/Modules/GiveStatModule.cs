using System.Collections.Generic;
using Tanks.Shared.Entities;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;
using Tanks.Shared.Weapons.Projectiles.Modules;
using UnityEngine;

namespace Tanks.Server.Weapons.Projectiles.Modules
{
    public sealed class GiveStatModule : ProjectileModuleBehaviourBase
    {
        [SerializeField] private Dictionary<StatType, PlayerStat> _stats = new();

        public IDictionary<StatType, PlayerStat> Stats => _stats;

        protected override void OnHitEntity(IEntityBehaviour target, IEntityBehaviour from)
        {
            IStatOwnerBehaviour statOwner = target.GetBehaviour<IStatOwnerBehaviour>();

            foreach ((StatType type, PlayerStat stat) in _stats)
            {
                statOwner.AddStat(type, stat.ValueType, stat.Value, stat.Duration);
            }
        }
    }
}
