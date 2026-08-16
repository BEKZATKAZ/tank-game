namespace Tanks.Core.Lobbies
{
    public struct LobbySearchState
    {
        public LobbyId Id;
        public LobbyAddress Address;
        public string Name;

        public int PlayerCount;
        public int MaxPlayerCount;
    }
}
