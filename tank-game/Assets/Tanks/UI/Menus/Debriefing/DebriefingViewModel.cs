using System;
using Tanks.Core.Debriefing;
using Tanks.Core.Players;
using Tanks.UI.Menus.Debriefing.Services;
using Tanks.UI.Menus.Sections;

namespace Tanks.UI.Menus.Debriefing
{
    public sealed class DebriefingViewModel : IViewModel, IDisposable
    {
        public DebriefingViewModel(
            IPendingDebriefingService pending,
            IMenuSectionService navigation,
            ILocalPlayerService player,
            IDebriefingAnimationService animation,
            IDebriefingAudioService audio)
        {
            _pending = pending;
            _navigation = navigation;
            _player = player;
            Animation = animation;
            _audio = audio;
        }

        private readonly IPendingDebriefingService _pending;
        private readonly IMenuSectionService _navigation;
        private readonly ILocalPlayerService _player;
        private readonly IDebriefingAudioService _audio;

        public IDebriefingAnimationService Animation { get; }
        public bool IsFailure => Data == null;

        public event Action Changed;

        public PlayerDebriefing Data { get; private set; }
        private bool _disposed;

        public void ConfigureAudio(string statPath, string levelPath, float maxPitch)
        {
            _audio.Configure(statPath, levelPath, maxPitch);
        }

        public void Initialize()
        {
            Animation.CountingChanged += OnCountingChanged;
            Animation.LevelUp += OnLevelUp;

            DebriefingPlayerResult result = _pending.Result;

            if (result?.ErrorCode == null && result?.Value?.Rows?.Length == 5)
            {
                Data = result.Value;
                Animation.Initialize(Data);
            }

            RefreshPlayerAsync();
            Changed?.Invoke();
        }

        public void Tick(float delta)
        {
            if (_disposed || IsFailure || Animation.IsComplete) return;

            Animation.Tick(delta);
            _audio.SetProgress(Animation.Progress);
            Changed?.Invoke();
        }

        public void Skip()
        {
            if (_disposed || IsFailure) return;

            Animation.Skip();
            Changed?.Invoke();
        }

        public void Close()
        {
            if (_disposed) return;

            Dispose();
            _pending.Consume();
            _navigation.OpenSection(MenuSectionType.Main);
        }

        private string RowKey(int index)
        {
            if (index == 4)
            {
                return Data.IsVictory ? "debriefing.victory" : "debriefing.defeat";
            }

            string key = index switch
            {
                0 => "eliminations",
                1 => "assists",
                2 => "deaths",
                _ => "score"
            };

            return "debriefing." + key;
        }

        public DebriefingRowPresentation GetRow(int index)
        {
            if (IsFailure || index < 0 || index >= Data.Rows.Length) return default;

            StatDebriefing target = Data.Rows[index];
            StatDebriefing value = Animation.Rows[index];

            return new DebriefingRowPresentation
            {
                IsVisible = index < Animation.VisibleRows,
                HasSpareParts = target.SpareParts != 0,
                HasPridePoints = target.PridePoints != 0,
                HasXP = target.XP != 0,
                LabelKey = RowKey(index),
                Value = value,
                NegativeSpareParts = target.SpareParts < 0,
                NegativePridePoints = target.PridePoints < 0
            };
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Animation.Stop();
            Animation.CountingChanged -= OnCountingChanged;
            Animation.LevelUp -= OnLevelUp;
            _audio.Stop();
        }

        private void OnCountingChanged(bool active)
        {
            _audio.SetProgress(Animation.Progress);
            _audio.SetCounting(active);
        }

        private void OnLevelUp()
        {
            _audio.SetProgress(0);
            _audio.LevelUp();
        }

        private async void RefreshPlayerAsync()
        {
            try
            {
                await _player.LoadDataAsync();
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
            }
        }
    }
}
