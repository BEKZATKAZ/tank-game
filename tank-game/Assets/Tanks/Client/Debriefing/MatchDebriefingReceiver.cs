using Newtonsoft.Json;
using System;
using Tanks.Core.Common.Scenes;
using Tanks.Core.Debriefing;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Messaging;
using UnityEngine;

namespace Tanks.Client.Debriefing
{
    public sealed class MatchDebriefingReceiver :
        NetworkComponent<IGameStartBehaviour>,
        IMessageHandler<EndGameFromServer>,
        INetworkMessageHandler<DebriefingDelivered>
    {
        public MatchDebriefingReceiver(
            IMessageBus bus,
            IPendingDebriefingService pending,
            GameplaySessionService session)
        {
            _bus = bus;
            _pending = pending;
            _session = session;
        }

        private readonly IMessageBus _bus;
        private readonly IPendingDebriefingService _pending;
        private readonly GameplaySessionService _session;

        protected override void OnInitialize()
        {
            _bus.RegisterHandler<EndGameFromServer>(this);
            Behaviour.RegisterMessageHandler(this);
        }

        protected override void OnDestroy()
        {
            _bus.UnregisterHandler<EndGameFromServer>(this);
        }

        public void Handle(EndGameFromServer message, IMessageSender sender)
        {
            if (_session.IsDirectSceneTest) return;

            _pending.Expect(message.MatchId);
        }

        public void Handle(DebriefingDelivered message)
        {
            if (_session.IsDirectSceneTest) return;

            try
            {
                Debug.LogFormat(
                    "Received debriefing of {0}: {1}", message.MatchId,
                        message.Json);

                DebriefingPlayerResult result = JsonConvert
                    .DeserializeObject<DebriefingPlayerResult>(message.Json);

                if (result == null) return;

                _pending.Store(message.MatchId, result);
                Behaviour.AcknowledgeDebriefingServerRpc(message.MatchId);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
