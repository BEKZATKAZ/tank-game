using Tanks.Shared;
using Tanks.Shared.Teams;

namespace Tanks.Client.Teams
{
    public sealed class LocalParticipantMarker : NetworkComponent<ITeamParticipantBehaviour>
    {
        protected override void OnUpdate(float deltaTime)
        {
            Behaviour.State?.SetLocalPlayer(Behaviour.IsLocalPlayer);
        }
    }
}
