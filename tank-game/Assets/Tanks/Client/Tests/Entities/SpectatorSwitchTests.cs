using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Entities.Health;
using Tanks.Client.Player;
using Tanks.Core.Helpers;
using Tanks.Shared;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
using Unity.Mathematics;

namespace Tanks.Client.Tests.Entities
{
    public class SpectatorSwitchTests
    {
        private ISpectatorService _spectatorService;
        private SpectatorSwitch _component;

        [SetUp]
        public void Setup()
        {
            _spectatorService = Substitute.For<ISpectatorService>();
            _component = new SpectatorSwitch(_spectatorService, Substitute.For<IMessageBus>());
            _component.Initialize(Substitute.For<IHealthBehaviour>());
        }

        [Test]
        public void Explode_NextRoundStarted_CancelsSpectating()
        {
            IEntityBehaviour from = Substitute.For<IEntityBehaviour>();
            float2 position = new float2(10, 20);

            from.IsValid.Returns(true);
            from.IsPossessed.Returns(true);

            _component.Handle(new Died
            {
                Target = from,
                From = from,
                WorldPosition = position
            });

            _component.Update(deltaTime: 1.9f);
            _component.Handle(new GameStartedFromServer());

            _component.Update(deltaTime: 0.12f);
            _spectatorService.DidNotReceiveWithAnyArgs().Spectate(default);
        }

        [Test]
        public void Explode_LocalPlayer_SpectatesOther()
        {
            IEntityBehaviour from = Substitute.For<IEntityBehaviour>();
            float2 position = new float2(10, 20);

            from.IsValid.Returns(true);
            from.IsPossessed.Returns(true);

            _component.Update(deltaTime: 0.1f);

            _component.Handle(new Died
            {
                Target = from,
                From = from,
                WorldPosition = position
            });

            _component.Update(deltaTime: 2.5f);

            _spectatorService.Received(1).Spectate(from.AsRequired());
        }

        [Test]
        public void Explode_OtherPlayer_DoesNothing()
        {
            IEntityBehaviour localPlayer = Substitute.For<IEntityBehaviour>();
            IEntityBehaviour otherPlayer = Substitute.For<IEntityBehaviour>();
            float2 position = new float2(10, 20);

            localPlayer.IsValid.Returns(true);
            localPlayer.IsPossessed.Returns(true);

            otherPlayer.IsValid.Returns(true);
            otherPlayer.IsPossessed.Returns(false);

            _component.Handle(new Died
            {
                Target = otherPlayer,
                From = otherPlayer,
                WorldPosition = position
            });

            _component.Update(deltaTime: 5);

            _spectatorService
                .DidNotReceiveWithAnyArgs()
                .Spectate(localPlayer.AsRequired());
        }
    }
}
