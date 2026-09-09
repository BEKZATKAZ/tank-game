using Tanks.Shared;
using Tanks.Shared.Entities;

namespace Tanks.Server.Entities
{
    public sealed class EntityInitializer : NetworkComponent<IEntityBehaviour>
    {
        protected override void OnInitialize()
        {
            Behaviour.SetIdEveryoneRpc(EntityIdGenerator.GenerateNext());
        }
    }
}
