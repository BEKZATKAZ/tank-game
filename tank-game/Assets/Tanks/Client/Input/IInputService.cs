namespace Tanks.Client.Input
{
    public enum InputScheme
    {
        KeyboardAndMouse,
        Gamepad
    }

    public interface IInputService<T> where T : unmanaged
    {
        bool IsEnabled { get; }
        InputScheme CurrentScheme { get; }

        T Read();
        void Enable();
        void Disable();
    }
}