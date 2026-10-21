using NUnit.Framework;
using Tanks.UI.Menus.Loading;

namespace Tanks.UI.Tests.Menus
{
    public class LoadingTests : ViewModelTestBase<LoadingViewModel>
    {
        protected override LoadingViewModel CreateViewModel() => new();

        [Test]
        public void Label_Updates()
        {
            ViewModel.Label.Value = "Joining lobby";
            Assert.AreEqual("Joining lobby", ViewModel.Label.Value);
        }
    }
}
