using NaughtyAttributes;
using System;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Client.Weapons.Projectiles
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(ProjectileBehaviour), typeof(SpriteRenderer))]
    public sealed class ProjectileSpriteAnimation : MonoBehaviour
    {
        [CurveRange(0, 0, 1, 1)]
        [SerializeField]
        private AnimationCurve _curve;

        [SerializeField]
        private Sprite[] _frames;

        [SerializeField]
        private float _baseSpeed = 2;

        [SerializeField]
        private float _maxSpeed = 10;

        private ProjectileBehaviour _behaviour;
        private SpriteRenderer _renderer;
        private float _time;

        private void Awake()
        {
            _behaviour = GetComponent<ProjectileBehaviour>();
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            _time = 0;
        }

        private void OnDisable()
        {
            _renderer.sprite = _frames[0];
        }

        private void LateUpdate()
        {
            if (_frames is not { Length: > 0 } || _behaviour.LifeTime <= 0) return;

            float elapsed = _behaviour.ElapsedSeconds;

#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                float seconds = (float)DateTime.Now.TimeOfDay.TotalSeconds;
                elapsed = seconds % _behaviour.LifeTime;
            }
#endif

            float t = math.saturate(elapsed / _behaviour.LifeTime);
            float speed = math.lerp(_baseSpeed, _maxSpeed, _curve.Evaluate(t));

            _renderer.sprite = _frames[(int)(_time * _frames.Length)];
            _time += Time.deltaTime * speed;
            _time %= 1;
        }
    }
}
