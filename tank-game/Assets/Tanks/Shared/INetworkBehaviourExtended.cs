using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Tanks.Shared.Adapters;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

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

        private readonly Dictionary<Type, List<INetworkMessageHandler>> _handlers = new();
        private readonly List<INetworkComponent> _components = new();

        protected void EmitMessage<T>(T message)
        {
            Type type = message.GetType();

            if (_handlers.TryGetValue(type, out List<INetworkMessageHandler> handlers) == false)
            {
                Debug.LogWarningFormat("No message handler found for '{0}'", type.Name);
                return;
            }

            foreach (INetworkMessageHandler handler in handlers)
            {
                // Safe cast
                ((INetworkMessageHandler<T>)handler).Handle(message);
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

        public override void OnNetworkSpawn()
        {
            NetworkManager.NetworkTickSystem.Tick += OnTick;

            IEnumerable<Type> types = SplitComponentFinder.FindFor(
                this, typeof(INetworkComponent<>));

#if UNITY_EDITOR
            // Filtering since all assemblies are loaded in the editor
            // This logic is not necessary in builds
            if (NetworkManager.IsServer)
            {
                types = types.Where(x => IsSharedOr(x.Assembly, "server"));
            }
            else
            {
                types = types.Where(x => IsSharedOr(x.Assembly, "client"));
            }
#endif

            foreach (Type type in types)
            {
                if (gameObject.TryGetComponent(type, out Component component) == false)
                {
                    component = gameObject.AddComponent(type);
                }

                // Safe cast
                _components.Add(component as INetworkComponent);
            }

            IObjectResolver container = LifetimeScope.Find<LifetimeScope>().Container;
            container.InjectGameObject(gameObject);
        }

        public override void OnNetworkDespawn()
        {
            NetworkManager.NetworkTickSystem.Tick -= OnTick;
        }

        protected virtual void Awake()
        {
            Transform = new UnityTransform(transform);
        }

        private void OnTick()
        {
            int tick = NetworkManager.NetworkTickSystem.LocalTime.Tick;
            float tickDelta = 1f / NetworkManager.NetworkTickSystem.TickRate;

            foreach (INetworkComponent component in _components)
            {
                component.OnTick(tick, tickDelta);
            }
        }

        private static bool IsSharedOr(Assembly assembly, string expectedName)
        {
            string pattern = $"shared|{expectedName}";
            return Regex.IsMatch(assembly.FullName, pattern, RegexOptions.IgnoreCase);
        }
    }
}