using Tanks.Client.Input;
using Tanks.Client.Input.Bindings;

namespace Tanks.UI.Gameplay.Input
{
    public struct UIGameplayInput
    {
        public bool Pause;
        public bool GameStatsOpen;
        public bool PreviousSpectatorTarget;
        public bool NextSpectatorTarget;
    }

    public sealed class UIGameplayInputService : InputServiceBase<UIGameplayInput>
    {
        public UIGameplayInputService(IControlBindingService bindings = null)
            : base(bindings) { }

        protected override void EnableMaps() => Controls.UI.Enable();

        public override UIGameplayInput Read()
        {
            if (IsEnabled == false || IsSuspended) return default;

            return new UIGameplayInput
            {
                Pause = Controls.UI.Pause.WasPressedThisFrame(),
                GameStatsOpen = Controls.UI.GameStats.IsPressed(),
                PreviousSpectatorTarget = Controls.UI.PreviousSpectatorTarget
                    .WasPressedThisFrame(),
                NextSpectatorTarget = Controls.UI.NextSpectatorTarget
                    .WasPressedThisFrame()
            };
        }
    }
}
