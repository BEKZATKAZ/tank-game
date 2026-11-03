using UnityEngine.UIElements;

namespace Tanks.UI.Menus.LobbyRoom.Elements
{
    [UxmlElement]
    public sealed partial class ChatServerMessage : ChatMessage
    {
        public ChatServerMessage()
        {
            AddToClassList("chat-server-message");
            UpdateText();
        }
    }
}
