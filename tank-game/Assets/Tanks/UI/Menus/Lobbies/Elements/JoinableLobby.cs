using System;
using Tanks.Core.Lobbies;
using Tanks.UI.Elements;
using Unity.Properties;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Lobbies.Elements
{
    [UxmlElement]
    public sealed partial class JoinableLobby : VisualElement
    {
        public JoinableLobby() : this(default, null) { }

        public JoinableLobby(LobbyId id, Action<LobbyId> onJoin)
        {
            _name = new Label(_nameValue);
            _name.AddToClassList("lobby-name");
            _id = id;

            _memberCount = new Label($"{_memberCountValue} / {_maxMemberCountValue}");
            _memberCount.AddToClassList("lobby-member-count");

            Button joinButton = new CustomButton
            {
                text = LocalizationSettings.StringDatabase
                   .GetLocalizedString("Menus", "menu.find-lobby.join")
            };

            Add(_name);
            Add(_memberCount);
            Add(joinButton);
            AddToClassList("lobby");

            joinButton.clicked += () => onJoin?.Invoke(_id);
        }

        [UxmlAttribute]
        [CreateProperty]
        public string Name
        {
            get => _nameValue;
            set
            {
                _nameValue = value;
                _name.text = value;
                NotifyPropertyChanged(nameof(Name));
            }
        }

        [UxmlAttribute]
        [CreateProperty]
        public int MemberCount
        {
            get => _memberCountValue;
            set
            {
                _memberCountValue = value;
                _memberCount.text = $"{_memberCountValue} / {_maxMemberCountValue}";
                NotifyPropertyChanged(nameof(MemberCount));
            }
        }

        [UxmlAttribute]
        [CreateProperty]
        public int MaxMemberCount
        {
            get => _maxMemberCountValue;
            set
            {
                _maxMemberCountValue = value;
                _memberCount.text = $"{_memberCountValue} / {_maxMemberCountValue}";
                NotifyPropertyChanged(nameof(MaxMemberCount));
            }
        }

        private readonly Label _name;
        private readonly Label _memberCount;

        private string _nameValue = "Lobby Name";
        private int _memberCountValue = 1;
        private int _maxMemberCountValue = 8;
        private LobbyId _id;
    }
}
