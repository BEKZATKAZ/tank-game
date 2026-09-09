using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Bootstrap;
using Tanks.Shared;
using Tanks.Shared.Messaging;
using Tanks.UI.Gameplay.Timer;
using UnityEngine;

namespace Tanks.UI.Tests.Gameplay
{
    public sealed class TimerTests : ViewModelTestBase<TimerViewModel>
    {
        private IMessageBus _bus;

        protected override TimerViewModel CreateViewModel()
        {
            _bus = Substitute.For<IMessageBus>();
            return new TimerViewModel(_bus);
        }

        [Test]
        public void Tick_WithDangerZone_SwitchesToDangerZone()
        {
            ViewModel.SetDangerZoneInfo(10, 20);
            ViewModel.Handle(new GameStartedFromClient { AdaptedStartDelaySeconds = 1 }, null);

            Color color = ViewModel.CounterColor.Value;
            ViewModel.Tick(deltaTime: 5);

            ViewModel.Handle(new DangerZoneStarted { ServerDuration = 30 }, null);
            ViewModel.Tick(deltaTime: 0.1f);

            Assert.AreEqual(30, ViewModel.Seconds.Value,
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
            ViewModel.SetDangerZoneInfo(dangerZoneDelay, 20);
            ViewModel.Handle(new GameStartedFromClient { AdaptedStartDelaySeconds = 1 }, null);
            Color color = ViewModel.CounterColor.Value;

            for (int second = dangerZoneDelay; second >= 1; second--)
            {
                ViewModel.Tick(deltaTime: 1);
                Assert.AreEqual(second, ViewModel.Seconds.Value, "Counter dismatch");
            }

            Assert.AreEqual(color, ViewModel.CounterColor.Value,
                "Color changed for no reason");

            ViewModel.Tick(deltaTime: 2.5f);

            Assert.AreEqual(20, ViewModel.Seconds.Value,
                "Counter should reset as the danger zone starts");

            Assert.AreNotEqual(color, ViewModel.CounterColor.Value,
                "Color should change as the danger zone starts");
        }
    }
}
