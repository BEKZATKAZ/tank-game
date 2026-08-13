using Tanks.UI.Elements;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Lobbies.Elements
{
    [UxmlElement]
    public partial class LobbyPlayer : VisualElement
    {
        public LobbyPlayer()
        {
            _username = new Label(Username);
            _username.AddToClassList("lobby-player__username");

            Add(_username);
            AddToClassList("lobby-player");
        }

        [UxmlAttribute]
        public string Username
        {
            get => _usernameValue;
            set
            {
                _usernameValue = value;
                _username.text = _usernameValue;
                NotifyPropertyChanged(nameof(Username));
            }
        }

        private readonly Label _username;

        private string _usernameValue = "Username";
    }
}
