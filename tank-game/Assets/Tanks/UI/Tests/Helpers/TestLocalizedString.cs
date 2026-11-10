using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using Tanks.Localization;

namespace Tanks.UI.Tests.Helpers
{
    public sealed class TestLocalizedString : ILocalizedString
    {
        public TestLocalizedString(string text) => _text = text;
        public event Action<string> StringChanged { add { } remove { } }
        public string EntryName => _text;

        private readonly string _text;
        private readonly Dictionary<string, string> _variables = new();

        public string Get()
        {
            string text = _text;

            foreach (var value in _variables)
            {
                text = text.Replace("{" + value.Key + "}", value.Value);
            }

            return text;
        }

        public UniTask<string> GetAsync() => UniTask.FromResult(Get());
        public void AddVariable<T>(string key, T value) => _variables[key] = value.ToString();
    }
}
