using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Bootstrap;
using Tanks.Core;
using Tanks.Core.Leaderboards;
using Tanks.Shared.Bootstrap;

namespace Tanks.Client.Tests.Bootstrap
{
    public sealed class MatchLeaderboardSubmitterTests
    {
        [Test]
        public void DuplicateResult_SubmitsOnlyOnceEvenWhilePending()
        {
            var service = Substitute.For<ILeaderboardService>();
            var pending = new UniTaskCompletionSource<ResultType>();
            service.SubmitMatchAsync(Arg.Any<int>(), Arg.Any<int>()).Returns(pending.Task);
            var component = new MatchLeaderboardSubmitter(service);
            var behaviour = Substitute.For<IGameStartBehaviour>();
            component.Initialize(behaviour);

            component.Handle(new LeaderboardMatchResult { Eliminations = 3, PickedUpItems = 5 });
            component.Handle(new LeaderboardMatchResult { Eliminations = 3, PickedUpItems = 5 });
            pending.TrySetResult(Result.Failure("timeout"));
            component.Handle(new LeaderboardMatchResult { Eliminations = 3, PickedUpItems = 5 });

            service.Received(1).SubmitMatchAsync(3, 5);
            behaviour.Received(1).RegisterMessageHandler<LeaderboardMatchResult>(component);
            component.Destroy();
        }
    }
}
