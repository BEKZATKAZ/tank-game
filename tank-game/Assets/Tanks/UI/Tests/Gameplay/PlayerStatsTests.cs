using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Entities;
using Tanks.Client.Entities.Health;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
using Tanks.UI.Gameplay.PlayerStats;
using UnityEngine;

namespace Tanks.UI.Tests.Gameplay
{
    public class PlayerStatsTests : ViewModelTestBase<PlayerStatsViewModel>
    {
        private IClientEntityService _clientEntityService;
        private IEntityBehaviour _localPlayer;

        protected override PlayerStatsViewModel CreateViewModel()
        {
            _localPlayer = Substitute.For<IEntityBehaviour>();

            _clientEntityService = Substitute.For<IClientEntityService>();
            _clientEntityService.GetPossessedPlayer().Returns(_localPlayer);

            return new PlayerStatsViewModel(Substitute.For<IMessageBus>(), _clientEntityService);
        }

        [Test]
        public void Prediction_WaitsForServerState_TimesOutIfNothingArrived()
        {
            ViewModel.Handle(new HealthChangedFromPrediction
            {
                Target = _localPlayer,
                OldState = new HealthState(100, 100),
                NewState = new HealthState(90, 100)
            }, null);

            float newOverlay = ViewModel.InstantHealthOverlay.Value;
            Color newColor = ViewModel.TankColor.Value;

            Assert.AreEqual(0.9f, newOverlay, delta: 0.01f, "Overlay not changed");
            Assert.AreNotEqual(Color.white, newColor, "Color not changed");

            // 1.9s in total
            for (int i = 0; i < 19; i++) ViewModel.Tick(0.1f);

            Assert.AreEqual(newOverlay, ViewModel.InstantHealthOverlay.Value, "Timed out too early");
            Assert.AreEqual(newColor, ViewModel.TankColor.Value, "Timed out too early");

            ViewModel.Tick(0.2f);
            newOverlay = ViewModel.InstantHealthOverlay.Value;

            Assert.AreEqual(1, newOverlay, "Should reset to old state");
        }

        [Test]
        public void Prediction_ServerStateArrives_AppliesServerState()
        {
            ViewModel.Handle(new HealthChangedFromPrediction
            {
                Target = _localPlayer,
                OldState = new HealthState(100, 100),
                NewState = new HealthState(90, 100)
            }, null);

            ViewModel.Tick(0.5f);
            ViewModel.Handle(new HealthChangedFromServer
            {
                Target = _localPlayer,
                OldState = new HealthState(100, 100),
                AuthoritativeState = new HealthState(80, 100)
            }, null);

            Assert.AreEqual(0.8f, ViewModel.InstantHealthOverlay.Value, delta: 0.01f);
        }
    }
}
