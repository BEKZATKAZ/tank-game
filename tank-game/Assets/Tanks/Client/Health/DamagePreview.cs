using Cysharp.Threading.Tasks;
using LitMotion;
using Tanks.Shared.Adapters;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Tanks.Client.Health
{
    public sealed class DamagePreview : MonoBehaviour
    {
        [SerializeField] private Text _text;

        public ITransform Transform { get; private set; }

        public void Initialize(float damage, ITransform at)
        {
            float maxOffset = 2;

            _text.text = damage.ToString("#.#");
            _text.color = Color.white;

            float x = UnityEngine.Random.Range(-maxOffset, maxOffset);
            float y = UnityEngine.Random.Range(-maxOffset, maxOffset);

            Transform.WorldPosition = at.WorldPosition + new float2(x, y);
        }

        public UniTask Animate(float duration = 0.5f)
        {
            float2 lastPosition = Transform.WorldPosition;

            return LMotion.Create(0f, 1f, duration)
                .WithEase(Ease.OutSine)
                .Bind(x =>
                {
                    if (this == false) return;
                    _text.color = new Color(1, 1, 1, 1 - x);

                    float2 positionOffset = x * new float2(0, 1);
                    Transform.WorldPosition = positionOffset + lastPosition;
                }).ToUniTask();
        }

        private void Awake()
        {
            Transform = new UnityTransform(transform);
        }
    }
}