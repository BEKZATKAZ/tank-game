using System;
using Tanks.Client.Bootstrap;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Localization;
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
        public TimerViewModel(
            IMessageBus bus,
            ISessionSettingsService settings,
            ILocalizationDatabase localization)
        {
            _bus = bus;
            _settings = settings;
            _localization = localization;

            _bus.RegisterHandler<GameStartedFromClient>(this);
            _bus.RegisterHandler<DangerZoneStarted>(this);
        }

        public BindableProperty<string> Seconds { get; } = new();
        public BindableProperty<string> Label { get; } = new();
        public BindableProperty<Color> CounterColor { get; } = new();

        private readonly IMessageBus _bus;
        private readonly ISessionSettingsService _settings;
        private readonly ILocalizationDatabase _localization;

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

            Seconds.Value = ((int)math.ceil(_seconds)).ToString();
            CounterColor.Value = Color.orangeRed;

            _localization.TryGet("Gameplay", "gameplay.timer.during-zone", out var zone);
            Label.Value = zone?.Get();
        }

        public void Handle(GameStartedFromClient message, IMessageSender sender)
        {
            _duration = _settings.GetSettings().DangerZone.StartDelaySeconds;
            _seconds = _duration + message.AdaptedStartDelaySeconds;
            _isDangerZoneStarted = false;

            Seconds.Value = "";
            CounterColor.Value = new Color(0.95f, 0.95f, 0.95f);

            _localization.TryGet("Gameplay", "gameplay.timer.before-zone", out var zone);
            Label.Value = zone?.Get();
        }

        public void Tick(float deltaTime)
        {
            if (_seconds <= 0) return;
            _seconds = math.max(_seconds - deltaTime, 0);

            if (_seconds > _duration)
            {
                Label.Value = "";
                return;
            }
            else if (Label.Value == "")
            {
                _localization.TryGet("Gameplay", "gameplay.timer.before-zone", out var before);
                Label.Value = before?.Get();
            }

            Seconds.Value = ((int)math.ceil(_seconds)).ToString();

            if (_seconds > 0) return;
            _seconds = -1;

            if (_isDangerZoneStarted == false)
            {
                Handle(new DangerZoneStarted(), null);
                return;
            }

            _localization.TryGet("Gameplay", "gameplay.timer.after-zone", out var after);
            Seconds.Value = after?.Get();
            Label.Value = "";
        }
    }
}
