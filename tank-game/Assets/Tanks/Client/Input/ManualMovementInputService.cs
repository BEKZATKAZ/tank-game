using Tanks.Client.Entities;
using Tanks.Client.Input.Bindings;
using Tanks.Core.Settings;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tanks.Client.Input
{
    public sealed class ManualMovementInputService :
        InputServiceBase<ManualDriveInput>
    {
        public ManualMovementInputService(
            ISettingsService settings,
            IClientEntityService entities,
            IControlBindingService bindings) : base(bindings)
        {
            _settings = settings;
            _entities = entities;
            _settings.Bind(PlayerSetting.ControlsMode, OnModeChanged);
        }

        private readonly ISettingsService _settings;
        private readonly IClientEntityService _entities;
        private ControlsMode _mode;

        private bool CanDrive => IsEnabled && IsSuspended == false &&
            _entities.GetPossessedEntity() is { IsValid: true, IsLocalPlayer: true };

        public override ManualDriveInput Read()
        {
            if (CanDrive == false) return default;

            if (_mode == ControlsMode.Classic)
            {
                return new ManualDriveInput
                {
                    Throttle = Controls.ManualMovement.Throttle.ReadValue<float>(),
                    Steering = Controls.ManualMovement.Steering.ReadValue<float>()
                };
            }

            InputActionMap modern = Controls.asset.FindActionMap("ModernMovement");
            bool reverse = modern["ToggleReverse"].IsPressed();

            if (reverse)
            {
                IEntityBehaviour entity = _entities.GetPossessedEntity();
                IHealthBehaviour health = entity.GetBehaviour<IHealthBehaviour>();
                if (health != null && health.ServerState.Value.Current <= 0)
                    return default;

                return new ManualDriveInput { IsModern = true, Reverse = true };
            }

            return new ManualDriveInput
            {
                IsModern = true,
                Direction = Vector2.ClampMagnitude(
                    modern["Direction"].ReadValue<Vector2>(), 1),
                TurnInPlace = modern["TurnInPlace"].IsPressed()
            };
        }

        protected override void EnableMaps()
        {
            string map = _mode == ControlsMode.Classic
                ? "ManualMovement" : "ModernMovement";
            InputActionMap actions = Controls.asset.FindActionMap(map, true);

            if (_mode != ControlsMode.Classic)
                actions["ToggleReverse"].wantsInitialStateCheck = true;

            actions.Enable();
        }

        public override void Dispose()
        {
            _settings.Unbind(PlayerSetting.ControlsMode, OnModeChanged);
            base.Dispose();
        }

        private void OnModeChanged(int value)
        {
            ControlsMode mode = value == (int)ControlsMode.Classic
                ? ControlsMode.Classic : ControlsMode.Modern;
            if (_mode == mode) return;

            _mode = mode;
            RefreshMaps();
        }
    }
}
