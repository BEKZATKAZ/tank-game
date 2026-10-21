using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tanks.UI.SplashScreen
{
    public sealed class DelayedSceneLoader : MonoBehaviour
    {
        [Scene]
        [SerializeField]
        private string _sceneName;

        [Range(1, 10)]
        [SerializeField]
        private float _delaySeconds = 7;

        private void OpenScene()
        {
            SceneManager.LoadScene(_sceneName);
        }

        private void Start()
        {
            Invoke(nameof(OpenScene), _delaySeconds);
        }
    }
}
