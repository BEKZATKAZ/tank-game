using Tanks.Core.Helpers;
using Tanks.Shared.Adapters;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Tanks.Client.Entities.Health
{
    public sealed class DamagePreviewText : MonoBehaviour
    {
        [SerializeField] private Animation _animation;
        [SerializeField] private Text _text;

        public ITransform Transform { get; private set; }

        public void Initialize(PositiveFloat damage, ITransform at)
        {
            float maxOffset = 2;
            int value = (int)-math.ceil(damage);

            float x = UnityEngine.Random.Range(-maxOffset, maxOffset);
            float y = UnityEngine.Random.Range(-maxOffset, maxOffset);

            _text.text = value.ToString();
            Transform.WorldPosition = at.WorldPosition + new float2(x, y);
        }

        public void Animate()
        {
            AnimationState state = _animation["DamagePreview_Popup"];
            state.speed = UnityEngine.Random.Range(0.75f, 1.25f);
            _animation.Play(state.name);
        }

        private void Awake()
        {
            Transform = new UnityTransform(transform);
        }
    }
}
