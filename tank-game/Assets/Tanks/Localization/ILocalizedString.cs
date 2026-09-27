using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace Tanks.Localization
{
    public interface ILocalizedString
    {
        event Action<string> StringChanged;

        string EntryName { get; }

        string Get();
        UniTask<string> GetAsync();
        void AddVariable<T>(string key, T value);
    }

    public sealed class UnityLocalizedString : ILocalizedString
    {
        public UnityLocalizedString(LocalizedString original, string entryName)
        {
            Original = original;
            EntryName = entryName;
        }

        public event Action<string> StringChanged
        {
            add
            {
                LocalizedString.ChangeHandler handler = new(x => value(x));
                _handlers[value] = handler;
                Original.StringChanged += handler;
            }
            remove
            {
                if (_handlers.TryGetValue(value, out var handler) == false) return;
                Original.StringChanged -= handler;
                _handlers.Remove(value);
            }
        }

        public LocalizedString Original { get; }
        public string EntryName { get; }

        private readonly Dictionary<Action<string>, LocalizedString.ChangeHandler> _handlers = new();

        public string Get()
        {
            return Original.GetLocalizedString();
        }

        public UniTask<string> GetAsync()
        {
            return Original.GetLocalizedStringAsync().Task.AsUniTask();
        }

        public void AddVariable<T>(string key, T value)
        {
            IVariable variable = value switch
            {
                UnityEngine.Object o => new ObjectVariable { Value = o },
                string s => new StringVariable { Value = s },
                float f => new FloatVariable { Value = f },
                double d => new DoubleVariable { Value = d },
                int i => new IntVariable { Value = i },
                uint ui => new UIntVariable { Value = ui },
                bool b => new BoolVariable { Value = b },
                long l => new LongVariable { Value = l },
                ulong ul => new ULongVariable { Value = ul },
                short s => new ShortVariable { Value = s },
                ushort us => new UShortVariable { Value = us },
                byte b => new ByteVariable { Value = b },
                sbyte sb => new SByteVariable { Value = sb },
                _ => throw new NotImplementedException($"Type '{typeof(T).Name}' is not supported")
            };

            Original.Add(key, variable);
        }
    }
}
