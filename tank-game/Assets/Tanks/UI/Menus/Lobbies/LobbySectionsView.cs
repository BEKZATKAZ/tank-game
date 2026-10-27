using NaughtyAttributes;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Lobbies
{
    public sealed class LobbySectionsView : ViewBase<LobbySectionsViewModel>
    {
        [Required]
        [SerializeField]
        private LobbyMainView _mainView;

        [Required]
        [SerializeField]
        private LobbyChatView _chatView;

        private VisualElement _root;
        private Label _lobbyName;
        private Label _lobbyStatus;
        private Label _chatUnreadBadge;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root;

            _lobbyName = _root.Q<Label>("LobbyName");
            _lobbyStatus = _root.Q<Label>("LobbyStatus");
            _chatUnreadBadge = _root.Q<Label>("ChatUnreadBadge");

            _root.Q<Button>("MainButton").clicked += OnMainClicked;
            _root.Q<Button>("ChatButton").clicked += OnChatClicked;
        }

        protected override void Bind(LobbySectionsViewModel viewModel)
        {
            viewModel.LobbyName.Bind(OnLobbyNameUpdated);
            viewModel.Status.Bind(OnStatusUpdated);
            viewModel.IsChatOpen.Bind(OnChatOpenChanged);
            viewModel.UnreadCount.Bind(OnUnreadCountChanged);
        }

        protected override void Unbind(LobbySectionsViewModel viewModel)
        {
            viewModel.LobbyName.Unbind(OnLobbyNameUpdated);
            viewModel.Status.Unbind(OnStatusUpdated);
            viewModel.IsChatOpen.Unbind(OnChatOpenChanged);
            viewModel.UnreadCount.Unbind(OnUnreadCountChanged);
        }

        private void LateUpdate()
        {
            float t = math.abs(math.sin(Time.time * math.PI));
            _lobbyStatus.style.opacity = math.lerp(0.25f, 1, t);
        }

        private void OnLobbyNameUpdated(string value)
        {
            _lobbyName.text = value;
        }

        private void OnStatusUpdated(string value) => _lobbyStatus.text = value;
        private void OnMainClicked() => ViewModel?.SetChatOpen(false);
        private void OnChatClicked() => ViewModel?.SetChatOpen(true);

        private void OnChatOpenChanged(bool open)
        {
            _mainView.Root.style.display = open ? DisplayStyle.None : DisplayStyle.Flex;
            _chatView.Root.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnUnreadCountChanged(int count)
        {
            _chatUnreadBadge.text = count > 99 ? "99+" : count.ToString();
            _chatUnreadBadge.style.display = count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
