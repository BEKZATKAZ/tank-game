using System.Collections.Generic;
using Tanks.Shared.Entities;

namespace Tanks.Shared.Effects
{
    public interface IEffectOwnerBehaviour : INetworkBehaviourExtended
    {
        IReadOnlyDictionary<EffectType, EffectEntry> Effects { get; }
        IEntityBehaviour Entity { get; }

        void AddEffect(EffectType type, float duration, IEntityBehaviour from = null);
    }
}
