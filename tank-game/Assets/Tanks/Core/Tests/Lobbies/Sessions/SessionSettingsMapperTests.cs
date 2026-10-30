using NSubstitute;
using NUnit.Framework;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Core.Lobbies.Sessions.DTOs;
using UnityEngine;

namespace Tanks.Core.Tests.Lobbies.Sessions
{
    public class SessionSettingsMapperTests
    {
        private ISessionDefaultSettingsConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = CreateConfig();
        }

        [Test]
        public void FromLobby_MapsValues()
        {
            var lobby = CreateLobby();
            var result = SessionSettingsMapper.FromLobby(lobby, _config);

            Assert.AreEqual(4, result.DesiredPlayerCount);
            Assert.AreEqual(45, result.MaxScore);
            Assert.AreEqual(200, result.SelfDamagePercent);
            Assert.AreEqual(20, result.Stats.ForwardMovementSpeed);
            Assert.AreEqual(15, result.Stats.BackwardsMovementSpeed);
            Assert.AreEqual(new Vector2Int(6, 4), result.MapGeneration.Resolution);
            Assert.AreEqual(7, result.MapGeneration.WallWidth);
            Assert.AreEqual(6, result.ItemSpawn.MaxCooldownSeconds);
            Assert.AreEqual(3, result.ItemSpawn.MinCooldownSeconds);
            Assert.AreEqual(2, result.ItemSpawn.MinDistanceToPlayers);
            Assert.AreEqual(30, result.DangerZone.StartDelaySeconds);
            Assert.AreEqual(50, result.DangerZone.DurationSeconds);
            Assert.AreEqual(20, result.DangerZone.DamagePerSecond);
        }

        [Test]
        public void FromLobby_InvalidValues_UsesDefaults()
        {
            var lobby = CreateLobby();

            lobby.PlayerCount = 999;
            lobby.MaxScore = 999;
            lobby.SelfDamagePercent = 999;
            lobby.Stats.ForwardMovementSpeed = 999;
            lobby.Stats.BackwardsMovementSpeed = 999;
            lobby.MapGeneration.Size = (MapSize)999;
            lobby.MapGeneration.Thickness = (MapThickness)999;
            lobby.ItemSpawn.Rate = (ItemSpawnRate)999;
            lobby.DangerZone.StartDelaySeconds = 999;
            lobby.DangerZone.DurationSeconds = 999;
            lobby.DangerZone.DamagePerSecond = 999;

            var result = SessionSettingsMapper.FromLobby(lobby, _config);

            Assert.AreEqual(1, result.DesiredPlayerCount);
            Assert.AreEqual(5, result.MaxScore);
            Assert.AreEqual(100, result.SelfDamagePercent);
            Assert.AreEqual(12, result.Stats.ForwardMovementSpeed);
            Assert.AreEqual(11, result.Stats.BackwardsMovementSpeed);
            Assert.AreEqual(new Vector2Int(3, 2), result.MapGeneration.Resolution);
            Assert.AreEqual(3, result.MapGeneration.WallWidth);
            Assert.AreEqual(30, result.ItemSpawn.MaxCooldownSeconds);
            Assert.AreEqual(15, result.ItemSpawn.MinCooldownSeconds);
            Assert.AreEqual(2, result.ItemSpawn.MinDistanceToPlayers);
            Assert.AreEqual(15, result.DangerZone.StartDelaySeconds);
            Assert.AreEqual(40, result.DangerZone.DurationSeconds);
            Assert.AreEqual(10, result.DangerZone.DamagePerSecond);
        }

        [Test]
        public void FromDefaultToLobby_MapsDefaults()
        {
            var result = SessionSettingsMapper.FromDefaultToLobby(_config);

            Assert.AreEqual(1, result.PlayerCount);
            Assert.AreEqual(5, result.MaxScore);
            Assert.AreEqual(100, result.SelfDamagePercent);
            Assert.AreEqual(12, result.Stats.ForwardMovementSpeed);
            Assert.AreEqual(11, result.Stats.BackwardsMovementSpeed);
            Assert.AreEqual(MapSize.Small, result.MapGeneration.Size);
            Assert.AreEqual(MapThickness.Thin, result.MapGeneration.Thickness);
            Assert.AreEqual(ItemSpawnRate.Slow, result.ItemSpawn.Rate);
            Assert.AreEqual(15, result.DangerZone.StartDelaySeconds);
            Assert.AreEqual(40, result.DangerZone.DurationSeconds);
            Assert.AreEqual(10, result.DangerZone.DamagePerSecond);
        }

        [Test]
        public void FromLobby_UsesConfiguredMinDistanceToPlayers()
        {
            var itemSpawn = new DefaultItemSpawn
            {
                CooldownSeconds = new PossibleIntegers(30, 12, 6, 2),
                MinDistanceToPlayers = 10
            };

            _config.ItemSpawn.Returns(itemSpawn);

            var result = SessionSettingsMapper.FromLobby(
                CreateLobby(),
                _config);

            Assert.AreEqual(
                10,
                result.ItemSpawn.MinDistanceToPlayers);
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(999)]
        public void FromLobby_InvalidSelfDamagePercent_UsesConfiguredDefault(int percent)
        {
            var settings = CreateLobby();
            settings.SelfDamagePercent = percent;

            var result = SessionSettingsMapper.FromLobby(settings, _config);

            Assert.That(result.SelfDamagePercent, Is.EqualTo(100));
        }

        [Test]
        public void FromDefaultToLobby_UsesConfiguredSelfDamageDefault()
        {
            _config.SelfDamagePercent.Returns(new PossibleIntegers(50, 100, 200) { Default = 200 });

            var result = SessionSettingsMapper.FromDefaultToLobby(_config);

            Assert.That(result.SelfDamagePercent, Is.EqualTo(200));
        }

        private static ISessionDefaultSettingsConfig CreateConfig()
        {
            var config = Substitute.For<ISessionDefaultSettingsConfig>();

            config.DesiredPlayerCount.Returns(
                new PossibleIntegers(1, 2, 3, 4, 5, 6, 7, 8));

            config.MaxScoreToReach.Returns(
                new PossibleIntegers(5, 15, 30, 45, 60, 75, 90));

            config.SelfDamagePercent.Returns(
                new PossibleIntegers(5, 25, 50, 100, 200, 500) { Default = 100 });

            config.Stats.Returns(new DefaultPlayerStats
            {
                ForwardMovementSpeed = new PossibleIntegers(8, 12, 20) { Default = 12 },
                BackwardsMovementSpeed = new PossibleIntegers(9, 11, 15) { Default = 11 }
            });

            config.MapGeneration.Returns(new DefaultMapGeneration
            {
                Resolution = new PossibleVectorIntegers(
                    new Vector2Int(3, 2),
                    new Vector2Int(6, 4),
                    new Vector2Int(10, 8)),

                WallWidth = new PossibleIntegers(3, 5, 7)
            });

            config.ItemSpawn.Returns(new DefaultItemSpawn
            {
                CooldownSeconds = new PossibleIntegers(30, 12, 6, 2),
                MinDistanceToPlayers = 2
            });

            config.DangerZone.Returns(new DefaultDangerZone
            {
                StartDelaySeconds = new PossibleIntegers(5, 15, 30, 45, 60)
                {
                    Default = 15
                },

                DurationSeconds = new PossibleIntegers(10, 20, 30, 40, 50, 60)
                {
                    Default = 40
                },

                DamagePerSecond = new PossibleIntegers(5, 10, 20, 50, 100)
                {
                    Default = 10
                }
            });

            return config;
        }

        private static SessionSettingsFromLobby CreateLobby()
        {
            return new SessionSettingsFromLobby
            {
                PlayerCount = 4,
                MaxScore = 45,
                SelfDamagePercent = 200,

                Stats = new PlayerStatSettingsFromLobby
                {
                    ForwardMovementSpeed = 20,
                    BackwardsMovementSpeed = 15
                },

                MapGeneration = new MapGenerationSettingsFromLobby
                {
                    Size = MapSize.Medium,
                    Thickness = MapThickness.Thick
                },

                ItemSpawn = new ItemSpawnSettingsFromLobby
                {
                    Rate = ItemSpawnRate.Fast
                },

                DangerZone = new DangerZoneSettingsFromLobby
                {
                    StartDelaySeconds = 30,
                    DurationSeconds = 50,
                    DamagePerSecond = 20
                }
            };
        }
    }
}
