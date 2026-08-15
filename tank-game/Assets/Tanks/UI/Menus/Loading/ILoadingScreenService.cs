using Tanks.Localization;

namespace Tanks.UI.Menus.Loading
{
    public interface ILoadingScreenService
    {
        void Begin(ILocalizedString label);
        void End();
    }
}
