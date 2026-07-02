namespace Tanks.Shared.Weapons
{
    public interface IReadOnlyUseMode
    {
        bool IsUsing { get; }
    }

    public interface IUseMode : IReadOnlyUseMode
    {
        void StartUsing();
        void StopUsing();
        void Tick();
    }

    public class SemiAutomaticUseMode : IUseMode
    {
        public bool IsUsing { get; private set; }

        public void StartUsing() => IsUsing = true;
        public void StopUsing() => IsUsing = false;
        public void Tick() => IsUsing = false;
    }

    public class FullAutomaticUseMode : IUseMode
    {
        public bool IsUsing { get; private set; }

        public void StartUsing() => IsUsing = true;
        public void StopUsing() => IsUsing = false;
        public void Tick() { }
    }
}