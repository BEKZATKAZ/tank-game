using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Core;
using Tanks.Core.Lobbies;
using Tanks.Core.Lobbies.Payloads;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Core.Lobbies.Sessions.DTOs;
using Tanks.Localization;
using Tanks.UI.Menus.Dialogs;
using Tanks.Utils;

namespace Tanks.UI.Menus.Lobbies
{
    public enum LobbySetting
    {
        MaxScore,
        SelfDamagePercent,
        MapSize,
        MapThickness,
        ItemSpawnRate,
        DangerZoneDelay,
        DangerZoneDuration,
        DangerZoneDamage
    }

    public sealed class LobbySettingOption
    {
        public LobbySetting Type;
        public string Label;
        public List<string> Choices;
        public int SelectedIndex;
    }

    public sealed class LobbySettingsViewModel : IViewModel, IDisposable
    {
        public LobbySettingsViewModel(
            ILobbyService service,
            IAuthService auth,
            ISessionSettingsService settings,
            ILocalizationDatabase localization,
            IDialogScreenService dialog)
        {
            _service = service;
            _auth = auth;
            _defaults = settings.DefaultSettings;
            _localization = localization;
            _dialog = dialog;

            _service.CurrentLobbyState.Settings.Bind(OnSettingsChanged);
            _service.CurrentLobbyState.HostId.Bind(OnHostChanged);
            _service.CurrentLobbyState.Status.Bind(OnStatusChanged);
            _service.CurrentLobbyState.Id.Bind(OnLobbyChanged);
            _service.CurrentLobbyState.IsUpdatingSettings.Bind(OnUpdatingChanged);
            _localization.LocaleChanged += OnLocaleChanged;
        }

        public BindableProperty<IReadOnlyList<LobbySettingOption>> Settings { get; } = new();
        public BindableProperty<bool> CanEdit { get; } = new();

        private readonly ILobbyService _service;
        private readonly IAuthService _auth;
        private readonly ISessionDefaultSettingsConfig _defaults;
        private readonly ILocalizationDatabase _localization;
        private readonly IDialogScreenService _dialog;

        private bool _disposed;

        private SessionSettingsFromLobby CurrentSettings =>
            _service.CurrentLobbyState.LobbyData.ContainsKey("settings")
                ? _service.CurrentLobbyState.Settings.Value
                : SessionSettingsMapper.FromDefaultToLobby(_defaults);

        private void OnSettingsChanged(SessionSettingsFromLobby settings) => Refresh();
        private void OnHostChanged(string host) => RefreshPermissions();
        private void OnStatusChanged(LobbyStatus status) => RefreshPermissions();
        private void OnUpdatingChanged(bool updating) => RefreshPermissions();
        private void OnLobbyChanged(LobbyId id) => Refresh();
        private void OnLocaleChanged(string locale) => Refresh();

        private void RefreshPermissions()
        {
            LobbyState lobby = _service.CurrentLobbyState;
            CanEdit.Value = _disposed == false && lobby.IsUpdatingSettings.Value == false &&
                string.IsNullOrEmpty(lobby.Id.Value.Value) == false &&
                lobby.HostId.Value == _auth.CurrentProfile.UserId.Value &&
                lobby.Status.Value == LobbyStatus.WaitingForPlayers;
        }

        private string Localize(string key)
        {
            return _localization.TryGet("Menus", key, out var text) ? text.Get() : key;
        }

        private LobbySettingOption Numeric(
            LobbySetting type, string key, PossibleIntegers values, int current, string suffix = "")
        {
            return new LobbySettingOption
            {
                Type = type,
                Label = Localize("menu.lobby.settings." + key),
                Choices = values.Values.Select(x => x.ToString() + suffix).ToList(),
                SelectedIndex = Array.IndexOf(values.Values, values.CorrectValue(current))
            };
        }

        private LobbySettingOption EnumOption<T>(LobbySetting type, string key, T current) where T : Enum
        {
            T[] values = (T[])Enum.GetValues(typeof(T));
            return new LobbySettingOption
            {
                Type = type,
                Label = Localize("menu.lobby.settings." + key),
                Choices = values.Select(x => Localize(
                    "menu.lobby.settings." + key + "." + x.ToString().ToLowerInvariant())).ToList(),
                SelectedIndex = Math.Max(Array.IndexOf(values, current), 0)
            };
        }

        private void Refresh()
        {
            SessionSettingsFromLobby settings = CurrentSettings;
            Settings.Value = new[]
            {
                Numeric(LobbySetting.MaxScore, "max-score", _defaults.MaxScoreToReach, settings.MaxScore),
                Numeric(LobbySetting.SelfDamagePercent, "self-damage-percent",
                    _defaults.SelfDamagePercent, settings.SelfDamagePercent, "%"),
                EnumOption(LobbySetting.MapSize, "map-size", settings.MapGeneration.Size),
                EnumOption(LobbySetting.MapThickness, "wall-thickness", settings.MapGeneration.Thickness),
                EnumOption(LobbySetting.ItemSpawnRate, "item-spawn-rate", settings.ItemSpawn.Rate),
                Numeric(LobbySetting.DangerZoneDelay, "danger-zone-delay",
                    _defaults.DangerZone.StartDelaySeconds, settings.DangerZone.StartDelaySeconds),
                Numeric(LobbySetting.DangerZoneDuration, "danger-zone-duration",
                    _defaults.DangerZone.DurationSeconds, settings.DangerZone.DurationSeconds),
                Numeric(LobbySetting.DangerZoneDamage, "danger-zone-damage",
                    _defaults.DangerZone.DamagePerSecond, settings.DangerZone.DamagePerSecond)
            };
            RefreshPermissions();
        }

        public async UniTask SetValueAsync(LobbySetting type, int index)
        {
            if (CanEdit.Value == false) return;
            LobbySettingOption option = Settings.Value.FirstOrDefault(x => x.Type == type);
            if (option == null || index < 0 || index >= option.Choices.Count) return;
            if (index == option.SelectedIndex) return;

            LobbyState lobby = _service.CurrentLobbyState;
            LobbyId lobbyId = lobby.Id.Value;
            lobby.IsUpdatingSettings.Value = true;
            await lobby.SettingsLock.WaitAsync();

            try
            {
                if (_disposed || lobby.Id.Value.Value != lobbyId.Value ||
                    lobby.HostId.Value != _auth.CurrentProfile.UserId.Value ||
                    lobby.Status.Value != LobbyStatus.WaitingForPlayers) return;

                SessionSettingsFromLobby settings = CurrentSettings;
                switch (type)
                {
                    case LobbySetting.MaxScore:
                        settings.MaxScore = _defaults.MaxScoreToReach.Values[index];
                        break;
                    case LobbySetting.SelfDamagePercent:
                        settings.SelfDamagePercent = _defaults.SelfDamagePercent.Values[index];
                        break;
                    case LobbySetting.MapSize:
                        settings.MapGeneration.Size = (MapSize)index;
                        break;
                    case LobbySetting.MapThickness:
                        settings.MapGeneration.Thickness = (MapThickness)index;
                        break;
                    case LobbySetting.ItemSpawnRate:
                        settings.ItemSpawn.Rate = (ItemSpawnRate)index;
                        break;
                    case LobbySetting.DangerZoneDelay:
                        settings.DangerZone.StartDelaySeconds = _defaults.DangerZone.StartDelaySeconds.Values[index];
                        break;
                    case LobbySetting.DangerZoneDuration:
                        settings.DangerZone.DurationSeconds = _defaults.DangerZone.DurationSeconds.Values[index];
                        break;
                    case LobbySetting.DangerZoneDamage:
                        settings.DangerZone.DamagePerSecond = _defaults.DangerZone.DamagePerSecond.Values[index];
                        break;
                }

                ResultType<LobbyState> result = await _service.UpdateLobbyAsync(new LobbyUpdateRequest
                {
                    Settings = settings
                });

                if (_disposed || lobby.Id.Value.Value != lobbyId.Value) return;
                if (result.IsSuccess == false) _dialog.ShowErrorFromCode("error.lobby", result.ErrorCode);
            }
            catch (Exception)
            {
                if (_disposed == false && lobby.Id.Value.Value == lobbyId.Value)
                {
                    _dialog.ShowErrorFromCode("error.lobby", "failed-to-update");
                }
            }
            finally
            {
                lobby.SettingsLock.Release();
                lobby.IsUpdatingSettings.Value = false;
                if (_disposed == false) Refresh();
            }
        }

        public void Dispose()
        {
            _disposed = true;
            CanEdit.Value = false;

            _service.CurrentLobbyState.Settings.Unbind(OnSettingsChanged);
            _service.CurrentLobbyState.HostId.Unbind(OnHostChanged);
            _service.CurrentLobbyState.Status.Unbind(OnStatusChanged);
            _service.CurrentLobbyState.Id.Unbind(OnLobbyChanged);
            _service.CurrentLobbyState.IsUpdatingSettings.Unbind(OnUpdatingChanged);
            _localization.LocaleChanged -= OnLocaleChanged;
        }
    }
}
