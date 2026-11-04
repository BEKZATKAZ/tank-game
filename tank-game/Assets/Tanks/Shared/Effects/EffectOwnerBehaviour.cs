using System;
using System.Collections.Generic;
using Tanks.Shared.Entities;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Tanks.Shared.Effects
{
    public readonly struct EffectEntry
    {
        public EffectEntry(int startTick, float duration, IEntityBehaviour from)
        {
            StartTick = startTick;
            Duration = duration;
            From = from;
        }

        public readonly int StartTick;
        public readonly float Duration;
        public readonly IEntityBehaviour From;
    }

    [RequireComponent(typeof(EntityBehaviour))]
    public sealed class EffectOwnerBehaviour : NetworkBehaviourExtended, IEffectOwnerBehaviour
    {
        public IReadOnlyDictionary<EffectType, EffectEntry> Effects => _effects;
        public IEntityBehaviour Entity { get; private set; }

        private readonly Dictionary<EffectType, EffectEntry> _effects = new();
        private int _lastTick;

        [Inject] private readonly IEntityService _entityService;

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        private void OverrideEffectClientRpc(
            EffectType type, int startTick, float duration, Entities.EntityId from)
        {
            IEntityBehaviour fromEntity = from == default ? null : _entityService.GetById(from);
            OverrideEffect(type, new EffectEntry(startTick, duration, fromEntity));
        }

        public void AddEffect(EffectType type, float duration, IEntityBehaviour from = null)
        {
            EffectEntry entry = _effects.TryGetValue(type, out EffectEntry existing)
                ? new EffectEntry(existing.StartTick, existing.Duration + duration, from)
                : new EffectEntry(_lastTick, duration, from);

            OverrideEffect(type, entry);
            if (IsServer == false) return;

            Entities.EntityId fromId = from?.Id ?? default;
            OverrideEffectClientRpc(type, entry.StartTick, entry.Duration, fromId);
        }

        private void OverrideEffect(EffectType type, EffectEntry entry)
        {
            if (type is EffectType.Burning)
            {
                _effects.Remove(EffectType.Frozen);
            }
            else if (type is EffectType.Frozen)
            {
                _effects.Remove(EffectType.Burning);
            }

            _effects[type] = entry;
        }

        public override void OnTick(int tick, float tickDelta)
        {
            _lastTick = tick;

            int toRemoveCount = 0;
            Span<EffectType> toRemove = stackalloc EffectType[_effects.Count];

            foreach ((EffectType type, EffectEntry effect) in _effects)
            {
                float elapsed = (tick - effect.StartTick) * tickDelta;

                if (elapsed < effect.Duration) continue;

                toRemove[toRemoveCount++] = type;
            }

            for (int i = 0; i < toRemoveCount; i++)
            {
                _effects.Remove(toRemove[i]);
            }
        }

        protected override void OnInitialize()
        {
            Entity = GetComponent<EntityBehaviour>();
            _lastTick = NetworkManager.NetworkTickSystem.LocalTime.Tick;
        }
    }
}
