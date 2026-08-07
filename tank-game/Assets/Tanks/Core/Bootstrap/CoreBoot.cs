using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace Tanks.Core.Bootstrap
{
    public sealed class CoreBoot : MonoBehaviour
    {
        [Scene]
        [SerializeField]
        private string _menuSceneName;

        private async void Start()
        {
            IObjectResolver container = LifetimeScope.Find<LifetimeScope>().Container;
            IAuthService auth = container.Resolve<IAuthService>();
            ResultType result = await auth.LoginWithDeviceIdAsync();

            if (result.IsSuccess == false)
            {
                Debug.LogError($"Login failed with code: '{result.ErrorCode}'");
                Application.Quit(exitCode: 1);
            }

            await SceneManager.LoadSceneAsync(_menuSceneName);
        }
    }
}