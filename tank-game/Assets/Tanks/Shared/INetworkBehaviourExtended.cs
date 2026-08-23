using System;
using System.Collections.Generic;
using Tanks.Shared.Adapters;
using Tanks.Utils;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Tanks.Shared
{
    public interface INetworkBehaviourExtended
    {
        ITransform Transform { get; }
    }

    public interface INetworkMessageHandler { }
    public interface INetworkMessageHandler<T> : INetworkMessageHandler
    {
        void Handle(T message);
    }

    public abstract class NetworkBehaviourExtended : NetworkBehaviour, INetworkBehaviourExtended
    {
        public ITransform Transform { get; private set; }
        public IEnumerable<INetworkComponent> Components => _components;

        private readonly Dictionary<Type, List<INetworkMessageHandler>> _handlers = new();
        private readonly List<INetworkComponent> _components = new();
        private IObjectResolver _container;

        private bool _isInjected;
        private bool _isSpawned;
        private bool _isInitialized;

        [Inject]
        public void Inject(IObjectResolver container)
        {
            _isInjected = true;
            _container = container;
            TryInitializeBehaviour();
        }

        protected void EmitMessage<T>(T message)
        {
            Type type = message.GetType();

            if (_handlers.TryGetValue(type, out List<INetworkMessageHandler> handlers) == false)
            {
                //Debug.LogWarningFormat("No message handler found for '{0}'", type.Name);
                return;
            }

            foreach (INetworkMessageHandler handler in handlers)
            {
                try
                {
                    // Safe cast
                    ((INetworkMessageHandler<T>)handler).Handle(message);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        private void TryInitializeBehaviour()
        {
            if (_isInitialized) return;
            if (_isSpawned == false || _isInjected == false) return;

            _isInitialized = true;
            NetworkManager.NetworkTickSystem.Tick += Tick;

            IEnumerable<Type> types = SplitComponentFinder
                .FindFor(this, typeof(INetworkComponent<>), NetworkManager.IsServer);

            foreach (Type componentType in types)
            {
                INetworkComponent component = _container
                    .CreateInjected(componentType) as INetworkComponent;

                component.Initialize(this);
                _components.Add(component);
            }

            OnInitialize();
        }

        public void RegisterMessageHandler<T>(INetworkMessageHandler<T> handler)
        {
            Type messageType = typeof(T);

            if (_handlers.TryGetValue(messageType, out var handlers) == false)
            {
                handlers = new List<INetworkMessageHandler>();
                _handlers[messageType] = handlers;
            }

            handlers.Add(handler);
        }

        public override void OnNetworkSpawn()
        {
            _isSpawned = true;
            TryInitializeBehaviour();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            foreach (INetworkComponent component in _components)
            {
                component.Destroy();
            }

            if (_isInitialized == false || NetworkManager == false) return;
            if (NetworkManager.NetworkTickSystem is null) return;

            NetworkManager.NetworkTickSystem.Tick -= Tick;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            foreach (INetworkComponent component in _components)
            {
                component.Update(deltaTime);
            }
        }

        private void Tick()
        {
            if (gameObject.activeInHierarchy == false) return;
            if (enabled == false) return;

            int tick = NetworkManager.NetworkTickSystem.LocalTime.Tick;
            float tickDelta = 1f / NetworkManager.NetworkTickSystem.TickRate;
            OnTick(tick, tickDelta);

            foreach (INetworkComponent component in _components)
            {
                if (component.IsEnabled == false) continue;
                component.Tick(tick, tickDelta);
            }
        }

        protected void ForceInitialize() => OnNetworkSpawn();
        protected virtual void Awake() => Transform = new UnityTransform(transform);
        protected virtual void OnTick(int tick, float tickDelta) { }
        protected virtual void OnInitialize() { }
    }
}
