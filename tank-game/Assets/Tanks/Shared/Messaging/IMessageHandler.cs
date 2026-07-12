namespace Tanks.Shared.Messaging
{
    public interface IMessageHandler { }

    public interface IMessageHandler<T> : IMessageHandler
    {
        void Handle(T message, IMessageSender sender);
    }
}
