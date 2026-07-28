using FMODUnity;
using Tanks.Shared.Adapters;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Audio
{
    public interface IAudioPlayer
    {
        void SetDefault(ITransform target);
        void PlayOneShot(string path, float2 position = default);
        void PlayOneShotAttached(string path, ITransform target, bool useDefault = false);
    }

    public sealed class FMODAudioPlayer : IAudioPlayer
    {
        public GameObject Default { get; private set; }

        public void SetDefault(ITransform target)
        {
            Default = target is UnityTransform transform
                ? transform.Transform.gameObject
                : null;
        }

        public void PlayOneShot(string path, float2 position = default)
        {
            RuntimeManager.PlayOneShot(path, (Vector2)position);
        }

        public void PlayOneShotAttached(string path, ITransform target, bool useDefault = false)
        {
            if (useDefault)
            {
                if (Default) RuntimeManager.PlayOneShotAttached(path, Default);
                else PlayOneShot(path);
                return;
            }

            if (target is not UnityTransform transform)
            {
                PlayOneShot(path, target.WorldPosition);
                return;
            }

            RuntimeManager.PlayOneShotAttached(path, transform.Transform.gameObject);
        }
    }
}

