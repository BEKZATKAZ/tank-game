using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Debriefing;
using Unity.Mathematics;

namespace Tanks.Server.Debriefing
{
    public sealed class MatchDebriefingSubmissionService
        : IMatchDebriefingSubmissionService
    {
        public MatchDebriefingSubmissionService(
            IDebriefingService backend,
            IDebriefingClockService clock)
        {
            _backend = backend;
            _clock = clock;
        }

        private readonly IDebriefingService _backend;
        private readonly IDebriefingClockService _clock;

        public async Task<DebriefingPlayerResult[]> SubmitAsync(
            string matchId,
            PlayerDebriefingPayload[] players,
            float timeout)
        {
            Dictionary<string, PlayerDebriefingPayload> pending = players
                .ToDictionary(player => player.PlayerId);

            Dictionary<string, DebriefingPlayerResult> results = new();

            float deadline = _clock.Seconds + timeout;
            int attempt = 0;

            while (pending.Count > 0 && _clock.Seconds < deadline)
            {
                Task<ResultType<DebriefingResponse>> request = RequestAsync(
                    matchId, pending.Values.ToArray());

                Task limit = _clock.DelayAsync(deadline - _clock.Seconds);

                if (await Task.WhenAny(request, limit) != request) break;

                ResultType<DebriefingResponse> response = await request;

                CollectResults(response, pending, results);

                if (pending.Count == 0) break;

                float delay = math.min(++attempt, 2);

                await _clock.DelayAsync(
                    math.min(delay, math.max(0, deadline - _clock.Seconds)));
            }

            foreach (string playerId in pending.Keys)
            {
                results[playerId] = new DebriefingPlayerResult
                {
                    PlayerId = playerId,
                    ErrorCode = "settlement-uncertain"
                };
            }

            return players.Select(player => results[player.PlayerId]).ToArray();
        }

        private async Task<ResultType<DebriefingResponse>> RequestAsync(
            string matchId,
            PlayerDebriefingPayload[] players)
        {
            try
            {
                return await _backend.SubmitAsync(matchId, players);
            }
            catch
            {
                return Result.Failure<DebriefingResponse>("unknown");
            }
        }

        private static void CollectResults(
            ResultType<DebriefingResponse> response,
            Dictionary<string, PlayerDebriefingPayload> pending,
            Dictionary<string, DebriefingPlayerResult> results)
        {
            foreach (PlayerDebriefingPayload player in pending.Values.ToArray())
            {
                DebriefingPlayerResult result = GetPlayerResult(response, player);

                if (Retryable(result.ErrorCode)) continue;

                results[player.PlayerId] = result;
                pending.Remove(player.PlayerId);
            }
        }

        private static DebriefingPlayerResult GetPlayerResult(
            ResultType<DebriefingResponse> response,
            PlayerDebriefingPayload player)
        {
            DebriefingPlayerResult[] conclusions = response.IsSuccess
                ? response.Value?.Conclusions : null;

            DebriefingPlayerResult result = conclusions?.FirstOrDefault(
                value => value.PlayerId == player.PlayerId);

            result ??= new DebriefingPlayerResult
            {
                PlayerId = player.PlayerId,
                ErrorCode = response.IsSuccess ? "unknown" : response.ErrorCode
            };

            if (result.ErrorCode == null && result.Value == null)
            {
                result.ErrorCode = "unknown";
            }

            return result;
        }

        private static bool Retryable(string code)
        {
            return code is "unknown" or "rate-limited" or "conflict";
        }
    }
}
