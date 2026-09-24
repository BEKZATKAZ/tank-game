using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using System;
using Tanks.Core;
using Tanks.Core.Common;
using Tanks.Core.Lobbies;
using Tanks.Core.Lobbies.UseCases;
using Tanks.Localization;
using Tanks.UI.Menus;
using Tanks.UI.Menus.Dialogs;
using Tanks.UI.Menus.Loading;
using Tanks.UI.Menus.Lobbies;

namespace Tanks.UI.Tests.Menus.Lobbies
{
    public class LobbyMainTests : ViewModelTestBase<LobbyMainViewModel>
    {
        private IAuthService _auth;
        private ILobbyService _service;
        private IMenuNavigation _navigation;
        private IDialogScreenService _dialog;
        private ILoadingScreenService _loading;
        private ILocalizationDatabase _localization;
        private IPrepareServerUseCase _prepareServer;

        protected override LobbyMainViewModel CreateViewModel()
        {
            _auth = Substitute.For<IAuthService>();
            _service = Substitute.For<ILobbyService>();
            _navigation = Substitute.For<IMenuNavigation>();
            _dialog = Substitute.For<IDialogScreenService>();
            _loading = Substitute.For<ILoadingScreenService>();
            _localization = Substitute.For<ILocalizationDatabase>();
            _prepareServer = Substitute.For<IPrepareServerUseCase>();

            _auth.CurrentProfile.Returns(new PlayerProfileState());
            _auth.CurrentProfile.UserId.Value = "test-id";

            _service.CurrentLobbyState.Returns(new LobbyState());
            _service.CurrentLobbyState.Refresh();

            return new LobbyMainViewModel(
                _auth, _service, _navigation, _dialog, _loading, _localization, _prepareServer);
        }

        [Test]
        public void StartGame_PreparesServer_NotifiesOthers()
        {
            var task = new UniTaskCompletionSource<ResultType<string>>();
            _prepareServer.PrepareAsync().Returns(task.Task);

            ViewModel.StartGameAsync().Forget();
            _prepareServer.Received(1).PrepareAsync();

            task.TrySetResult(Result.Success("some-code"));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Leave_ConfirmationYes(bool isSuccess)
        {
            var task = new UniTaskCompletionSource<ResultType>();
            _service.LeaveLobbyAsync().Returns(task.Task);

            // Clicks yes
            _dialog
                .When(x => x.ShowConfirmation("leave-lobby", Arg.Any<Action>(), Arg.Any<Action>()))
                .Do(x => ((Action)x[1])?.Invoke());

            ViewModel.LeaveLobbyConfirmation();

            _loading.Received(1).Begin(Arg.Any<ILocalizedString>());
            _loading.DidNotReceiveWithAnyArgs().End();

            task.TrySetResult(isSuccess ? Result.Success() : Result.Failure("some-error"));
            _loading.Received(1).End();

            if (isSuccess)
            {
                _navigation.Received(1).Navigate(MenuType.Main);
                return;
            }

            _dialog.Received(1).ShowErrorFromCode("error.lobby", "some-error");
        }
    }
}
