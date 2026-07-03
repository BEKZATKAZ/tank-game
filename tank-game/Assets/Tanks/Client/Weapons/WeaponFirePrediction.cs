using Tanks.Client.Input;
using Tanks.Shared;
using Tanks.Shared.Weapons;
using VContainer;

namespace Tanks.Client.Weapons
{
    public sealed class WeaponFirePrediction : NetworkComponent<WeaponHolderBehaviour>
    {
        [Inject] private readonly IInputService<TurretInput> _input;

        private bool _isFireDown;

        private void Update()
        {
            TurretInput input = _input.Read();

            if (input.Fire == _isFireDown) return;

            if (input.Fire && _isFireDown == false)
            {
                Behaviour.StartUsing();
                Behaviour.StartUsingServerRpc();
            }

            if (input.Fire == false && _isFireDown)
            {
                Behaviour.StopUsing();
                Behaviour.StopUsingServerRpc();
            }

            _isFireDown = input.Fire;
        }
    }
}
