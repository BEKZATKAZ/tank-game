using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Common;
using Tanks.Core.Lobbies;
using Tanks.Core.Lobbies.Payloads;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Core.Lobbies.UseCases;
using Tanks.Localization;
using Tanks.UI.Menus.Dialogs;
using Tanks.UI.Menus.Loading;
using Tanks.UI.Menus.LobbyRoom;
using Tanks.UI.Menus.Sections;

namespace Tanks.UI.Tests.Menus.LobbyRoom
{
    public sealed class LobbyRoomReadinessTests : ViewModelTestBase<LobbyRoomViewModel>
    {
        private LobbyState _lobby;
        private ILobbyService _service;
        private IAuthService _auth;
        private IDialogScreenService _dialog;
        private ILocalizationDatabase _localization;
        private IPrepareServerUseCase _prepare;
        private ISessionSettingsService _settings;

        protected override LobbyRoomViewModel CreateViewModel()
        {
            _lobby = new LobbyState();
            _lobby.Id.Value = new LobbyId("lobby");
            _lobby.HostId.Value = "host";
            SetPlayers(false, false);
            _service = Substitute.For<ILobbyService>();
            _service.CurrentLobbyState.Returns(_lobby);
            _service.SetReadyAsync(Arg.Any<bool>()).Returns(Task.FromResult(Result.Success()));
            _service.UpdateLobbyAsync(Arg.Any<LobbyUpdateRequest>()).Returns(Task.FromResult(Result.Success(_lobby)));

            _auth = Substitute.For<IAuthService>();
            _auth.CurrentProfile.Returns(new PlayerProfileState());
            _auth.CurrentProfile.UserId.Value = "host";
            _dialog = Substitute.For<IDialogScreenService>();
            _localization = Substitute.For<ILocalizationDatabase>();
            _prepare = Substitute.For<IPrepareServerUseCase>();
            _prepare.PrepareAsync().Returns(_ => UniTask.FromResult(Result.Success("JOIN")));

            var defaults = Substitute.For<ISessionDefaultSettingsConfig>();
            defaults.DesiredPlayerCount.Returns(new PossibleIntegers(1, 2));
            defaults.MaxScoreToReach.Returns(new PossibleIntegers(5, 15));
            defaults.SelfDamagePercent.Returns(new PossibleIntegers(100));
            defaults.Stats.Returns(new DefaultPlayerStats());
            defaults.MapGeneration.Returns(new DefaultMapGeneration());
            defaults.ItemSpawn.Returns(new DefaultItemSpawn());
            defaults.DangerZone.Returns(new DefaultDangerZone());
            _settings = Substitute.For<ISessionSettingsService>();
            _settings.DefaultSettings.Returns(defaults);

            return CreateRoom();
        }

        private LobbyRoomViewModel CreateRoom()
        {
            return new LobbyRoomViewModel(_service, _auth, _dialog,
                Substitute.For<IMenuSectionService>(), Substitute.For<ILoadingScreenService>(),
                _localization, _prepare, Substitute.For<IConnectToServerUseCase>(),
                new LobbySettingsViewModel(_service, _auth, _settings, _localization, _dialog));
        }

        private static LobbyPlayerState Player(string id, bool ready)
        {
            return new LobbyPlayerState
            {
                UserId = id,
                Data = new Dictionary<string, string> { ["ready"] = ready.ToString() }
            };
        }

        private void SetPlayers(bool host, bool guest)
        {
            _lobby.Players.Value = new Dictionary<string, LobbyPlayerState>
            {
                ["host"] = Player("host", host),
                ["guest"] = Player("guest", guest)
            };
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        public void MixedReadiness_DoesNotStart(bool host, bool guest)
        {
            SetPlayers(host, guest);

            _prepare.DidNotReceive().PrepareAsync();
            Assert.That(ViewModel.CanReady.Value, Is.True);
        }

        [Test]
        public void Starting_DisablesReadyAndIgnoresRepeatedUpdates()
        {
            var pending = new UniTaskCompletionSource<ResultType<string>>();
            _prepare.PrepareAsync().Returns(pending.Task);
            SetPlayers(true, true);
            SetPlayers(true, true);

            Assert.That(ViewModel.CanReady.Value, Is.False);
            _prepare.Received(1).PrepareAsync();

            _lobby.Status.Value = LobbyStatus.Connecting;
            pending.TrySetResult(Result.Success("JOIN"));
        }

        [Test]
        public async Task TwoClients_OnlyHostStartsAfterBothReadyUpdates()
        {
            var guestLobby = new LobbyState();
            guestLobby.Id.Value = new LobbyId("lobby");
            guestLobby.HostId.Value = "host";
            guestLobby.Players.Value = new Dictionary<string, LobbyPlayerState>(_lobby.Players.Value);
            
            var guestService = Substitute.For<ILobbyService>();
            guestService.CurrentLobbyState.Returns(guestLobby);
            
            var guestAuth = Substitute.For<IAuthService>();
            guestAuth.CurrentProfile.Returns(new PlayerProfileState());
            guestAuth.CurrentProfile.UserId.Value = "guest";
            
            var guestPrepare = Substitute.For<IPrepareServerUseCase>();

            using var guest = new LobbyRoomViewModel(guestService, guestAuth, _dialog,
                Substitute.For<IMenuSectionService>(), Substitute.For<ILoadingScreenService>(),
                _localization, guestPrepare, Substitute.For<IConnectToServerUseCase>(),
                new LobbySettingsViewModel(guestService, guestAuth, _settings, _localization, _dialog));

            _service.SetReadyAsync(true).Returns(_ =>
            {
                SetPlayers(true, false);
                guestLobby.Players.Value = new Dictionary<string, LobbyPlayerState>(_lobby.Players.Value);
                return Task.FromResult(Result.Success());
            });

            guestService.SetReadyAsync(true).Returns(_ =>
            {
                SetPlayers(true, true);
                guestLobby.Players.Value = new Dictionary<string, LobbyPlayerState>(_lobby.Players.Value);
                return Task.FromResult(Result.Success());
            });

            await ViewModel.ToggleReadyAsync();
            _prepare.DidNotReceive().PrepareAsync().Forget();
            await guest.ToggleReadyAsync();

            _prepare.Received(1).PrepareAsync().Forget();
            guestPrepare.DidNotReceive().PrepareAsync().Forget();

            Assert.That(_lobby.AllPlayersReady, Is.True);
            Assert.That(guestLobby.AllPlayersReady, Is.True);
        }

        [Test]
        public void ReadyText_UsesLocalizationAndRefreshesWithLocale()
        {
            var ready = Substitute.For<ILocalizedString>();
            ready.Get().Returns("Ready localized");
            _localization.TryGet("Menus", "menu.lobby.ready", out Arg.Any<ILocalizedString>())
                .Returns(call => { call[2] = ready; return true; });
            _localization.LocaleChanged += Raise.Event<LocaleChangedEventHandler>("en");

            Assert.That(ViewModel.ReadyText.Value, Is.EqualTo("Ready localized"));

            ready.Get().Returns("Ready translated");
            _localization.LocaleChanged += Raise.Event<LocaleChangedEventHandler>("other");

            Assert.That(ViewModel.ReadyText.Value, Is.EqualTo("Ready translated"));
        }

        [Test]
        public void EveryoneReady_StartsOnlyOnceAcrossRepeatedSnapshots()
        {
            SetPlayers(true, true);
            SetPlayers(true, true);
            _localization.LocaleChanged += Raise.Event<LocaleChangedEventHandler>("en");

            _prepare.Received(1).PrepareAsync();
        }

        [Test]
        public void Guest_CanToggleButCannotStart()
        {
            ViewModel.Dispose();
            _auth.CurrentProfile.UserId.Value = "guest";
            using var guest = CreateRoom();
            SetPlayers(true, true);

            Assert.That(guest.CanReady.Value, Is.True);
            _prepare.DidNotReceive().PrepareAsync();
        }

        [Test]
        public async Task Toggle_DisablesDuringRequestAndUsesConfirmedState()
        {
            var pending = new TaskCompletionSource<ResultType>();
            _service.SetReadyAsync(true).Returns(pending.Task);
            Task toggling = ViewModel.ToggleReadyAsync().AsTask();

            Assert.That(ViewModel.CanReady.Value, Is.False);
            await ViewModel.ToggleReadyAsync();
            await _service.Received(1).SetReadyAsync(true);

            SetPlayers(true, false);
            pending.SetResult(Result.Success());
            await toggling;
            Assert.That(ViewModel.ReadyText.Value, Is.EqualTo("UNREADY"));
            await ViewModel.ToggleReadyAsync();
            await _service.Received(1).SetReadyAsync(false);
        }

        [Test]
        public async Task ToggleFailure_ReportsErrorAndAllowsRetry()
        {
            _service.SetReadyAsync(true).Returns(Task.FromResult(Result.Failure("ready-error")));
            await ViewModel.ToggleReadyAsync();

            _dialog.Received(1).ShowErrorFromCode("error.lobby", "ready-error");
            Assert.That(ViewModel.CanReady.Value, Is.True);
            Assert.That(_lobby.Players.Value["host"].IsReady, Is.False);
        }

        [Test]
        public void StartFailure_DoesNotRetryUntilReadinessChanges()
        {
            _prepare.PrepareAsync().Returns(_ => UniTask.FromResult(Result.Failure<string>("start-error")));
            SetPlayers(true, true);
            SetPlayers(true, true);

            _prepare.Received(1).PrepareAsync();
            _dialog.Received(1).ShowErrorFromCode("error.lobby", "start-error");

            SetPlayers(true, false);
            SetPlayers(true, true);
            _prepare.Received(2).PrepareAsync();
        }

        [Test]
        public void UnreadyMemberLeaves_StartsWithRemainingReadyPlayers()
        {
            SetPlayers(true, false);
            _lobby.Players.Value = new Dictionary<string, LobbyPlayerState> { ["host"] = Player("host", true) };

            _prepare.Received(1).PrepareAsync();
        }

        [Test]
        public async Task UnreadyJoinDuringSettingsSave_PreventsStartUntilNewMemberIsReady()
        {
            var pending = new TaskCompletionSource<ResultType<LobbyState>>();
            _service.UpdateLobbyAsync(Arg.Any<LobbyUpdateRequest>()).Returns(pending.Task);
            
            Task saving = ViewModel.Settings.SetValueAsync(LobbySetting.MaxScore, 1).AsTask();
            SetPlayers(true, true);
            _prepare.DidNotReceive().PrepareAsync().Forget();

            var joined = new Dictionary<string, LobbyPlayerState>(_lobby.Players.Value)
            {
                ["new"] = Player("new", false)
            };

            _lobby.Players.Value = joined;
            pending.SetResult(Result.Success(_lobby));
            await saving;

            _prepare.DidNotReceive().PrepareAsync().Forget();
            joined["new"] = Player("new", true);

            _lobby.Players.Value = new Dictionary<string, LobbyPlayerState>(joined);
            _prepare.Received(1).PrepareAsync().Forget();
        }

        [Test]
        public void JoiningMemberAndEmptyLobby_PreventStart()
        {
            SetPlayers(true, false);
            _lobby.Players.Value = new Dictionary<string, LobbyPlayerState>();

            _prepare.DidNotReceive().PrepareAsync();
            Assert.That(ViewModel.CanReady.Value, Is.False);
            Assert.That(_lobby.AllPlayersReady, Is.False);
        }

        [TestCase(LobbyStatus.PreparingServer)]
        [TestCase(LobbyStatus.Connecting)]
        public void Startup_DisablesReady(LobbyStatus status)
        {
            _lobby.Status.Value = status;
            SetPlayers(true, true);

            Assert.That(ViewModel.CanReady.Value, Is.False);
            _prepare.DidNotReceive().PrepareAsync();
        }

        [Test]
        public void SettingsChange_PreservesReadiness()
        {
            SetPlayers(true, false);
            _lobby.Settings.Value = new() { MaxScore = 15 };

            Assert.That(_lobby.Players.Value["host"].IsReady, Is.True);
        }
    }
}
