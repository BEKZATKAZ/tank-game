using System.Collections.Generic;
using System.Linq;
using Tanks.Shared.Stats;
using UnityEngine.UIElements;

namespace Tanks.UI.Gameplay.PlayerStats
{
    public sealed class PlayerStatsView : ViewBase<PlayerStatsViewModel>
    {
        private VisualElement _root;
        private Dictionary<StatType, PlayerStatElement> _stats;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("PlayerStats");

            _stats = _root
                .Query<PlayerStatElement>(className: "stat")
                .Build()
                .ToDictionary(k => k.Type);
        }

        protected override void Bind(PlayerStatsViewModel viewModel)
        {
            viewModel.Stats.Bind(UpdateStats);
        }

        private void UpdateStats(IDictionary<StatType, StatChanges> stats)
        {
            foreach ((StatType type, StatChanges newState) in stats)
            {
                PlayerStatElement element = _stats[type];
                element.Value = newState.CurrentValue;
                element.Change = newState.Change;
                element.UpdateTimers(newState.Timers);
            }
        }

        private void LateUpdate()
        {
            ViewModel?.Update();
        }
    }
}
