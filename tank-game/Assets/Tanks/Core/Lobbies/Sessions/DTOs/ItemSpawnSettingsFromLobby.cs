using System;

namespace Tanks.Core.Lobbies.Sessions.DTOs
{
    public enum ItemSpawnRate
    {
        Slow,
        Normal,
        Fast,
        Chaos
    }

    [Serializable]
    public struct ItemSpawnSettingsFromLobby
    {
        public ItemSpawnRate Rate;
    }
}