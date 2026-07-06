using LitMotion;
using Tanks.Core.Repositories;
using Tanks.Shared;
using Tanks.Shared.Health;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;

namespace Tanks.Client.Health
{
    public sealed class DamagePreviewEffect :
        NetworkComponent<HealthBehaviour>,
        INetworkMessageHandler<DamageTaken>
    {
        [Inject] private readonly IResourceLoader _loader;

        private static readonly string _address = "UI/DamagePreview";
        private ObjectPool<DamagePreview> _pool;

        public async void Handle(DamageTaken message)
        {
            DamagePreview preview = _pool.Get();

            preview.Initialize(message.Amount, Behaviour.Transform);
            await preview.Animate();

            if (this) _pool.Release(preview);
        }

        private async void Start()
        {
            if (Behaviour.IsOwner)
            {
                enabled = false;
                return;
            }

            DamagePreview prefab = null;

            try
            {
                GameObject resource = await _loader.LoadAsync<GameObject>(_address);
                prefab = resource.GetComponent<DamagePreview>();

                _pool = new ObjectPool<DamagePreview>(
                    createFunc: () => Instantiate(prefab),
                    actionOnGet: x => x.gameObject.SetActive(true),
                    actionOnRelease: x => x.gameObject.SetActive(false),
                    actionOnDestroy: x => Destroy(x.gameObject),
                    collectionCheck: false,
                    defaultCapacity: 5,
                    maxSize: 50);
            }
            finally
            {
                if (prefab) _loader.ReleaseFromResource(prefab.gameObject);
            }

            Behaviour.RegisterMessageHandler(this);
        }
    }
}