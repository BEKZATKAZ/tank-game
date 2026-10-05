using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Gameplay.PlayerStats
{
    public sealed class PlayerHealthView : ViewBase<PlayerHealthViewModel>
    {
        [Required]
        [SerializeField]
        private Material _tankMaterial;

        private VisualElement _root;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("PlayerHealth");
            _root.Q("PlayerTank").style.unityMaterial = _tankMaterial;
        }

        protected override void Bind(PlayerHealthViewModel viewModel)
        {
            viewModel.InstantHealthOverlay.Bind(
                x => _tankMaterial.SetFloat("_InstantOverlay", x));

            viewModel.RedHealthOverlay.Bind(
                x => _tankMaterial.SetFloat("_RedOverlay", x));

            viewModel.TankColor.Bind(
                x => _tankMaterial.SetColor("_TankColor", x));
        }

        private void LateUpdate()
        {
            ViewModel?.Tick(Time.deltaTime);
        }
    }
}
