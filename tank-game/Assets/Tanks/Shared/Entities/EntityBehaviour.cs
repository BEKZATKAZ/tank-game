using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Tanks.Shared.Entities
{
    public interface IEntityBehaviour : INetworkBehaviourExtended
    {
        EntityId Id { get; }
        float2 Size { get; }
        bool IsPossessed { get; }
        bool IsValid { get; }

        void SetIdEveryoneRpc(EntityId value);
        void MarkAsPossessed();
        void MarkAsUnpossessed();
    }

    public sealed class EntityBehaviour : NetworkBehaviourExtended, IEntityBehaviour
    {
        [SerializeField] private Vector2 _size = new Vector2(1, 1);

        [Inject] private readonly IEntityService _service;

        public EntityId Id { get; private set; }
        public float2 Size => _size;

        public bool IsPossessed { get; private set; }
        public bool IsValid => this;

        private bool _isAddedToService;

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        public void SetIdEveryoneRpc(EntityId value)
        {
            if (Id != default) _service?.RemoveEntity(this);
            Id = value;

            if (value != default) _service?.AddEntity(this);
            _isAddedToService = _service != null;
        }

        protected override void OnInitialize()
        {
            if (_isAddedToService || Id == default) return;
            _service.AddEntity(this);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            _service?.RemoveEntity(this);
        }

        public void MarkAsPossessed() => IsPossessed = true;
        public void MarkAsUnpossessed() => IsPossessed = false;
    }
}
