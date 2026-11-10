using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Players;
using Tanks.Core.Store;

namespace Tanks.Infrastructure
{
    public sealed class UGSPlayerService : ILocalPlayerService, IStoreBackendService
    {
        public UGSPlayerService(ICloudCodeCaller caller, IServerClock clock)
        {
            _caller = caller;
            _clock = clock;
        }

        public LocalPlayer LocalPlayer { get; } = new();

        private readonly ICloudCodeCaller _caller;
        private readonly IServerClock _clock;
        private readonly SemaphoreSlim _requests = new(1, 1);
        private readonly Dictionary<TimedRewardType, Task<ResultType>> _claims = new();

        public bool IsLoaded { get; private set; }

        public Task<ResultType> LoadDataAsync()
        {
            return RequestAsync<GetLocalPlayerData, PlayerSnapshot>(default, ApplySnapshot);
        }

        public Task<ResultType> ClaimRewardAsync(TimedRewardType type)
        {
            if (_claims.TryGetValue(type, out Task<ResultType> pending) &&
                pending.IsCompleted == false) return pending;

            Task<ResultType> claim = CreateClaimAsync(type);
            _claims[type] = claim;
            return claim;
        }

        private Task<ResultType> CreateClaimAsync(TimedRewardType type)
        {
            return type switch
            {
                TimedRewardType.Playtime => RequestAsync<ClaimPlaytimeReward,
                    RewardClaimResult>(
                    default, result => ApplyClaim(type, result)),
                TimedRewardType.Daily => RequestAsync<ClaimDailyReward, RewardClaimResult>(
                    default, result => ApplyClaim(type, result)),
                _ => Task.FromResult(Result.Failure("invalid-data"))
            };
        }

        public Task<ResultType> PurchaseAsync(
            string identifier,
            string currency,
            int expectedPrice)
        {
            PurchaseCosmetic request = new()
            {
                ItemId = identifier,
                Currency = currency,
                ExpectedPrice = expectedPrice
            };

            return RequestAsync<PurchaseCosmetic, PlayerSnapshot>(request, ApplySnapshot);
        }

        public Task<ResultType> EquipAsync(string identifier)
        {
            return RequestAsync<EquipCosmetic, PlayerSnapshot>(
                new EquipCosmetic { ItemId = identifier }, ApplySnapshot);
        }

        public Task<ResultType<CosmeticDefinitions>> GetDefinitionsAsync()
        {
            return _caller.CallAsync<GetCosmeticDefinitions, CosmeticDefinitions>(default);
        }

        private ResultType ApplySnapshot(PlayerSnapshot snapshot)
        {
            if (snapshot.ServerUtc == default) return Result.Failure("invalid-data");
            _clock.Synchronize(snapshot.ServerUtc);
            IsLoaded = true;
            LocalPlayer.Apply(snapshot);
            return Result.Success();
        }

        private ResultType ApplyClaim(TimedRewardType type, RewardClaimResult result)
        {
            if (result.ServerUtc == default || result.NextClaimAt < result.ServerUtc)
            {
                return Result.Failure("invalid-data");
            }

            _clock.Synchronize(result.ServerUtc);
            LocalPlayer.ApplyReward(type, result);
            return Result.Success();
        }

        private async Task<ResultType> RequestAsync<TRequest, TResponse>(
            TRequest request,
            Func<TResponse, ResultType> apply)
        {
            await _requests.WaitAsync();

            try
            {
                ResultType<TResponse> result = await _caller
                    .CallAsync<TRequest, TResponse>(request);

                return result.IsSuccess ? apply(result.Value) :
                    Result.Failure(result.ErrorCode);
            }
            finally
            {
                _requests.Release();
            }
        }
    }
}
