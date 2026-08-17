using Tanks.UI.Menus.Lobbies.Elements;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Lobbies
{
    public sealed class LobbyChatView : ViewBase<LobbyChatViewModel>
    {
        private VisualElement _root;
        private ScrollView _messages;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("Chat");
            _messages = _root.Q<ScrollView>();
        }

        protected override void Bind(LobbyChatViewModel viewModel)
        {
            viewModel.CurrentServerMessage.Bind(OnServerMessageAdded);
            viewModel.DisplayWelcomeMessage();
        }

        private void OnServerMessageAdded(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            _messages.Add(new ChatServerMessage { Content = message });
        }
    }
}
