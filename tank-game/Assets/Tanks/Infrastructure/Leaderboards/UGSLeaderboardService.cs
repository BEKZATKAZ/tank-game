using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using Tanks.Core;
using Tanks.Core.Leaderboards;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
using VContainer;

namespace Tanks.Infrastructure.Leaderboards
{
    public sealed class UGSLeaderboardService : ILeaderboardService
    {
        [Inject]
        public UGSLeaderboardService(IAuthService auth, ICountryService country) : this(auth, country, null) { }

        public UGSLeaderboardService(IAuthService auth, ICountryService country, ILeaderboardsService service)
        {
            _auth = auth;
            _country = country;
            _service = service;
        }

        [Serializable]
        private sealed class ScoreMetadata
        {
            public string countryCode;
            public string displayName;
        }

        private readonly IAuthService _auth;
        private readonly ICountryService _country;
        private readonly ILeaderboardsService _service;

        private ILeaderboardsService Service => _service ?? LeaderboardsService.Instance;
        private bool IsSignedIn => string.IsNullOrEmpty(_auth.CurrentProfile.UserId.Value) == false;

        public async UniTask<ResultType<LeaderboardPage>> GetPageAsync(
            LeaderboardMetric metric, LeaderboardCategory category, int page)
        {
            if (IsSignedIn == false)
            {
                return Result.Failure<LeaderboardPage>("not-signed-in");
            }

            if (page < 0)
            {
                return Result.Failure<LeaderboardPage>("invalid-page");
            }

            try
            {
                UniTask<string> countryTask = _country.GetCountryCodeAsync();
                string id = LeaderboardSettings.GetId(metric, category);

                LeaderboardScoresPage scores = await Service.GetScoresAsync(id, new GetScoresOptions
                {
                    Offset = checked(page * LeaderboardSettings.PageSize),
                    Limit = LeaderboardSettings.PageSize,
                    IncludeMetadata = true
                });

                LeaderboardEntry ownScore = null;

                try
                {
                    ownScore = await Service.GetPlayerScoreAsync(id, new GetPlayerScoreOptions
                    {
                        IncludeMetadata = true
                    });
                }
                catch (LeaderboardsException ex)
                    when (ex.Reason == LeaderboardsExceptionReason.EntryNotFound)
                {
                    // A player has no entry until their first completed match in this period.
                }

                string countryCode = await countryTask;

                return new LeaderboardPage
                {
                    Entries = scores.Results.Select(x => Map(x, countryCode)).ToArray(),
                    OwnEntry = ownScore != null ? Map(ownScore, countryCode) : new LeaderboardScoreEntry
                    {
                        PlayerId = _auth.CurrentProfile.UserId.Value,
                        PlayerName = _auth.CurrentProfile.DisplayName.Value,
                        CountryCode = countryCode
                    },
                    Total = scores.Total
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Could not load leaderboard: {ex.Message}");
                return Result.Failure<LeaderboardPage>("unavailable");
            }
        }

        public async UniTask<ResultType> SubmitMatchAsync(int eliminations, int pickedUpItems)
        {
            if (IsSignedIn == false) return Result.Failure("not-signed-in");

            var metadata = new ScoreMetadata
            {
                countryCode = await _country.GetCountryCodeAsync(),
                displayName = _auth.CurrentProfile.DisplayName.Value
            };

            bool failed = false;

            foreach (LeaderboardMetric metric in Enum.GetValues(typeof(LeaderboardMetric)))
            {
                int score = metric == LeaderboardMetric.Eliminations ? eliminations : pickedUpItems;

                foreach (LeaderboardCategory category in Enum.GetValues(typeof(LeaderboardCategory)))
                {
                    string id = LeaderboardSettings.GetId(metric, category);

                    try
                    {
                        // Aggregate boards accept match deltas. Retrying an ambiguous failure could double them.
                        await Service.AddPlayerScoreAsync(id, score,
                            new AddPlayerScoreOptions { Metadata = metadata });
                    }
                    catch (Exception ex)
                    {
                        failed = true;
                        Debug.LogWarning($"Could not submit score to '{id}': {ex.Message}");
                    }
                }
            }

            return failed ? Result.Failure("submission-failed") : Result.Success();
        }

        private LeaderboardScoreEntry Map(LeaderboardEntry entry, string localCountryCode)
        {
            string countryCode = "";
            string displayName = entry.PlayerName?.Split('#')[0] ?? "";

            if (string.IsNullOrEmpty(entry.Metadata) == false)
            {
                try
                {
                    ScoreMetadata metadata = JsonUtility.FromJson<ScoreMetadata>(entry.Metadata);
                    countryCode = LeaderboardSettings.NormalizeCountryCode(metadata?.countryCode);

                    if (string.IsNullOrWhiteSpace(metadata?.displayName) == false)
                    {
                        displayName = metadata.displayName;
                    }
                }
                catch (ArgumentException) { }
            }

            bool isLocalPlayer = entry.PlayerId == _auth.CurrentProfile.UserId.Value;

            if (isLocalPlayer &&
                string.IsNullOrWhiteSpace(_auth.CurrentProfile.DisplayName.Value) == false)
            {
                displayName = _auth.CurrentProfile.DisplayName.Value;
            }

            return new LeaderboardScoreEntry
            {
                PlayerId = entry.PlayerId,
                PlayerName = displayName,
                CountryCode = isLocalPlayer ? localCountryCode : countryCode,
                Rank = entry.Rank + 1,
                Score = entry.Score
            };
        }
    }
}
