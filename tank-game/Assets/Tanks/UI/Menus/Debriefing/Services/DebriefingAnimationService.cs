using System;
using Tanks.Core.Debriefing;
using Tanks.Core.Players;
using Unity.Mathematics;

namespace Tanks.UI.Menus.Debriefing.Services
{
    public sealed class DebriefingAnimationService : IDebriefingAnimationService
    {
        public DebriefingAnimationService(IPlayerLevelService levels)
        {
            _levels = levels;
        }

        private readonly IPlayerLevelService _levels;

        public event Action<bool> CountingChanged;
        public event Action LevelUp;

        public StatDebriefing[] Rows { get; } = new StatDebriefing[5];
        public PlayerLevel Level { get; private set; }
        public int VisibleRows { get; private set; }
        public bool IsComplete { get; private set; }
        public bool IsCounting { get; private set; }
        public float Progress { get; private set; }

        private PlayerDebriefing _data;
        private int _row;
        private int _completedXP;
        private float _elapsed;

        public void Initialize(PlayerDebriefing data)
        {
            _data = data;
            SetCounting(false);
            _row = -1;
            _elapsed = 0;
            _completedXP = 0;

            Array.Clear(Rows, 0, Rows.Length);
            Level = data.BeforeLevel;
            Progress = (float)Level.XP / _levels.RequiredXP(Level.Value);
            VisibleRows = 0;
            IsComplete = false;
        }

        public void Tick(float delta)
        {
            if (_data == null || IsComplete || delta <= 0) return;

            while (delta > 0 && IsComplete == false)
            {
                float duration = _row < 0 || IsCounting ? 1 : 0.5f;

                float step = math.min(delta, duration - _elapsed);

                _elapsed += step;
                delta -= step;

                if (IsCounting) Present(math.min(1, _elapsed));
                if (_elapsed < duration) break;

                _elapsed = 0;

                if (IsCounting)
                {
                    _completedXP = checked(_completedXP + _data.Rows[_row].XP);
                    SetCounting(false);

                    if (_row == Rows.Length - 1) IsComplete = true;

                    continue;
                }

                _row++;
                VisibleRows = _row + 1;
                SetCounting(true);
            }
        }

        public void Skip()
        {
            if (_data == null || IsComplete) return;

            Array.Copy(_data.Rows, Rows, Rows.Length);
            Level = _data.AfterLevel;
            Progress = (float)Level.XP / _levels.RequiredXP(Level.Value);
            VisibleRows = Rows.Length;
            IsComplete = true;
            SetCounting(false);
        }

        public void Stop()
        {
            IsComplete = true;
            SetCounting(false);
        }

        private void Present(float progress)
        {
            StatDebriefing target = _data.Rows[_row];

            StatDebriefing displayed = new StatDebriefing
            {
                Value = Interpolate(target.Value, progress),
                XP = Interpolate(target.XP, progress),
                SpareParts = Interpolate(target.SpareParts, progress),
                PridePoints = Interpolate(target.PridePoints, progress)
            };

            Rows[_row] = displayed;

            PlayerLevel level = _levels.AddXP(
                _data.BeforeLevel, checked(_completedXP + displayed.XP));

            int previous = Level.Value;
            Level = level;
            float fractionalXP = target.XP * progress - displayed.XP;

            int requiredXP = _levels.RequiredXP(Level.Value);

            Progress = math.saturate((Level.XP + fractionalXP) / requiredXP);

            for (int i = previous; i < level.Value; i++) LevelUp?.Invoke();
        }

        private static int Interpolate(int value, float progress)
        {
            return progress >= 1 ? value : (int)(value * progress);
        }

        private void SetCounting(bool value)
        {
            if (IsCounting == value) return;
            IsCounting = value;

            bool audible = value && _data.Rows[_row].XP != 0;
            CountingChanged?.Invoke(audible);
        }
    }
}
