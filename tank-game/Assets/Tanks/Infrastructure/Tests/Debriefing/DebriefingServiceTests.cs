using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Debriefing;
using Tanks.Core.Store;
using Tanks.Infrastructure.Debriefing;

namespace Tanks.Infrastructure.Tests.Debriefing
{
    public sealed class DebriefingServiceTests
    {
        [Test]
        public async Task Submit_UsesWholeMatchContract_AndPreservesPerPlayerErrors()
        {
            ICloudCodeCaller caller = Substitute.For<ICloudCodeCaller>();

            PlayerDebriefingPayload[] players =
            {
                new PlayerDebriefingPayload { PlayerId = "one" },
                new PlayerDebriefingPayload { PlayerId = "two" }
            };

            DebriefingResponse response = new DebriefingResponse
            {
                Conclusions = new[]
                {
                    new DebriefingPlayerResult { PlayerId = "one",
                        ErrorCode = "conflict" },
                    new DebriefingPlayerResult { PlayerId = "two", Value = new() }
                }
            };

            caller.CallAsync<GetDebriefing, DebriefingResponse>(
                Arg.Any<GetDebriefing>())
                .Returns(Result.Success(response));

            UGSDebriefingService service = new(caller);

            ResultType<DebriefingResponse> result = await service.SubmitAsync(
                "match", players);

            Assert.That(result.Value, Is.SameAs(response));

            await caller.Received(1).CallAsync<GetDebriefing, DebriefingResponse>(
                Arg.Is<GetDebriefing>(request => request.MatchId == "match" &&
                    request.Players == players));

            JObject wire = JObject.Parse(
                JsonConvert.SerializeObject(new GetDebriefing
            {
                MatchId = "match", Players = players
            }));

            Assert.That(wire["matchId"].Value<string>(), Is.EqualTo("match"));
            Assert.That(((JArray)wire["players"]).Count, Is.EqualTo(players.Length));
        }
    }
}
