using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tanks.Core.Lobbies
{
    public interface ILobbyChatService
    {
        const int MaxMessageLength = 500;

        event Action<LobbyChatMessage> MessageReceived;

        IReadOnlyList<LobbyChatMessage> Messages { get; }

        Task<ResultType> ConnectAsync(LobbyId lobbyId);
        Task<ResultType> SendAsync(LobbyId lobbyId, string message);
        Task<ResultType> DisconnectAsync();
    }
}
