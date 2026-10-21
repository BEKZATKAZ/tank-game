using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;
using Tanks.Client.Entities;
using Tanks.Client.Entities.Health;
using Tanks.Client.Player;
using Tanks.Core.Helpers;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Unity.Mathematics;

namespace Tanks.Client.Tests.Entities
{
    public class SpectatorSwitchTests
    {
        private ISpectatorService _spectatorService;
        private IClientEntityService _clientEntityService;
        private SpectatorSwitch _component;

        [SetUp]
        public void Setup()
        {
            _clientEntityService = Substitute.For<IClientEntityService>();
            _spectatorService = Substitute.For<ISpectatorService>();

            _component = new SpectatorSwitch(_clientEntityService, _spectatorService);
            _component.Initialize(Substitute.For<IHealthBehaviour>());
        }

        [Test]
        public async Task Explode_NextRoundStarted_CancelsSpectating()
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

            _clientEntityService.GetPossessedEntity().IsValid.Returns(true);
            await Task.Delay(3000);

            _spectatorService.DidNotReceiveWithAnyArgs().Spectate(default);
        }

        [Test]
        public async Task Explode_LocalPlayer_SpectatesOther()
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

            await Task.Delay(4000);

            _spectatorService.Received(1).Spectate(from.AsRequired());
        }

        [Test]
        public async Task Explode_OtherPlayer_DoesNothing()
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

            await Task.Delay(3000);

            _spectatorService
                .DidNotReceiveWithAnyArgs()
                .Spectate(localPlayer.AsRequired());
        }
    }
}
