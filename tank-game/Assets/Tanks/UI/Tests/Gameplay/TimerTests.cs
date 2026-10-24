using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Bootstrap;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Localization;
using Tanks.Shared;
using Tanks.Shared.Messaging;
using Tanks.UI.Gameplay.Timer;
using UnityEngine;

namespace Tanks.UI.Tests.Gameplay
{
    public sealed class TimerTests : ViewModelTestBase<TimerViewModel>
    {
        private IMessageBus _bus;
        private ISessionSettingsService _settings;
        private ILocalizationDatabase _localization;

        protected override TimerViewModel CreateViewModel()
        {
            _bus = Substitute.For<IMessageBus>();
            _settings = Substitute.For<ISessionSettingsService>();
            _localization = Substitute.For<ILocalizationDatabase>();
            return new TimerViewModel(_bus, _settings, _localization);
        }

        [Test]
        public void Tick_WithDangerZone_SwitchesToDangerZone()
        {
            _settings.GetSettings().DangerZone.Returns(new DangerZoneSettings
            {
                StartDelaySeconds = 10,
                DurationSeconds = 20
            });

            ViewModel.Handle(new GameStartedFromClient { AdaptedStartDelaySeconds = 1 }, null);

            Color color = ViewModel.CounterColor.Value;
            ViewModel.Tick(deltaTime: 5);

            ViewModel.Handle(new DangerZoneStarted(), null);
            ViewModel.Tick(deltaTime: 0.1f);

            Assert.AreEqual("20", ViewModel.Seconds.Value,
                "Counter should reset as the danger zone starts");

            Assert.AreNotEqual(color, ViewModel.CounterColor.Value,
                "Color should change as the danger zone starts");
        }

        [TestCase(2)]
        [TestCase(6)]
        [TestCase(10)]
        [TestCase(30)]
        public void Tick_CountsDown_PredictsDangerZone(int dangerZoneDelay)
        {
            _settings.GetSettings().DangerZone.Returns(new DangerZoneSettings
            {
                StartDelaySeconds = dangerZoneDelay,
                DurationSeconds = 20
            });

            ViewModel.Handle(new GameStartedFromClient { AdaptedStartDelaySeconds = 1 }, null);
            Color color = ViewModel.CounterColor.Value;

            for (int second = dangerZoneDelay; second >= 1; second--)
            {
                ViewModel.Tick(deltaTime: 1);
                Assert.AreEqual(second.ToString(), ViewModel.Seconds.Value, "Counter dismatch");
            }

            Assert.AreEqual(color, ViewModel.CounterColor.Value,
                "Color changed for no reason");

            ViewModel.Tick(deltaTime: 2.5f);

            Assert.AreEqual("20", ViewModel.Seconds.Value,
                "Counter should reset as the danger zone starts");

            Assert.AreNotEqual(color, ViewModel.CounterColor.Value,
                "Color should change as the danger zone starts");
        }
    }
}
