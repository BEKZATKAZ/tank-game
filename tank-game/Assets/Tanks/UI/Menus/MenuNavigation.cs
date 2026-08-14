using System;
using UnityEngine;

namespace Tanks.UI.Menus
{
    public sealed class MenuNavigation : MonoBehaviour, IMenuNavigation
    {
        [SerializeField] private GameObject _mainMenu;
        [SerializeField] private GameObject _lobbyMenu;

        private GameObject _currentMenu;

        public void Navigate(MenuType type)
        {
            if (_currentMenu) _currentMenu.SetActive(false);

            _currentMenu = type switch
            {
                MenuType.Main => _mainMenu,
                MenuType.Lobby => _lobbyMenu,
                _ => throw new NotImplementedException()
            };

            _currentMenu.SetActive(true);
        }

        private void Awake()
        {
            _mainMenu.SetActive(true);
            _lobbyMenu.SetActive(false);
        }
    }
}
