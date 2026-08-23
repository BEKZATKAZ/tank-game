using LitMotion;
using Tanks.Shared;
using Tanks.Shared.Items;
using UnityEngine;
using VContainer;

namespace Tanks.Client.Items
{
    public sealed class ItemOwnerAccent :
        NetworkComponent<ItemOwnerBehaviour>,
        INetworkMessageHandler<ItemPickedUp>
    {
        [Inject] private readonly IClientItemDatabase _database;

        private MaterialPropertyBlock _block;
        private MotionHandle _motion;

        public void Handle(ItemPickedUp message)
        {
            IClientItemConfig config = _database.Get(message.ItemId);
            if (config.AccentColor.a == 0) return;

            _motion.TryCancel();
            _block.SetColor("_Color", config.AccentColor);
            Behaviour.Accent.gameObject.SetActive(true);

            _motion = LMotion.Create(1f, 0, duration: 0.4f)
                .WithOnComplete(() => Behaviour.Accent.gameObject.SetActive(false))
                .WithEase(Ease.InSine)
                .Bind(x =>
                {
                    if (Behaviour == false) return;
                    _block.SetFloat("_Intensity", x);
                    Behaviour.Accent.SetPropertyBlock(_block);
                });
        }

        protected override void OnInitialize()
        {
            _block = new MaterialPropertyBlock();
            Behaviour.RegisterMessageHandler(this);
            Behaviour.Accent.gameObject.SetActive(false);
        }
    }
}
