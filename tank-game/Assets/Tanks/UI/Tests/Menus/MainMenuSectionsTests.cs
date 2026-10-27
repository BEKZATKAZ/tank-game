using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Common;
using Tanks.Core.Common.Scenes;
using Tanks.Core.Lobbies;
using Tanks.Core.Lobbies.Payloads;
using Tanks.Core.Lobbies.UseCases;
using Tanks.Localization;
using Tanks.Shared.Playground;
using Tanks.UI.Menus;
using Tanks.UI.Menus.Dialogs;
using Tanks.UI.Menus.Loading;
using Tanks.UI.Menus.Main;

namespace Tanks.UI.Tests.Menus
{
    public class MainMenuSectionsTests : ViewModelTestBase<MainMenuSectionsViewModel>
    {
        private IAuthService _auth;
        private ILobbyService _lobbies;
        private ILobbyChatService _chat;
        private ILocalizationDatabase _localization;
        private ILoadingScreenService _loading;
        private IDialogScreenService _dialog;
        private IMenuNavigation _navigation;
        private ISceneLoader _sceneLoader;
        private IRunServerUseCase _runServer;

        protected override MainMenuSectionsViewModel CreateViewModel()
        {
            _auth = Substitute.For<IAuthService>();
            _lobbies = Substitute.For<ILobbyService>();
            _chat = Substitute.For<ILobbyChatService>();
            _chat.ConnectAsync(Arg.Any<LobbyId>()).Returns(Task.FromResult(Result.Success()));
            _chat.DisconnectAsync().Returns(Task.FromResult(Result.Success()));
            _lobbies.LeaveLobbyAsync().Returns(Task.FromResult(Result.Success()));
            _localization = Substitute.For<ILocalizationDatabase>();
            _loading = Substitute.For<ILoadingScreenService>();
            _dialog = Substitute.For<IDialogScreenService>();
            _navigation = Substitute.For<IMenuNavigation>();
            _sceneLoader = Substitute.For<ISceneLoader>();
            _runServer = Substitute.For<IRunServerUseCase>();

            var profile = new PlayerProfileState();
            profile.DisplayName.Value = "Player";
            _auth.CurrentProfile.Returns(profile);

            return new MainMenuSectionsViewModel(
                _auth, new EnterLobbyUseCase(_lobbies, _chat), _localization, _loading, _dialog,
                _navigation, _sceneLoader, _runServer);
        }

        [Test]
        public void OpenSection_ChangesSection()
        {
            ViewModel.OpenSection(MainMenuSection.Settings);
            Assert.AreEqual(MainMenuSection.Settings, ViewModel.Section.Value);
        }

        [Test]
        public void ShowQuitGameConfirmation_OpensDialog()
        {
            ViewModel.ShowQuitGameConfirmation();
            _dialog.Received(1).ShowConfirmation("quit", Arg.Any<Action>());
        }

        [Test]
        public void OpenTutorial_Success_LoadsPlayground()
        {
            _runServer
                .RunAsync(RequestedServerScene.Playground, authorize: false)
                .Returns(UniTask.FromResult(Result.Success()));

            _sceneLoader.StartLoading(Arg.Any<LoadPlayground>(), true, true);
            ViewModel.OpenTutorial().Forget();

            _sceneLoader.Received(1).StartLoading(Arg.Any<LoadPlayground>(), true, true);
            _loading.Received(1).End();
            _dialog.DidNotReceiveWithAnyArgs().ShowErrorFromCode(default, default);
        }

        [Test]
        public void OpenTutorial_ServerFailure_ShowsError()
        {
            _runServer.RunAsync(RequestedServerScene.Playground, authorize: false)
                .Returns(UniTask.FromResult(Result.Failure("start-failed")));

            ViewModel.OpenTutorial().Forget();

            _dialog.Received(1).ShowErrorFromCode("error.lobby", "start-failed");
            _loading.Received(1).End();
            _sceneLoader.DidNotReceiveWithAnyArgs()
                .StartLoading(Arg.Any<LoadPlayground>(), true, true);
        }

        [Test]
        public void CreateLobby_Success_UsesLocalizedNameAndNavigates()
        {
            ILocalizedString name = Substitute.For<ILocalizedString>();
            name.Get().Returns("Player's lobby");

            _localization
                .TryGet("Menus", "menu.lobby.default-name", out Arg.Any<ILocalizedString>())
                .Returns(args =>
                {
                    args[2] = name;
                    return true;
                });

            _runServer.RunAsync().Returns(UniTask.FromResult(Result.Success()));

            _lobbies.HostLobbyAsync("Player's lobby", 4)
                .Returns(Task.FromResult(Result.Success(new LobbyState())));

            ViewModel.CreateLobby().Forget();

            name.Received(1).AddVariable("name", "Player");
            _lobbies.Received(1).HostLobbyAsync("Player's lobby", 4);
            _navigation.Received(1).Navigate(MenuType.Lobby);
            _loading.Received(1).End();
        }

        [Test]
        public void CreateLobby_ServerFailure_ShowsError()
        {
            _runServer.RunAsync().Returns(UniTask.FromResult(Result.Failure("start-failed")));

            ViewModel.CreateLobby().Forget();

            _dialog.Received(1).ShowErrorFromCode("error.lobby", "start-failed");
            _lobbies.DidNotReceiveWithAnyArgs().HostLobbyAsync(default, default);
            _loading.Received(1).End();
        }

        [Test]
        public void CreateLobby_HostFailure_ShowsError()
        {
            _runServer.RunAsync().Returns(UniTask.FromResult(Result.Success()));

            _lobbies.HostLobbyAsync("My lobby", 4)
                .Returns(Task.FromResult(Result.Failure<LobbyState>("host-failed")));

            ViewModel.CreateLobby().Forget();

            _dialog.Received(1).ShowErrorFromCode("error.lobby", "host-failed");
            _navigation.DidNotReceiveWithAnyArgs().Navigate(default);
            _loading.Received(1).End();
        }

        [Test]
        public void CreateLobby_WaitsForChatBeforeEndingLoading()
        {
            _runServer.RunAsync().Returns(UniTask.FromResult(Result.Success()));
            _lobbies.HostLobbyAsync(Arg.Any<string>(), Arg.Any<uint>())
                .Returns(Task.FromResult(Result.Success(new LobbyState())));
            var connected = new TaskCompletionSource<ResultType>();
            _chat.ConnectAsync(Arg.Any<LobbyId>()).Returns(connected.Task);

            ViewModel.CreateLobby().Forget();

            _loading.DidNotReceive().End();
            _navigation.DidNotReceive().Navigate(MenuType.Lobby);

            connected.SetResult(Result.Success());

            _loading.Received(1).End();
            _navigation.Received(1).Navigate(MenuType.Lobby);
        }

        [Test]
        public void CreateLobby_ChatFailure_LeavesAndKeepsMainMenu()
        {
            _runServer.RunAsync().Returns(UniTask.FromResult(Result.Success()));
            _lobbies.HostLobbyAsync(Arg.Any<string>(), Arg.Any<uint>())
                .Returns(Task.FromResult(Result.Success(new LobbyState())));
            _chat.ConnectAsync(Arg.Any<LobbyId>()).Returns(Task.FromResult(Result.Failure("chat-unavailable")));

            ViewModel.CreateLobby().Forget();

            _lobbies.Received(1).LeaveLobbyAsync();
            _loading.Received(1).End();
            _dialog.Received(1).ShowErrorFromCode("error.lobby", "chat-unavailable");
            _navigation.DidNotReceive().Navigate(MenuType.Lobby);
        }
    }
}
