using UnityEngine;

namespace Tanks.Client.Rendering
{
    public interface ICameraMainService
    {
        ICamera Get();
    }

    public sealed class CameraMainService : ICameraMainService
    {
        private UnityCamera _camera;

        public ICamera Get()
        {
            if (_camera is null || _camera.OriginalCamera == false)
            {
                Camera main = Camera.main;
                _camera = main ? new UnityCamera(main) : null;
            }

            return _camera;
        }
    }
}
