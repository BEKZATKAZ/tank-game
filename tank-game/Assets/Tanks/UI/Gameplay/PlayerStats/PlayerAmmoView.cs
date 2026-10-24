using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Gameplay.PlayerStats
{
    public sealed class PlayerAmmoView : ViewBase<PlayerAmmoViewModel>
    {
        [SerializeField]
        private Gradient _ammoGradient = new Gradient
        {
            colorKeys = new[]
            {
                new GradientColorKey(Color.red, 0),
                new GradientColorKey(Color.yellow, 1f / 3),
                new GradientColorKey(Color.white, 0.5f),
                new GradientColorKey(Color.white, 1)
            },
            alphaKeys = new[]
            {
                new GradientAlphaKey(1, 0),
                new GradientAlphaKey(1, 1)
            }
        };

        private static readonly Color BackgroundColor = new Color32(64, 64, 64, 255);
        private static readonly Color EmptySlotColor = new Color32(32, 32, 32, 255);
        private static readonly Color FillColor = new Color32(128, 128, 128, 255);

        private const float PulseDuration = 0.8f;

        private VisualElement _root;
        private readonly List<AmmoSlot> _slots = new();
        private Color _backgroundColor;
        private bool _isPulsing;
        private float _pulseElapsed;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("PlayerAmmo");
        }

        protected override void Bind(PlayerAmmoViewModel viewModel)
        {
            _root.Clear();
            _slots.Clear();

            ResetPulse();

            viewModel.Ammo.Bind(UpdateAmmo);
        }

        protected override void Unbind(PlayerAmmoViewModel viewModel)
        {
            viewModel.Ammo.Unbind(UpdateAmmo);

            ResetPulse();
        }

        private void UpdateAmmo((ushort Current, ushort Max) ammo)
        {
            _root.style.visibility = ammo.Max > 0 ? Visibility.Visible : Visibility.Hidden;

            if (_slots.Count != ammo.Max)
            {
                _root.Clear();
                _slots.Clear();

                ResetPulse();

                for (int i = 0; i < ammo.Max; i++)
                {
                    VisualElement slot = new VisualElement();
                    slot.AddToClassList("player-ammo__slot");
                    slot.style.marginBottom = i < ammo.Max - 1 ? 2 : 0;

                    VisualElement fill = new VisualElement();
                    fill.AddToClassList("player-ammo__fill");

                    slot.Add(fill);

                    _slots.Add(new AmmoSlot(fill, i >= ammo.Max - ammo.Current));
                    _root.Add(slot);
                }
            }

            float ratio = ammo.Max > 0 ? Mathf.Clamp01((float)ammo.Current / ammo.Max) : 0;
            Color tint = _ammoGradient.Evaluate(ratio);

            _backgroundColor = BackgroundColor * tint;
            _root.style.backgroundColor = _backgroundColor;

            bool isEmpty = ammo.Max > 0 && ammo.Current == 0;

            if (_isPulsing != isEmpty)
            {
                _pulseElapsed = 0;
                _isPulsing = isEmpty;
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                _root[i].style.backgroundColor = EmptySlotColor * tint;
                _root[i][0].style.backgroundColor = FillColor * tint;

                _slots[i].SetFilled(i >= ammo.Max - ammo.Current);
            }
        }

        private void LateUpdate()
        {
            ViewModel?.Update();

            TickPulse(Time.unscaledDeltaTime);

            foreach (AmmoSlot slot in _slots)
            {
                slot.Tick(Time.unscaledDeltaTime);
            }
        }

        private void ResetPulse()
        {
            _isPulsing = false;
            _pulseElapsed = 0;

            _root.style.backgroundColor = _backgroundColor;
        }

        private void TickPulse(float deltaTime)
        {
            if (_isPulsing == false) return;

            _pulseElapsed = Mathf.Repeat(_pulseElapsed + deltaTime, PulseDuration);

            float phase = _pulseElapsed / PulseDuration * Mathf.PI * 2;
            float intensity = 0.5f - 0.5f * Mathf.Cos(phase);

            Color color = Color.Lerp(_backgroundColor, _ammoGradient.Evaluate(0), intensity);
            color.a = _backgroundColor.a;

            _root.style.backgroundColor = color;
        }

        private sealed class AmmoSlot
        {
            public AmmoSlot(VisualElement fill, bool filled)
            {
                _fill = fill;

                _height = filled ? 100 : 0;
                _targetHeight = _height;

                _fill.style.height = Length.Percent(_height);
            }

            private const float Duration = 0.12f;

            private readonly VisualElement _fill;
            private float _height;
            private float _startHeight;
            private float _targetHeight;
            private float _elapsed;

            public void SetFilled(bool filled)
            {
                float target = filled ? 100 : 0;

                if (_targetHeight == target) return;

                _startHeight = _height;
                _targetHeight = target;
                _elapsed = 0;
            }

            public void Tick(float deltaTime)
            {
                if (_height == _targetHeight) return;

                _elapsed += deltaTime;

                float t = Mathf.Clamp01(_elapsed / Duration);
                float eased = 1 - (1 - t) * (1 - t) * (1 - t);

                _height = Mathf.Lerp(_startHeight, _targetHeight, eased);

                _fill.style.height = Length.Percent(_height);
            }
        }
    }
}
