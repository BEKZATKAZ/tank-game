using Unity.Properties;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Lobbies.Elements
{
    public enum ServerMessageType
    {
        PlayerJoined,
        PlayerDisconnected
    }

    [UxmlElement]
    public sealed partial class ChatServerMessage : ChatMessage
    {
        public ChatServerMessage()
        {
            AddToClassList("chat-server-message");
            UpdateText();
        }

        [UxmlAttribute]
        [CreateProperty]
        public ServerMessageType Type
        {
            get => _type;
            set
            {
                _type = value;
                NotifyPropertyChanged(nameof(Type));
                UpdateText();
            }
        }

        [UxmlAttribute]
        [CreateProperty]
        public string[] Arguments
        {
            get => _arguments;
            set
            {
                _arguments = value;
                NotifyPropertyChanged(nameof(Arguments));
                UpdateText();
            }
        }

        private ServerMessageType _type;
        private string[] _arguments = { "Username" };

        protected override void UpdateText()
        {
            string entry = _type switch
            {
                ServerMessageType.PlayerJoined => "chat-player-joined",
                ServerMessageType.PlayerDisconnected => "chat-player-disconnected",
                _ => null
            };

            if (entry is null)
            {
                ContentLabel.text = "";
                return;
            }

            ContentLabel.text = LocalizationSettings.StringDatabase
                .GetLocalizedString("Menus", $"menu.lobby.{entry}", _arguments);
        }
    }
}
