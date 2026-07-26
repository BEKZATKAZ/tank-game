using UnityEngine.UIElements;

namespace Tanks.UI.Gameplay.StartCountdown
{
    public sealed class StartCountdownView : IView<StartCountdownModel>
    {
        private Label _label;

        public void Hide()
        {
            _label.style.display = DisplayStyle.None;
        }

        public void Initialize(VisualElement root)
        {
            _label = root.Q<Label>("StartText");
            _label.style.display = DisplayStyle.None;
        }

        public void Show(StartCountdownModel data)
        {
            _label.style.display = DisplayStyle.Flex;
            _label.text = data.Text;
        }
    }
}
