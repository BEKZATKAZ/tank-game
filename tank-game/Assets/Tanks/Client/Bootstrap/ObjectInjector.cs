using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Items;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Tanks.Client.Bootstrap
{
    public sealed class ObjectInjector :
        NetworkComponent<IGameStartBehaviour>,
        INetworkMessageHandler<GameStartedFromServer>
    {
        public ObjectInjector(IObjectResolver container)
        {
            _container = container;
        }

        private readonly IObjectResolver _container;

        public void Handle(GameStartedFromServer message)
        {
            CrateBehaviour[] crates = Object.FindObjectsByType<CrateBehaviour>();

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
