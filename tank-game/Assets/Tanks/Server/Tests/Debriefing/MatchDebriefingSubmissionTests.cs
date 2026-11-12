using NSubstitute;
using NUnit.Framework;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Debriefing;
using Tanks.Server.Debriefing;
using Tanks.Server.Tests.Helpers;
using UnityEngine.TestTools;

namespace Tanks.Server.Tests.Debriefing
{
    public sealed class MatchDebriefingSubmissionTests
    {
        [UnityTest]
        public IEnumerator PartialSuccess_RetriesOnlyUnresolvedPlayersWithSameMatch()
        {
            IDebriefingService backend = Substitute.For<IDebriefingService>();

            DebriefingTestClockService clock = new();

            backend.SubmitAsync("match",
                Arg.Any<PlayerDebriefingPayload[]>()).Returns(
                Result.Success(new DebriefingResponse
                {
                    Conclusions = new[]
                    {
                        new DebriefingPlayerResult
                        {
                            PlayerId = "first", Value = new PlayerDebriefing()
                        },
                        new DebriefingPlayerResult
                        {
                            PlayerId = "second", ErrorCode = "conflict"
                        }
                    }
                }),
                Result.Success(new DebriefingResponse
                {
                    Conclusions = new[]
                    {
                        new DebriefingPlayerResult
                        {
                            PlayerId = "second", Value = new PlayerDebriefing()
                        }
                    }
                }));

            MatchDebriefingSubmissionService service = new(backend, clock);

            Task<DebriefingPlayerResult[]> pending = service.SubmitAsync("match",
                new[] { Player("first"), Player("second") }, 30);

            clock.Advance(1);
            yield return DebriefingTestTasks.Complete(pending);
            DebriefingPlayerResult[] results = pending.Result;

            Assert.That(results.All(result => result.ErrorCode == null), Is.True);

            _ = backend.Received(1).SubmitAsync("match",
                Arg.Is<PlayerDebriefingPayload[]>(players => players.Length == 1 &&
                    players[0].PlayerId == "second"));
        }

        [UnityTest]
        public IEnumerator Deadline_ReleasesHungRequestAndReportsUncertainCommit()
        {
            IDebriefingService backend = Substitute.For<IDebriefingService>();

            TaskCompletionSource<ResultType<DebriefingResponse>> request = new();

            backend.SubmitAsync("match", Arg.Any<PlayerDebriefingPayload[]>())
                .Returns(request.Task);

            DebriefingTestClockService clock = new();
            MatchDebriefingSubmissionService service = new(backend, clock);

            Task<DebriefingPlayerResult[]> pending = service.SubmitAsync("match",
                new[] { Player("player") }, 7);

            clock.Advance(7);
            yield return DebriefingTestTasks.Complete(pending);
            DebriefingPlayerResult[] results = pending.Result;

            Assert.That(results.Single().ErrorCode,
                Is.EqualTo("settlement-uncertain"));

            request.SetResult(Result.Failure<DebriefingResponse>("unknown"));
        }

        [Test]
        public async Task PermanentFailure_DoesNotRetry()
        {
            IDebriefingService backend = Substitute.For<IDebriefingService>();

            backend.SubmitAsync("match", Arg.Any<PlayerDebriefingPayload[]>())
                .Returns(Result.Failure<DebriefingResponse>("unauthorized"));

            DebriefingTestClockService clock = new();
            MatchDebriefingSubmissionService service = new(backend, clock);

            DebriefingPlayerResult[] results = await service.SubmitAsync("match",
                new[] { Player("player") }, 30);

            Assert.That(results.Single().ErrorCode, Is.EqualTo("unauthorized"));

            await backend.Received(1).SubmitAsync("match",
                Arg.Any<PlayerDebriefingPayload[]>());
        }

        private static PlayerDebriefingPayload Player(string id)
        {
            return new PlayerDebriefingPayload { PlayerId = id };
        }
    }
}
