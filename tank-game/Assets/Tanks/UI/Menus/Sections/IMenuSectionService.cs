namespace Tanks.UI.Menus.Sections
{
    public interface IMenuSectionService
    {
        MenuSectionType Current { get; }

        void OpenSection(MenuSectionType type, bool instant = false);
    }
}
