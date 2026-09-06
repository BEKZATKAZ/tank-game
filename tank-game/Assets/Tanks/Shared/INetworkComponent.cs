using Tanks.Shared.Adapters;

namespace Tanks.Shared
{
    public interface INetworkComponent
    {
        bool IsEnabled { get; set; }

        void Initialize(INetworkBehaviourExtended behaviour);
        void Destroy();

        void EarlyUpdate(float deltaTime);
        void Update(float deltaTime);
        void LateUpdate(float deltaTime);

        void Tick(int tick, float tickDelta);
        void LateTick(int tick, float tickDelta);
    }

    public interface INetworkComponent<T> : INetworkComponent where T : INetworkBehaviourExtended
    {
        T Behaviour { get; }
    }

    public class NetworkComponent<T> : INetworkComponent<T> where T : INetworkBehaviourExtended
    {
        public T Behaviour { get; private set; }
        public ITransform Transform => Behaviour.Transform;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value) return;
                _isEnabled = value;

                if (value) OnEnable();
                else OnDisable();
            }
        }

        private bool _isEnabled;

        public void Initialize(INetworkBehaviourExtended behaviour)
        {
            Behaviour = (T)behaviour;
            _isEnabled = true;

            OnInitialize();
            if (_isEnabled) OnEnable();
        }

        public void Destroy()
        {
            IsEnabled = false;
            OnDestroy();
        }

        public void EarlyUpdate(float deltaTime) => OnEarlyUpdate(deltaTime);
        public void Update(float deltaTime) => OnUpdate(deltaTime);
        public void LateUpdate(float deltaTime) => OnLateUpdate(deltaTime);

        public void Tick(int tick, float tickDelta) => OnTick(tick, tickDelta);
        public void LateTick(int tick, float tickDelta) => OnLateTick(tick, tickDelta);

        protected virtual void OnInitialize() { }
        protected virtual void OnEnable() { }
        protected virtual void OnEarlyUpdate(float deltaTime) { }
        protected virtual void OnUpdate(float deltaTime) { }
        protected virtual void OnLateUpdate(float deltaTime) { }
        protected virtual void OnTick(int tick, float tickDelta) { }
        protected virtual void OnLateTick(int tick, float tickDelta) { }
        protected virtual void OnDisable() { }
        protected virtual void OnDestroy() { }
    }
}
