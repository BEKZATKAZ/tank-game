using Tanks.Core.Helpers;
using Tanks.Shared.Adapters;
using Tanks.Shared.Entities.Health;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Tanks.Client.Entities.Health
{
    public sealed class DamagePreviewText : MonoBehaviour
    {
        [SerializeField] private Animation _animation;
        [SerializeField] private Text _text;
        [SerializeField] private Gradient _color;

        public ITransform Transform { get; private set; }

        public void Initialize(PositiveFloat damage, HealthState state, ITransform at)
        {
            float maxOffset = 2;
            int value = (int)-math.ceil(damage);

            float x = UnityEngine.Random.Range(-maxOffset, maxOffset);
            float y = UnityEngine.Random.Range(-maxOffset, maxOffset);

            _text.color = _color.Evaluate(1 - state.NormalizedCurrent);
            _text.text = value.ToString();

            Transform ??= new UnityTransform(transform);
            Transform.WorldPosition = at.WorldPosition + new float2(x, y);
        }

        public void Animate()
        {
            AnimationState state = _animation["DamagePreview_Popup"];
            state.speed = UnityEngine.Random.Range(0.75f, 1.25f);
            _animation.Play(state.name);
        }
    }
}
