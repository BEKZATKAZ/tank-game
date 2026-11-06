using System.Collections.Generic;
using System.Linq;

namespace Tanks.Core.Settings
{
    public interface IFrameRateController
    {
        int TargetFrameRate { get; set; }
        int VSyncCount { get; set; }
    }

    public interface IFrameRateLimitService
    {
        void SetFrameRate(int value);
        void SetVSync(int value);
        void SetLimit(object owner, int value);
        void RemoveLimit(object owner);
    }

    public sealed class FrameRateLimitService : IFrameRateLimitService
    {
        public FrameRateLimitService(IFrameRateController controller)
        {
            _controller = controller;
            _frameRate = controller.TargetFrameRate;
            _vSync = controller.VSyncCount;
        }

        private readonly IFrameRateController _controller;
        private readonly Dictionary<object, int> _limits = new();
        private int _frameRate;
        private int _vSync;

        public void SetFrameRate(int value)
        {
            _frameRate = value;
            Apply();
        }

        public void SetVSync(int value)
        {
            _vSync = value;
            Apply();
        }

        public void SetLimit(object owner, int value)
        {
            _limits[owner] = value;
            Apply();
        }

        public void RemoveLimit(object owner)
        {
            if (_limits.Remove(owner)) Apply();
        }

        private void Apply()
        {
            int limit = _limits.Count == 0 ? int.MaxValue : _limits.Values.Min();
            _controller.TargetFrameRate = _limits.Count == 0 ? _frameRate :
                _frameRate > 0 ? System.Math.Min(_frameRate, limit) : limit;
            _controller.VSyncCount = _limits.Count == 0 ? _vSync : 0;
        }
    }
}
