using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Elements
{
    [UxmlElement]
    public sealed partial class CopyrightNotice : Label
    {
        public CopyrightNotice()
        {
            text = $"© 2026 BEKZATKAZ v{Application.version}";
            AddToClassList("notice");
            NotifyPropertyChanged(nameof(text));
        }
    }
}
