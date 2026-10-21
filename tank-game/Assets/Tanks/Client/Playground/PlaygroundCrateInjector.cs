using Cysharp.Threading.Tasks;
using Tanks.Shared;
using Tanks.Shared.Items;
using Tanks.Shared.Playground;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Tanks.Client.Bootstrap
{
    public sealed class PlaygroundCrateInjector :
        NetworkComponent<IPlaygroundStartBehaviour>,
        INetworkMessageHandler<PlaygroundStartedFromServer>
    {
        public PlaygroundCrateInjector(IObjectResolver container)
        {
            _container = container;
        }

        private readonly IObjectResolver _container;

        public async void Handle(PlaygroundStartedFromServer message)
        {
            CrateBehaviour[] crates = null;

            while (crates is not { Length: > 0 })
            {
                await UniTask.Delay(100);
                crates = Object.FindObjectsByType<CrateBehaviour>();
            }

            foreach (CrateBehaviour obj in crates)
            {
                _container.InjectGameObject(obj.gameObject);
            }
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
