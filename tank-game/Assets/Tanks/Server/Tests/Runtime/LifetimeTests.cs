using NUnit.Framework;
using System.Collections;
using Tanks.Server.Bootstrap;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tanks.Server.Tests.Runtime.Bootstrap
{
    public class LifetimeTests
    {
        private UnityServerLifetimeService _component;

        [SetUp]
        public void Setup()
        {
            _component = new GameObject().AddComponent<UnityServerLifetimeService>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_component) Object.DestroyImmediate(_component.gameObject);
        }

        [UnityTest]
        public IEnumerator AllTimeouts()
        {
            // Switching to End to reset the current step
            _component.TrySetStep(GameStep.End);
            _component.SetLifetime(session: 2, end: 4);

            _component.TrySetStep(GameStep.Session);
            yield return TestTimeout(2);

            _component.TrySetStep(GameStep.End);
            yield return TestTimeout(4);

            _component.TrySetStep(GameStep.Session);
            yield return TestTimeout(2);

            _component.TrySetStep(GameStep.Session);
            Assert.True(_component.IsTimedOut, "Should not reset if given the same step");
        }

        private IEnumerator TestTimeout(int timeout)
        {
            float time = 0;

            while (time < timeout)
            {
                Assert.False(_component.IsTimedOut, $"Timed out too early: {time} / {timeout}");
                yield return null;
                time += Time.deltaTime;
            }

            Assert.True(_component.IsTimedOut,
                "This would be a disaster in a virtual machine");
        }
    }
}
