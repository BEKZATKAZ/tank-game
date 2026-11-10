using System;
using Tanks.Utils;

namespace Tanks.Core.Players
{
    public sealed class MenuLoadingState
    {
        public BindableProperty<bool> IsReady { get; } = new();
        public BindableProperty<bool> HasError { get; } = new();

        public event Action RetryRequested;

        public void Retry() => RetryRequested?.Invoke();

        public void Reset()
        {
            IsReady.Value = false;
            HasError.Value = false;
        }
    }
}
