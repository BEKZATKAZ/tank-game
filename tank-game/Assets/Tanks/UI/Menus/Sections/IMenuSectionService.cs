namespace Tanks.UI.Menus.Sections
{
    public interface IMenuSectionService
    {
        void OpenSection(MenuSectionType type, bool instant = false);
    }
}
