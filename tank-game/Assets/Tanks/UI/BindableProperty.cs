using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tanks.UI
{
    public sealed class BindableProperty<T>
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

        public void BindToTarget(Action<T> callback)
        {
            callback(_value);
            _toTargetBindings.Add(callback);
        }
    }
}
