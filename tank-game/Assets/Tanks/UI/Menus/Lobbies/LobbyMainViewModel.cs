using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Core;
using Tanks.Core.Lobbies;
using Tanks.Core.Lobbies.Payloads;
using Tanks.Core.Lobbies.UseCases;
using Tanks.Localization;
using Tanks.UI.Menus.Dialogs;
using Tanks.UI.Menus.Loading;
using Tanks.Utils;

namespace Tanks.UI.Menus.Lobbies
{
    public sealed class LobbyMainViewModel : IViewModel, IDisposable
    {
        public LobbyMainViewModel(
            IAuthService auth,
            ILobbyService service,
            IMenuNavigation navigation,
            IDialogScreenService dialog,
            ILoadingScreenService loading,
            ILocalizationDatabase localization,
            IPrepareServerUseCase startServer)
        {
            _auth = auth;
            _service = service;
            _navigation = navigation;
            _dialog = dialog;
            _loading = loading;
            _localization = localization;
            _startServer = startServer;

            Players = service.CurrentLobbyState.Players;

            _service.CurrentLobbyState.Status.Bind(OnStatusChanged);
            _service.CurrentLobbyState.HostId.Bind(OnHostChanged);

            Players.Bind(OnPlayersChanged);

            _localization.LocaleChanged += OnLocaleChanged;
            RefreshText();
        }

        public IReadOnlyBindableProperty<IDictionary<string, LobbyPlayerState>> Players { get; }
        public BindableProperty<bool> CanToggleReady { get; } = new();
        public BindableProperty<string> ReadyButtonText { get; } = new();
        public BindableProperty<string> ReadyText { get; } = new();
        public BindableProperty<string> NotReadyText { get; } = new();

        private readonly IAuthService _auth;
        private readonly ILobbyService _service;
        private readonly IMenuNavigation _navigation;
        private readonly IDialogScreenService _dialog;
        private readonly ILoadingScreenService _loading;
        private readonly ILocalizationDatabase _localization;
        private readonly IPrepareServerUseCase _startServer;

        private bool _updatingReady;
        private bool _starting;
        private bool _startFailed;
        private bool _disposed;

        private bool IsReady => Players.Value.TryGetValue(_auth.CurrentProfile.UserId.Value, out var player) && player.IsReady;
        private bool IsHost => _service.CurrentLobbyState.HostId.Value == _auth.CurrentProfile.UserId.Value;

        private void OnStatusChanged(LobbyStatus status) => RefreshReady();
        private void OnHostChanged(string hostId) => RefreshReady();
        private void OnLocaleChanged(string locale) => RefreshText();

        private string Localize(string entry, string fallback)
        {
            return _localization.TryGet("Menus", entry, out var text) ? text.Get() : fallback;
        }

        private void RefreshText()
        {
            ReadyText.Value = Localize("menu.lobby.player-ready", "Ready");
            NotReadyText.Value = Localize("menu.lobby.player-not-ready", "Not ready");

            ReadyButtonText.Value = IsReady
                ? Localize("menu.lobby.unready", "CANCEL")
                : Localize("menu.lobby.ready", "READY");
        }

        private void RefreshReady()
        {
            CanToggleReady.Value = _disposed == false && _updatingReady == false && _starting == false &&
                _service.CurrentLobbyState.Id.Value.Value != null &&
                _service.CurrentLobbyState.Status.Value == LobbyStatus.WaitingForPlayers &&
                Players.Value.ContainsKey(_auth.CurrentProfile.UserId.Value);

            RefreshText();
        }

        private void OnPlayersChanged(IDictionary<string, LobbyPlayerState> players)
        {
            if (IsReady == false) _startFailed = false;
            RefreshReady();
            TryStartGame();
        }

        private void TryStartGame()
        {
            if (_disposed || _starting || _updatingReady || _startFailed || IsHost == false) return;
            if (CanToggleReady.Value == false || Players.Value.Count == 0) return;
            if (Players.Value.Values.All(x => x.IsReady) == false) return;

            StartGameAsync().Forget();
        }

        public async UniTask ToggleReadyAsync()
        {
            if (CanToggleReady.Value == false) return;

            _updatingReady = true;
            RefreshReady();

            try
            {
                ResultType result = await _service.SetReadyAsync(IsReady == false);

                if (_disposed) return;
                if (result.IsSuccess) return;

                _dialog.ShowErrorFromCode("error.lobby", result.ErrorCode);
            }
            finally
            {
                _updatingReady = false;

                if (_disposed == false)
                {
                    RefreshReady();
                    TryStartGame();
                }
            }
        }

        private async UniTask StartGameAsync()
        {
            _starting = true;
            LobbyId lobbyId = _service.CurrentLobbyState.Id.Value;
            RefreshReady();

            try
            {
                ResultType<string> result = await _startServer.PrepareAsync();
                
                if (_disposed || _service.CurrentLobbyState.Id.Value.Value != lobbyId.Value) return;
                if (result.IsSuccess) return;

                _startFailed = true;
                _dialog.ShowErrorFromCode("error.lobby", result.ErrorCode);
                
                ResultType restored = await _service.UpdateLobbyAsync(new LobbyUpdateRequest
                {
                    Status = LobbyStatus.WaitingForPlayers
                });

                if (_disposed || _service.CurrentLobbyState.Id.Value.Value != lobbyId.Value) return;
                
                if (restored.IsSuccess)
                {
                    _service.CurrentLobbyState.Status.Value = LobbyStatus.WaitingForPlayers;
                }
                else
                {
                    _dialog.ShowErrorFromCode("error.lobby", restored.ErrorCode);
                }
            }
            finally
            {
                _starting = false;
                if (_disposed == false) RefreshReady();
            }
        }

        public void Dispose()
        {
            _disposed = true;
            CanToggleReady.Value = false;

            _service.CurrentLobbyState.Status.Unbind(OnStatusChanged);
            _service.CurrentLobbyState.HostId.Unbind(OnHostChanged);

            Players.Unbind(OnPlayersChanged);
            _localization.LocaleChanged -= OnLocaleChanged;
        }

        public void LeaveLobbyConfirmation()
        {
            _dialog.ShowConfirmation("leave-lobby", onYes: async () =>
            {
                _localization.TryGet("Menus", "menu.lobby.leaving", out var leaving);
                _loading.Begin(leaving);

                ResultType result = await _service.LeaveLobbyAsync();
                _loading.End();

                if (result.IsSuccess)
                {
                    _navigation.Navigate(MenuType.Main);
                    return;
                }

                _dialog.ShowErrorFromCode("error.lobby", result.ErrorCode);
            });
        }
    }
}
