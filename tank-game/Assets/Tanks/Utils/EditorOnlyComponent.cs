using UnityEngine;

namespace Tanks.Utils
{
    public sealed class EditorOnlyComponent : MonoBehaviour
    {
        private void Awake()
        {
            if (Application.isEditor) return;
            Destroy(gameObject);
        }
    }
}
