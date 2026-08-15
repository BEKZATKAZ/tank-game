using Tanks.Utils;

namespace Tanks.UI.Menus.Loading
{
    public sealed class LoadingViewModel : IViewModel
    {
        public BindableProperty<string> Label { get; } = new();
    }
}
