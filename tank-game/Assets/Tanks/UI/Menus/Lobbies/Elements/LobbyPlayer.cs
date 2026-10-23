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

            _readiness = new Label();
            _readiness.AddToClassList("lobby-player__readiness");
            Add(_username);
            Add(_readiness);
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

        public string Readiness
        {
            get => _readiness.text;
            set => _readiness.text = value;
        }

        private readonly Label _readiness;
        private readonly Label _username;

        private string _usernameValue = "Username";
    }
}
