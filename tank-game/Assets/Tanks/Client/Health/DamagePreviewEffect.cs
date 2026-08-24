using Tanks.Core.Repositories;
using Tanks.Shared;
using Tanks.Shared.Health;
using UnityEngine;
using UnityEngine.Pool;

namespace Tanks.Client.Health
{
    public sealed class DamagePreviewEffect :
        NetworkComponent<IHealthBehaviour>,
        INetworkMessageHandler<DamageTaken>
    {
        public DamagePreviewEffect(IResourceLoader loader)
        {
            _loader = loader;
        }

        private readonly IResourceLoader _loader;

        private static readonly string _address = "UI/DamagePreview";
        private ObjectPool<DamagePreviewText> _pool;

        public async void Handle(DamageTaken message)
        {
            DamagePreviewText preview = _pool.Get();

            preview.Initialize(message.Amount, Transform);
            await preview.Animate(duration: 0.5f);

            _pool?.Release(preview);
        }

        protected override void OnDestroy()
        {
            _pool?.Dispose();
            _pool = null;
        }

        protected override async void OnInitialize()
        {
            if (Behaviour.IsLocalPlayer)
            {
                IsEnabled = false;
                return;
            }

            GameObject resource = await _loader.LoadAsync<GameObject>(_address);
            DamagePreviewText prefab = resource.GetComponent<DamagePreviewText>();

            _pool = new ObjectPool<DamagePreviewText>(
                createFunc: () => Object.Instantiate(prefab),
                actionOnGet: x => x.gameObject.SetActive(true),
                actionOnRelease: x => x.gameObject.SetActive(false),
                actionOnDestroy: x => Object.Destroy(x.gameObject),
                collectionCheck: false,
                defaultCapacity: 5,
                maxSize: 50);

            Behaviour.RegisterMessageHandler(this);
        }
    }
}
