using Cysharp.Threading.Tasks;
using Tanks.Core.Leaderboards;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;

namespace Tanks.Client.Bootstrap
{
    public sealed class MatchLeaderboardSubmitter :
        NetworkComponent<IGameStartBehaviour>, INetworkMessageHandler<LeaderboardMatchResult>
    {
        public MatchLeaderboardSubmitter(ILeaderboardService leaderboards)
        {
            _leaderboards = leaderboards;
        }

        private readonly ILeaderboardService _leaderboards;
        private bool _submitted;

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }

        public void Handle(LeaderboardMatchResult message)
        {
            if (_submitted) return;
            _submitted = true;

            _leaderboards.SubmitMatchAsync(message.Eliminations, message.PickedUpItems).Forget();
        }
    }
}
