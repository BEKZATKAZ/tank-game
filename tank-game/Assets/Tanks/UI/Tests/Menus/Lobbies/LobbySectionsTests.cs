using NSubstitute;
using NUnit.Framework;
using System;
using Tanks.Core.Lobbies;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Core.Lobbies.UseCases;
using Tanks.Localization;
using Tanks.UI.Menus;
using Tanks.UI.Menus.Dialogs;
using Tanks.UI.Menus.Lobbies;

namespace Tanks.UI.Tests.Menus.Lobbies
{
    public class LobbySectionsTests : ViewModelTestBase<LobbySectionsViewModel>
    {
        private ILobbyService _service;
        private ILocalizationDatabase _localization;
        private IDialogScreenService _dialog;
        private IMenuNavigation _navigation;
        private IConnectToServerUseCase _connectToServer;
        private LobbyState _lobby;

        protected override LobbySectionsViewModel CreateViewModel()
        {
            _service = Substitute.For<ILobbyService>();
            _localization = Substitute.For<ILocalizationDatabase>();
            _dialog = Substitute.For<IDialogScreenService>();
            _navigation = Substitute.For<IMenuNavigation>();
            _connectToServer = Substitute.For<IConnectToServerUseCase>();
            _lobby = new LobbyState();
            _service.CurrentLobbyState.Returns(_lobby);
            return new LobbySectionsViewModel(
                _service, _localization, _dialog, _navigation, _connectToServer);
        }

        [Test]
        public void LobbyName_UsesCurrentLobby()
        {
            Assert.AreSame(_lobby.Name, ViewModel.LobbyName);
        }

        [TestCase(LobbyStatus.PreparingServer, "menu.lobby.preparing-server")]
        [TestCase(LobbyStatus.Connecting, "menu.lobby.connecting")]
        public void Status_UsesLocalizedMessage(LobbyStatus status, string entry)
        {
            _localization.TryGet("Menus", entry, out Arg.Any<ILocalizedString>())
                .Returns(args =>
                {
                    ILocalizedString text = Substitute.For<ILocalizedString>();
                    text.Get().Returns("Localized status");
                    args[2] = text;
                    return true;
                });

            _lobby.Status.Value = status;
            Assert.AreEqual("Localized status", ViewModel.Status.Value);
        }

        [Test]
        public void ConfirmedSettings_ConnectsToServer()
        {
            var settings = new SessionSettings { DesiredPlayerCount = 4 };
            _lobby.JoinCode.Value = "join-code";

            _lobby.ServerConfirmedSettings.Value = settings;

            _connectToServer.Received(1).Load("join-code", settings);
        }

        [Test]
        public void DefaultSettings_DoNotConnect()
        {
            _lobby.ServerConfirmedSettings.Value = new SessionSettings { MaxScore = 10 };
            _connectToServer.DidNotReceiveWithAnyArgs().Load(default, default);
        }

        [Test]
        public void LobbyDeleted_ShowsErrorAndReturnsToMain()
        {
            _service.LobbyDeleted += Raise.Event<Action>();

            _dialog.Received(1).ShowErrorFromCode("error.lobby", "lobby-disbanded");
            _navigation.Received(1).Navigate(MenuType.Main);
        }
    }
}
