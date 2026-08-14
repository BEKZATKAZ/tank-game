using Tanks.Utils;

namespace Tanks.UI.Menus.Main
{
    public sealed class MainMenuSectionsViewModel : IViewModel
    {
        public MainMenuSectionsViewModel(IMenuNavigation navigation)
        {
            _navigation = navigation;
        }

        public BindableProperty<MainMenuSection> Section { get; } = new();

        private readonly IMenuNavigation _navigation;

        public void OpenSection(MainMenuSection section)
        {
            Section.Value = section;
        }

        public void OpenMenu(MenuType type)
        {
            _navigation.Navigate(type);
        }
    }
}
