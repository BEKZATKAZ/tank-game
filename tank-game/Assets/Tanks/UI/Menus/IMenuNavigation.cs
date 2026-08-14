using UnityEngine;

namespace Tanks.UI.Menus
{
    public enum MenuType
    {
        Main,
        Lobby
    }

    public enum MainMenuSection
    {
        None,
        FindLobby,
        Settings
    }

    public interface IMenuNavigation
    {
        void Navigate(MenuType type);
    }
}
