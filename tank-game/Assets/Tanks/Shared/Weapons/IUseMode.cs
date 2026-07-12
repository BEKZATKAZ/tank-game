namespace Tanks.Shared.Weapons
{
    public interface IReadOnlyUseMode
    {
        bool IsUsing { get; }
    }

    public interface IUseMode : IReadOnlyUseMode
    {
        void Tick(bool isHoldingDown);
    }

    public class SemiAutomaticUseMode : IUseMode
    {
        public bool IsUsing { get; private set; }

        private bool _isPressed;

        public void Tick(bool isHoldingDown)
        {
            if (isHoldingDown && _isPressed == false)
            {
                IsUsing = true;
            }
            else IsUsing = false;

            _isPressed = isHoldingDown;
        }
    }

    public class FullAutomaticUseMode : IUseMode
    {
        public bool IsUsing { get; private set; }

        public void Tick(bool isHoldingDown) => IsUsing = isHoldingDown;
    }
}
