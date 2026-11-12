using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Debriefing;
using Tanks.Core.Common.Scenes;
using Tanks.Core.Debriefing;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Messaging;

namespace Tanks.Client.Tests.Debriefing
{
    public sealed class MatchDebriefingReceiverTests
    {
        [Test]
        public void DirectSceneMatch_DoesNotExpectOrAcceptDebriefing()
        {
            IGameStartBehaviour behaviour = Substitute.For<IGameStartBehaviour>();
            IMessageBus bus = Substitute.For<IMessageBus>();

            PendingDebriefingService pending = new();
            GameplaySessionService session = new();

            session.Begin(true, null);

            MatchDebriefingReceiver receiver = new(bus, pending, session);

            receiver.Initialize(behaviour);
            receiver.Handle(new EndGameFromServer { MatchId = "match" }, null);
            receiver.Handle(new DebriefingDelivered { MatchId = "match" });

            Assert.That(pending.HasPending, Is.False);

            behaviour.DidNotReceive().AcknowledgeDebriefingServerRpc(
                Arg.Any<string>(), default);

            receiver.Destroy();
        }

        [Test]
        public void Delivery_StoresBeforeAcknowledgmentAndIgnoresConsumedMatches()
        {
            IGameStartBehaviour behaviour = Substitute.For<IGameStartBehaviour>();
            IMessageBus bus = Substitute.For<IMessageBus>();

            PendingDebriefingService pending = new();

            bool storedAtAcknowledgment = false;

            behaviour.When(value => value.AcknowledgeDebriefingServerRpc("match",
                default))
                .Do(_ => storedAtAcknowledgment = pending.Result != null);

            MatchDebriefingReceiver receiver = new(bus, pending, new());

            receiver.Initialize(behaviour);
            receiver.Handle(new EndGameFromServer { MatchId = "match" }, null);

            DebriefingDelivered message = new DebriefingDelivered
            {
                MatchId = "match",
                Json = JsonConvert.SerializeObject(new DebriefingPlayerResult
                {
                    Value = new PlayerDebriefing { MatchId = "match" }
                })
            };

            receiver.Handle(message);

            Assert.That(storedAtAcknowledgment, Is.True);

            pending.Consume();
            receiver.Handle(message);

            Assert.That(pending.HasPending, Is.False);

            receiver.Destroy();
        }
    }
}
