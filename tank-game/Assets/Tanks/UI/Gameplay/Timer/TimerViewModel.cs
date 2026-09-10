using System;
using Tanks.Client.Bootstrap;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Shared;
using Tanks.Shared.Messaging;
using Tanks.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.UI.Gameplay.Timer
{
    public sealed class TimerViewModel :
        IViewModel,
        IMessageHandler<GameStartedFromClient>,
        IMessageHandler<DangerZoneStarted>,
        IDisposable
    {
        public TimerViewModel(IMessageBus bus, ISessionSettingsService settings)
        {
            _bus = bus;
            _settings = settings;

            _bus.RegisterHandler<GameStartedFromClient>(this);
            _bus.RegisterHandler<DangerZoneStarted>(this);
        }

        public BindableProperty<int> Seconds { get; } = new();
        public BindableProperty<Color> CounterColor { get; } = new();

        private readonly IMessageBus _bus;
        private readonly ISessionSettingsService _settings;

        private float _seconds;
        private float _duration;
        private bool _isDangerZoneStarted;

        public void Dispose()
        {
            _bus.UnregisterHandler<GameStartedFromClient>(this);
            _bus.UnregisterHandler<DangerZoneStarted>(this);
        }

        public void Handle(DangerZoneStarted message, IMessageSender sender)
        {
            if (_isDangerZoneStarted) return;

            _isDangerZoneStarted = true;
            _duration = _settings.GetSettings().DangerZone.DurationSeconds;
            _seconds = _duration;

            Seconds.Value = (int)math.ceil(_seconds);
            CounterColor.Value = Color.orangeRed;
        }

        public void Handle(GameStartedFromClient message, IMessageSender sender)
        {
            _duration = _settings.GetSettings().DangerZone.StartDelaySeconds;
            _seconds = _duration + message.AdaptedStartDelaySeconds;
            _isDangerZoneStarted = false;

            Seconds.Value = -1;
            CounterColor.Value = new Color(0.95f, 0.95f, 0.95f);
        }

        public void Tick(float deltaTime)
        {
            if (_seconds <= 0) return;
            _seconds = math.max(_seconds - deltaTime, 0);

            if (_seconds > _duration) return;
            int ceiled = (int)math.ceil(_seconds);

            if (Seconds.Value == ceiled) return;
            Seconds.Value = ceiled;

            // Predict danger zone start
            if (_isDangerZoneStarted || _seconds > 0) return;
            Handle(new DangerZoneStarted(), null);
        }
    }
}
