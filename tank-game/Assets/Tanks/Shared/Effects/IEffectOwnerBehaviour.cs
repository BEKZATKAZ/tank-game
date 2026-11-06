using System;
using System.Collections.Generic;
using Tanks.Shared.Entities;

namespace Tanks.Shared.Effects
{
    public interface IEffectOwnerBehaviour : INetworkBehaviourExtended
    {
        event Action<EffectType, EffectEndReason> Ended;
        event Action<EffectId, EffectType, EffectEndReason> EntryEnded;

        IReadOnlyDictionary<EffectType, EffectEntry> Effects { get; }
        IReadOnlyDictionary<EffectId, EffectEntry> Entries { get; }
        IEntityBehaviour Entity { get; }

        bool HasEffect(EffectId id);
        bool TryGetEffect(EffectId id, out EffectEntry entry);

        // Always creates an independent entry.
        EffectId AddEffect(EffectType type, float duration, IEntityBehaviour from = null);
        void OverrideEffect(EffectId id, EffectType type, float duration, IEntityBehaviour from = null);
        void ExtendEffect(EffectId id, float duration);
        void RemoveEffect(EffectId id);

        // Reuses the ID for this attacker, producer and effect type on this target.
        EffectId ApplyEffect(
            EffectType type, float duration, string producer,
            IEntityBehaviour from = null, bool restart = false);
    }
}
