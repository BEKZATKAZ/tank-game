namespace Tanks.Core.Lobbies
{
    public readonly struct LobbyId
    {
        public LobbyId(string value) => Value = value;

        public readonly string Value;
    }
}
