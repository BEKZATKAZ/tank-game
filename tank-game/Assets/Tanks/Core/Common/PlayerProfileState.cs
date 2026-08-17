using Tanks.Utils;

namespace Tanks.Core.Common
{
    public sealed class PlayerProfileState
    {
        public BindableProperty<string> UserId { get; } = new("");
        public BindableProperty<string> DisplayName { get; } = new("");
    }
}
