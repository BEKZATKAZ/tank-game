using System;
using System.Collections.Generic;

namespace Tanks.Utils
{
    public interface IReadOnlyBindableProperty<T>
    {
        T Value { get; }

        void Bind(Action<T> callback);
        void Unbind(Action<T> callback);
    }

    public sealed class BindableProperty<T> : IReadOnlyBindableProperty<T>
    {
        public BindableProperty(T value = default)
        {
            _value = value;
            _toTargetBindings = new List<Action<T>>();
        }

        private readonly List<Action<T>> _toTargetBindings;
        private T _value;

        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                foreach (Action<T> binding in _toTargetBindings) binding(value);
            }
        }

        public void Bind(Action<T> callback)
        {
            callback(_value);
            _toTargetBindings.Add(callback);
        }

        public void Unbind(Action<T> callback)
        {
            _toTargetBindings.Remove(callback);
        }
    }
}
