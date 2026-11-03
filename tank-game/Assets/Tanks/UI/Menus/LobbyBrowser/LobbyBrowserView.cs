using Cysharp.Threading.Tasks;
using Tanks.Core.Lobbies;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.LobbyBrowser
{
    public sealed class LobbyBrowserView : ViewBase<LobbyBrowserViewModel>
    {
        private VisualElement _root;
        private ScrollView _list;
        private TextField _searchField;
        private Label _status;
        private Button _refreshButton;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("LobbyBrowserSection");
            _root.AddManipulator(new MenuHighlightManipulator());

            _list = _root.Q<ScrollView>();
            _status = _root.Q<Label>("LobbyStatus");

            _searchField = _root.Q<TextField>("SearchField");
            _searchField.RegisterValueChangedCallback(OnSearchValueChanged);

            _refreshButton = _root.Q<Button>("RefreshButton");
            _refreshButton.clicked += OnRefreshed;

            _root.Q<Button>("BackButton").clicked += () => ViewModel.Back();
        }

        protected override void Bind(LobbyBrowserViewModel viewModel)
        {
            viewModel.QueryName.Bind(_searchField.SetValueWithoutNotify);
            viewModel.FoundLobbies.Bind(OnFoundLobbiesChanged);
            viewModel.Status.Bind(x => _status.text = x);

            viewModel.IsLoading.Bind(x => _refreshButton.enabledSelf = !x);
            viewModel.IsLoading.Bind(x => _searchField.enabledSelf = !x);

            OnRefreshed();
        }

        private void OnFoundLobbiesChanged(LobbySearchState[] lobbies)
        {
            _list.Clear();

            if (lobbies is null || lobbies.Length == 0) return;

            foreach (LobbySearchState lobby in lobbies)
            {
                _list.Add(new JoinableLobby(lobby.Id, OnJoined)
                {
                    Name = lobby.Name,
                    MemberCount = lobby.PlayerCount,
                    MaxMemberCount = lobby.MaxPlayerCount
                });
            }
        }

        private void OnJoined(LobbyId id)
        {
            ViewModel.JoinLobbyAsync(id).Forget();
        }

        private void OnRefreshed()
        {
            ViewModel.RefreshAsync().Forget();
        }

        private void OnSearchValueChanged(ChangeEvent<string> e)
        {
            ViewModel.QueryName.Value = e.newValue?.Trim() ?? "";
            OnRefreshed();
        }
    }
}
