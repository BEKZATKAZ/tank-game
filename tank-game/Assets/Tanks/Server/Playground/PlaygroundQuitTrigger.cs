using Unity.Netcode;
using UnityEngine;

namespace Tanks.Server.Playground
{
    public sealed class PlaygroundQuitTrigger : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.TryGetComponent(out NetworkObject other) == false) return;
            if (other.OwnerClientId == 0) return;

            NetworkManager.Singleton.Shutdown(discardMessageQueue: true);
            Application.Quit();
        }
    }
}
