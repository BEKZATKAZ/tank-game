using Tanks.Core.Lobbies.Sessions;

namespace Tanks.Infrastructure.Lobbies
{
    public sealed class SessionConnectionService : ISessionConnectionService
    {
        public string JoinCode { get; private set; }

        public void SetConnectionData(string joinCode)
        {
            JoinCode = joinCode;
        }
    }
}
