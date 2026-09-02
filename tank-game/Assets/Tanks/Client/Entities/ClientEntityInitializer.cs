using Tanks.Core.Helpers;
using Tanks.Shared;
using Tanks.Shared.Entities;

namespace Tanks.Client.Entities
{
    public sealed class ClientEntityInitializer : NetworkComponent<IEntityBehaviour>
    {
        public ClientEntityInitializer(IClientEntityService service)
        {
            _service = service;
        }

        private readonly IClientEntityService _service;

        protected override void OnInitialize()
        {
            if (Behaviour.IsLocalPlayer == false) return;
            _service.PossessEntity(Behaviour.AsRequired());
        }
    }
}
