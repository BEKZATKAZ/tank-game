using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Common;
using Tanks.Core.Lobbies;
using Tanks.Core.Lobbies.Payloads;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Core.Lobbies.Sessions.DTOs;
using Tanks.Localization;
using Tanks.UI.Menus.Dialogs;
using Tanks.UI.Menus.Lobbies;

namespace Tanks.UI.Tests.Menus.Lobbies
{
    public sealed class LobbySettingsTests : ViewModelTestBase<LobbySettingsViewModel>
    {
        private ILobbyService _service;
        private IAuthService _auth;
        private ILocalizationDatabase _localization;
        private IDialogScreenService _dialog;
        private ISessionDefaultSettingsConfig _defaults;
        private LobbyState _lobby;

        protected override LobbySettingsViewModel CreateViewModel()
        {
            _service = Substitute.For<ILobbyService>();
            _auth = Substitute.For<IAuthService>();
            _auth.CurrentProfile.Returns(new PlayerProfileState());
            _auth.CurrentProfile.UserId.Value = "host";
            _localization = Substitute.For<ILocalizationDatabase>();
            _dialog = Substitute.For<IDialogScreenService>();

            _defaults = Substitute.For<ISessionDefaultSettingsConfig>();
            _defaults.DesiredPlayerCount.Returns(new PossibleIntegers(1, 2, 3, 4));
            _defaults.MaxScoreToReach.Returns(new PossibleIntegers(5, 15, 30) { Default = 15 });
            _defaults.MapGeneration.Returns(new DefaultMapGeneration());
            _defaults.ItemSpawn.Returns(new DefaultItemSpawn());
            _defaults.DangerZone.Returns(new DefaultDangerZone());
            var settings = Substitute.For<ISessionSettingsService>();
            settings.DefaultSettings.Returns(_defaults);

            _lobby = new LobbyState();
            _lobby.Id.Value = new LobbyId("lobby");
            _lobby.HostId.Value = "host";
            _service.CurrentLobbyState.Returns(_lobby);
            Confirm(SessionSettingsMapper.FromDefaultToLobby(_defaults));
            _service.UpdateLobbyAsync(Arg.Any<LobbyUpdateRequest>()).Returns(call =>
            {
                var request = call.Arg<LobbyUpdateRequest>();
                if (request.Settings.HasValue) Confirm(request.Settings.Value);
                return Task.FromResult(Result.Success(_lobby));
            });

            return new LobbySettingsViewModel(_service, _auth, settings, _localization, _dialog);
        }

        private void Confirm(SessionSettingsFromLobby settings)
        {
            _lobby.LobbyData["settings"] = "confirmed";
            _lobby.Settings.Value = settings;
        }

        private LobbySettingOption Option(LobbySetting type) => ViewModel.Settings.Value.Single(x => x.Type == type);

        [Test]
        public void Options_UseConfiguredValuesAndDefaults()
        {
            Assert.That(ViewModel.Settings.Value.Count, Is.EqualTo(7));
            Assert.That(Option(LobbySetting.MaxScore).Choices, Is.EqualTo(new[] { "5", "15", "30" }));
            Assert.That(Option(LobbySetting.MaxScore).SelectedIndex, Is.EqualTo(1));
            Assert.That(Option(LobbySetting.MapSize).Choices.Count, Is.EqualTo(3));
            Assert.That(Option(LobbySetting.ItemSpawnRate).Choices.Count, Is.EqualTo(4));
        }

        [TestCase(LobbySetting.MaxScore)]
        [TestCase(LobbySetting.MapSize)]
        [TestCase(LobbySetting.MapThickness)]
        [TestCase(LobbySetting.ItemSpawnRate)]
        [TestCase(LobbySetting.DangerZoneDelay)]
        [TestCase(LobbySetting.DangerZoneDuration)]
        [TestCase(LobbySetting.DangerZoneDamage)]
        public async Task Selection_PublishesCorrectValueAndPreservesOtherSettings(LobbySetting type)
        {
            var expected = _lobby.Settings.Value;
            switch (type)
            {
                case LobbySetting.MaxScore: expected.MaxScore = _defaults.MaxScoreToReach.Values[0]; break;
                case LobbySetting.MapSize: expected.MapGeneration.Size = MapSize.Medium; break;
                case LobbySetting.MapThickness: expected.MapGeneration.Thickness = MapThickness.Medium; break;
                case LobbySetting.ItemSpawnRate: expected.ItemSpawn.Rate = ItemSpawnRate.Normal; break;
                case LobbySetting.DangerZoneDelay: expected.DangerZone.StartDelaySeconds = _defaults.DangerZone.StartDelaySeconds.Values[0]; break;
                case LobbySetting.DangerZoneDuration: expected.DangerZone.DurationSeconds = _defaults.DangerZone.DurationSeconds.Values[0]; break;
                case LobbySetting.DangerZoneDamage: expected.DangerZone.DamagePerSecond = _defaults.DangerZone.DamagePerSecond.Values[0]; break;
            }
            int index = type == LobbySetting.MapSize || type == LobbySetting.MapThickness ||
                type == LobbySetting.ItemSpawnRate ? 1 : 0;

            await ViewModel.SetValueAsync(type, index);

            Assert.That(_lobby.Settings.Value, Is.EqualTo(expected));
            Assert.That(Option(type).SelectedIndex, Is.EqualTo(index));
            await _service.Received(1).UpdateLobbyAsync(Arg.Is<LobbyUpdateRequest>(x => x.Settings.Value.Equals(expected)));
        }

        [Test]
        public async Task Member_CannotSaveButReceivesHostChanges()
        {
            _lobby.HostId.Value = "other";
            Assert.That(ViewModel.CanEdit.Value, Is.False);

            await ViewModel.SetValueAsync(LobbySetting.MaxScore, 2);
            await _service.DidNotReceive().UpdateLobbyAsync(Arg.Any<LobbyUpdateRequest>());

            var settings = _lobby.Settings.Value;
            settings.MaxScore = 30;
            Confirm(settings);
            Assert.That(Option(LobbySetting.MaxScore).SelectedIndex, Is.EqualTo(2));
        }

        [TestCase(LobbyStatus.WaitingForPlayers, true)]
        [TestCase(LobbyStatus.PreparingServer, false)]
        [TestCase(LobbyStatus.Connecting, false)]
        public async Task Status_ControlsEditing(LobbyStatus status, bool canEdit)
        {
            _lobby.Status.Value = status;
            Assert.That(ViewModel.CanEdit.Value, Is.EqualTo(canEdit));

            if (canEdit) return;
            await ViewModel.SetValueAsync(LobbySetting.MaxScore, 2);
            await _service.DidNotReceive().UpdateLobbyAsync(Arg.Any<LobbyUpdateRequest>());
        }

        [Test]
        public async Task FailedSave_RestoresConfirmedSelectionAndShowsError()
        {
            _service.UpdateLobbyAsync(Arg.Any<LobbyUpdateRequest>())
                .Returns(Task.FromResult(Result.Failure<LobbyState>("save-failed")));

            await ViewModel.SetValueAsync(LobbySetting.MaxScore, 2);

            Assert.That(Option(LobbySetting.MaxScore).SelectedIndex, Is.EqualTo(1));
            Assert.That(ViewModel.CanEdit.Value, Is.True);
            _dialog.Received(1).ShowErrorFromCode("error.lobby", "save-failed");
            Assert.That(_lobby.SettingsLock.CurrentCount, Is.EqualTo(1));
        }

        [Test]
        public async Task PendingSave_BlocksFurtherEditsUntilConfirmed()
        {
            var completion = new TaskCompletionSource<ResultType<LobbyState>>();
            _service.UpdateLobbyAsync(Arg.Any<LobbyUpdateRequest>()).Returns(completion.Task);

            var save = ViewModel.SetValueAsync(LobbySetting.MaxScore, 2);
            Assert.That(ViewModel.CanEdit.Value, Is.False);
            Assert.That(_lobby.SettingsLock.CurrentCount, Is.Zero);
            await ViewModel.SetValueAsync(LobbySetting.MaxScore, 0);
            await _service.Received(1).UpdateLobbyAsync(Arg.Any<LobbyUpdateRequest>());

            var settings = _lobby.Settings.Value;
            settings.MaxScore = 30;
            Confirm(settings);
            completion.SetResult(Result.Success(_lobby));
            await save;

            Assert.That(Option(LobbySetting.MaxScore).SelectedIndex, Is.EqualTo(2));
            Assert.That(ViewModel.CanEdit.Value, Is.True);
        }

        [Test]
        public async Task InvalidOrUnchangedSelection_DoesNotSave()
        {
            await ViewModel.SetValueAsync(LobbySetting.MaxScore, -1);
            await ViewModel.SetValueAsync(LobbySetting.MaxScore, 3);
            await ViewModel.SetValueAsync(LobbySetting.MaxScore, 1);
            await ViewModel.SetValueAsync((LobbySetting)999, 0);
            await _service.DidNotReceive().UpdateLobbyAsync(Arg.Any<LobbyUpdateRequest>());
        }

        [Test]
        public async Task ThrownSave_RestoresSelectionAndReleasesLock()
        {
            _service.UpdateLobbyAsync(Arg.Any<LobbyUpdateRequest>())
                .Returns(Task.FromException<ResultType<LobbyState>>(new InvalidOperationException()));

            await ViewModel.SetValueAsync(LobbySetting.MaxScore, 2);

            Assert.That(Option(LobbySetting.MaxScore).SelectedIndex, Is.EqualTo(1));
            Assert.That(ViewModel.CanEdit.Value, Is.True);
            Assert.That(_lobby.IsUpdatingSettings.Value, Is.False);
            Assert.That(_lobby.SettingsLock.CurrentCount, Is.EqualTo(1));
            _dialog.Received(1).ShowErrorFromCode("error.lobby", "failed-to-update");
        }

        [Test]
        public async Task PreparationBeginsWhileWaiting_RejectsSelection()
        {
            await _lobby.SettingsLock.WaitAsync();
            Task saving = ViewModel.SetValueAsync(LobbySetting.MaxScore, 2).AsTask();

            _lobby.Status.Value = LobbyStatus.PreparingServer;
            _lobby.SettingsLock.Release();
            await saving;

            Assert.That(ViewModel.CanEdit.Value, Is.False);
            Assert.That(_lobby.IsUpdatingSettings.Value, Is.False);
            Assert.That(Option(LobbySetting.MaxScore).SelectedIndex, Is.EqualTo(1));
            await _service.DidNotReceive().UpdateLobbyAsync(Arg.Any<LobbyUpdateRequest>());
        }

        [Test]
        public async Task LeaveDuringSave_DoesNotShowStaleFailure()
        {
            var completion = new TaskCompletionSource<ResultType<LobbyState>>();
            _service.UpdateLobbyAsync(Arg.Any<LobbyUpdateRequest>()).Returns(completion.Task);
            var save = ViewModel.SetValueAsync(LobbySetting.MaxScore, 2);

            _lobby.Id.Value = default;
            completion.SetResult(Result.Failure<LobbyState>("save-failed"));
            await save;

            Assert.That(ViewModel.CanEdit.Value, Is.False);
            Assert.That(_lobby.IsUpdatingSettings.Value, Is.False);
            _dialog.DidNotReceiveWithAnyArgs().ShowErrorFromCode(default, default);
        }

        [Test]
        public void LocaleChange_UpdatesLabelsAndEnumChoicesWithoutChangingSelection()
        {
            var localized = Substitute.For<ILocalizedString>();
            localized.Get().Returns("Translated");
            _localization.TryGet("Menus", Arg.Any<string>(), out Arg.Any<ILocalizedString>())
                .Returns(call => { call[2] = localized; return true; });

            _localization.LocaleChanged += Raise.Event<LocaleChangedEventHandler>("ru");

            Assert.That(Option(LobbySetting.MapSize).Label, Is.EqualTo("Translated"));
            Assert.That(Option(LobbySetting.MapSize).Choices, Is.All.EqualTo("Translated"));
            Assert.That(Option(LobbySetting.MaxScore).SelectedIndex, Is.EqualTo(1));
            Assert.That(Option(LobbySetting.MaxScore).Choices[1], Is.EqualTo("15"));
        }

        [Test]
        public async Task DisposedViewModel_DoesNotSaveOrReactToUpdates()
        {
            ViewModel.Dispose();
            await ViewModel.SetValueAsync(LobbySetting.MaxScore, 2);
            var before = ViewModel.Settings.Value;
            Confirm(new SessionSettingsFromLobby { MaxScore = 30 });

            Assert.That(ViewModel.CanEdit.Value, Is.False);
            Assert.That(ViewModel.Settings.Value, Is.SameAs(before));
            await _service.DidNotReceive().UpdateLobbyAsync(Arg.Any<LobbyUpdateRequest>());
        }
    }
}
