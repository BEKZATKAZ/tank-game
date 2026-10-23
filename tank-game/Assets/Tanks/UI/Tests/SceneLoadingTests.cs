using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Tanks.Core.Common.Scenes;
using Tanks.Localization;
using Tanks.UI.Loading;

namespace Tanks.UI.Tests
{
    public sealed class SceneLoadingTests : ViewModelTestBase<SceneLoadingViewModel>
    {
        private ISceneLoader _loader;
        private ILocalizedString _firstTip;
        private ILocalizedString _secondTip;
        private ILocalizedString _otherEntry;

        protected override SceneLoadingViewModel CreateViewModel()
        {
            _loader = Substitute.For<ISceneLoader>();
            _firstTip = CreateString("loading.tip.first", "First tip");
            _secondTip = CreateString("loading.tip.second", "Second tip");
            _otherEntry = CreateString("loading.title", "Loading");

            ILocalizationDatabase localization = Substitute.For<ILocalizationDatabase>();

            localization.GetAllFromTable("Loading")
                .Returns(new[] { _firstTip, _otherEntry, _secondTip });

            return new SceneLoadingViewModel(localization, _loader);
        }

        [Test]
        public void LoadingEvents_UpdateIsLoadingAcrossMultipleLoads()
        {
            Assert.True(ViewModel.IsLoading.Value);

            for (int i = 0; i < 2; i++)
            {
                _loader.LoadingEnded += Raise.Event<Action>();
                Assert.False(ViewModel.IsLoading.Value);

                _loader.LoadingStarted += Raise.Event<Action>();
                Assert.True(ViewModel.IsLoading.Value);
            }
        }

        [Test]
        public void WithTips_ReflectsEachLoadingRequest()
        {
            _loader.WithTips.Returns(false);
            Assert.False(ViewModel.WithTips);

            _loader.LoadingEnded += Raise.Event<Action>();
            _loader.WithTips.Returns(true);
            _loader.LoadingStarted += Raise.Event<Action>();
            Assert.True(ViewModel.WithTips);

            _loader.LoadingEnded += Raise.Event<Action>();
            _loader.WithTips.Returns(false);
            _loader.LoadingStarted += Raise.Event<Action>();
            Assert.False(ViewModel.WithTips);
        }

        [TestCase(0f, 0.8f, 0.1f, 0.2f)]
        [TestCase(0.4f, 0.8f, 0.1f, 0.6f)]
        [TestCase(0.7f, 0.8f, 1f, 0.8f)]
        [TestCase(0.8f, 0.1f, 0.01f, 0.1f)]
        [TestCase(0.8f, 0f, 0f, 0f)]
        [TestCase(0.4f, 0.8f, 0f, 0.4f)]
        public void Tick_UpdatesProgress(float initial, float target, float deltaTime, float expected)
        {
            ViewModel.Progress.Value = initial;
            _loader.Progress.Returns(target);

            ViewModel.Tick(deltaTime);
            Assert.AreEqual(expected, ViewModel.Progress.Value, delta: 0.0001f);
        }

        [Test]
        public async Task GetNextTipAsync_ReturnsAllTipsWithoutOtherEntries()
        {
            string first = await ViewModel.GetNextTipAsync();
            string second = await ViewModel.GetNextTipAsync();

            CollectionAssert.AreEquivalent(
                new[] { "First tip", "Second tip" },
                new[] { first, second });

            await _otherEntry.DidNotReceive().GetAsync();
        }

        [Test]
        public async Task GetNextTipAsync_RepeatsAfterLastTip()
        {
            string first = await ViewModel.GetNextTipAsync();
            string second = await ViewModel.GetNextTipAsync();

            Assert.AreEqual(first, await ViewModel.GetNextTipAsync());
            Assert.AreEqual(second, await ViewModel.GetNextTipAsync());
        }

        [Test]
        public async Task GetNextTipAsync_WaitsForLocalizedString()
        {
            var completion = new UniTaskCompletionSource<string>();
            _firstTip.GetAsync().Returns(_ => completion.Task);
            _secondTip.GetAsync().Returns(_ => completion.Task);

            Task<string> result = ViewModel.GetNextTipAsync().AsTask();
            Assert.False(result.IsCompleted);

            completion.TrySetResult("Localized tip");
            Assert.AreEqual("Localized tip", await result);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Dispose_UnsubscribesFromLoadingEvents(bool isLoading)
        {
            ViewModel.IsLoading.Value = isLoading;
            ViewModel.Dispose();

            if (isLoading) _loader.LoadingEnded += Raise.Event<Action>();
            else _loader.LoadingStarted += Raise.Event<Action>();

            Assert.AreEqual(isLoading, ViewModel.IsLoading.Value);
        }

        private static ILocalizedString CreateString(string entryName, string value)
        {
            ILocalizedString localized = Substitute.For<ILocalizedString>();
            localized.EntryName.Returns(entryName);
            localized.GetAsync().Returns(_ => UniTask.FromResult(value));
            return localized;
        }
    }
}
