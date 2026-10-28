using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Common;
using Tanks.Core.Common.Scenes;
using Tanks.Core.Repositories;
using Tanks.UI.Entry;
using Tanks.UI.Entry.AccountSetup;
using Tanks.UI.Menus.Dialogs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;

namespace Tanks.UI.Tests
{
    public class PrivacyPolicyTests
    {
        private const string PolicyText = "# Privacy Policy\n**Game:** T.A.N.K.S.\n**Last updated:** October 28, 2026\n## 12. Contact Us\nbekzatkaz.games@gmail.com";

        private IAuthService _auth;
        private IPrivacyPolicyService _privacy;
        private AccountSetupViewModel _viewModel;
        private PrivacyPolicy _policy;

        [SetUp]
        public void Setup()
        {
            _policy = PrivacyPolicyService.Parse(PolicyText);
            _privacy = Substitute.For<IPrivacyPolicyService>();
            _privacy.FetchAsync(Arg.Any<CancellationToken>()).Returns(UniTask.FromResult(_policy));

            _auth = Substitute.For<IAuthService>();
            _auth.InitializeAsync().Returns(UniTask.CompletedTask);
            _auth.SignInWithDeviceIdAsync().Returns(UniTask.FromResult(Result.Success(new PlayerProfileResult
            {
                UserId = "player",
                DisplayName = "Tank"
            })));

            _viewModel = new AccountSetupViewModel(_auth, Substitute.For<IResourceLoader>(),
                Substitute.For<IDialogScreenService>(), Substitute.For<ISceneLoader>(), _privacy);
        }

        [TearDown]
        public void TearDown()
        {
            _viewModel.Dispose();
        }

        [Test]
        public async Task FirstLaunch_BlocksInitializationAndSignIn()
        {
            Assert.IsFalse(await _viewModel.LoginAsync());

            Assert.IsTrue(_viewModel.IsReviewingPolicy.Value);
            Assert.AreEqual(PolicyText, _viewModel.PolicyText.Value);
            await _auth.DidNotReceive().InitializeAsync();
            await _auth.DidNotReceive().SignInWithDeviceIdAsync();
        }

        [Test]
        public async Task Decline_DoesNotAcceptOrSignIn()
        {
            await _viewModel.LoginAsync();
            Assert.IsFalse(await _viewModel.AcceptPolicyAsync(false));

            _privacy.DidNotReceive().Accept(Arg.Any<PrivacyPolicy>());
            await _auth.DidNotReceive().InitializeAsync();
        }

        [Test]
        public async Task Acceptance_IsSavedBeforeInitializationAndSignIn()
        {
            await _viewModel.LoginAsync();
            Assert.IsTrue(await _viewModel.AcceptPolicyAsync(true));

            Received.InOrder(() =>
            {
                _privacy.Accept(_policy);
                _auth.InitializeAsync();
                _auth.SignInWithDeviceIdAsync();
            });
        }

        [Test]
        public async Task AcceptedPolicy_StillFetchesBeforeAuthentication()
        {
            _privacy.IsAccepted(_policy).Returns(true);

            Assert.IsTrue(await _viewModel.LoginAsync());
            Assert.IsFalse(_viewModel.IsReviewingPolicy.Value);

            Received.InOrder(() =>
            {
                _privacy.FetchAsync(Arg.Any<CancellationToken>());
                _auth.InitializeAsync();
                _auth.SignInWithDeviceIdAsync();
            });
        }

        [Test]
        public async Task ChangedPolicy_RequiresAgreementAgain()
        {
            _privacy.IsAccepted(_policy).Returns(true);
            PrivacyPolicy changed = PrivacyPolicyService.Parse(PolicyText + "\nUpdated.");
            _privacy.FetchAsync(Arg.Any<CancellationToken>()).Returns(UniTask.FromResult(changed));

            Assert.IsFalse(await _viewModel.LoginAsync());
            Assert.IsTrue(_viewModel.IsReviewingPolicy.Value);
            await _auth.DidNotReceive().InitializeAsync();
        }

        [Test]
        public async Task FailedCheck_BlocksEvenPreviouslyAcceptedPolicy_AndCanRetry()
        {
            _privacy.IsAccepted(_policy).Returns(true);
            _privacy.FetchAsync(Arg.Any<CancellationToken>()).Returns(
                UniTask.FromException<PrivacyPolicy>(new InvalidOperationException("Offline")));

            Assert.IsFalse(await _viewModel.LoginAsync());
            Assert.IsTrue(_viewModel.HasPolicyError.Value);
            await _auth.DidNotReceive().InitializeAsync();

            _privacy.FetchAsync(Arg.Any<CancellationToken>()).Returns(UniTask.FromResult(_policy));

            Assert.IsTrue(await _viewModel.LoginAsync());
            Assert.IsFalse(_viewModel.HasPolicyError.Value);
        }

        [Test]
        public async Task RepeatedAcceptance_DoesNotSignInTwice()
        {
            await _viewModel.LoginAsync();
            await _viewModel.AcceptPolicyAsync(true);

            Assert.IsFalse(await _viewModel.AcceptPolicyAsync(true));
            _privacy.Received(1).Accept(_policy);
            await _auth.Received(1).SignInWithDeviceIdAsync();
        }

        [Test]
        public async Task AcceptanceInProgress_RejectsDuplicateSubmission()
        {
            var pending = new UniTaskCompletionSource();
            _auth.InitializeAsync().Returns(pending.Task);
            await _viewModel.LoginAsync();

            UniTask<bool> acceptance = _viewModel.AcceptPolicyAsync(true);
            Assert.IsFalse(await _viewModel.AcceptPolicyAsync(true));
            pending.TrySetResult();

            Assert.IsTrue(await acceptance);
            _privacy.Received(1).Accept(_policy);
            await _auth.Received(1).SignInWithDeviceIdAsync();
        }

        [Test]
        public async Task NewAccount_ChoosesNameOnlyAfterAcceptance()
        {
            _auth.SignInWithDeviceIdAsync().Returns(UniTask.FromResult(Result.Success(new PlayerProfileResult
            {
                UserId = "player",
                DisplayName = ""
            })));

            await _viewModel.LoginAsync();
            Assert.IsFalse(_viewModel.IsChoosingDisplayName.Value);
            Assert.IsFalse(await _viewModel.AcceptPolicyAsync(true));

            Assert.IsTrue(_viewModel.IsChoosingDisplayName.Value);
            Assert.IsFalse(_viewModel.IsReviewingPolicy.Value);
        }

        [Test]
        public async Task FailedSignIn_OffersRetry()
        {
            _privacy.IsAccepted(_policy).Returns(true);
            _auth.SignInWithDeviceIdAsync().Returns(UniTask.FromResult(Result.Failure<PlayerProfileResult>("unknown")));

            Assert.IsFalse(await _viewModel.LoginAsync());
            Assert.IsTrue(_viewModel.HasPolicyError.Value);
            Assert.IsFalse(_viewModel.IsLoading.Value);
        }

        [Test]
        public async Task DisposalDuringFetch_CancelsAndPreventsAuthentication()
        {
            var pending = new UniTaskCompletionSource<PrivacyPolicy>();
            CancellationToken token = default;
            _privacy.FetchAsync(Arg.Any<CancellationToken>()).Returns(call =>
            {
                token = call.Arg<CancellationToken>();
                return pending.Task;
            });
            _privacy.IsAccepted(_policy).Returns(true);

            UniTask<bool> login = _viewModel.LoginAsync();
            Assert.IsFalse(await _viewModel.LoginAsync());
            _viewModel.Dispose();
            pending.TrySetResult(_policy);

            Assert.IsTrue(token.IsCancellationRequested);
            Assert.IsFalse(await login);
            await _auth.DidNotReceive().InitializeAsync();
        }

        [Test]
        public async Task DisposalDuringInitialization_PreventsSignIn()
        {
            var pending = new UniTaskCompletionSource();
            _auth.InitializeAsync().Returns(pending.Task);
            _privacy.IsAccepted(_policy).Returns(true);

            UniTask<bool> login = _viewModel.LoginAsync();
            _viewModel.Dispose();
            pending.TrySetResult();

            Assert.IsFalse(await login);
            await _auth.DidNotReceive().SignInWithDeviceIdAsync();
        }

        [Test]
        public async Task ProfileSetup_CannotBypassPolicyGate()
        {
            Assert.IsFalse(await _viewModel.ConfirmAsync());
            await _auth.DidNotReceive().SetupProfileAsync(Arg.Any<string>());
        }

        [TestCase("")]
        [TestCase("<html>Service unavailable</html>")]
        [TestCase("# Privacy Policy\nTruncated")]
        public void InvalidDocument_IsRejected(string text)
        {
            Assert.Throws<FormatException>(() => PrivacyPolicyService.Parse(text));
        }

        [Test]
        public void Acceptance_PersistsAcrossServiceInstances_AndChangesInvalidateIt()
        {
            bool hadValue = PlayerPrefs.HasKey(PrivacyPolicyService.AcceptanceKey);
            string previous = PlayerPrefs.GetString(PrivacyPolicyService.AcceptanceKey);

            try
            {
                PlayerPrefs.DeleteKey(PrivacyPolicyService.AcceptanceKey);
                var service = new PrivacyPolicyService();
                Assert.IsFalse(service.IsAccepted(_policy));

                service.Accept(_policy);

                Assert.IsTrue(new PrivacyPolicyService().IsAccepted(PrivacyPolicyService.Parse(PolicyText)));
                Assert.IsFalse(service.IsAccepted(PrivacyPolicyService.Parse(PolicyText + "\nUpdated.")));
                Assert.AreEqual(64, _policy.Hash.Length);
            }
            finally
            {
                if (hadValue) PlayerPrefs.SetString(PrivacyPolicyService.AcceptanceKey, previous);
                else PlayerPrefs.DeleteKey(PrivacyPolicyService.AcceptanceKey);

                PlayerPrefs.Save();
            }
        }

        [Test]
        public void EntryDocument_HasScrollablePolicyAndSeparateAccountSetup()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Tanks/UI/Entry/Entry.uxml");
            TemplateContainer root = asset.CloneTree();

            Assert.IsNotNull(root.Q<ScrollView>("PolicyScroll"));
            Assert.IsFalse(root.Q<Label>("PolicyText").enableRichText);
            Assert.IsFalse(root.Q<Toggle>("PolicyConsent").value);
            Assert.IsNotNull(root.Q<Button>("PolicyContinue"));
            Assert.IsNotNull(root.Q<Button>("PolicyRetry"));
            Assert.IsNotNull(root.Q<Button>("PolicyQuit"));
            Assert.IsNull(root.Q("AccountSetup").Q<Toggle>());
        }

        [Test]
        public void ProjectPolicy_PassesDownloadValidation()
        {
            PrivacyPolicy policy = PrivacyPolicyService.Parse(File.ReadAllText("privacy-policy.md"));

            Assert.IsNotEmpty(policy.Hash);
            StringAssert.Contains("Vivox", policy.Text);
            StringAssert.Contains("Cloud Code", policy.Text);
            StringAssert.Contains("Cloud Save", policy.Text);
        }

        [TestCase("en")]
        [TestCase("ru")]
        [TestCase("fr")]
        public void EntryControls_AreLocalized(string locale)
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>(
                $"Assets/Tanks/Localization/Tables/Entry/Entry_{locale}.asset");

            foreach (string key in new[] { "title", "loading", "error", "agreement", "continue", "retry", "quit" })
            {
                StringTableEntry entry = table.GetEntry("entry.privacy." + key);
                Assert.IsNotNull(entry, key);
                Assert.IsNotEmpty(entry.LocalizedValue, key);
            }
        }
    }
}
