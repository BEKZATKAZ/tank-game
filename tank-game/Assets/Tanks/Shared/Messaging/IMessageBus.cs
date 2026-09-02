using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Core.Helpers;
using UnityEngine;

namespace Tanks.Shared.Messaging
{
    public interface IMessageBus
    {
        void RegisterHandler<T>(Required<IMessageHandler<T>> handler);
        void UnregisterHandler<T>(Required<IMessageHandler<T>> handler);
        void Publish<T>(T message, IMessageSender sender);
    }

    public sealed class MessageBus : IMessageBus
    {
        private readonly Dictionary<Type, List<IMessageHandler>> _handlers = new();

        public void Publish<T>(T message, IMessageSender sender)
        {
            Type type = message.GetType();

            if (_handlers.TryGetValue(type, out List<IMessageHandler> list) == false)
            {
                Debug.LogWarningFormat("No message handlers found for {0}", type.Name);
                return;
            }

            foreach (IMessageHandler openHandler in list)
            {
                // If one throws, continue with others
                try
                {
                    // Safe cast
                    var closedHandler = (IMessageHandler<T>)openHandler;
                    closedHandler.Handle(message, sender);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        public void RegisterHandler<T>(Required<IMessageHandler<T>> handler)
        {
            Type type = typeof(T);

            if (_handlers.TryGetValue(type, out List<IMessageHandler> list) == false)
            {
                list = new List<IMessageHandler>();
                _handlers[type] = list;
            }

            Type handlerType = handler.Value.GetType();

            if (list.Any(x => x.GetType() == handlerType))
            {
                string name = handlerType.Name;
                throw new ArgumentException($"Handler '{name}' already registered");
            }

            list.Add(handler.Value);
        }

        public void UnregisterHandler<T>(Required<IMessageHandler<T>> handler)
        {
            Type type = typeof(T);

            if (_handlers.TryGetValue(type, out List<IMessageHandler> list) == false)
            {
                throw new ArgumentException($"No message handlers found for {type.Name}");
            }

            if (list.Remove(handler.Value)) return;

            throw new ArgumentException($"Handler '{type.Name}' not registered");
        }
    }
}
