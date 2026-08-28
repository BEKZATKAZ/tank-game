using Unity.Collections;
using Unity.Netcode;
using VContainer;

namespace Tanks.Shared.Entities
{
    public interface IEntityBehaviour : INetworkBehaviourExtended, IEntity
    {
        void SetId(EntityId value);
        void SetDisplayName(string value);
    }

    public sealed class EntityBehaviour : NetworkBehaviourExtended, IEntityBehaviour
    {
        [Inject] private readonly IEntityService _service;

        public EntityId Id => _id.Value;
        public string DisplayName => _displayName.Value.ToString();

        private readonly NetworkVariable<EntityId> _id = new();
        private readonly NetworkVariable<FixedString32Bytes> _displayName = new();

        public void SetId(EntityId value) => _id.Value = value;
        public void SetDisplayName(string value) => _displayName.Value = value;

        public override void OnDestroy()
        {
            base.OnDestroy();
            _service?.RemoveEntity(this);
        }

        protected override void OnInitialize()
        {
            _service.AddEntity(this);
        }
    }
}
