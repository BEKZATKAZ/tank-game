using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tanks.UI.Menus
{
    public sealed class MenuNavigation : MonoBehaviour, IMenuNavigation
    {
        [SerializeField] private GameObject _mainMenu;
        [SerializeField] private GameObject _lobbyMenu;

        private readonly Dictionary<GameObject, ViewBase[]> _allMenuViews = new();
        private GameObject _currentMenu;

        public void Navigate(MenuType type)
        {
            if (_currentMenu) HideCurrentMenu();

            _currentMenu = type switch
            {
                MenuType.Main => _mainMenu,
                MenuType.Lobby => _lobbyMenu,
                _ => throw new NotImplementedException()
            };

            _currentMenu.SetActive(true);
            ShowCurrentMenu();
        }

        private void ShowCurrentMenu()
        {
            foreach (ViewBase view in _allMenuViews[_currentMenu])
            {
                if (view.ShowOnPlay) view.Show();
            }
        }

        private void HideCurrentMenu()
        {
            foreach (ViewBase view in _allMenuViews[_currentMenu]) view.Hide();
        }

        private void Awake()
        {
            _allMenuViews[_mainMenu] = _mainMenu.GetComponents<ViewBase>();
            _allMenuViews[_lobbyMenu] = _lobbyMenu.GetComponents<ViewBase>();
            Navigate(MenuType.Main);
        }
    }
}
