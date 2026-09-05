using NUnit.Framework;
using System.Text;
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

        private byte[] CreatePayload(string json)
        {
            return Encoding.UTF8.GetBytes(json);
        }
    }
}
