using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Lobbies;
using Tanks.Localization;
using Tanks.UI.Menus;
using Tanks.UI.Menus.Dialogs;
using Tanks.UI.Menus.Loading;
using Tanks.UI.Menus.Lobbies;

namespace Tanks.UI.Tests.Menus.Lobbies
{
    public class LobbyBrowserTests : ViewModelTestBase<LobbyBrowserViewModel>
    {
        private ILobbyService _service;
        private ILocalizationDatabase _localization;
        private ILoadingScreenService _loading;
        private IDialogScreenService _dialog;
        private IMenuNavigation _navigation;

        protected override LobbyBrowserViewModel CreateViewModel()
        {
            _service = Substitute.For<ILobbyService>();
            _localization = Substitute.For<ILocalizationDatabase>();
            _loading = Substitute.For<ILoadingScreenService>();
            _dialog = Substitute.For<IDialogScreenService>();
            _navigation = Substitute.For<IMenuNavigation>();

            return new LobbyBrowserViewModel(
                _service, _localization, _loading, _dialog, _navigation);
        }

        [TestCase("some name")]
        [TestCase("test")]
        [TestCase("Lobby Name")]
        [TestCase("")]
        [TestCase(null)]
        public void Refresh_Success_UpdatesLobbyList(string queryName)
        {
            MockLocalizationReturn("Menus", "menu.find-lobby.searching", "Searching");
            MockLocalizationReturn("Menus", "menu.find-lobby.found", "Found");

            ViewModel.QueryName.Value = queryName;

            IEnumerable<LobbySearchState> lobbies = new LobbySearchState[]
            {
                new LobbySearchState { Name = queryName },
                new LobbySearchState { Name = queryName }
            };

            var tcs = new UniTaskCompletionSource<ResultType<IEnumerable<LobbySearchState>>>();

            _service.FindLobbiesAsync(queryName).Returns(tcs.Task);
            ViewModel.RefreshAsync().Forget();

            _dialog
                .DidNotReceiveWithAnyArgs()
                .ShowErrorFromCode(Arg.Any<string>(), Arg.Any<string>());

            Assert.AreEqual("Searching", ViewModel.Status.Value, "Invalid search status");
            Assert.Null(ViewModel.FoundLobbies.Value, "Lobbies should reset on refresh");

            tcs.TrySetResult(Result.Success(lobbies));

            Assert.NotNull(ViewModel.FoundLobbies.Value, "No lobbies were found");
            Assert.AreEqual(2, ViewModel.FoundLobbies.Value.Length);
            Assert.AreEqual("Found", ViewModel.Status.Value, "Invalid found status");
        }

        [TestCase("too-many-requests")]
        [TestCase("bad-request")]
        public async Task Refresh_Failure_ShowsErrorScreen(string errorCode)
        {
            var result = Result.Failure<IEnumerable<LobbySearchState>>(errorCode);

            _service
                .FindLobbiesAsync(Arg.Any<string>())
                .Returns(UniTask.FromResult(result));

            await ViewModel.RefreshAsync();
            _dialog.Received(1).ShowErrorFromCode(Arg.Any<string>(), errorCode);
        }

        [TestCase("some-id")]
        [TestCase("123456")]
        public void Join_Success_NavigatesToLobby(string id)
        {
            MockLocalizationReturn("Menus", "menu.lobby.joining", "Joining lobby");

            var tcs = new UniTaskCompletionSource<ResultType<LobbyState>>();
            _service.JoinLobbyAsync(Arg.Any<LobbyId>()).Returns(tcs.Task);
            ViewModel.JoinLobbyAsync(new LobbyId(id)).Forget();

            _loading.Received(1).Begin(Arg.Any<ILocalizedString>());
            _loading.DidNotReceiveWithAnyArgs().End();
            _navigation.DidNotReceiveWithAnyArgs().Navigate(Arg.Any<MenuType>());

            tcs.TrySetResult(Result.Success(new LobbyState()));
            _loading.Received(1).End();
            _navigation.Received(1).Navigate(MenuType.Lobby);
        }

        [TestCase("too-many-requests")]
        [TestCase("bad-request")]
        public async Task Join_Failure_ShowsErrorScreen(string errorCode)
        {
            _service
                .JoinLobbyAsync(Arg.Any<LobbyId>())
                .Returns(UniTask.FromResult(Result.Failure<LobbyState>(errorCode)));

            await ViewModel.JoinLobbyAsync(new LobbyId("some_address"));
            _loading.Received(1).End();

            _dialog.Received(1).ShowErrorFromCode(Arg.Any<string>(), errorCode);
            _navigation.DidNotReceiveWithAnyArgs().Navigate(MenuType.Lobby);
        }

        private void MockLocalizationReturn(string tableName, string entryName, string expected)
        {
            _localization.TryGet(tableName, entryName, out _)
                .Returns(x =>
                {
                    ILocalizedString result = Substitute.For<ILocalizedString>();
                    result.Get().Returns(expected);

                    x[2] = result;
                    return true;
                });
        }
    }
}
