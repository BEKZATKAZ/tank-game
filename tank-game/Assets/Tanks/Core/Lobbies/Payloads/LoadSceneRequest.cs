namespace Tanks.Core.Lobbies.Payloads
{
    public enum RequestedServerScene
    {
        Gameplay,
        Playground
    }

    public struct LoadSceneRequest
    {
        public RequestedServerScene Scene;
    }
}
