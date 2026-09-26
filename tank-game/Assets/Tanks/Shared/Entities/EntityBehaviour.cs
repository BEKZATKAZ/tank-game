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

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        public void SetIdEveryoneRpc(EntityId value) => Id = value;

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
