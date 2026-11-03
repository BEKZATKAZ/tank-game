using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using Tanks.Core;
using Tanks.Core.Lobbies;
using Tanks.Core.Lobbies.Payloads;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Core.Lobbies.UseCases;
using Tanks.Localization;
using Tanks.UI.Menus.Dialogs;
using Tanks.UI.Menus.Loading;
using Tanks.UI.Menus.Sections;
using Tanks.Utils;

namespace Tanks.UI.Menus.LobbyRoom
{
    public sealed class LobbyRoomViewModel : IViewModel, IDisposable
    {
        public LobbyRoomViewModel(
            ILobbyService service,
            IAuthService auth,
            IDialogScreenService dialog,
            IMenuSectionService navigation,
            ILoadingScreenService loading,
            ILocalizationDatabase localization,
            IPrepareServerUseCase startServer,
            IConnectToServerUseCase connectToServer,
            LobbySettingsViewModel settings)
        {
            _service = service;
            _auth = auth;
            _dialog = dialog;
            _navigation = navigation;
            _loading = loading;
            _localization = localization;
            _startServer = startServer;
            _connectToServer = connectToServer;

            Settings = settings;
            Settings.CanEdit.Bind(OnCanEditChanged);

            _service.CurrentLobbyState.Players.Bind(OnPlayersChanged);
            _service.CurrentLobbyState.HostId.Bind(OnHostChanged);
            _service.CurrentLobbyState.Id.Bind(OnLobbyChanged);
            _localization.LocaleChanged += OnLocaleChanged;

            _service.CurrentLobbyState.Status.Bind(OnStatusChanged);
            _service.CurrentLobbyState.ServerConfirmedSettings.Bind(OnConnectionAllowed);
            _initialized = true;
            RefreshReadiness();
        }

        public LobbySettingsViewModel Settings { get; }
        public BindableProperty<bool> CanStart { get; } = new();
        public BindableProperty<bool> CanReady { get; } = new();
        public BindableProperty<string> ReadyText { get; } = new();
        public BindableProperty<string> Status { get; } = new();

        public IReadOnlyBindableProperty<string> Name => _service.CurrentLobbyState.Name;
        public IReadOnlyBindableProperty<LobbyAddress> Address => _service.CurrentLobbyState.Address;

        private readonly ILobbyService _service;
        private readonly IAuthService _auth;
        private readonly IDialogScreenService _dialog;
        private readonly IMenuSectionService _navigation;
        private readonly ILoadingScreenService _loading;
        private readonly ILocalizationDatabase _localization;
        private readonly IPrepareServerUseCase _startServer;
        private readonly IConnectToServerUseCase _connectToServer;

        private bool _isStarting;
        private bool _isUpdatingReady;
        private bool _startAttempted;
        private bool _initialized;
        private bool _disposed;

        private void OnStatusChanged(LobbyStatus status)
        {
            RefreshReadiness();
            string entry = status switch
            {
                LobbyStatus.PreparingServer => "preparing-server",
                LobbyStatus.Connecting => "connecting",
                _ => null
            };

            if (entry is null)
            {
                Status.Value = "";
                return;
            }

            _localization.TryGet("Menus", $"menu.lobby.{entry}", out var statusText);
            Status.Value = statusText?.Get() ?? "";
        }

        private void OnCanEditChanged(bool canEdit)
        {
            RefreshReadiness();
        }

        private void OnPlayersChanged(IDictionary<string, LobbyPlayerState> players) => RefreshReadiness();
        private void OnHostChanged(string host) => RefreshReadiness();
        private void OnLocaleChanged(string locale) => RefreshReadiness();

        private void OnLobbyChanged(LobbyId id)
        {
            _startAttempted = false;
            RefreshReadiness();
        }

        private void RefreshReadiness()
        {
            LobbyState lobby = _service.CurrentLobbyState;
            bool member = lobby.Players.Value.TryGetValue(_auth.CurrentProfile.UserId.Value ?? "", out var player);
            bool waiting = _disposed == false && _isStarting == false &&
                string.IsNullOrEmpty(lobby.Id.Value.Value) == false &&
                lobby.Status.Value == LobbyStatus.WaitingForPlayers;

            CanReady.Value = waiting && member && _isUpdatingReady == false;
            string entry = player.IsReady ? "unready" : "ready";
            _localization.TryGet("Menus", $"menu.lobby.{entry}", out var text);
            ReadyText.Value = text?.Get() ?? entry.ToUpperInvariant();
            CanStart.Value = waiting && Settings.CanEdit.Value && member &&
                lobby.HostId.Value == _auth.CurrentProfile.UserId.Value && lobby.AllPlayersReady;

            if (lobby.AllPlayersReady == false) _startAttempted = false;
            if (_initialized && CanStart.Value && _startAttempted == false) StartGameAsync().Forget();
        }

        public async UniTask ToggleReadyAsync()
        {
            if (CanReady.Value == false) return;
            LobbyState lobby = _service.CurrentLobbyState;
            LobbyId id = lobby.Id.Value;
            bool ready = lobby.Players.Value[_auth.CurrentProfile.UserId.Value].IsReady;
            _isUpdatingReady = true;
            RefreshReadiness();

            try
            {
                ResultType result = await _service.SetReadyAsync(!ready);
                if (_disposed || lobby.Id.Value.Value != id.Value) return;
                if (result.IsSuccess == false) _dialog.ShowErrorFromCode("error.lobby", result.ErrorCode);
            }
            catch (Exception)
            {
                if (_disposed == false && lobby.Id.Value.Value == id.Value)
                {
                    _dialog.ShowErrorFromCode("error.lobby", "failed-to-update");
                }
            }
            finally
            {
                _isUpdatingReady = false;
                if (_disposed == false) RefreshReadiness();
            }
        }

        private void OnConnectionAllowed(SessionSettings settings)
        {
            // Join code is updated before settings
            string code = _service.CurrentLobbyState.JoinCode.Value;

            // Prevent false updates when we switch to another lobby
            if (string.IsNullOrWhiteSpace(code)) return;

            _connectToServer.Load(code, settings);
        }

        public async UniTask StartGameAsync()
        {
            if (CanStart.Value == false || _isStarting || _startAttempted ||
                _service.CurrentLobbyState.HostId.Value != _auth.CurrentProfile.UserId.Value ||
                _service.CurrentLobbyState.AllPlayersReady == false) return;

            _startAttempted = true;
            _isStarting = true;
            Settings.SetStarting(true);
            LobbyId lobbyId = _service.CurrentLobbyState.Id.Value;

            try
            {
                ResultType<string> result = await _startServer.PrepareAsync();
                
                if (_disposed || _service.CurrentLobbyState.Id.Value.Value != lobbyId.Value) return;
                if (result.IsSuccess) return;

                _dialog.ShowErrorFromCode("error.lobby", result.ErrorCode);
                
                await RestoreWaitingAsync(lobbyId);
            }
            catch (Exception)
            {
                if (_disposed == false && _service.CurrentLobbyState.Id.Value.Value == lobbyId.Value)
                {
                    _dialog.ShowErrorFromCode("error.lobby", "failed-to-update");
                    await RestoreWaitingAsync(lobbyId);
                }
            }
            finally
            {
                _isStarting = false;
                Settings.SetStarting(false);
            }
        }

        private async UniTask RestoreWaitingAsync(LobbyId lobbyId)
        {
            if (_service.CurrentLobbyState.HostId.Value != _auth.CurrentProfile.UserId.Value) return;

            try
            {
                ResultType restored = await _service.UpdateLobbyAsync(new LobbyUpdateRequest
                {
                    Status = LobbyStatus.WaitingForPlayers
                });

                if (_disposed || _service.CurrentLobbyState.Id.Value.Value != lobbyId.Value) return;
                if (restored.IsSuccess == false) _dialog.ShowErrorFromCode("error.lobby", restored.ErrorCode);
            }
            catch (Exception)
            {
                if (_disposed == false && _service.CurrentLobbyState.Id.Value.Value == lobbyId.Value)
                {
                    _dialog.ShowErrorFromCode("error.lobby", "failed-to-update");
                }
            }
        }

        public void Dispose()
        {
            _disposed = true;
            CanStart.Value = false;
            CanReady.Value = false;

            Settings.CanEdit.Unbind(OnCanEditChanged);
            _service.CurrentLobbyState.Players.Unbind(OnPlayersChanged);
            _service.CurrentLobbyState.HostId.Unbind(OnHostChanged);
            _service.CurrentLobbyState.Id.Unbind(OnLobbyChanged);
            _localization.LocaleChanged -= OnLocaleChanged;
            Settings.Dispose();

            _service.CurrentLobbyState.Status.Unbind(OnStatusChanged);
            _service.CurrentLobbyState.ServerConfirmedSettings.Unbind(OnConnectionAllowed);
        }

        public void LeaveLobbyConfirmation()
        {
            _dialog.ShowConfirmation("leave-lobby", onYes: async () =>
            {
                _localization.TryGet("Menus", "menu.lobby.leaving", out var leaving);
                _loading.Begin(leaving);
                _navigation.OpenSection(MenuSectionType.None, instant: true);

                ResultType result = await _service.LeaveLobbyAsync();
                _loading.End();

                if (result.IsSuccess)
                {
                    _navigation.OpenSection(MenuSectionType.Main);
                    return;
                }

                _dialog.ShowErrorFromCode("error.lobby", result.ErrorCode);
                _navigation.OpenSection(MenuSectionType.LobbyRoom, instant: true);
            });
        }
    }
}
