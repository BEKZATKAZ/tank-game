using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Tanks.Core.Lobbies.Payloads;
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

        private void EnterLobby(bool host = true)
        {
            _service.CurrentLobbyState.Id.Value = new LobbyId("lobby");
            _service.CurrentLobbyState.HostId.Value = host ? "test-id" : "other";
            _prepareServer.PrepareAsync().Returns(UniTask.FromResult(Result.Success("code")));
            SetPlayer("test-id", false);
        }

        private void SetPlayer(string id, bool ready)
        {
            _service.CurrentLobbyState.Players.Value[id] = new LobbyPlayerState
            {
                UserId = id,
                Data = new Dictionary<string, string> { ["ready"] = ready.ToString() }
            };
            _service.CurrentLobbyState.Players.ForceRefresh();
        }

        [Test]
        public void AllReady_HostPreparesOnce_EvenWithDuplicateEvents()
        {
            EnterLobby();
            SetPlayer("guest", false);
            SetPlayer("test-id", true);
            _prepareServer.DidNotReceive().PrepareAsync();
            var task = new UniTaskCompletionSource<ResultType<string>>();
            _prepareServer.PrepareAsync().Returns(task.Task);
            SetPlayer("guest", true);
            _service.CurrentLobbyState.Players.ForceRefresh();
            _prepareServer.Received(1).PrepareAsync();
            Assert.That(ViewModel.CanToggleReady.Value, Is.False);
            task.TrySetResult(Result.Success("code"));
        }

        [Test]
        public void SoloHost_ReadyStartsGame()
        {
            EnterLobby();
            SetPlayer("test-id", true);
            _prepareServer.Received(1).PrepareAsync();
        }

        [Test]
        public void Guest_AllReady_DoesNotPrepareServer()
        {
            EnterLobby(false);
            SetPlayer("other", true);
            SetPlayer("test-id", true);
            _prepareServer.DidNotReceive().PrepareAsync();
        }

        [Test]
        public void UnreadyMemberLeaves_RemainingReadyHostStarts()
        {
            EnterLobby();
            SetPlayer("guest", false);
            SetPlayer("test-id", true);
            _service.CurrentLobbyState.Players.Value.Remove("guest");
            _service.CurrentLobbyState.Players.ForceRefresh();
            _prepareServer.Received(1).PrepareAsync();
        }

        [Test]
        public void NewUnreadyMember_BlocksStart()
        {
            EnterLobby();
            SetPlayer("guest", false);
            SetPlayer("new", false);
            SetPlayer("test-id", true);
            SetPlayer("guest", true);
            _prepareServer.DidNotReceive().PrepareAsync();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ToggleReady_DisablesUntilResponse_ReportsFailure(bool success)
        {
            EnterLobby(false);
            var task = new UniTaskCompletionSource<ResultType>();
            _service.SetReadyAsync(true).Returns(task.Task);
            ViewModel.ToggleReadyAsync().Forget();
            ViewModel.ToggleReadyAsync().Forget();
            Assert.That(ViewModel.CanToggleReady.Value, Is.False);
            _service.Received(1).SetReadyAsync(true);
            task.TrySetResult(success ? Result.Success() : Result.Failure("ready-error"));
            Assert.That(ViewModel.CanToggleReady.Value, Is.True);
            if (success == false) _dialog.Received(1).ShowErrorFromCode("error.lobby", "ready-error");
        }

        [Test]
        public void ToggleReady_WhenReady_SendsUnready()
        {
            EnterLobby(false);
            SetPlayer("test-id", true);
            _service.SetReadyAsync(false).Returns(UniTask.FromResult(Result.Success()));
            ViewModel.ToggleReadyAsync().Forget();
            _service.Received(1).SetReadyAsync(false);
        }

        [Test]
        public void PreparingServer_DisablesToggle()
        {
            EnterLobby();
            _service.CurrentLobbyState.Status.Value = LobbyStatus.PreparingServer;
            ViewModel.ToggleReadyAsync().Forget();
            Assert.That(ViewModel.CanToggleReady.Value, Is.False);
            _service.DidNotReceiveWithAnyArgs().SetReadyAsync(default);
        }

        [Test]
        public void FailedStartup_RestoresWaiting_RequiresHostToUnreadyBeforeRetry()
        {
            EnterLobby();
            _prepareServer.PrepareAsync().Returns(UniTask.FromResult(Result.Failure<string>("start-error")));
            var restored = UniTask.FromResult(Result.Success(_service.CurrentLobbyState));
            _service.UpdateLobbyAsync(Arg.Any<LobbyUpdateRequest>()).Returns(restored);
            SetPlayer("test-id", true);
            _dialog.Received(1).ShowErrorFromCode("error.lobby", "start-error");
            Assert.That(_service.CurrentLobbyState.Status.Value, Is.EqualTo(LobbyStatus.WaitingForPlayers));
            _service.Received(1).UpdateLobbyAsync(Arg.Is<LobbyUpdateRequest>(x => x.Status == LobbyStatus.WaitingForPlayers));
            _service.CurrentLobbyState.Players.ForceRefresh();
            _prepareServer.Received(1).PrepareAsync();
            SetPlayer("test-id", false);
            SetPlayer("test-id", true);
            _prepareServer.Received(2).PrepareAsync();
        }

        [Test]
        public void Disposed_DoesNotStartOrToggle()
        {
            EnterLobby();
            ViewModel.Dispose();
            SetPlayer("test-id", true);
            ViewModel.ToggleReadyAsync().Forget();
            _prepareServer.DidNotReceive().PrepareAsync();
            _service.DidNotReceiveWithAnyArgs().SetReadyAsync(default);
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
