using Tanks.Core.Lobbies;
using Tanks.UI.Menus.Lobbies.Elements;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Lobbies
{
    public sealed class LobbyChatView : ViewBase<LobbyChatViewModel>
    {
        private VisualElement _root;
        private ScrollView _messages;
        private LobbyId _lastLobbyId;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("Chat");
            _messages = _root.Q<ScrollView>();
            _messages.Clear();
        }

        protected override void Bind(LobbyChatViewModel viewModel)
        {
            viewModel.CurrentServerMessage.Bind(OnServerMessageAdded);
            viewModel.LobbyId.Bind(OnLobbyIdChanged);
        }

        protected override void Unbind(LobbyChatViewModel viewModel)
        {
            viewModel.CurrentServerMessage.Unbind(OnServerMessageAdded);
            viewModel.LobbyId.Unbind(OnLobbyIdChanged);
        }

        private void OnLobbyIdChanged(LobbyId id)
        {
            if (_lastLobbyId.Value == id.Value) return;
            _lastLobbyId = id;
            ViewModel.DisplayWelcomeMessage();
        }

        private void OnServerMessageAdded(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            _messages.Add(new ChatServerMessage { Content = message });
        }
    }
}
