using System.Collections.Generic;
using Tanks.Core.Players;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Rewards
{
    public sealed class RewardsView : ViewBase<RewardsViewModel>
    {
        private VisualElement _root;
        private readonly List<Label> _titles = new();
        private readonly List<Label> _timers = new();
        private readonly List<Button> _claimButtons = new();

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("StoreRewards");

            for (int i = 0; i < 2; i++)
            {
                VisualElement reward = _root.Q(i == 0 ? "PlaytimeReward" : "DailyReward");
                _titles.Add(reward.Q<Label>("RewardTitle"));
                _timers.Add(reward.Q<Label>("RewardTimer"));

                Button claim = reward.Q<Button>("ClaimButton");
                TimedRewardType type = (TimedRewardType)i;
                claim.clicked += () => _ = ViewModel?.ClaimAsync(type);
                _claimButtons.Add(claim);
            }
        }

        protected override void Bind(RewardsViewModel viewModel)
        {
            viewModel.Rewards.Bind(OnRewardsChanged);
        }

        protected override void Unbind(RewardsViewModel viewModel)
        {
            viewModel.Rewards.Unbind(OnRewardsChanged);
        }

        private void LateUpdate()
        {
            ViewModel?.Tick();
        }

        private void OnRewardsChanged(RewardCardData[] rewards)
        {
            for (int i = 0; i < rewards.Length; i++)
            {
                _titles[i].text = rewards[i].Title;
                _timers[i].text = rewards[i].Timer;
                _timers[i].style.display = string.IsNullOrEmpty(rewards[i].Timer)
                    ? DisplayStyle.None : DisplayStyle.Flex;

                _claimButtons[i].SetEnabled(rewards[i].CanClaim);
            }
        }
    }
}
