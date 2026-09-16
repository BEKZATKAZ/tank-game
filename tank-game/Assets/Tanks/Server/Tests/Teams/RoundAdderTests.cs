using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Teams;
using Tanks.Shared;
using Tanks.Shared.Messaging;
using Tanks.Shared.Teams;

namespace Tanks.Server.Tests.Teams
{
    public class RoundAdderTests
    {
        private RoundAdder _component;

        [SetUp]
        public void Setup()
        {
            _component = new RoundAdder(Substitute.For<IMessageBus>());
            _component.Initialize(Substitute.For<ITeamBehaviourService>());
        }

        [TearDown]
        public void TearDown()
        {
            _component.Destroy();
        }

        [Test]
        public void RoundStart_AddsRound()
        {
            for (int i = 1; i <= 10; i++)
            {
                _component.Handle(new GameStartedFromServer(), null);
                _component.Behaviour.Received(1).SetRoundEveryoneRpc(i);
            }
        }
    }
}
