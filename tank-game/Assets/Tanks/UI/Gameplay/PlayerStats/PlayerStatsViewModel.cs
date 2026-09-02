using System;
using Tanks.Client.Entities;
using Tanks.Client.Entities.Health;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
using Tanks.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.UI.Gameplay.PlayerStats
{
    public sealed class PlayerStatsViewModel :
        IViewModel,
        IMessageHandler<HealthChangedFromPrediction>,
        IMessageHandler<HealthChangedFromServer>,
        IDisposable
    {
        public PlayerStatsViewModel(IMessageBus bus, IClientEntityService clientEntityService)
        {
            _clientEntityService = clientEntityService;
            _bus = bus;

            _bus.RegisterHandler<HealthChangedFromPrediction>(this);
            _bus.RegisterHandler<HealthChangedFromServer>(this);

            _clientEntityService.Changed += OnPlayerChanged;
            _currentPlayer ??= _clientEntityService.GetPossessedEntity();
        }

        public BindableProperty<float> InstantHealthOverlay { get; } = new(1);
        public BindableProperty<float> RedHealthOverlay { get; } = new(1);
        public BindableProperty<Color> TankColor { get; } = new(Color.white);

        private readonly IMessageBus _bus;
        private readonly IClientEntityService _clientEntityService;
        private IEntityBehaviour _currentPlayer;
        private float _lastFrozenTime;
        private float _currentTime;
        private float _latestAuthoritativeOverlay = 1;

        public void Dispose()
        {
            _clientEntityService.Changed -= OnPlayerChanged;
        }

        public void Handle(HealthChangedFromServer message, IMessageSender sender)
        {
            if (_currentPlayer != message.Target) return;

            OnHealthChanged(message.OldState, message.AuthoritativeState);
            _latestAuthoritativeOverlay = InstantHealthOverlay.Value;

            // Authoritative health arrives, overlay unfreezes
            _lastFrozenTime = -1;
        }

        public void Handle(HealthChangedFromPrediction message, IMessageSender sender)
        {
            if (_currentPlayer != message.Target) return;
            OnHealthChanged(message.OldState, message.NewState);

            // Freeze overlay and wait for authoritative health
            _lastFrozenTime = _currentTime;
        }

        public void Tick(float deltaTime)
        {
            _currentTime += deltaTime;

            float elapsed = _currentTime - _lastFrozenTime;
            float timeout = 2;

            // Wait until authoritative health arrives or timeout in 2 seconds
            if (_lastFrozenTime >= 0)
            {
                if (elapsed < timeout) return;
                _lastFrozenTime = -1;

                // Reset back to latest state if no new authoritative state arrived
                InstantHealthOverlay.Value = _latestAuthoritativeOverlay;
            }

            UpdateColor(deltaTime);
            UpdateRedOverlay(deltaTime);
        }

        private void OnPlayerChanged(IEntityBehaviour previous, IEntityBehaviour current)
        {
            _currentPlayer = current;
            HealthState state = current.GetBehaviour<IHealthBehaviour>().ServerState.Value;

            OnHealthChanged(state, state);
            TankColor.Value = Color.white;
        }

        private void OnHealthChanged(in HealthState oldState, in HealthState newState)
        {
            float old = oldState.Current;
            float current = newState.Current;
            float max = newState.Max;

            InstantHealthOverlay.Value = current / max;
            if (old == current) return;

            TankColor.Value = old > current ? Color.red : Color.green;
        }

        private void UpdateColor(float deltaTime)
        {
            if (TankColor.Value == Color.white) return;

            float t = deltaTime * 5;
            TankColor.Value = Color.Lerp(TankColor.Value, Color.white, t);
        }

        private void UpdateRedOverlay(float deltaTime)
        {
            float target = InstantHealthOverlay.Value;

            if (RedHealthOverlay.Value == target) return;

            // Instant if healed
            if (RedHealthOverlay.Value < target)
            {
                RedHealthOverlay.Value = target;
                return;
            }

            // Prevent unnoticeable UI changes
            if (math.abs(RedHealthOverlay.Value - target) < 0.001f)
            {
                RedHealthOverlay.Value = target;
                return;
            }

            float t = deltaTime * 5;
            RedHealthOverlay.Value = math.lerp(RedHealthOverlay.Value, target, t);
        }
    }
}
