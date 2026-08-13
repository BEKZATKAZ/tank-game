using Unity.Properties;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Lobbies.Elements
{
    [UxmlElement]
    public sealed partial class ChatUserMessage : ChatMessage
    {
        public ChatUserMessage()
        {
            AddToClassList("chat-user-message");
            UpdateText();
        }

        [UxmlAttribute]
        [CreateProperty]
        public string UsernameHexColor
        {
            get => _usernameHexColor;
            set
            {
                _usernameHexColor = value;
                NotifyPropertyChanged(nameof(UsernameHexColor));
                UpdateText();
            }
        }

        [UxmlAttribute]
        [CreateProperty]
        public string Username
        {
            get => _usernameValue;
            set
            {
                _usernameValue = value;
                NotifyPropertyChanged(nameof(Username));
                UpdateText();
            }
        }

        private string _usernameHexColor = "#0f0";
        private string _usernameValue = "Username";

        protected override void UpdateText()
        {
            ContentLabel.text = $"<{_usernameHexColor}><b>{_usernameValue}:</b></color> {Content}";
        }
    }
}
