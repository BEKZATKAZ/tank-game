using Unity.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tanks.Editor
{
    public static class Boot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            MultiplayerRoleFlags role = MultiplayerRolesManager.ActiveMultiplayerRoleMask;

            if (role is MultiplayerRoleFlags.Client) return;
            if (SceneManager.GetActiveScene().name != "ClientBoot") return;

            SceneManager.LoadScene("Gameplay");
        }
    }
}
