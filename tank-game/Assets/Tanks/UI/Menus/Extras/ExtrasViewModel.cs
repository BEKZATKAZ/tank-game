using Tanks.UI.Menus.Sections;

namespace Tanks.UI.Menus.Extras
{
    public sealed class ExtrasViewModel : IViewModel
    {
        public ExtrasViewModel(IMenuSectionService navigation)
        {
            _navigation = navigation;
        }

        private readonly IMenuSectionService _navigation;

        public void Back() => _navigation.OpenSection(MenuSectionType.Main);
        public void OpenItems() => _navigation.OpenSection(MenuSectionType.ItemBrowser);
        public void OpenLeaderboards() => _navigation.OpenSection(MenuSectionType.Leaderboards);
    }
}
