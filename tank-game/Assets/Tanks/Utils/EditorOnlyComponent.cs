using UnityEngine;

namespace Tanks.Utils
{
    public sealed class EditorOnlyComponent : MonoBehaviour
    {
        private void Awake()
        {
#if UNITY_EDITOR
            if (Application.isEditor) return;
#endif
            Destroy(gameObject);
        }
    }
}
