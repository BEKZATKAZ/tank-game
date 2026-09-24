using System;
using System.Collections.Generic;
using UnityEngine;

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
                if (_value != null && _value.Equals(value)) return;
                _value = value;
                ForceRefresh();
            }
        }

        public void ForceRefresh()
        {
            foreach (Action<T> binding in _toTargetBindings)
            {
                try { binding(_value); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }

        public void Bind(Action<T> callback)
        {
            try { callback(_value); }
            catch (Exception ex) { Debug.LogException(ex); }
            _toTargetBindings.Add(callback);
        }

        public void Unbind(Action<T> callback)
        {
            _toTargetBindings.Remove(callback);
        }
    }
}
