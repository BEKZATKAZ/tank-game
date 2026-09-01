using Unity.Netcode;

namespace Tanks.Shared.Stats
{
    public struct StatAddedFromServer : INetworkSerializeByMemcpy
    {
        public StatType Type;
        public StatValueType ValueType;
        public float Value;
        public float Duration;
    }

    public interface IStatOwnerBehaviour : INetworkBehaviourExtended
    {
        float GetStat(StatType type, float baseValue);

        void AddStat(
            StatType type, StatValueType valueType, float multiplier, float duration = -1);
    }

    public sealed class StatOwnerBehaviour : NetworkBehaviourExtended, IStatOwnerBehaviour
    {
        private readonly StatLogic _logic = new StatLogic();

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        private void AddStatEveryoneRpc(StatAddedFromServer message)
        {
            _logic.AddStat(message.Type, message.ValueType, message.Value, message.Duration);
        }

        public void AddStat(
            StatType type, StatValueType valueType, float multiplier, float duration = -1)
        {
            AddStatEveryoneRpc(new StatAddedFromServer
            {
                Type = type,
                ValueType = valueType,
                Value = multiplier,
                Duration = duration
            });
        }

        public float GetStat(StatType type, float baseValue) => _logic.GetStat(type, baseValue);

        protected override void OnTick(int tick, float tickDelta)
        {
            _logic.Tick(tick, tickDelta);
        }
    }
}