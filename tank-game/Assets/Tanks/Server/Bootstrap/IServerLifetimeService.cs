namespace Tanks.Server.Bootstrap
{
    public enum GameStep
    {
        Session,
        End
    }

    public interface IServerLifetimeService
    {
        bool IsTimedOut { get; }

        void TrySetStep(GameStep value);
    }
}
