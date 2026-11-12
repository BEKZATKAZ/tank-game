using Backend.Models;
using Backend.Models.Debriefing;
using Backend.Modules;
using Backend.Services;
using Backend.Services.Adapters;
using NSubstitute;
using Unity.Services.CloudCode.Core;

namespace Backend.Tests.Modules.Debriefing;

public sealed class GetDebriefingValidationTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public async Task MissingMatchIdentity_RejectsRequest(string? matchId)
    {
        IExecutionContext context = Substitute.For<IExecutionContext>();
        ICloudSaveService save = Substitute.For<ICloudSaveService>();

        GetDebriefingModule module = new(new PlayerDebriefingService(
            save, new DebriefingCalculationService()));

        ApiResult<DebriefingResponse> result = await module.Handle(
            context, matchId!,
            [new("player", 0, 0, 0, 0, false)]);

        Assert.That(result.ErrorCode, Is.EqualTo("invalid-payload"));
        Assert.That(save.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public async Task EmptyMatch_RejectsRequest()
    {
        IExecutionContext context = Substitute.For<IExecutionContext>();
        ICloudSaveService save = Substitute.For<ICloudSaveService>();

        GetDebriefingModule module = new(new PlayerDebriefingService(
            save, new DebriefingCalculationService()));

        ApiResult<DebriefingResponse> result = await module.Handle(
            context, "match", []);

        Assert.That(result.ErrorCode, Is.EqualTo("invalid-payload"));
    }
}
