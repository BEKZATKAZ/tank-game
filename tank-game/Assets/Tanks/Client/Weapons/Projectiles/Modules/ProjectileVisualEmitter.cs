using Tanks.Audio;
using Tanks.Shared.Adapters;
using Tanks.Shared.Entities;
using Tanks.Shared.Weapons.Projectiles.Modules;
using Unity.Mathematics;
using UnityEngine;
using VContainer;

namespace Tanks.Client.Weapons.Projectiles.Modules
{
    public sealed class ProjectileVisualEmitter : ProjectileModuleBehaviourBase
    {
        public ProjectileModuleCallbackFlags EmitOn;
        public string SoundPath;
        public ParticleSystem Particle;

        [Inject] private readonly IAudioPlayer _audioPlayer;

        private ITransform _particleTransform;

        private void Emit()
        {
            if (string.IsNullOrWhiteSpace(SoundPath) == false)
            {
                _audioPlayer.PlayOneShot(SoundPath, Transform.WorldPosition);
            }

            if (_particleTransform != null)
            {
                _particleTransform.WorldPosition = Transform.WorldPosition;
                Particle.gameObject.SetActive(true);
                Particle.Play(true);
            }
        }

        protected override void OnHitEntity(IEntityBehaviour target, IEntityBehaviour from)
        {
            if (EmitOn.HasFlag(ProjectileModuleCallbackFlags.HitEntity) == false) return;
            Emit();
        }

        protected override WallHitOutput OnHitWall(float2 point, float2 normal)
        {
            if (EmitOn.HasFlag(ProjectileModuleCallbackFlags.HitWall)) Emit();
            return base.OnHitWall(point, normal);
        }

        protected override void OnProjectileDone()
        {
            if (EmitOn.HasFlag(ProjectileModuleCallbackFlags.ProjectileDone) == false) return;
            Emit();
        }

        protected override void Awake()
        {
            base.Awake();
            if (Particle == false) return;

            _particleTransform = new UnityTransform(Particle.transform);
            _particleTransform.SetParent(null);
            Particle.gameObject.SetActive(false);
        }
    }
}
