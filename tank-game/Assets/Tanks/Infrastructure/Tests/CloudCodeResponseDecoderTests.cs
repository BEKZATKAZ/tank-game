using NUnit.Framework;
using Newtonsoft.Json;
using System;
using Tanks.Core;
using Tanks.Core.Players;

namespace Tanks.Infrastructure.Tests
{
    public sealed class CloudCodeResponseDecoderTests
    {
        [TestCase("unknown")]
        [TestCase("unauthorized")]
        [TestCase("conflict")]
        [TestCase("rate-limited")]
        [TestCase("")]
        public void Failure_DoesNotDeserializeNullSnapshotFields(string code)
        {
            string json = JsonConvert.SerializeObject(new
            {
                errorCode = code,
                value = new { balance = (object)null, level = (object)null }
            });

            ResultType<PlayerSnapshot> result =
                CloudCodeResponseDecoder.Decode<PlayerSnapshot>(json);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(code));
        }

        [Test]
        public void Success_DeserializesCompactClaimAndNullErrorMeansSuccess()
        {
            RewardClaimResult expected = new()
            {
                Balance = new PlayerBalance { SpareParts = 75, PridePoints = 12 },
                ServerUtc = DateTimeOffset.UtcNow,
                NextClaimAt = DateTimeOffset.UtcNow.AddMinutes(1)
            };

            string json = JsonConvert.SerializeObject(new { errorCode = (string)null,
                value = expected });

            ResultType<RewardClaimResult> result =
                CloudCodeResponseDecoder.Decode<RewardClaimResult>(json);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(expected));
            ResultType status = CloudCodeResponseDecoder.Decode("{\"errorCode\":null}");
            Assert.That(status.IsSuccess, Is.True);
        }

        [Test]
        public void Success_RequiresAValueForTypedResponses()
        {
            ResultType<RewardClaimResult> result = CloudCodeResponseDecoder
                .Decode<RewardClaimResult>("{\"errorCode\":null,\"value\":null}");

            Assert.That(result.IsSuccess, Is.False);
        }
    }
}
