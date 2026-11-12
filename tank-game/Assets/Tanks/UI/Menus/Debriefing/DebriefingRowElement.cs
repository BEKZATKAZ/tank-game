using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Debriefing
{
    public sealed class DebriefingRowElement : VisualElement
    {
        public DebriefingRowElement()
        {
            AddToClassList("debriefing-row");
            _statistic = new Label();
            _statistic.AddToClassList("debriefing-statistic");
            Add(_statistic);

            VisualElement rewards = new VisualElement();

            rewards.AddToClassList("debriefing-rewards");
            Add(rewards);
            _parts = CreateReward(rewards, "currency-icon--parts", out _partsText);
            _pride = CreateReward(rewards, "currency-icon--pride", out _prideText);
            _xp = new Label();
            _xp.AddToClassList("debriefing-xp-reward");
            rewards.Add(_xp);
            _partsBinding.Bind(_partsText);
            _prideBinding.Bind(_prideText);
            _xpBinding.Bind(_xp, xp: true);
        }

        private readonly Label _statistic;
        private readonly VisualElement _parts;
        private readonly VisualElement _pride;
        private readonly Label _partsText;
        private readonly Label _prideText;
        private readonly Label _xp;
        private readonly IntVariable _value = new();
        private readonly DebriefingRewardBinding _partsBinding = new();
        private readonly DebriefingRewardBinding _prideBinding = new();
        private readonly DebriefingRewardBinding _xpBinding = new();

        private string _entry;

        public void Present(DebriefingViewModel model, int index)
        {
            DebriefingRowPresentation row = model.GetRow(index);

            style.visibility = row.IsVisible
                ? Visibility.Visible : Visibility.Hidden;

            if (row.IsVisible == false) return;
            if (_entry != row.LabelKey) BindStatistic(row.LabelKey, index == 4);

            _value.Value = row.Value.Value;

            _parts.style.display = row.HasSpareParts
                ? DisplayStyle.Flex : DisplayStyle.None;

            _pride.style.display = row.HasPridePoints
                ? DisplayStyle.Flex : DisplayStyle.None;

            _xp.style.display = row.HasXP ? DisplayStyle.Flex : DisplayStyle.None;
            _partsBinding.SetValue(row.Value.SpareParts, row.NegativeSpareParts);
            _prideBinding.SetValue(row.Value.PridePoints, row.NegativePridePoints);
            _xpBinding.SetValue(row.Value.XP);
        }

        private void BindStatistic(string entry, bool outcome)
        {
            _entry = entry;
            LocalizedString name = new("Menus", entry);

            if (outcome)
            {
                _statistic.SetBinding(nameof(Label.text), name);
                return;
            }

            LocalizedString statistic = new("Menus", "debriefing.statistic")
            {
                { "name", name },
                { "value", _value }
            };

            _statistic.SetBinding(nameof(Label.text), statistic);
        }

        private static VisualElement CreateReward(
            VisualElement parent,
            string iconClass,
            out Label counter)
        {
            VisualElement reward = new VisualElement();

            reward.AddToClassList("debriefing-reward");
            counter = new Label();
            reward.Add(counter);

            VisualElement icon = new VisualElement();

            icon.AddToClassList("currency-icon");
            icon.AddToClassList(iconClass);
            reward.Add(icon);
            parent.Add(reward);
            return reward;
        }
    }
}
