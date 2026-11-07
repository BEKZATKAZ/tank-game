using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine;

namespace Tanks.Infrastructure.Tests
{
    public sealed class UGSEnvironmentTests
    {
        [TestCase(false, false, "production")]
        [TestCase(false, true, "development")]
        [TestCase(true, false, "development")]
        [TestCase(true, true, "development")]
        public async Task Initialization_UsesSelectedEnvironment(
            bool isEditor, bool isDevelopmentBuild, string expected)
        {
            EnvironmentSettings production = JsonUtility.FromJson<EnvironmentSettings>(
                "{\"_environmentName\":\"production\"}");
            EnvironmentSettings development = JsonUtility.FromJson<EnvironmentSettings>(
                "{\"_environmentName\":\"development\"}");
            EnvironmentSettings selected = EnvironmentSettings.Select(
                production, development, isEditor, isDevelopmentBuild);

            InitializationOptions captured = null;
            var auth = new UGSAuthService(selected, options =>
            {
                captured = options;
                return UniTask.CompletedTask;
            });

            await auth.InitializeAsync();

            Assert.AreEqual(expected, selected.EnvironmentName);
            Assert.IsTrue(captured.TryGetOption("com.unity.services.core.environment-name", out string environment));
            Assert.AreEqual(expected, environment);
        }
    }
}
