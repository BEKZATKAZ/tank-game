using Unity.Netcode;
using VContainer;

namespace Tanks.Shared.Entities
{
    public interface IEntityBehaviour : INetworkBehaviourExtended
    {
        EntityId Id { get; }
        string DisplayName { get; }
        bool IsPossessed { get; }
        bool IsValid { get; }

        void SetIdEveryoneRpc(EntityId value);
        void SetDisplayNameEveryoneRpc(string value);

        void MarkAsPossessed();
        void MarkAsUnpossessed();
    }

    public sealed class EntityBehaviour : NetworkBehaviourExtended, IEntityBehaviour
    {
        [Inject] private readonly IEntityService _service;

        public EntityId Id { get; private set; }
        public string DisplayName { get; private set; }
        public bool IsPossessed { get; private set; }
        public bool IsValid => this;

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        public void SetIdEveryoneRpc(EntityId value) => Id = value;

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        public void SetDisplayNameEveryoneRpc(string value) => DisplayName = value;

        public override void OnDestroy()
        {
            base.OnDestroy();
            _service?.RemoveEntity(this);
        }

        protected override void OnInitialize() => _service.AddEntity(this);
        public void MarkAsPossessed() => IsPossessed = true;
        public void MarkAsUnpossessed() => IsPossessed = false;
    }
}
