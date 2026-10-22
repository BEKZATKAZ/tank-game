using System;
using System.Collections;
using NUnit.Framework;
using Tanks.Core.Common.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tanks.Infrastructure.Tests.Runtime
{
    public class UnitySceneLoaderTests
    {
        private GameObject _gameObject;
        private UnitySceneLoader _loader;
        private bool _loadingSceneWasLoaded;

        [SetUp]
        public void Setup()
        {
            _loadingSceneWasLoaded = SceneManager.GetSceneByName("Loading").isLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            _gameObject = new GameObject("UnitySceneLoader");
            _loader = _gameObject.AddComponent<UnitySceneLoader>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_gameObject);
            yield return null;

            Scene loadingScene = SceneManager.GetSceneByName("Loading");
            if (_loadingSceneWasLoaded == false && loadingScene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(loadingScene);
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        [Test]
        public void EndLoading_WithoutStart_Throws()
        {
            Assert.False(_loader.IsLoading);
            Assert.Throws<Exception>(() => _loader.EndLoading<LoadMenus>());
        }

        [UnityTest]
        public IEnumerator StartLoading_WithoutActualScene_LoadsScreenAndKeepsActiveScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            LoadMenus parameters = new LoadMenus();
            int startedCount = 0;
            _loader.LoadingStarted += () => startedCount++;
            _loader.Progress = 0.3f;

            _loader.StartLoading(parameters, withTips: false, loadActualScene: false);
            yield return null;

            Assert.True(SceneManager.GetSceneByName("Loading").isLoaded);
            Assert.AreEqual(activeScene, SceneManager.GetActiveScene());
            Assert.AreEqual(1, startedCount);
            Assert.True(_loader.IsLoading);
            Assert.False(_loader.WithTips);
            Assert.AreEqual(0, _loader.Progress);
            Assert.AreEqual(parameters.Name, _loader.GetLoadingParameters<LoadMenus>().Name);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Loading") return;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                root.SetActive(false);
            }
        }
    }
}
