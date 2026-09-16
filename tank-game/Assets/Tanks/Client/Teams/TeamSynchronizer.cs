using Tanks.Shared;
using Tanks.Shared.Teams;

namespace Tanks.Client.Teams
{
    public sealed class TeamSynchronizer :
        NetworkComponent<ITeamBehaviourService>,
        INetworkMessageHandler<AddPlayerPayload>,
        INetworkMessageHandler<UpdatePlayerPointsPayload>
    {
        public void Handle(AddPlayerPayload message)
        {
            string displayName = message.DisplayName.ToString();
            Team team = Behaviour.GetTeam(message.BelongsTo);
            team.AddPlayer(message.OwnerPlayerId, displayName);
        }

        public void Handle(UpdatePlayerPointsPayload message)
        {
            Team team = Behaviour.GetTeamByPlayerId(message.TargetPlayer);
            PlayerState state = team.GetPlayer(message.TargetPlayer);
            state.OverridePoints(message.Eliminations, message.Assistance, message.Deaths);
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler<AddPlayerPayload>(this);
            Behaviour.RegisterMessageHandler<UpdatePlayerPointsPayload>(this);
        }
    }
}
