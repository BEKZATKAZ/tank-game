using Backend.Models;
using Backend.Models.Economy;
using Backend.Modules;
using Backend.Services;
using Backend.Services.Adapters;
using Backend.Tests.Helpers;
using NSubstitute;
using Newtonsoft.Json.Linq;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Model;

namespace Backend.Tests.Modules;

public abstract class ClaimRewardTestsBase
{
    protected IExecutionContext _context = null!;
    protected ICloudSaveService _cloudSave = null!;
    protected ClaimRewardModule _module = null!;
    protected List<Item> _items = null!;
    protected CloudItems _loaded = null!;

    [SetUp]
    public void SetUp()
    {
        _context = Substitute.For<IExecutionContext>();
        _cloudSave = Substitute.For<ICloudSaveService>();
        _module = new ClaimRewardModule(_cloudSave);
        _items = CreateItems(DateTimeOffset.MinValue);

        _cloudSave.LoadItemsAsync(_context, Arg.Any<List<string>>())
            .Returns(call =>
            {
                List<string> keys = call.Arg<List<string>>();
                _loaded = new CloudItems(_items.Where(item =>
                    keys.Contains(item.Key)).ToList());

                return ApiResult.Ok(_loaded);
            });

        _cloudSave.SaveItemsAsync(_context, Arg.Any<CloudItems>())
            .Returns(call =>
            {
                CloudItems player = call.Arg<CloudItems>();
                _items = player.GetSetters().Select(setter => CloudSaveItems.Create(
                    setter.Key, JToken.FromObject(setter.Value), setter.WriteLock ??
                        "saved-lock")).ToList();

                return ApiResult.Ok();
            });
    }

    protected Task<ApiResult<RewardClaimResult>> Claim(TimedRewardType type)
    {
        return type == TimedRewardType.Playtime
            ? _module.ClaimPlaytime(_context)
            : _module.ClaimDaily(_context);
    }

    protected static List<Item> CreateItems(DateTimeOffset previous)
    {
        return [
            CloudSaveItems.Create("balance", JObject.FromObject(new { PridePoints = 100,
                SpareParts = 100 }), "balance-lock"),
            CloudSaveItems.Create("lastClaimedTimes",
                JObject.FromObject(new { Playtime = previous, Daily = previous }),
                    "times-lock")
        ];
    }

    protected static DateTimeOffset GetTime(
        LastClaimedTimes times,
        TimedRewardType type)
    {
        return type == TimedRewardType.Playtime ? times.Playtime : times.Daily;
    }

    protected static TimedRewardType OtherType(TimedRewardType type)
    {
        return type == TimedRewardType.Playtime ? TimedRewardType.Daily :
            TimedRewardType.Playtime;
    }
    protected static (int Min, int Max) GetRange(TimedRewardType type, CurrencyType
        currency)
    {
        RewardClaim[] rewards = new RewardDefinitionService().Get(type);
        return (rewards.Where(reward => reward.CurrencyType == currency).Sum(reward =>
            reward.MinValue),
            rewards.Where(reward => reward.CurrencyType == currency).Sum(reward =>
                reward.MaxValue));
    }

    protected static (int Min, int Max) GetCombinedRange(CurrencyType currency)
    {
        (int firstMin, int firstMax) = GetRange(TimedRewardType.Playtime, currency);
        (int secondMin, int secondMax) = GetRange(TimedRewardType.Daily, currency);
        return (firstMin + secondMin, firstMax + secondMax);
    }
}
