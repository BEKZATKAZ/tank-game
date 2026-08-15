using NSubstitute;
using NUnit.Framework;
using Tanks.Localization;
using Tanks.UI.Menus.Dialogs;

namespace Tanks.UI.Tests.Menus
{
    public class DialogScreenTests : ViewModelTestBase<DialogScreenViewModel>
    {
        private ILocalizationDatabase _localization;

        private void MockLocalizationReturn(string tableName, string entryName, string expected)
        {
            _localization.TryGet(tableName, entryName, out _)
                .Returns(x =>
                {
                    ILocalizedString result = Substitute.For<ILocalizedString>();
                    result.Get().Returns(expected);

                    x[2] = result;
                    return true;
                });
        }

        protected override DialogScreenViewModel CreateViewModel()
        {
            _localization = Substitute.For<ILocalizationDatabase>();
            return new DialogScreenViewModel(_localization);
        }

        [TestCase("quit")]
        [TestCase("action")]
        public void Confirmation_FiresYesAndNo(string code)
        {
            MockLocalizationReturn("Confirmations", $"confirm.{code}.message", "Some message");
            MockLocalizationReturn("Confirmations", $"confirm.{code}.header", "Some header");

            int state = 0;
            ViewModel.ShowConfirmation(code, () => state = 1, () => state = 2);

            Assert.AreEqual(DialogType.Confirmation, ViewModel.Type.Value, "Invalid dialog type");
            Assert.AreEqual("Some message", ViewModel.Message.Value, "Invalid message");
            Assert.AreEqual("Some header", ViewModel.Header.Value, "Invalid header");

            ViewModel.ConfirmationYes();
            Assert.AreEqual(1, state, "Should invoke YES");

            ViewModel.ShowConfirmation(code, () => state = 1, () => state = 2);
            ViewModel.ConfirmationNo();
            Assert.AreEqual(2, state, "Should invoke NO");
        }

        [Test]
        public void Confirmation_NoCallbacks_FiresNothing()
        {
            ViewModel.ShowConfirmation("quit");
            ViewModel.ConfirmationYes();

            ViewModel.ShowConfirmation("quit");
            ViewModel.ConfirmationNo();
        }

        [TestCase("lobby-not-found")]
        [TestCase("lobby-not-joinable")]
        public void Error(string code)
        {
            MockLocalizationReturn("Errors", $"error.lobby.{code}", "Some error");
            ViewModel.ShowErrorFromCode("error.lobby", code);
            Assert.AreEqual("Some error", ViewModel.Message.Value, "Invalid message");
        }
    }
}
