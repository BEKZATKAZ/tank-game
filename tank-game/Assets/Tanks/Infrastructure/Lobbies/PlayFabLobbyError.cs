namespace Tanks.Infrastructure.Lobbies
{
    public enum PlayFabLobbyError
    {
        TooManyRequests = unchecked((int)0x801901AD),
        InvalidParameter = unchecked((int)0x8923620B),
        EntityTokenExpired = unchecked((int)0x8923620D),
        Unauthorized = unchecked((int)0x8923620E),
        BadRequest = unchecked((int)0x8923621E),
        NotMember = unchecked((int)0x89236220),
        LobbyNotFound = unchecked((int)0x89236226),
        LobbyNotJoinable = unchecked((int)0x89236227)
    }
}
