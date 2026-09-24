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

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root;

            _lobbyName = _root.Q<Label>("LobbyName");
            _lobbyStatus = _root.Q<Label>("LobbyStatus");

            _root.Q<Button>("MainButton").clicked += OnMainClicked;
            _root.Q<Button>("ChatButton").clicked += OnChatClicked;
        }

        protected override void Bind(LobbySectionsViewModel viewModel)
        {
            viewModel.LobbyName.Bind(OnLobbyNameUpdated);
            viewModel.Status.Bind(x => _lobbyStatus.text = x);

            OnMainClicked();
        }

        protected override void Unbind(LobbySectionsViewModel viewModel)
        {
            viewModel.LobbyName.Unbind(OnLobbyNameUpdated);
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

        private void OnMainClicked()
        {
            _mainView.Root.style.display = DisplayStyle.Flex;
            _chatView.Root.style.display = DisplayStyle.None;
        }

        private void OnChatClicked()
        {
            _mainView.Root.style.display = DisplayStyle.None;
            _chatView.Root.style.display = DisplayStyle.Flex;
        }
    }
}
