using System.Collections.Generic;
using System.Security.Cryptography;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using UnityEngine;

namespace Tanks.Server.Bootstrap
{
    public sealed class PlayerReadyHandler :
        NetworkComponent<IGameStartBehaviour>,
        INetworkMessageHandler<WaitingForPlayersRequested>,
        INetworkMessageHandler<PlayerReadyConfirmed>
    {
        public PlayerReadyHandler(IConnectedClientService clientService)
        {
            _clientService = clientService;
        }

        public ISet<int> Pending => _pendingConfirmations;

        private readonly IConnectedClientService _clientService;
        private readonly HashSet<int> _pendingConfirmations = new();

        private int _lastTick;
        private float _lastTickDelta;

        public void Handle(PlayerReadyConfirmed message)
        {
            if (_pendingConfirmations.Remove(message.SecretHandle) == false)
            {
                Debug.LogWarning("Invalid secret handle!");
                return;
            }

            if (_pendingConfirmations.Count > 0) return;

            Behaviour.StartGameEveryoneRpc(new GameStartedFromServer
            {
                ServerTick = _lastTick,
                ServerTickDelta = _lastTickDelta,
                StartDelaySeconds = 2
            });
        }

        public void Handle(WaitingForPlayersRequested message)
        {
            _pendingConfirmations.Clear();

            foreach (ulong clientId in _clientService.GetConnectedClientIds())
            {
                int secret = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);

                // Increment if duplicated
                while (_pendingConfirmations.Add(secret) == false) secret++;

                Behaviour.RequestReadyConfirmation(clientId, secret);
            }
        }

        protected override void OnTick(int tick, float tickDelta)
        {
            _lastTick = tick;
            _lastTickDelta = tickDelta;
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler<WaitingForPlayersRequested>(this);
            Behaviour.RegisterMessageHandler<PlayerReadyConfirmed>(this);
        }
    }
}
