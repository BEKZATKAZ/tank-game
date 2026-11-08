using Cysharp.Threading.Tasks;
using System;

namespace Tanks.Core.Leaderboards
{
    public enum LeaderboardMetric
    {
        Eliminations,
        PickedUpItems
    }

    public enum LeaderboardCategory
    {
        Total,
        Weekly,
        Daily
    }

    public sealed class LeaderboardScoreEntry
    {
        public string PlayerId;
        public string PlayerName;
        public string CountryCode;
        public int Rank;
        public double Score;
    }

    public sealed class LeaderboardPage
    {
        public LeaderboardScoreEntry[] Entries = Array.Empty<LeaderboardScoreEntry>();
        public LeaderboardScoreEntry OwnEntry;
        public int Total;
    }

    public interface ILeaderboardService
    {
        UniTask<ResultType<LeaderboardPage>> GetPageAsync(
            LeaderboardMetric metric, LeaderboardCategory category, int page);

        UniTask<ResultType> SubmitMatchAsync(int eliminations, int pickedUpItems);
    }

    public static class LeaderboardSettings
    {
        public const int PageSize = 10;

        public static string GetId(LeaderboardMetric metric, LeaderboardCategory category)
        {
            string prefix = metric switch
            {
                LeaderboardMetric.Eliminations => "eliminations",
                LeaderboardMetric.PickedUpItems => "picked-up-items",
                _ => throw new ArgumentOutOfRangeException(nameof(metric))
            };

            string suffix = category switch
            {
                LeaderboardCategory.Total => "total",
                LeaderboardCategory.Weekly => "weekly",
                LeaderboardCategory.Daily => "daily",
                _ => throw new ArgumentOutOfRangeException(nameof(category))
            };

            return $"{prefix}-{suffix}";
        }

        public static string NormalizeCountryCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            string code = value.Trim().ToUpperInvariant();

            return code.Length == 2 && code[0] is >= 'A' and <= 'Z' &&
                code[1] is >= 'A' and <= 'Z' ? code : "";
        }
    }
}
