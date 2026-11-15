using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Gameplay.Effects
{
    internal sealed class EffectCircle
    {
        public EffectCircle(VisualElement element) => _element = element;

        private readonly VisualElement _element;
        private bool _expiring;
        private float _elapsed;

        public void SetExpiring(bool expiring)
        {
            if (_expiring == expiring) return;
            _expiring = expiring;
            _elapsed = 0;
            _element.style.opacity = 1;
        }

        public void Tick(float deltaTime)
        {
            if (_expiring == false) return;
            _elapsed = Mathf.Repeat(_elapsed + deltaTime, 0.5f);
            _element.style.opacity = 1 - Mathf.PingPong(_elapsed * 4, 1);
        }
    }
}
