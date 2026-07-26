using Tanks.Shared.Health;
using Tanks.Shared.Messaging;

namespace Tanks.UI.Gameplay.Eliminated
{
    public sealed class EliminatedPresenter :
        PresenterBase<GameplayDocument, EliminatedView, EliminatedModel>,
        IMessageHandler<DiedLocal>
    {
        public EliminatedPresenter(IMessageBus bus)
        {
            _bus = bus;
            _bus.RegisterHandler<DiedLocal>(this);
        }

        private readonly IMessageBus _bus;
        
        public void Handle(DiedLocal message, IMessageSender sender)
        {
            View.Show(new EliminatedModel());
        }
    }
}