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
        bool IsOwner { get; }
        bool IsLocalPlayer { get; }
        bool IsServer { get; }

        ulong OwnerClientId { get; }
        ITransform Transform { get; }

        void EmitMessage<T>(T message);
        void RegisterMessageHandler<T>(INetworkMessageHandler<T> handler);

        T GetBehaviour<T>() where T : INetworkBehaviourExtended;
        T Require<T>() where T : INetworkComponent;
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

        public void EmitMessage<T>(T message)
        {
            Type type = message.GetType();
            if (_handlers.TryGetValue(type, out var handlers) == false) return;

            foreach (INetworkMessageHandler handler in handlers)
            {
                // Safe cast
                var casted = (INetworkMessageHandler<T>)handler;
                
                try { casted.Handle(message); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
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

        public T GetBehaviour<T>() where T : INetworkBehaviourExtended
        {
            return this ? GetComponent<T>() : default;
        }

        public T Require<T>() where T : INetworkComponent
        {
            foreach (INetworkComponent component in _components)
            {
                if (component is T result) return result;
            }

            return default;
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
                try { component.Destroy(); }
                catch (Exception ex) { Debug.LogException(ex); }
            }

            if (_isInitialized == false || NetworkManager == false) return;
            if (NetworkManager.NetworkTickSystem is null) return;

            NetworkManager.NetworkTickSystem.Tick -= Tick;
        }

        protected virtual void OnEnable()
        {
            foreach (INetworkComponent component in _components)
            {
                try { component.IsEnabled = true; }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }

        protected virtual void OnDisable()
        {
            foreach (INetworkComponent component in _components)
            {
                try { component.IsEnabled = false; }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }

        protected virtual void Update()
        {
            float deltaTime = Time.deltaTime;

            foreach (INetworkComponent component in _components)
            {
                try { component.EarlyUpdate(deltaTime); }
                catch (Exception ex) { Debug.LogException(ex); }
            }

            foreach (INetworkComponent component in _components)
            {
                try { component.Update(deltaTime); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }

        protected virtual void LateUpdate()
        {
            float deltaTime = Time.deltaTime;

            foreach (INetworkComponent component in _components)
            {
                try { component.LateUpdate(deltaTime); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }

        private void Tick()
        {
            if (this == false)
            {
                if (NetworkManager == false) return;
                if (NetworkManager.NetworkTickSystem is null) return;

                NetworkManager.NetworkTickSystem.Tick -= Tick;
                return;
            }

            if (this == false || gameObject.activeInHierarchy == false) return;
            if (enabled == false) return;

            int tick = NetworkManager.NetworkTickSystem.LocalTime.Tick;
            float tickDelta = 1f / NetworkManager.NetworkTickSystem.TickRate;

            OnTick(tick, tickDelta);

            foreach (INetworkComponent component in _components)
            {
                if (component.IsEnabled == false) continue;
                try { component.Tick(tick, tickDelta); }
                catch (Exception ex) { Debug.LogException(ex); }
            }

            OnLateTick(tick, tickDelta);

            foreach (INetworkComponent component in _components)
            {
                if (component.IsEnabled == false) continue;
                try { component.LateTick(tick, tickDelta); }
                catch (Exception ex) { Debug.LogException(ex); }
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
                try
                {
                    INetworkComponent component = _container
                        .CreateInjected(componentType) as INetworkComponent;

                    component.Initialize(this);
                    _components.Add(component);
                }
                catch (Exception ex)
                {
                    Debug.LogErrorFormat(
                        "Initialization of {0} has thrown {1}", componentType.Name, ex);
                }
            }

            OnInitialize();
        }

        protected virtual void Awake() => Transform = new UnityTransform(transform);
        protected virtual void OnInitialize() { }

        public void ForceInitialize() => OnNetworkSpawn();
        public virtual void OnTick(int tick, float tickDelta) { }
        public virtual void OnLateTick(int tick, float tickDelta) { }
    }
}
