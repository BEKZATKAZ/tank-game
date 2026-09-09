namespace Tanks.Client.Rendering
{
    public interface ICameraShakeService
    {
        void AddShake(in CameraImpulse impulse);
    }
}