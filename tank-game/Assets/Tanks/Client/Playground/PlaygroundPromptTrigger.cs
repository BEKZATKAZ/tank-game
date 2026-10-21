using Tanks.Shared.Messaging;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Tanks.Client.Playground
{
    public sealed class PlaygroundPromptTrigger : MonoBehaviour, IMessageSender
    {
        [SerializeField] private string _key;

        [Inject] private readonly IMessageBus _bus;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.TryGetComponent(out NetworkObject other) == false) return;
            if (other.IsLocalPlayer == false) return;

            _bus.Publish(new PlaygroundPromptRequested { Key = _key }, this);
            Destroy(gameObject);
        }
    }
}
