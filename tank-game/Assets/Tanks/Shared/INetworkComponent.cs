using Unity.Netcode;

namespace Tanks.Shared
{
    public interface INetworkComponent
    {
        bool IsEnabled { get; }
        void OnTick(int tick, float tickDelta);
    }

    public interface INetworkComponent<T> : INetworkComponent where T : INetworkBehaviourExtended
    {
        T Behaviour { get; }
    }

    public abstract class NetworkComponent<T> : NetworkBehaviour, INetworkComponent<T>
        where T : NetworkBehaviourExtended
    {
        public T Behaviour { get; private set; }
        public bool IsEnabled => enabled;

        public virtual void OnTick(int tick, float tickDelta) { }

        protected virtual void Awake()
        {
            Behaviour = GetComponent<T>();
        }
    }
}
