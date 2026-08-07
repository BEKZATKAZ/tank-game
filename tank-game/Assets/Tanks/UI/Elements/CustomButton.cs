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
            RegisterCallback<ClickEvent>(OnButtonClicked);
        }

        [UxmlAttribute] public string HoverSoundPath { get; set; } = "event:/UI/Button/Hover";
        [UxmlAttribute] public string ClickSoundPath { get; set; } = "event:/UI/Button/Click";

        private void OnButtonHover(PointerEnterEvent e)
        {
            if (string.IsNullOrWhiteSpace(HoverSoundPath)) return;
            RuntimeManager.PlayOneShot(HoverSoundPath);
        }

        private void OnButtonClicked(ClickEvent e)
        {
            if (string.IsNullOrWhiteSpace(ClickSoundPath)) return;
            RuntimeManager.PlayOneShot(ClickSoundPath);
        }
    }
}
