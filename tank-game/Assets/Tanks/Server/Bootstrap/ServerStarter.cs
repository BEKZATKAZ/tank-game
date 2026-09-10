using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using VContainer;

namespace Tanks.Server.Bootstrap
{
    public class ServerStarter : MonoBehaviour
    {
        [Inject] private readonly IVirtualMachineService _vmService;

        private void Start()
        {
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            transport.SetConnectionData("0.0.0.0", _vmService.GetListeningPort());

            NetworkManager.Singleton.StartServer();
        }
    }
}
