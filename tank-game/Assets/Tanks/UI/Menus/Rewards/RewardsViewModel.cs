using System;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Players;
using Tanks.Localization;
using Tanks.UI.Menus.Dialogs;
using Tanks.Utils;

namespace Tanks.UI.Menus.Rewards
{
    public sealed class RewardsViewModel : IViewModel, IDisposable
    {
        public RewardsViewModel(
            ILocalPlayerService player,
            IServerClock clock,
            ILocalizationDatabase localization,
            IDialogScreenService dialog)
        {
            _player = player;
            _clock = clock;
            _localization = localization;
            _dialog = dialog;

            _player.LocalPlayer.Revision.Bind(OnPlayerChanged);
            _localization.LocaleChanged += OnLocaleChanged;
        }

        public BindableProperty<RewardCardData[]> Rewards { get; } =
            new(Array.Empty<RewardCardData>());

        public BindableProperty<bool> IsBusy { get; } = new();

        private readonly ILocalPlayerService _player;
        private readonly IServerClock _clock;
        private readonly ILocalizationDatabase _localization;
        private readonly IDialogScreenService _dialog;
        private long _lastSecond = -1;
        private bool _disposed;

        public void Tick()
        {
            if (_disposed) return;
            long second = _clock.UtcNow.UtcDateTime.Ticks / TimeSpan.TicksPerSecond;
            if (second == _lastSecond) return;

            _lastSecond = second;
            RefreshRewards();
        }

        public async Task ClaimAsync(TimedRewardType type)
        {
            if (_disposed || IsBusy.Value || _player.IsLoaded == false) return;
            if (type is not (TimedRewardType.Playtime or TimedRewardType.Daily)) return;

            RewardDeadlines rewards = _player.LocalPlayer.Rewards.Value;
            DateTimeOffset deadline = type == TimedRewardType.Playtime ?
                rewards.Playtime : rewards.Daily;

            if (deadline > _clock.UtcNow) return;

            IsBusy.Value = true;
            RefreshRewards();

            try
            {
                ResultType result = await _player.ClaimRewardAsync(type);
                if (_disposed || result.IsSuccess) return;

                if (result.ErrorCode is "already-claimed" or "conflict")
                {
                    await _player.LoadDataAsync();
                }

                if (_disposed) return;

                _dialog.ShowErrorFromCode("store", result.ErrorCode);
            }
            finally
            {
                IsBusy.Value = false;
                if (_disposed == false) RefreshRewards();
            }
        }

        private void RefreshRewards()
        {
            RewardDeadlines deadlines = _player.LocalPlayer.Rewards.Value;
            Rewards.Value = new[]
            {
                CreateReward(TimedRewardType.Playtime, deadlines.Playtime,
                    "menu.reward.playtime"),
                CreateReward(TimedRewardType.Daily, deadlines.Daily, "menu.reward.daily")
            };
        }

        private RewardCardData CreateReward(
            TimedRewardType type,
            DateTimeOffset deadline,
            string title)
        {
            double remaining = Math.Max(0, (deadline - _clock.UtcNow).TotalSeconds);
            TimeSpan time = TimeSpan.FromSeconds(Math.Ceiling(remaining));
            string timer = "";

            if (remaining > 0 && _localization.TryGet("Menus", "menu.reward.timer", out
                ILocalizedString text))
            {
                text.AddVariable("hour", (int)time.TotalHours);
                text.AddVariable("minutes", time.Minutes);
                text.AddVariable("seconds", time.Seconds);
                timer = text.Get();
            }

            _localization.TryGet("Menus", title, out ILocalizedString name);

            return new RewardCardData
            {
                Type = type,
                Title = name?.Get() ?? "",
                Timer = timer,
                CanClaim = _player.IsLoaded && remaining <= 0 && IsBusy.Value == false
            };
        }

        private void OnPlayerChanged(int revision) => RefreshRewards();
        private void OnLocaleChanged(string locale) => RefreshRewards();

        public void Dispose()
        {
            _disposed = true;
            _player.LocalPlayer.Revision.Unbind(OnPlayerChanged);
            _localization.LocaleChanged -= OnLocaleChanged;
        }
    }
}
