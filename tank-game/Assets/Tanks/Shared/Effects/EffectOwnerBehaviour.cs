using System;
using System.Collections.Generic;
using Tanks.Shared.Entities;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Tanks.Shared.Effects
{
    public enum EffectEndReason
    {
        Expired,
        Replaced,
        Removed
    }

    public readonly struct EffectEntry
    {
        public EffectEntry(int startTick, float duration, IEntityBehaviour from)
            : this(default, startTick, duration, from, 0)
        {
        }

        public EffectEntry(
            EffectType type, int startTick, float duration, IEntityBehaviour from, ulong order = 0)
        {
            Type = type;
            StartTick = startTick;
            Duration = duration;
            From = from;
            Order = order;
        }

        public readonly EffectType Type;
        public readonly int StartTick;
        public readonly float Duration;
        public readonly IEntityBehaviour From;
        public readonly ulong Order;
    }

    [RequireComponent(typeof(EntityBehaviour))]
    public sealed class EffectOwnerBehaviour : NetworkBehaviourExtended, IEffectOwnerBehaviour
    {
        public event Action<EffectType, EffectEndReason> Ended;
        public event Action<EffectId, EffectType, EffectEndReason> EntryEnded;

        public IReadOnlyDictionary<EffectType, EffectEntry> Effects => _effects;
        public IReadOnlyDictionary<EffectId, EffectEntry> Entries => _entries;
        public IEntityBehaviour Entity { get; private set; }

        private readonly Dictionary<EffectType, EffectEntry> _effects = new();
        private readonly Dictionary<EffectId, EffectEntry> _entries = new();
        private readonly Dictionary<(Entities.EntityId From, string Producer, EffectType Type), EffectId> _sources = new();
        private readonly Dictionary<EffectId, (Entities.EntityId From, string Producer, EffectType Type)> _sourcesById = new();
        
        private int _lastTick;
        private float _tickDelta;
        private ulong _nextOrder;
        private bool _clearing;
        private Rigidbody2D _body;
        private LayerMask _originalExcludedLayers;
        private bool _ignoringWalls;

        [Inject] private readonly IEntityService _entityService;

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        private void OverrideEffectClientRpc(
            EffectId id, EffectType type, int startTick, float duration, Entities.EntityId from, ulong order)
        {
            IEntityBehaviour fromEntity = from == default ? null : _entityService.GetById(from);
            SetEffect(id, new EffectEntry(type, startTick, duration, fromEntity, order));
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        private void RemoveEffectClientRpc(EffectId id, EffectEndReason reason)
        {
            RemoveEffect(id, reason);
        }

        public bool HasEffect(EffectId id)
        {
            return _entries.ContainsKey(id);
        }

        public bool TryGetEffect(EffectId id, out EffectEntry entry)
        {
            return _entries.TryGetValue(id, out entry);
        }

        public EffectId AddEffect(EffectType type, float duration, IEntityBehaviour from = null)
        {
            EffectId id = EffectIdGenerator.GenerateNext();
            OverrideEffect(id, type, duration, from);
            return id;
        }

        public void OverrideEffect(
            EffectId id, EffectType type, float duration, IEntityBehaviour from = null)
        {
            ApplyEffect(id, new EffectEntry(type, _lastTick, duration, from, ++_nextOrder));
        }

        public void ExtendEffect(EffectId id, float duration)
        {
            if (_entries.TryGetValue(id, out EffectEntry entry) == false) return;

            ApplyEffect(id, new EffectEntry(
                entry.Type, entry.StartTick, entry.Duration + duration, entry.From, ++_nextOrder));
        }

        public EffectId ApplyEffect(
            EffectType type, float duration, string producer,
            IEntityBehaviour from = null, bool restart = false)
        {
            var source = (from?.Id ?? default, producer, type);

            if (_sources.TryGetValue(source, out EffectId id) == false)
            {
                id = EffectIdGenerator.GenerateNext();
                if (_clearing) return id;

                _sources.Add(source, id);
                _sourcesById.Add(id, source);
            }

            if (restart || HasEffect(id) == false) OverrideEffect(id, type, duration, from);
            else ExtendEffect(id, duration);

            return id;
        }

        private void ApplyEffect(EffectId id, EffectEntry entry)
        {
            if (_clearing) return;

            SetEffect(id, entry);
            if (IsServer == false) return;

            Entities.EntityId fromId = entry.From?.Id ?? default;
            OverrideEffectClientRpc(
                id, entry.Type, entry.StartTick, entry.Duration, fromId, entry.Order);
        }

        private void SetEffect(EffectId id, EffectEntry entry)
        {
            if (entry.Type == EffectType.Burning)
            {
                RemoveEffects(EffectType.Frozen, EffectEndReason.Replaced);
            }
            else if (entry.Type == EffectType.Frozen)
            {
                RemoveEffects(EffectType.Burning, EffectEndReason.Replaced);
            }

            if (_entries.TryGetValue(id, out EffectEntry previous) && previous.Type != entry.Type)
            {
                RemoveEffect(id, EffectEndReason.Replaced);
            }

            _entries[id] = entry;
            _nextOrder = Math.Max(_nextOrder, entry.Order);

            UpdateEffect(entry.Type);
            if (entry.Type == EffectType.Noclip) SetIgnoringWalls(true);
        }

        public void RemoveEffect(EffectId id)
        {
            if (RemoveEffect(id, EffectEndReason.Removed) && IsServer)
            {
                RemoveEffectClientRpc(id, EffectEndReason.Removed);
            }
        }

        private bool RemoveEffect(EffectId id, EffectEndReason reason)
        {
            if (_entries.Remove(id, out EffectEntry entry) == false) return false;
            if (_sourcesById.Remove(id, out var source)) _sources.Remove(source);

            UpdateEffect(entry.Type);
            bool ended = _effects.ContainsKey(entry.Type) == false;

            try
            {
                EntryEnded?.Invoke(id, entry.Type, reason);
            }
            finally
            {
                if (ended && _effects.ContainsKey(entry.Type) == false)
                {
                    if (entry.Type == EffectType.Noclip) SetIgnoringWalls(false);
                    Ended?.Invoke(entry.Type, reason);
                }
            }

            return true;
        }

        private void RemoveEffects(EffectType type, EffectEndReason reason)
        {
            if (_effects.ContainsKey(type) == false) return;

            Span<EffectId> ids = stackalloc EffectId[_entries.Count];
            int count = 0;

            foreach ((EffectId id, EffectEntry entry) in _entries)
            {
                if (entry.Type == type) ids[count++] = id;
            }

            for (int i = 0; i < count; i++) RemoveEffect(ids[i], reason);
        }

        private void UpdateEffect(EffectType type)
        {
            bool found = false;
            EffectEntry longest = default;
            EffectEntry latest = default;

            foreach (EffectEntry entry in _entries.Values)
            {
                if (entry.Type != type) continue;

                if (found == false || entry.StartTick * _tickDelta + entry.Duration >
                    longest.StartTick * _tickDelta + longest.Duration)
                {
                    longest = entry;
                }

                if (found == false || entry.Order > latest.Order)
                {
                    latest = entry;
                }

                found = true;
            }

            if (found == false) _effects.Remove(type);
            else _effects[type] = new EffectEntry(
                type, longest.StartTick, longest.Duration, latest.From, latest.Order);
        }

        private void SetIgnoringWalls(bool value)
        {
            if (_ignoringWalls == value) return;
            _body ??= GetComponent<Rigidbody2D>();
            if (_body == false) return;

            if (value)
            {
                _originalExcludedLayers = _body.excludeLayers;
                _body.excludeLayers = _originalExcludedLayers | 1;
            }
            else _body.excludeLayers = _originalExcludedLayers;

            _ignoringWalls = value;
        }

        public override void OnTick(int tick, float tickDelta)
        {
            _lastTick = tick;
            bool timingChanged = _tickDelta != tickDelta;
            _tickDelta = tickDelta;

            int toRemoveCount = 0;
            Span<EffectId> toRemove = stackalloc EffectId[_entries.Count];

            foreach ((EffectId id, EffectEntry effect) in _entries)
            {
                float elapsed = (tick - effect.StartTick) * tickDelta;

                if (elapsed < effect.Duration) continue;

                toRemove[toRemoveCount++] = id;
            }

            for (int i = 0; i < toRemoveCount; i++)
            {
                EffectId id = toRemove[i];
                if (_entries.TryGetValue(id, out EffectEntry entry) == false ||
                    (tick - entry.StartTick) * tickDelta < entry.Duration) continue;

                if (RemoveEffect(id, EffectEndReason.Expired) && IsServer)
                {
                    RemoveEffectClientRpc(id, EffectEndReason.Expired);
                }
            }

            if (timingChanged)
            {
                foreach (EffectType type in Enum.GetValues(typeof(EffectType))) UpdateEffect(type);
            }
        }

        public override void OnDestroy()
        {
            ClearEffects();
            base.OnDestroy();
        }

        protected override void OnInitialize()
        {
            Entity = GetComponent<EntityBehaviour>();
            _lastTick = NetworkManager.NetworkTickSystem.LocalTime.Tick;
            _tickDelta = 1f / NetworkManager.NetworkConfig.TickRate;
        }

        private void ClearEffects()
        {
            if (_clearing) return;
            _clearing = true;

            try
            {
                EffectId[] ids = new EffectId[_entries.Count];
                _entries.Keys.CopyTo(ids, 0);

                foreach (EffectId id in ids) RemoveEffect(id, EffectEndReason.Removed);
                _sources.Clear();
                _sourcesById.Clear();
                _nextOrder = 0;
            }
            finally { _clearing = false; }
        }
    }
}
