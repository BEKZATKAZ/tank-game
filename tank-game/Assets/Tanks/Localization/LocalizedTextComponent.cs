using UnityEngine;
using UnityEngine.Localization;

namespace Tanks.Localization
{
    [ExecuteAlways]
    [RequireComponent(typeof(TextMesh))]
    public sealed class LocalizedTextComponent : MonoBehaviour
    {
        [SerializeField] private LocalizedString _string;

        private TextMesh _text;

        private async void Start()
        {
            if (_string is null) return;

            _text = GetComponent<TextMesh>();
            _string.StringChanged += StringChanged;

            _text.text = await _string.GetLocalizedStringAsync().Task;
        }

        private void OnDestroy()
        {
            if (_string is null) return;
            _string.StringChanged -= StringChanged;
        }

        private void StringChanged(string value)
        {
            _text.text = value;
        }
    }
}
