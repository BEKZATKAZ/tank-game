using System;

namespace Tanks.Core.Lobbies
{
    public sealed class LobbyChatMessage
    {
        public LobbyId LobbyId { get; set; }
        public string Id { get; set; }
        public string SenderId { get; set; }
        public string Username { get; set; }
        public string Content { get; set; }
        public DateTime ReceivedTime { get; set; }
        public bool IsHistory { get; set; }
    }
}
