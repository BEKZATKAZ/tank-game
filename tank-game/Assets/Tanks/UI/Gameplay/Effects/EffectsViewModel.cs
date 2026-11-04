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

                _effectBuffer.Clear();
                PublishEffects(ActiveEffects, _activeEffects);
                PublishEffects(ExpiringEffects, _expiringEffects);

                return;
            }

            foreach (EffectType type in _owner.Effects.Keys)
            {
                CurrentEffect.Value = type;
                break;
            }

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
