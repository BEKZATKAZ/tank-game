using System;
using UnityEngine;

namespace Tanks.Client.Effects
{
    [Serializable]
    public sealed class EffectConfig
    {
        [SerializeField] private string _entryName;
        [SerializeField] private string _localizedFormatName;
        [SerializeField] private string _hexColor;
        [SerializeField] private Sprite _icon;

        public string EntryName => _entryName;
        public string LocalizedFormatName => _localizedFormatName;
        public string Color => _hexColor;
        public Sprite Sprite => _icon;
    }
}
