using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Common;
using Tanks.Core.Leaderboards;
using Tanks.Localization;
using Tanks.UI.Menus.Leaderboards;
using Tanks.UI.Menus.Sections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Tests.Menus
{
    public sealed class LeaderboardsTests
    {
        private ILeaderboardService _service;
        private LeaderboardsViewModel _viewModel;

        [SetUp]
        public void Setup()
        {
            _service = Substitute.For<ILeaderboardService>();
            var auth = Substitute.For<IAuthService>();
            var profile = new PlayerProfileState();
            profile.UserId.Value = "self";
            profile.DisplayName.Value = "Local";
            auth.CurrentProfile.Returns(profile);

            _viewModel = new LeaderboardsViewModel(_service, auth,
                Substitute.For<IMenuSectionService>(), Substitute.For<ILocalizationDatabase>());
        }

        [TearDown]
        public void TearDown() => _viewModel.Dispose();

        [Test]
        public async Task OldResponse_CannotReplaceNewSelection()
        {
            var oldResponse = new UniTaskCompletionSource<ResultType<LeaderboardPage>>();
            _service.GetPageAsync(LeaderboardMetric.Eliminations, LeaderboardCategory.Total, 0)
                .Returns(oldResponse.Task);
            _service.GetPageAsync(LeaderboardMetric.PickedUpItems, LeaderboardCategory.Daily, 0)
                .Returns(UniTask.FromResult(Result.Success(new LeaderboardPage { Total = 5 })));

            UniTask oldRequest = _viewModel.RefreshAsync();
            await _viewModel.SelectAsync(LeaderboardMetric.PickedUpItems, LeaderboardCategory.Daily);
            oldResponse.TrySetResult(Result.Success(new LeaderboardPage { Total = 100 }));
            await oldRequest;

            Assert.AreEqual(5, _viewModel.Total);
            Assert.IsFalse(_viewModel.IsLoading.Value);
        }

        [Test]
        public async Task PeriodReset_ReturnsToFirstPageAndKeepsOwnEntryPinned()
        {
            var own = new LeaderboardScoreEntry { PlayerId = "self", Rank = 100, Score = 5 };
            _service.GetPageAsync(LeaderboardMetric.Eliminations, LeaderboardCategory.Total, 0)
                .Returns(UniTask.FromResult(Result.Success(new LeaderboardPage { Total = 100, OwnEntry = own })));
            _service.GetPageAsync(LeaderboardMetric.Eliminations, LeaderboardCategory.Total, 1)
                .Returns(UniTask.FromResult(Result.Success(new LeaderboardPage())));

            await _viewModel.RefreshAsync();
            Assert.AreSame(own, _viewModel.OwnEntry.Value);
            Assert.IsEmpty(_viewModel.Entries.Value);

            await _viewModel.ChangePageAsync(1);

            Assert.AreEqual(0, _viewModel.Page);
            Assert.AreSame(own, _viewModel.OwnEntry.Value);
        }

        [Test]
        public async Task Dispose_IgnoresPendingResponse()
        {
            var pending = new UniTaskCompletionSource<ResultType<LeaderboardPage>>();
            _service.GetPageAsync(Arg.Any<LeaderboardMetric>(), Arg.Any<LeaderboardCategory>(), 0).Returns(pending.Task);

            UniTask request = _viewModel.RefreshAsync();
            _viewModel.Dispose();
            pending.TrySetResult(Result.Success(new LeaderboardPage { Total = 100 }));
            await request;

            Assert.AreEqual(0, _viewModel.Total);
            Assert.IsNull(_viewModel.OwnEntry.Value);
        }

        [Test]
        public async Task Failure_ShowsRecoverableErrorAndEmptyPageShowsEmptyState()
        {
            _service.GetPageAsync(Arg.Any<LeaderboardMetric>(), Arg.Any<LeaderboardCategory>(), 0)
                .Returns(UniTask.FromResult(Result.Failure<LeaderboardPage>("unavailable")));

            await _viewModel.RefreshAsync();
            Assert.IsFalse(_viewModel.IsLoading.Value);
            Assert.IsNotEmpty(_viewModel.Status.Value);
            Assert.IsFalse(_viewModel.CanGoNext);

            _service.GetPageAsync(Arg.Any<LeaderboardMetric>(), Arg.Any<LeaderboardCategory>(), 0)
                .Returns(UniTask.FromResult(Result.Success(new LeaderboardPage())));

            await _viewModel.RefreshAsync();
            Assert.AreEqual("No scores yet.", _viewModel.Status.Value);
            Assert.AreEqual("1 / 1", _viewModel.PageLabel.Value);
        }

        [Test]
        public void FlagDatabase_EveryFlagResolvesAndUnknownUsesNeutralPlaceholder()
        {
            FlagDatabase flags = AssetDatabase.LoadAssetAtPath<FlagDatabase>("Assets/Tanks/ScriptableObjects/FlagDatabase.asset");
            Assert.IsNotNull(flags);

            foreach (string path in System.IO.Directory.GetFiles(Application.dataPath + "/Tanks/Art/Flags", "*.png"))
            {
                string code = System.IO.Path.GetFileNameWithoutExtension(path);
                Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tanks/Art/Flags/" +
                    System.IO.Path.GetFileName(path)), flags.GetFlag(code), code);
                Assert.IsNotNull(flags.GetFlag(code), code);
            }

            Assert.IsNull(flags.GetFlag("unknown"));
            var row = new LeaderboardRow(new LeaderboardScoreEntry
            {
                PlayerName = "Player", CountryCode = "", Rank = 1, Score = 42
            }, flags, true, "Player", "Unranked");

            Assert.AreEqual("Player", row.Q<Label>("PlayerName").text);
            Assert.AreEqual("42", row.Q<Label>("Score").text);
            Assert.IsTrue(row.Q<Image>("Flag").ClassListContains("leaderboards__flag--unknown"));
            Assert.IsTrue(row.ClassListContains("leaderboards__row--local"));
        }

        [Test]
        public void MenuTemplate_ContainsSelectorsPagedTableAndPinnedRow()
        {
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Tanks/UI/Menus/Menus.uxml");
            VisualElement root = template.CloneTree().Q("LeaderboardsSection");

            Assert.IsNotNull(root.Q<DropdownField>("LeaderboardMetric"));
            Assert.IsNotNull(root.Q<DropdownField>("LeaderboardCategory"));
            Assert.IsNotNull(root.Q<ScrollView>("LeaderboardEntries"));
            Assert.IsNotNull(root.Q("OwnEntry"));
            Assert.IsNotNull(root.Q<Button>("NextPageButton"));
        }

        [Test]
        public void MenuScene_RegistersLeaderboardsAndAssignsFlagDatabase()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Tanks/Scenes/Menus.unity");

            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                LeaderboardsView view = roots.SelectMany(x => x.GetComponentsInChildren<LeaderboardsView>(true)).Single();
                MenuSectionService navigation = roots.SelectMany(x => x.GetComponentsInChildren<MenuSectionService>(true)).Single();
                var sections = (Dictionary<MenuSectionType, ViewBase>)typeof(MenuSectionService)
                    .GetField("_sections", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(navigation);

                Assert.AreSame(view, sections[MenuSectionType.Leaderboards]);
                Assert.IsNotNull(new SerializedObject(view).FindProperty("_flags").objectReferenceValue);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
