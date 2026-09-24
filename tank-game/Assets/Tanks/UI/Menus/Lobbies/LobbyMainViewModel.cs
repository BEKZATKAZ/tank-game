using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using Tanks.Core;
using Tanks.Core.Lobbies;
using Tanks.Core.Lobbies.UseCases;
using Tanks.Localization;
using Tanks.UI.Menus.Dialogs;
using Tanks.UI.Menus.Loading;
using Tanks.Utils;
using UnityEngine;

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
        }

        public IReadOnlyBindableProperty<IDictionary<string, LobbyPlayerState>> Players { get; }
        public BindableProperty<bool> CanStartGame { get; } = new();
        public BindableProperty<bool> IsHost { get; } = new();

        private readonly IAuthService _auth;
        private readonly ILobbyService _service;
        private readonly IMenuNavigation _navigation;
        private readonly IDialogScreenService _dialog;
        private readonly ILoadingScreenService _loading;
        private readonly ILocalizationDatabase _localization;
        private readonly IPrepareServerUseCase _startServer;

        private void OnStatusChanged(LobbyStatus status)
        {
            CanStartGame.Value = status is LobbyStatus.WaitingForPlayers;
        }

        private void OnHostChanged(string hostId)
        {
            string playerId = _auth.CurrentProfile.UserId.Value;
            IsHost.Value = playerId == hostId;
        }

        public void Dispose()
        {
            _service.CurrentLobbyState.Status.Unbind(OnStatusChanged);
            _service.CurrentLobbyState.HostId.Unbind(OnHostChanged);
        }

        public async UniTask StartGameAsync()
        {
            CanStartGame.Value = false;
            ResultType<string> result = await _startServer.PrepareAsync();

            if (result.IsSuccess) return;
            _dialog.ShowErrorFromCode("error.lobby", result.ErrorCode);
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
