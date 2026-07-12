namespace Tanks.Client.Input
{
    public struct ManualDriveInput
    {
        public float Throttle;
        public float Steering;
    }

    public sealed class ManualMovementInputService : InputServiceBase<ManualDriveInput>
    {
        public override ManualDriveInput Read()
        {
            Controls.ManualMovementActions movement = Controls.ManualMovement;

            return new ManualDriveInput
            {
                Throttle = movement.Throttle.ReadValue<float>(),
                Steering = movement.Steering.ReadValue<float>()
            };
        }
    }
}
