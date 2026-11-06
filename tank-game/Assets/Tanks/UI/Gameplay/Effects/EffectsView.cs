using System.Collections.Generic;
using LitMotion;
using NaughtyAttributes;
using Tanks.Client.Effects;
using Tanks.Shared.Effects;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Gameplay.Effects
{
    public sealed class EffectsView : ViewBase<EffectsViewModel>
    {
        [Required]
        [SerializeField]
        private Material _fullscreenMaterial;

        [Required]
        [Expandable]
        [SerializeField]
        private EffectListConfig _config;

        private VisualElement _root;
        private VisualElement _flashOverlay;
        private MotionHandle _fade;
        private EffectType? _currentEffect;
        private readonly Dictionary<EffectType, EffectCircle> _circles = new();

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("PlayerEffects");
            _flashOverlay = root.Q("FlashOverlay");
            _fullscreenMaterial.SetFloat("_Opacity", 0);
            UpdateFlash(1);
        }

        protected override void Bind(EffectsViewModel viewModel)
        {
            viewModel.ActiveEffects.Bind(UpdateEffects);
            viewModel.CurrentEffect.Bind(OnEffectChanged);
            viewModel.ExpiringEffects.Bind(UpdateExpiringEffects);
            viewModel.FlashProgress.Bind(UpdateFlash);
        }

        protected override void Unbind(EffectsViewModel viewModel)
        {
            viewModel.ActiveEffects.Unbind(UpdateEffects);
            viewModel.CurrentEffect.Unbind(OnEffectChanged);
            viewModel.ExpiringEffects.Unbind(UpdateExpiringEffects);
            viewModel.FlashProgress.Unbind(UpdateFlash);
            _circles.Clear();
            _root.Clear();
            ResetEffect();
        }

        private void UpdateEffects(IReadOnlyList<EffectType> effects)
        {
            _circles.Clear();
            _root.Clear();

            if (effects is null) return;

            foreach (EffectType type in effects)
            {
                if (_config.Effects.TryGetValue(type, out EffectConfig config) == false) continue;

                VisualElement circle = new VisualElement { pickingMode = PickingMode.Ignore };
                circle.AddToClassList("player-effects__circle");
                circle.style.marginBottom = _root.childCount > 0 ? 8 : 0;

                if (ColorUtility.TryParseHtmlString($"#{config.Color.TrimStart('#')}", out Color color))
                {
                    circle.style.backgroundColor = color;
                }

                VisualElement icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("player-effects__icon");
                icon.style.backgroundImage = new StyleBackground(config.Sprite);

                circle.Add(icon);
                _root.Add(circle);
                _circles.Add(type, new EffectCircle(circle));
            }

            UpdateExpiringEffects(ViewModel.ExpiringEffects.Value);
        }

        private void UpdateExpiringEffects(IReadOnlyList<EffectType> effects)
        {
            foreach ((EffectType type, EffectCircle circle) in _circles)
            {
                bool expiring = false;
                for (int i = 0; effects != null && i < effects.Count; i++)
                {
                    if (effects[i] != type) continue;
                    expiring = true;
                    break;
                }

                circle.SetExpiring(expiring);
            }
        }

        private void OnEffectChanged(EffectType? type)
        {
            if (_currentEffect == type) return;
            _currentEffect = type;

            _fade.TryCancel();
            float opacity = _fullscreenMaterial.GetFloat("_Opacity");

            if (type.HasValue)
            {
                _fullscreenMaterial.SetInt("_Type", (int)type.Value);

                _fade = LMotion.Create(opacity, 1f, duration: 0.3f)
                    .WithEase(Ease.OutSine)
                    .Bind(x => _fullscreenMaterial.SetFloat("_Opacity", x));

                return;
            }

            _fade = LMotion.Create(opacity, 0f, duration: 0.6f)
                .WithEase(Ease.OutSine)
                .Bind(x => _fullscreenMaterial.SetFloat("_Opacity", x));
        }

        private void LateUpdate()
        {
            NetworkManager network = NetworkManager.Singleton;

            if (network != null && network.IsListening && network.NetworkTickSystem != null)
            {
                ViewModel?.Tick(network.NetworkTickSystem.LocalTime.Tick,
                    1f / network.NetworkTickSystem.TickRate);
            }
            else
            {
                ViewModel?.Tick();
            }

            foreach (EffectCircle circle in _circles.Values) circle.Tick(Time.unscaledDeltaTime);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _circles.Clear();
            ResetEffect();
        }

        private void ResetEffect()
        {
            _fade.TryCancel();
            _currentEffect = null;

            if (_fullscreenMaterial == false) return;
            _fullscreenMaterial.SetFloat("_Opacity", 0);
            UpdateFlash(1);
        }

        private void UpdateFlash(float progress)
        {
            _fullscreenMaterial.SetFloat("_FlashProgress", progress);
            float hold = _fullscreenMaterial.GetFloat("_FlashWhiteout");
            float fade = _fullscreenMaterial.GetFloat("_FlashFade");
            float recovery = Mathf.Clamp01((progress - hold) / Mathf.Max(fade, 0.001f));
            float opacity = 1 - Mathf.SmoothStep(0, 1, recovery);

            _flashOverlay.style.opacity = opacity;
            _flashOverlay.style.display = opacity > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private sealed class EffectCircle
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
}
