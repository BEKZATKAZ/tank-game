using System.Globalization;
using Tanks.Core.Leaderboards;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Leaderboards
{
    public sealed class LeaderboardRow : VisualElement
    {
        public LeaderboardRow(
            LeaderboardScoreEntry entry, FlagDatabase flags, bool isLocalPlayer,
            string unnamed, string unranked)
        {
            AddToClassList("leaderboards__row");
            EnableInClassList("leaderboards__row--local", isLocalPlayer);

            Add(new Label(entry.Rank > 0 ? entry.Rank.ToString() : "—")
            {
                name = "Rank",
                tooltip = entry.Rank > 0 ? "" : unranked
            });

            var flag = new Image { name = "Flag", sprite = flags ? flags.GetFlag(entry.CountryCode) : null };
            flag.AddToClassList("leaderboards__flag");
            flag.EnableInClassList("leaderboards__flag--unknown", flag.sprite == null);
            flag.tooltip = entry.CountryCode;
            Add(flag);

            Add(new Label(string.IsNullOrWhiteSpace(entry.PlayerName) ? unnamed : entry.PlayerName)
            {
                name = "PlayerName",
                enableRichText = false,
                tooltip = entry.PlayerName
            });

            Add(new Label(entry.Rank > 0 ? entry.Score.ToString("N0", CultureInfo.CurrentCulture) : unranked)
            {
                name = "Score"
            });
        }
    }
}
