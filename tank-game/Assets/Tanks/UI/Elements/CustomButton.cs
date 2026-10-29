using FMODUnity;
using UnityEngine.UIElements;

namespace Tanks.UI.Elements
{
    [UxmlElement]
    public partial class CustomButton : Button
    {
        public CustomButton()
        {
            RegisterCallback<PointerEnterEvent>(OnButtonHover);
            clicked += OnButtonClicked;
        }

        [UxmlAttribute] public string HoverSoundPath { get; set; } = "event:/UI/Button/Hover";
        [UxmlAttribute] public string ClickSoundPath { get; set; } = "event:/UI/Button/Click";

        private void OnButtonHover(PointerEnterEvent e)
        {
            if (string.IsNullOrWhiteSpace(HoverSoundPath)) return;
            if (enabledInHierarchy) RuntimeManager.PlayOneShot(HoverSoundPath);
        }

        private void OnButtonClicked()
        {
            if (string.IsNullOrWhiteSpace(ClickSoundPath)) return;
            if (enabledInHierarchy) RuntimeManager.PlayOneShot(ClickSoundPath);
        }
    }
}
