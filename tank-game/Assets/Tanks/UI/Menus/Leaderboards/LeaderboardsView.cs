using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Tanks.Core.Leaderboards;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Leaderboards
{
    public sealed class LeaderboardsView : ViewBase<LeaderboardsViewModel>
    {
        [SerializeField] private FlagDatabase _flags;

        private VisualElement _root;
        private ScrollView _list;
        private VisualElement _ownRow;
        private DropdownField _metric;
        private DropdownField _category;
        private Label _status;
        private Label _page;
        private Button _previous;
        private Button _next;
        private Button _refresh;
        private bool _binding;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("LeaderboardsSection");
            _root.AddManipulator(new MenuHighlightManipulator());
            _list = _root.Q<ScrollView>("LeaderboardEntries");
            _ownRow = _root.Q("OwnEntry");
            _metric = _root.Q<DropdownField>("LeaderboardMetric");
            _category = _root.Q<DropdownField>("LeaderboardCategory");
            _status = _root.Q<Label>("LeaderboardStatus");
            _page = _root.Q<Label>("LeaderboardPage");
            _previous = _root.Q<Button>("PreviousPageButton");
            _next = _root.Q<Button>("NextPageButton");
            _refresh = _root.Q<Button>("RefreshButton");

            _metric.RegisterValueChangedCallback(OnSelectionChanged);
            _category.RegisterValueChangedCallback(OnSelectionChanged);
            _previous.clicked += () => ViewModel.ChangePageAsync(-1).Forget();
            _next.clicked += () => ViewModel.ChangePageAsync(1).Forget();
            _refresh.clicked += () => ViewModel.RefreshAsync().Forget();
            _root.Q<Button>("BackButton").clicked += () => ViewModel.Back();
        }

        protected override void Bind(LeaderboardsViewModel viewModel)
        {
            _binding = true;
            _metric.choices = new List<string>
            {
                viewModel.Text("menu.leaderboards.eliminations", "Eliminations"),
                viewModel.Text("menu.leaderboards.items", "Picked-up Items")
            };

            _category.choices = new List<string>
            {
                viewModel.Text("menu.leaderboards.total", "Total"),
                viewModel.Text("menu.leaderboards.weekly", "Weekly"),
                viewModel.Text("menu.leaderboards.daily", "Daily")
            };

            _metric.SetValueWithoutNotify(_metric.choices[0]);
            _category.SetValueWithoutNotify(_category.choices[0]);
            _binding = false;

            viewModel.Entries.Bind(OnEntriesChanged);
            viewModel.OwnEntry.Bind(OnOwnEntryChanged);
            viewModel.Status.Bind(OnStatusChanged);
            viewModel.PageLabel.Bind(OnPageChanged);
            viewModel.IsLoading.Bind(OnLoadingChanged);
            viewModel.RefreshAsync().Forget();
        }

        protected override void Unbind(LeaderboardsViewModel viewModel)
        {
            viewModel.Entries.Unbind(OnEntriesChanged);
            viewModel.OwnEntry.Unbind(OnOwnEntryChanged);
            viewModel.Status.Unbind(OnStatusChanged);
            viewModel.PageLabel.Unbind(OnPageChanged);
            viewModel.IsLoading.Unbind(OnLoadingChanged);
        }

        private void OnSelectionChanged(ChangeEvent<string> change)
        {
            if (_binding || ViewModel is null || _metric.index < 0 || _category.index < 0) return;

            ViewModel.SelectAsync((LeaderboardMetric)_metric.index,
                (LeaderboardCategory)_category.index).Forget();
        }

        private LeaderboardRow CreateRow(LeaderboardScoreEntry entry)
        {
            return new LeaderboardRow(entry, _flags, entry.PlayerId == ViewModel.LocalPlayerId,
                ViewModel.Text("menu.leaderboards.unnamed", "Player"),
                ViewModel.Text("menu.leaderboards.unranked", "Unranked"));
        }

        private void OnEntriesChanged(LeaderboardScoreEntry[] entries)
        {
            _list.Clear();
            foreach (LeaderboardScoreEntry entry in entries) _list.Add(CreateRow(entry));
            _list.scrollOffset = Vector2.zero;
        }

        private void OnOwnEntryChanged(LeaderboardScoreEntry entry)
        {
            _ownRow.Clear();
            if (entry != null) _ownRow.Add(CreateRow(entry));
        }

        private void OnStatusChanged(string value) => _status.text = value;
        private void OnPageChanged(string value) => _page.text = value;

        private void OnLoadingChanged(bool value)
        {
            _refresh.SetEnabled(value == false);
            _previous.SetEnabled(ViewModel.CanGoPrevious);
            _next.SetEnabled(ViewModel.CanGoNext);
        }
    }
}
