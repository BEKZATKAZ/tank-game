using System;

namespace Tanks.Core.Common.Scenes
{
    public sealed class GameplaySessionService
    {
        public bool IsDirectSceneTest { get; private set; }
        public string PlayerId { get; private set; }
        public string ConnectionError { get; set; }

        public void Begin(bool directSceneTest, string authenticatedPlayerId)
        {
            IsDirectSceneTest = directSceneTest;
            ConnectionError = null;

            PlayerId = directSceneTest
                ? "editor-" + Guid.NewGuid().ToString("N") : authenticatedPlayerId;
        }
    }
}
