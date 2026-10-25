using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using System;
using Tanks.Core.Settings;
using Tanks.Localization;
using Tanks.UI.Menus.Settings;

namespace Tanks.UI.Tests.Menus
{
    public class GeneralSettingsApplierTests
    {
        private ISettingsService _settings;
        private ILocalizationDatabase _localization;
        private UniTaskCompletionSource _initialization;
        private GeneralSettingsApplier _applier;
        private Action<int> _languageChanged;

        [SetUp]
        public void Setup()
        {
            _settings = Substitute.For<ISettingsService>();
            _localization = Substitute.For<ILocalizationDatabase>();
            _initialization = new UniTaskCompletionSource();

            _localization.WaitForInitializationAsync().Returns(_initialization.Task);
            _settings.When(x => x.Bind(PlayerSetting.Language, Arg.Any<Action<int>>()))
                .Do(args =>
                {
                    _languageChanged = args.Arg<Action<int>>();
                    _languageChanged((int)GameLanguage.Russian);
                });

            _applier = new GeneralSettingsApplier(_settings, _localization);
        }

        [TearDown]
        public void TearDown()
        {
            _applier.Dispose();
            _initialization.TrySetResult();
        }

        [Test]
        public void Create_InitializationPending_DoesNotChangeLocale()
        {
            _localization.Received(1).WaitForInitializationAsync();
            _localization.DidNotReceiveWithAnyArgs().SetLocale(default);
        }

        [Test]
        public void InitializationCompleted_AppliesSavedLanguage()
        {
            _initialization.TrySetResult();

            _localization.Received(1).SetLocale("ru");
        }

        [Test]
        public void LanguageChanged_BeforeInitialization_AppliesOnlyLatestLanguage()
        {
            _languageChanged((int)GameLanguage.English);
            _languageChanged((int)GameLanguage.French);

            _localization.DidNotReceiveWithAnyArgs().SetLocale(default);
            _initialization.TrySetResult();

            _localization.Received(1).SetLocale("fr");
            _localization.DidNotReceive().SetLocale("en");
            _localization.DidNotReceive().SetLocale("ru");
        }

        [TestCase(GameLanguage.English, "en")]
        [TestCase(GameLanguage.Russian, "ru")]
        [TestCase(GameLanguage.French, "fr")]
        [TestCase((GameLanguage)99, "en")]
        public void LanguageChanged_AfterInitialization_AppliesImmediately(GameLanguage language, string locale)
        {
            _initialization.TrySetResult();
            _localization.ClearReceivedCalls();

            _languageChanged((int)language);

            _localization.Received(1).SetLocale(locale);
        }

        [Test]
        public void Dispose_BeforeInitialization_DoesNotApplyPendingLanguage()
        {
            _applier.Dispose();

            _initialization.TrySetResult();

            _localization.DidNotReceiveWithAnyArgs().SetLocale(default);
            _settings.Received(1).Unbind(PlayerSetting.Language, _languageChanged);
        }

        [Test]
        public void Dispose_Repeated_UnbindsOnceAndIgnoresFurtherChanges()
        {
            _applier.Dispose();
            _applier.Dispose();

            _initialization.TrySetResult();
            _languageChanged((int)GameLanguage.French);

            _settings.Received(1).Unbind(PlayerSetting.Language, _languageChanged);
            _localization.DidNotReceiveWithAnyArgs().SetLocale(default);
        }
    }
}
