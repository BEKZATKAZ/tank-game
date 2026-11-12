using Tanks.Core.Players;
using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Debriefing
{
    public sealed class DebriefingLevelBinding
    {
        private readonly IntVariable _level = new();
        private readonly IntVariable _nextLevel = new();
        private readonly IntVariable _currentXP = new();
        private readonly IntVariable _requiredXP = new();

        public void Bind(VisualElement root)
        {
            LocalizedString level = new("Menus", "debriefing.level")
            {
                { "value", _level }
            };

            LocalizedString next = new("Menus", "debriefing.level")
            {
                { "value", _nextLevel }
            };

            LocalizedString xp = new("Menus", "debriefing.progress")
            {
                { "current", _currentXP },
                { "required", _requiredXP }
            };

            root.Q<Label>("DebriefingCurrentLevel")
                .SetBinding(nameof(Label.text), level);

            root.Q<Label>("DebriefingNextLevel")
                .SetBinding(nameof(Label.text), next);

            root.Q<Label>("DebriefingXP").SetBinding(nameof(Label.text), xp);
        }

        public void SetValue(PlayerLevel level, int required)
        {
            _level.Value = level.Value;
            _nextLevel.Value = level.Value + 1;
            _currentXP.Value = level.XP;
            _requiredXP.Value = required;
        }
    }
}
