using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using Tanks.Client.Entities;
using Tanks.Client.Player;
using Tanks.Client.Rendering;
using Tanks.Core.Helpers;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;

namespace Tanks.Client.Tests.Player
{
    public class SpectatorTests
    {
        private ICameraFollowService _followService;
        private IClientEntityService _clientEntityService;
        private ISpectatorService _service;
        private List<IEntityBehaviour> _targets;

        [SetUp]
        public void Setup()
        {
            _followService = Substitute.For<ICameraFollowService>();
            _clientEntityService = Substitute.For<IClientEntityService>();
            _targets = new List<IEntityBehaviour>();
            ISpectatorTargetProvider targetProvider = Substitute.For<ISpectatorTargetProvider>();
            targetProvider.GetTargets().Returns(_targets);

            _service = new SpectatorService(_clientEntityService, _followService, targetProvider);
        }

        [Test]
        public void Spectate_FollowsEntity()
        {
            IEntityBehaviour entity = Substitute.For<IEntityBehaviour>();
            _service.Spectate(entity.AsRequired());

            _followService.Received(1).SetFollowTarget(entity.Transform, instant: false);
            _clientEntityService.Received(1).PossessEntity(entity.AsRequired());
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SwitchTarget_WrapsInClientIdOrder(bool next)
        {
            IEntityBehaviour last = CreateTarget(30);
            CreateTarget(20);
            IEntityBehaviour first = CreateTarget(10);
            _clientEntityService.GetPossessedEntity().Returns(next ? last : first);

            if (next) _service.NextTarget();
            else _service.PreviousTarget();

            IEntityBehaviour expected = next ? first : last;
            _clientEntityService.Received(1).PossessEntity(expected.AsRequired());
            _followService.Received(1).SetFollowTarget(expected.Transform, instant: false);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SwitchTarget_SelectsAdjacentTarget(bool next)
        {
            IEntityBehaviour last = CreateTarget(30);
            IEntityBehaviour middle = CreateTarget(20);
            IEntityBehaviour first = CreateTarget(10);
            _clientEntityService.GetPossessedEntity().Returns(middle);

            if (next) _service.NextTarget();
            else _service.PreviousTarget();

            _clientEntityService.Received(1).PossessEntity((next ? last : first).AsRequired());
        }

        [Test]
        public void NextTarget_SkipsDeadInvalidAndDisconnectedPlayers()
        {
            IEntityBehaviour current = CreateTarget(10);
            IEntityBehaviour dead = CreateTarget(20);
            IEntityBehaviour invalid = CreateTarget(30);
            IEntityBehaviour disconnected = CreateTarget(40);
            IEntityBehaviour expected = CreateTarget(50);
            _clientEntityService.GetPossessedEntity().Returns(current);

            dead.GetBehaviour<IHealthBehaviour>().ServerState.Value.Returns(new HealthState(0, 100));
            invalid.IsValid.Returns(false);
            _targets.Remove(disconnected);
            _targets.Add(null);

            _service.NextTarget();

            _clientEntityService.Received(1).PossessEntity(expected.AsRequired());
        }

        [TestCase(0)]
        [TestCase(1)]
        public void SwitchTarget_FewerThanTwoSurvivors_DoesNothing(int survivors)
        {
            IEntityBehaviour current = CreateTarget(10);
            _clientEntityService.GetPossessedEntity().Returns(current);
            if (survivors == 0) _targets.Clear();

            _service.NextTarget();
            _service.PreviousTarget();

            Assert.That(_service.IsSpectating, Is.True);
            Assert.That(_service.CanSwitchTarget, Is.False);
            _clientEntityService.DidNotReceiveWithAnyArgs().PossessEntity(default);
        }

        [Test]
        public void SwitchTarget_RespawnedLocalPlayer_DoesNothing()
        {
            IEntityBehaviour current = CreateTarget(10);
            CreateTarget(20);
            CreateTarget(30);
            _clientEntityService.GetPossessedEntity().Returns(current);
            Assert.That(_service.CanSwitchTarget, Is.True);

            current.IsLocalPlayer.Returns(true);
            _service.NextTarget();
            _service.PreviousTarget();

            Assert.That(_service.IsSpectating, Is.False);
            Assert.That(_service.CanSwitchTarget, Is.False);
            _clientEntityService.DidNotReceiveWithAnyArgs().PossessEntity(default);
        }

        [Test]
        public void SwitchTarget_NoPossessedPlayer_DoesNothing()
        {
            _clientEntityService.GetPossessedEntity().Returns((IEntityBehaviour)null);
            CreateTarget(10);
            CreateTarget(20);

            _service.NextTarget();
            _service.PreviousTarget();

            Assert.That(_service.IsSpectating, Is.False);
            _clientEntityService.DidNotReceiveWithAnyArgs().PossessEntity(default);
        }

        [Test]
        public void SwitchTarget_DestroyedPossessedPlayer_DoesNothing()
        {
            IEntityBehaviour current = CreateTarget(10);
            CreateTarget(20);
            CreateTarget(30);
            _clientEntityService.GetPossessedEntity().Returns(current);
            current.IsValid.Returns(false);

            _service.NextTarget();
            _service.PreviousTarget();

            Assert.That(_service.IsSpectating, Is.False);
            _clientEntityService.DidNotReceiveWithAnyArgs().PossessEntity(default);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SwitchTarget_CurrentPlayerRemoved_SelectsSurvivor(bool next)
        {
            IEntityBehaviour current = CreateTarget(20);
            IEntityBehaviour first = CreateTarget(10);
            IEntityBehaviour last = CreateTarget(30);
            _clientEntityService.GetPossessedEntity().Returns(current);
            _targets.Remove(current);

            if (next) _service.NextTarget();
            else _service.PreviousTarget();

            _clientEntityService.Received(1).PossessEntity((next ? first : last).AsRequired());
        }

        private IEntityBehaviour CreateTarget(ulong clientId)
        {
            IEntityBehaviour target = Substitute.For<IEntityBehaviour>();
            target.OwnerClientId.Returns(clientId);
            target.IsValid.Returns(true);
            target.GetBehaviour<IHealthBehaviour>().ServerState.Value.Returns(new HealthState(100, 100));
            _targets.Add(target);

            return target;
        }
    }
}
