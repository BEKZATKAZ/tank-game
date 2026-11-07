using NSubstitute;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Common;
using Tanks.Core.Leaderboards;
using Tanks.Infrastructure.Leaderboards;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tanks.Infrastructure.Tests.Leaderboards
{
    public sealed class UGSLeaderboardServiceTests
    {
        private ILeaderboardsService _sdk;
        private UGSLeaderboardService _service;
        private ICountryService _country;

        [SetUp]
        public void Setup()
        {
            var auth = Substitute.For<IAuthService>();
            var profile = new PlayerProfileState();
            profile.UserId.Value = "self";
            profile.DisplayName.Value = "Chosen#Name";
            auth.CurrentProfile.Returns(profile);

            _country = Substitute.For<ICountryService>();
            _country.GetCountryCodeAsync().Returns(UniTask.FromResult("KZ"));

            _sdk = Substitute.For<ILeaderboardsService>();
            _sdk.AddPlayerScoreAsync(Arg.Any<string>(), Arg.Any<double>(), Arg.Any<AddPlayerScoreOptions>())
                .Returns(Task.FromResult(new LeaderboardEntry("self", "Player", 0, 1)));
            _service = new UGSLeaderboardService(auth, _country, _sdk);
        }

        [Test]
        public async Task SubmitMatch_AddsDeltasToEveryPeriodIncludingZeroAndPenalty()
        {
            ResultType result = await _service.SubmitMatchAsync(-2, 0);

            Assert.IsTrue(result.IsSuccess);

            foreach (string period in new[] { "total", "weekly", "daily" })
            {
                await _sdk.Received(1).AddPlayerScoreAsync("eliminations-" + period, -2,
                    Arg.Is<AddPlayerScoreOptions>(x => JsonUtility.ToJson(x.Metadata).Contains("KZ") &&
                        JsonUtility.ToJson(x.Metadata).Contains("Chosen#Name")));
                await _sdk.Received(1).AddPlayerScoreAsync("picked-up-items-" + period, 0,
                    Arg.Any<AddPlayerScoreOptions>());
            }
        }

        [Test]
        public async Task SubmitMatch_FailureDoesNotRetryOrSkipOtherBoards()
        {
            _sdk.AddPlayerScoreAsync("eliminations-total", Arg.Any<double>(), Arg.Any<AddPlayerScoreOptions>())
                .Returns(Task.FromException<LeaderboardEntry>(new TimeoutException("Timed out")));
            LogAssert.Expect(LogType.Warning, new Regex("Could not submit score to 'eliminations-total'"));

            ResultType result = await _service.SubmitMatchAsync(3, 4);

            Assert.IsFalse(result.IsSuccess);
            await _sdk.Received(1).AddPlayerScoreAsync("eliminations-total", 3, Arg.Any<AddPlayerScoreOptions>());
            await _sdk.Received(1).AddPlayerScoreAsync("picked-up-items-daily", 4, Arg.Any<AddPlayerScoreOptions>());
        }

        [Test]
        public async Task GetPage_RequestsTwentyWithMetadataAndMapsDisplayData()
        {
            _sdk.GetScoresAsync("picked-up-items-weekly", Arg.Any<GetScoresOptions>())
                .Returns(Task.FromResult(new LeaderboardScoresPage(20, 20, 45, new List<LeaderboardEntry>
                {
                    new("other", "Tank#1234", 20, 123, metadata: "{\"countryCode\":\"fr\"}"),
                    new("legacy", null, 21, 10, metadata: "invalid json")
                })));
            _sdk.GetPlayerScoreAsync("picked-up-items-weekly", Arg.Any<GetPlayerScoreOptions>())
                .Returns(Task.FromResult(new LeaderboardEntry("self", "Me#4321", 40, 5)));

            ResultType<LeaderboardPage> result = await _service.GetPageAsync(
                LeaderboardMetric.PickedUpItems, LeaderboardCategory.Weekly, 1);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(45, result.Value.Total);
            Assert.AreEqual(21, result.Value.Entries[0].Rank);
            Assert.AreEqual("Tank", result.Value.Entries[0].PlayerName);
            Assert.AreEqual("FR", result.Value.Entries[0].CountryCode);
            Assert.AreEqual("", result.Value.Entries[1].CountryCode);
            Assert.AreEqual(41, result.Value.OwnEntry.Rank);

            await _sdk.Received(1).GetScoresAsync("picked-up-items-weekly",
                Arg.Is<GetScoresOptions>(x => x.Offset == 20 && x.Limit == 20 && x.IncludeMetadata == true));
        }

        [TestCase("kz", "KZ")]
        [TestCase(" US ", "US")]
        [TestCase(null, "")]
        [TestCase("USA", "")]
        [TestCase("12", "")]
        public void CountryCode_NormalizesOrUsesUnknown(string input, string expected)
        {
            Assert.AreEqual(expected, LeaderboardSettings.NormalizeCountryCode(input));
        }

        [Test]
        public async Task GetPage_UnrankedPlayerDoesNotFailTheTable()
        {
            var notFound = (LeaderboardsException)Activator.CreateInstance(typeof(LeaderboardsException),
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { LeaderboardsExceptionReason.EntryNotFound, 404, "No entry", null }, null);
            _sdk.GetScoresAsync("eliminations-total", Arg.Any<GetScoresOptions>())
                .Returns(Task.FromResult(new LeaderboardScoresPage(0, 20, 0, new List<LeaderboardEntry>())));
            _sdk.GetPlayerScoreAsync("eliminations-total", Arg.Any<GetPlayerScoreOptions>())
                .Returns(Task.FromException<LeaderboardEntry>(notFound));

            ResultType<LeaderboardPage> result = await _service.GetPageAsync(
                LeaderboardMetric.Eliminations, LeaderboardCategory.Total, 0);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(0, result.Value.OwnEntry.Rank);
            Assert.AreEqual("KZ", result.Value.OwnEntry.CountryCode);
            Assert.IsEmpty(result.Value.Entries);
        }

        [Test]
        public async Task SignedOut_DoesNotAccessTheSdk()
        {
            var auth = Substitute.For<IAuthService>();
            auth.CurrentProfile.Returns(new PlayerProfileState());
            var service = new UGSLeaderboardService(auth, _country, _sdk);

            Assert.IsFalse((await service.SubmitMatchAsync(1, 2)).IsSuccess);
            Assert.IsFalse((await service.GetPageAsync(
                LeaderboardMetric.Eliminations, LeaderboardCategory.Total, 0)).IsSuccess);
            Assert.IsEmpty(_sdk.ReceivedCalls());
        }

        [TestCase("KZ")]
        [TestCase("")]
        public async Task GetPage_OverridesStaleLocalFlagsWithoutUpdatingScores(string country)
        {
            _country.GetCountryCodeAsync().Returns(UniTask.FromResult(country));
            var self = new LeaderboardEntry("self", "Local", 0, 42, metadata: "{\"countryCode\":\"RU\"}");
            _sdk.GetScoresAsync("eliminations-total", Arg.Any<GetScoresOptions>())
                .Returns(Task.FromResult(new LeaderboardScoresPage(0, 20, 2, new List<LeaderboardEntry>
                {
                    self,
                    new("other", "Other", 1, 30, metadata: "{\"countryCode\":\"RU\"}")
                })));
            _sdk.GetPlayerScoreAsync("eliminations-total", Arg.Any<GetPlayerScoreOptions>())
                .Returns(Task.FromResult(self));

            ResultType<LeaderboardPage> result = await _service.GetPageAsync(
                LeaderboardMetric.Eliminations, LeaderboardCategory.Total, 0);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(country, result.Value.Entries[0].CountryCode);
            Assert.AreEqual(country, result.Value.OwnEntry.CountryCode);
            Assert.AreEqual("RU", result.Value.Entries[1].CountryCode);
            Assert.AreEqual(42, result.Value.OwnEntry.Score);
            await _sdk.DidNotReceiveWithAnyArgs().AddPlayerScoreAsync(default, default, default);
        }

        [Test]
        public async Task SubmitMatch_UnknownCountryStillSubmitsScores()
        {
            _country.GetCountryCodeAsync().Returns(UniTask.FromResult(""));

            ResultType result = await _service.SubmitMatchAsync(3, 4);

            Assert.IsTrue(result.IsSuccess);
            await _sdk.Received(1).AddPlayerScoreAsync("eliminations-total", 3,
                Arg.Is<AddPlayerScoreOptions>(x => JsonUtility.ToJson(x.Metadata).Contains("\"countryCode\":\"\"")));
        }

        [Test]
        public async Task GetPage_UsesChosenNamesInsteadOfGeneratedServiceNames()
        {
            var self = new LeaderboardEntry("self", "GeneratedLocal#1234", 0, 42,
                metadata: "{\"displayName\":\"Old name\"}");
            _sdk.GetScoresAsync("eliminations-total", Arg.Any<GetScoresOptions>())
                .Returns(Task.FromResult(new LeaderboardScoresPage(0, 20, 3, new List<LeaderboardEntry>
                {
                    self,
                    new("other", "GeneratedRemote#5678", 1, 30,
                        metadata: "{\"countryCode\":\"KZ\",\"displayName\":\"Chosen remote#name\"}"),
                    new("legacy", "Legacy#9999", 2, 10)
                })));
            _sdk.GetPlayerScoreAsync("eliminations-total", Arg.Any<GetPlayerScoreOptions>())
                .Returns(Task.FromResult(self));

            ResultType<LeaderboardPage> result = await _service.GetPageAsync(
                LeaderboardMetric.Eliminations, LeaderboardCategory.Total, 0);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual("Chosen#Name", result.Value.Entries[0].PlayerName);
            Assert.AreEqual("Chosen#Name", result.Value.OwnEntry.PlayerName);
            Assert.AreEqual("Chosen remote#name", result.Value.Entries[1].PlayerName);
            Assert.AreEqual("Legacy", result.Value.Entries[2].PlayerName);
            await _sdk.DidNotReceiveWithAnyArgs().AddPlayerScoreAsync(default, default, default);
        }
    }
}
