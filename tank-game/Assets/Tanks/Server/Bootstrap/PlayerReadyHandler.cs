using System.Collections.Generic;
using System.Security.Cryptography;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Unity.Netcode;
using UnityEngine;

namespace Tanks.Server.Bootstrap
{
    public sealed class PlayerReadyHandler :
        NetworkComponent<GameStartBehaviour>,
        INetworkMessageHandler<WaitingForPlayersRequested>,
        INetworkMessageHandler<PlayerReadyConfirmed>
    {
        private readonly HashSet<int> _pendingConfirmations = new();

        public void Handle(PlayerReadyConfirmed message)
        {
            if (_pendingConfirmations.Remove(message.SecretHandle) == false)
            {
                Debug.LogWarning("Invalid secret handle!");
                return;
            }

            if (_pendingConfirmations.Count > 0) return;

            Behaviour.StartGameClientRpc(new GameStarted
            {
                StartDelaySeconds = 2
            });
        }

        public void Handle(WaitingForPlayersRequested message)
        {
            _pendingConfirmations.Clear();

            foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            {
                int secret = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);

                // Increment if duplicated
                while (_pendingConfirmations.Add(secret) == false)
                {
                    secret++;
                }

                BaseRpcTarget target = Behaviour.RpcTarget.Single(clientId, RpcTargetUse.Temp);
                Behaviour.RequestReadyConfirmationClientRpc(secret, target);
            }
        }

        protected override void Awake()
        {
            base.Awake();
            Behaviour.RegisterMessageHandler<WaitingForPlayersRequested>(this);
            Behaviour.RegisterMessageHandler<PlayerReadyConfirmed>(this);
        }
    }
}
