using System.Collections.Generic;
using Tanks.Utils;

namespace Tanks.Core.Players
{
    public sealed class LocalPlayer
    {
        public BindableProperty<PlayerLevel> Level { get; } = new();
        public BindableProperty<PlayerBalance> Balance { get; } = new();
        public BindableProperty<List<string>> Cosmetics { get; } = new(new());
        public BindableProperty<EquippedCosmetics> Equipped { get; } =
            new(EquippedCosmetics.Default);

        public BindableProperty<RewardDeadlines> Rewards { get; } = new();
        public BindableProperty<int> Revision { get; } = new();

        public void Apply(PlayerSnapshot snapshot)
        {
            Level.Value = snapshot.Level;
            Balance.Value = snapshot.Balance;
            Cosmetics.Value = snapshot.Cosmetics ?? new();
            Equipped.Value = new EquippedCosmetics
            {
                Body = string.IsNullOrEmpty(snapshot.Equipped.Body) ? "v3-body" :
                    snapshot.Equipped.Body,
                Turret = string.IsNullOrEmpty(snapshot.Equipped.Turret) ? "v3-turret" :
                    snapshot.Equipped.Turret
            };

            Rewards.Value = snapshot.Rewards;
            Revision.Value++;
        }

        public void ApplyReward(TimedRewardType type, RewardClaimResult result)
        {
            RewardDeadlines deadlines = Rewards.Value;
            if (type == TimedRewardType.Playtime) deadlines.Playtime = result.NextClaimAt;
            if (type == TimedRewardType.Daily) deadlines.Daily = result.NextClaimAt;

            Balance.Value = result.Balance;
            Rewards.Value = deadlines;
            Revision.Value++;
        }

        public bool Owns(string identifier)
        {
            return identifier is "v3-body" or "v3-turret" ||
                Cosmetics.Value.Contains(identifier);
        }
    }
}
