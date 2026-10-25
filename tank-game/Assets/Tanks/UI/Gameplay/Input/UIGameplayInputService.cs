using Tanks.Client.Input;

namespace Tanks.UI.Gameplay.Input
{
    public struct UIGameplayInput
    {
        public bool GameStatsOpen;
        public bool PreviousSpectatorTarget;
        public bool NextSpectatorTarget;
    }

    public sealed class UIGameplayInputService : InputServiceBase<UIGameplayInput>
    {
        public override UIGameplayInput Read() => new UIGameplayInput
        {
            GameStatsOpen = Controls.UI.GameStats.IsPressed(),
            PreviousSpectatorTarget = Controls.UI.PreviousSpectatorTarget.WasPressedThisFrame(),
            NextSpectatorTarget = Controls.UI.NextSpectatorTarget.WasPressedThisFrame()
        };
    }
}
