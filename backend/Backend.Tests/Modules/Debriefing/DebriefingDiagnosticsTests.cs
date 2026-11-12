using Backend.Models;
using Backend.Models.Debriefing;
using Backend.Modules;
using Backend.Services;
using Backend.Services.Adapters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Unity.Services.CloudCode.Core;

namespace Backend.Tests.Modules.Debriefing;

public sealed class DebriefingDiagnosticsTests
{
    [TestCase("missing-match-id")]
    [TestCase("missing-players")]
    [TestCase("empty-players")]
    [TestCase("missing-player-id")]
    [TestCase("duplicate-player-id")]
    public async Task RejectedRequest_LogsReasonWithoutProcessing(string reason)
    {
        var players = Substitute.For<IPlayerDebriefingService>();
        var logger = Substitute.For<ILogger<GetDebriefingModule>>();
        IExecutionContext context = Substitute.For<IExecutionContext>();

        PlayerDebriefingPayload player = new("player", 0, 0, 0, 0, false);
        PlayerDebriefingPayload[] payload = [player];
        string matchId = "match";

        switch (reason)
        {
            case "missing-match-id": matchId = ""; break;
            case "missing-players": payload = null!; break;
            case "empty-players": payload = []; break;
            case "missing-player-id": payload = [player with { PlayerId = "" }]; break;
            case "duplicate-player-id": payload = [player, player]; break;
        }

        GetDebriefingModule module = new(players, logger);

        ApiResult<DebriefingResponse> result = await module.Handle(
            context, matchId, payload);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCode, Is.EqualTo("invalid-payload"));
            Assert.That(players.ReceivedCalls(), Is.Empty);

            Assert.That(logger.ReceivedCalls().Single().GetArguments()[2]!
                .ToString(), Does.Contain(reason));
        });
    }

    [Test]
    public async Task Overflow_LogsExceptionAndDoesNotSave()
    {
        ICloudSaveService save = Substitute.For<ICloudSaveService>();
        var calculation = Substitute.For<IDebriefingCalculationService>();
        var logger = Substitute.For<ILogger<PlayerDebriefingService>>();
        IExecutionContext context = Substitute.For<IExecutionContext>();

        OverflowException overflow = new("test overflow");
        PlayerDebriefingPayload payload = new("player", 1, 0, 0, 0, false);

        save.LoadItemsAsync(context, "player", Arg.Any<List<string>>())
            .Returns(ApiResult.Ok(new CloudItems([])));

        calculation.Calculate("match", payload,
            Arg.Any<Backend.Models.Leveling.Level>(),
            Arg.Any<Backend.Models.Economy.PlayerBalance>())
            .Returns(_ => throw overflow);

        PlayerDebriefingService service = new(save, calculation, logger);

        ApiResult<PlayerDebriefing> result = await service.CalculateAsync(
            context, "match", payload);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCode, Is.EqualTo("invalid-payload"));

            Assert.That(logger.ReceivedCalls().Single().GetArguments()[3],
                Is.SameAs(overflow));
        });

        await save.DidNotReceive().SaveItemsAsync(context, "player",
            Arg.Any<CloudItems>());
    }

    [Test]
    public void ModuleSetup_RegistersDebriefingInterfaces()
    {
        ICloudCodeConfig config = Substitute.For<ICloudCodeConfig>();

        ServiceCollection services = new();

        config.Dependencies.Returns(services);

        new ModuleSetup().Setup(config);

        Assert.Multiple(() =>
        {
            Assert.That(services.Single(value =>
                value.ServiceType == typeof(IPlayerDebriefingService))
                .ImplementationType, Is.EqualTo(typeof(PlayerDebriefingService)));

            Assert.That(services.Single(value =>
                value.ServiceType == typeof(IDebriefingCalculationService))
                .ImplementationType,
                    Is.EqualTo(typeof(DebriefingCalculationService)));
        });
    }
}
