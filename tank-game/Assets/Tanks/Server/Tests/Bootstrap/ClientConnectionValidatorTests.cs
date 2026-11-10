using NUnit.Framework;
using System.Text;
using Tanks.Core.Players;
using Tanks.Shared.Teams;
using Newtonsoft.Json;
using Tanks.Shared.Bootstrap;
using Tanks.Core;
using Tanks.Server.Bootstrap;

namespace Tanks.Server.Tests.Bootstrap
{
    public class ClientConnectionValidatorTests
    {
        private IClientConnectionValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new ClientConnectionValidator();
        }

        [TestCase("test")]
        [TestCase("QWERTYUIOPASDFGHJKLZXCVBN")]
        public void Validate_ValidDisplayName_Approves(string name)
        {
            ResultType result = _validator.Validate(
                CreatePayload($"{{ \"DisplayName\": \"{name}\" }}"));

            Assert.True(result.IsSuccess);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("QWERTYUIOPASDFGHJKLZXCVBNM")]
        public void Validate_InvalidDisplayName_Rejects(string name)
        {
            ResultType result = _validator.Validate(
                CreatePayload($"{{ \"DisplayName\": \"{name}\" }}"));

            Assert.False(result.IsSuccess);
        }

        [TestCase("", "missing-payload")]
        [TestCase("   ", "missing-payload")]
        [TestCase("}", "invalid-payload")]
        [TestCase("{ \"otherValue\": { }", "invalid-payload")]
        public void Validate_InvalidJson_Rejects(string json, string reason)
        {
            ResultType result = _validator.Validate(CreatePayload(json));
            Assert.AreEqual(reason, result.ErrorCode, "Reason dismatch");
            Assert.False(result.IsSuccess);
        }

        [Test]
        public void Appearance_SurvivesConnectionAndTeamState()
        {
            var equipped = new EquippedCosmetics { Body = "v4-body", Turret = "kr1-black-turret" };
            string json = JsonConvert.SerializeObject(new ClientConnectionPayload { DisplayName = "Player", Equipped = equipped });
            var result = _validator.Validate(CreatePayload(json));
            Assert.True(result.IsSuccess);

            var team = new Team(new TeamId(1));
            var player = team.AddPlayer(1, result.Value.DisplayName, result.Value.Equipped);
            Assert.That(player.Equipped.Body, Is.EqualTo(equipped.Body));
            Assert.That(player.Equipped.Turret, Is.EqualTo(equipped.Turret));
        }

        [Test]
        public void Appearance_RejectsIdsTooLargeForReplication()
        {
            string json = JsonConvert.SerializeObject(new ClientConnectionPayload
            {
                DisplayName = "Player",
                Equipped = new EquippedCosmetics { Body = new string('я', 32) }
            });

            Assert.That(_validator.Validate(CreatePayload(json)).ErrorCode, Is.EqualTo("invalid-cosmetics"));
        }

        private byte[] CreatePayload(string json)
        {
            return Encoding.UTF8.GetBytes(json);
        }
    }
}
