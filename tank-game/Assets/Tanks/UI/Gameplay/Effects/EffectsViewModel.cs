using System;
using System.Collections.Generic;
using Tanks.Client.Entities;
using Tanks.Shared.Effects;
using Tanks.Shared.Entities;
using Tanks.Utils;

namespace Tanks.UI.Gameplay.Effects
{
    public sealed class EffectsViewModel : IViewModel, IDisposable
    {
        public EffectsViewModel(IClientEntityService clientEntityService)
        {
            _clientEntityService = clientEntityService;
            _clientEntityService.Changed += OnPossessed;

            OnPossessed(null, _clientEntityService.GetPossessedEntity());
        }

        public BindableProperty<EffectType?> CurrentEffect { get; } = new();
        public BindableProperty<float> FlashProgress { get; } = new(1);
        public BindableProperty<IReadOnlyList<EffectType>> ActiveEffects { get; } = new();
        public BindableProperty<IReadOnlyList<EffectType>> ExpiringEffects { get; } = new();

        private readonly IClientEntityService _clientEntityService;
        private readonly List<EffectType> _activeEffects = new();
        private readonly List<EffectType> _expiringEffects = new();
        private readonly List<EffectType> _effectBuffer = new();

        private IEffectOwnerBehaviour _owner;
        private int _tick;
        private float _tickDelta;

        public void Dispose()
        {
            _clientEntityService.Changed -= OnPossessed;
        }

        public void Tick(int tick, float tickDelta)
        {
            _tick = tick;
            _tickDelta = tickDelta;
            Tick();
        }

        public void Tick()
        {
            if (_owner is null || _owner.Effects.Count == 0 || _owner.Entity.IsValid == false)
            {
                CurrentEffect.Value = null;
                FlashProgress.Value = 1;

                _effectBuffer.Clear();
                PublishEffects(ActiveEffects, _activeEffects);
                PublishEffects(ExpiringEffects, _expiringEffects);

                return;
            }

            EffectType? current = null;

            foreach (EffectType type in _owner.Effects.Keys)
            {
                if (type is EffectType.Noclip or EffectType.Flashed) continue;
                current = type;
                break;
            }

            CurrentEffect.Value = current;
            float flashProgress = _owner.Effects.TryGetValue(EffectType.Flashed, out EffectEntry flash)
                && flash.Duration > 0
                ? UnityEngine.Mathf.Clamp01((_tick - flash.StartTick) * _tickDelta / flash.Duration)
                : 1;

            foreach (EffectEntry entry in _owner.Entries.Values)
            {
                if (entry.Type != EffectType.Flashed || entry.Duration <= 0) continue;

                float progress = UnityEngine.Mathf.Clamp01((_tick - entry.StartTick) * _tickDelta / entry.Duration);
                flashProgress = UnityEngine.Mathf.Min(flashProgress, progress);
            }

            FlashProgress.Value = flashProgress;

            UpdateActiveEffects();
            UpdateExpiringEffects();
        }

        private void UpdateExpiringEffects()
        {
            _effectBuffer.Clear();

            if (_tickDelta > 0)
            {
                foreach (EffectType type in _activeEffects)
                {
                    EffectEntry effect = _owner.Effects[type];
                    float remaining = effect.Duration - (_tick - effect.StartTick) * _tickDelta;
                    if (remaining <= 2) _effectBuffer.Add(type);
                }
            }

            PublishEffects(ExpiringEffects, _expiringEffects);
        }

        private void UpdateActiveEffects()
        {
            _effectBuffer.Clear();

            foreach (EffectType type in _owner.Effects.Keys)
            {
                _effectBuffer.Add(type);
            }

            PublishEffects(ActiveEffects, _activeEffects);
        }

        private void PublishEffects(BindableProperty<IReadOnlyList<EffectType>> property,
            List<EffectType> effects)
        {
            bool changed = effects.Count != _effectBuffer.Count;

            for (int i = 0; changed == false && i < effects.Count; i++)
            {
                changed = effects[i] != _effectBuffer[i];
            }

            if (changed)
            {
                effects.Clear();
                effects.AddRange(_effectBuffer);
            }

            if (effects.Count == 0)
            {
                if (property.Value != null) property.Value = null;
                return;
            }

            if (ReferenceEquals(property.Value, effects) == false) property.Value = effects;
            else if (changed) property.ForceRefresh();
        }

        private void OnPossessed(IEntityBehaviour previous, IEntityBehaviour current)
        {
            _owner = current is { IsValid: true }
                ? current.GetBehaviour<IEffectOwnerBehaviour>()
                : null;

            Tick();
        }
    }
}
