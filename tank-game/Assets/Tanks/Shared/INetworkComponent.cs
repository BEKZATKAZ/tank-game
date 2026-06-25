using Unity.Netcode;

namespace Tanks.Shared
{
    public interface INetworkComponent
    {
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

        public virtual void OnTick(int tick, float tickDelta) { }

        protected virtual void Awake()
        {
            Behaviour = GetComponent<T>();
        }
    }
}