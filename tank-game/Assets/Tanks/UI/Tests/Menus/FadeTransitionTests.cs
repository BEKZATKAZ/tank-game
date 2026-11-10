using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Players;
using Tanks.Core.Common.Scenes;
using Tanks.UI.Menus.Transitions;

namespace Tanks.UI.Tests.Menus
{
    public class FadeTransitionTests
    {
        [Test]
        public async Task Loading_DoesNotEndBeforeDataArrives()
        {
            var loader = Substitute.For<ISceneLoader>();
            loader.IsLoading.Returns(true);
            var player = Substitute.For<ILocalPlayerService>();
            var pending = new TaskCompletionSource<ResultType>();
            player.LoadDataAsync().Returns(pending.Task);
            var state = new MenuLoadingState();
            using var model = new FadeTransitionViewModel(loader, player, state);

            Task loading = model.InitializeAsync();
            loader.DidNotReceive().EndLoading<LoadMenus>();
            Assert.False(state.IsReady.Value);

            pending.SetResult(Result.Success());
            await loading;

            loader.Received(1).EndLoading<LoadMenus>();
            Assert.True(state.IsReady.Value);
        }

        [Test]
        public async Task Failure_KeepsLoadingBlockedUntilSuccessfulRetry()
        {
            var loader = Substitute.For<ISceneLoader>();
            loader.IsLoading.Returns(true);
            var player = Substitute.For<ILocalPlayerService>();
            player.LoadDataAsync().Returns(Result.Failure("unknown"), Result.Success());
            var state = new MenuLoadingState();
            using var model = new FadeTransitionViewModel(loader, player, state);

            await model.InitializeAsync();
            Assert.True(state.HasError.Value);
            Assert.False(state.IsReady.Value);
            loader.DidNotReceive().EndLoading<LoadMenus>();

            state.Retry();

            Assert.False(state.HasError.Value);
            Assert.True(state.IsReady.Value);
            loader.Received(1).EndLoading<LoadMenus>();
        }

        [Test]
        public async Task DisposedMenu_IgnoresLateLoadCompletion()
        {
            var loader = Substitute.For<ISceneLoader>();
            var player = Substitute.For<ILocalPlayerService>();
            var pending = new TaskCompletionSource<ResultType>();
            player.LoadDataAsync().Returns(pending.Task);
            var state = new MenuLoadingState();
            var model = new FadeTransitionViewModel(loader, player, state);

            Task loading = model.InitializeAsync();
            model.Dispose();
            pending.SetResult(Result.Success());
            await loading;

            Assert.False(state.IsReady.Value);
            loader.DidNotReceive().EndLoading<LoadMenus>();
        }
    }
}
