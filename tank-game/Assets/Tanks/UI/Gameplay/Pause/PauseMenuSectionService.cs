using Tanks.UI.Menus.Sections;

namespace Tanks.UI.Gameplay.Pause
{
    public sealed class PauseMenuSectionService : IMenuSectionService
    {
        public PauseMenuSectionService(PauseMenuViewModel pause)
        {
            _pause = pause;
        }

        private readonly PauseMenuViewModel _pause;

        public void OpenSection(MenuSectionType type, bool instant = false)
        {
            if (type is MenuSectionType.Main && _pause.Section.Value is PauseMenuSection.Settings)
                _pause.Toggle();
        }
    }
}
