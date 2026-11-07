using Cysharp.Threading.Tasks;
using System;
using Tanks.Core;
using Tanks.Core.Leaderboards;
using Tanks.Localization;
using Tanks.UI.Menus.Sections;
using Tanks.Utils;

namespace Tanks.UI.Menus.Leaderboards
{
    public sealed class LeaderboardsViewModel : IViewModel, IDisposable
    {
        public LeaderboardsViewModel(
            ILeaderboardService service,
            IAuthService auth,
            IMenuSectionService navigation,
            ILocalizationDatabase localization)
        {
            _service = service;
            _auth = auth;
            _navigation = navigation;
            _localization = localization;
        }

        public BindableProperty<LeaderboardScoreEntry[]> Entries { get; } = new(Array.Empty<LeaderboardScoreEntry>());
        public BindableProperty<LeaderboardScoreEntry> OwnEntry { get; } = new();
        public BindableProperty<bool> IsLoading { get; } = new();
        public BindableProperty<string> Status { get; } = new("");
        public BindableProperty<string> PageLabel { get; } = new("");

        public LeaderboardMetric Metric { get; private set; }
        public LeaderboardCategory Category { get; private set; }
        public int Page { get; private set; }
        public int Total { get; private set; }
        public string LocalPlayerId => _auth.CurrentProfile.UserId.Value;
        public bool CanGoPrevious => IsLoading.Value == false && Page > 0;
        public bool CanGoNext => IsLoading.Value == false && (Page + 1) * LeaderboardSettings.PageSize < Total;

        private readonly ILeaderboardService _service;
        private readonly IAuthService _auth;
        private readonly IMenuSectionService _navigation;
        private readonly ILocalizationDatabase _localization;

        private int _request;
        private bool _disposed;

        public string Text(string key, string fallback)
        {
            return _localization.TryGet("Menus", key, out ILocalizedString text) ? text.Get() : fallback;
        }

        public void Back() => _navigation.OpenSection(MenuSectionType.Extras);

        public UniTask SelectAsync(LeaderboardMetric metric, LeaderboardCategory category)
        {
            Metric = metric;
            Category = category;
            Page = 0;
            return RefreshAsync();
        }

        public UniTask ChangePageAsync(int direction)
        {
            if (direction == -1 && CanGoPrevious) Page--;
            else if (direction == 1 && CanGoNext) Page++;
            else return UniTask.CompletedTask;

            return RefreshAsync();
        }

        public async UniTask RefreshAsync()
        {
            if (_disposed) return;
            int request = ++_request;

            IsLoading.Value = true;
            Entries.Value = Array.Empty<LeaderboardScoreEntry>();
            OwnEntry.Value = null;
            Status.Value = Text("menu.leaderboards.loading", "Loading...");
            PageLabel.Value = "";

            ResultType<LeaderboardPage> result;

            try { result = await _service.GetPageAsync(Metric, Category, Page); }
            catch (Exception) { result = Result.Failure<LeaderboardPage>("unavailable"); }

            if (_disposed || request != _request) return;

            if (result.IsSuccess)
            {
                if (Page > 0 && Page * LeaderboardSettings.PageSize >= result.Value.Total)
                {
                    Page = 0;
                    await RefreshAsync();
                    return;
                }

                Total = result.Value.Total;
                Entries.Value = result.Value.Entries;
                OwnEntry.Value = result.Value.OwnEntry ?? new LeaderboardScoreEntry
                {
                    PlayerId = LocalPlayerId,
                    PlayerName = _auth.CurrentProfile.DisplayName.Value,
                    Rank = 0
                };

                Status.Value = Total == 0 ? Text("menu.leaderboards.empty", "No scores yet.") : "";
                int pages = Math.Max(1, (Total + LeaderboardSettings.PageSize - 1) / LeaderboardSettings.PageSize);
                PageLabel.Value = $"{Page + 1} / {pages}";
            }
            else
            {
                Total = 0;
                Status.Value = Text("menu.leaderboards.error", "Could not load scores. Try refreshing.");
            }

            IsLoading.Value = false;
        }

        public void Dispose()
        {
            _disposed = true;
            _request++;
        }
    }
}
