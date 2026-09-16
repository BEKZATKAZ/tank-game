using Tanks.Client.Input;

namespace Tanks.UI.Gameplay.Input
{
    public struct UIGameplayInput
    {
        public bool GameStatsOpen;
    }

    public sealed class UIGameplayInputService : InputServiceBase<UIGameplayInput>
    {
        public override UIGameplayInput Read() => new UIGameplayInput
        {
            GameStatsOpen = Controls.UI.GameStats.IsPressed()
        };
    }
}
