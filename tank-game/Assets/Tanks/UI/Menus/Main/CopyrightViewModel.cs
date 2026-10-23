using Tanks.Utils;
using UnityEngine;

namespace Tanks.UI.Menus.Main
{
    public sealed class CopyrightViewModel : IViewModel
    {
        public CopyrightViewModel()
        {
            Label = new BindableProperty<string>(
                $"© 2026 {Application.companyName} v{Application.version}");
        }

        public IReadOnlyBindableProperty<string> Label { get; }
    }
}
