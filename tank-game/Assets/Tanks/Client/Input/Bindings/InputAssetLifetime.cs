using UnityEngine;
using UnityEngine.InputSystem;

namespace Tanks.Client.Input.Bindings
{
    public static class InputAssetLifetime
    {
        public static void Destroy(InputActionAsset asset)
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                Object.DestroyImmediate(asset);
                return;
            }
#endif
            Object.Destroy(asset);
        }
    }
}
