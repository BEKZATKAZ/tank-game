using NUnit.Framework;
using Tanks.UI.Menus.Main;
using UnityEngine;

namespace Tanks.UI.Tests.Menus
{
    public class CopyrightTests : ViewModelTestBase<CopyrightViewModel>
    {
        protected override CopyrightViewModel CreateViewModel()
        {
            return new CopyrightViewModel();
        }

        [Test]
        public void Constructor_LabelContainsCopyrightCompanyAndVersion()
        {
            Assert.AreEqual(
                $"© 2026 {Application.companyName} v{Application.version}",
                ViewModel.Label.Value);
        }

        [Test]
        public void Bind_ImmediatelyProvidesCopyrightLabel()
        {
            string label = null;
            int updates = 0;

            ViewModel.Label.Bind(value =>
            {
                label = value;
                updates++;
            });

            Assert.AreEqual(ViewModel.Label.Value, label);
            Assert.AreEqual(1, updates);
        }
    }
}
