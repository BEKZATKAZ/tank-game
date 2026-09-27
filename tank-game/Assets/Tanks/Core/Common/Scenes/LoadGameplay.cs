using Tanks.Core.Lobbies.Sessions;

namespace Tanks.Core.Common.Scenes
{
    public struct LoadGameplay : ISceneParameters
    {
        public readonly string Name => "Gameplay";

        public SessionSettings Settings;
        public string JoinCode;
    }
}
