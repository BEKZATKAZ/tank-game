using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Entities;
using Tanks.Client.Player;
using Tanks.Client.Rendering;
using Tanks.Core.Helpers;
using Tanks.Shared.Entities;

namespace Tanks.Client.Tests.Player
{
    public class SpectatorTests
    {
        private ICameraFollowService _followService;
        private IClientEntityService _clientEntityService;
        private ISpectatorService _service;

        [SetUp]
        public void Setup()
        {
            _followService = Substitute.For<ICameraFollowService>();
            _clientEntityService = Substitute.For<IClientEntityService>();
            _service = new SpectatorService(_clientEntityService, _followService);
        }

        [Test]
        public void Spectate_FollowsEntity()
        {
            IEntityBehaviour entity = Substitute.For<IEntityBehaviour>();
            _service.Spectate(entity.AsRequired());

            _followService.Received(1).SetFollowTarget(entity.Transform);
            _clientEntityService.Received(1).PossessEntity(entity.AsRequired());
        }
    }
}
