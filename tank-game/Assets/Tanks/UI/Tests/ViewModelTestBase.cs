using NUnit.Framework;
using System;

namespace Tanks.UI.Tests
{
    public abstract class ViewModelTestBase<TViewModel>
    {
        protected TViewModel ViewModel { get; private set; }

        protected abstract TViewModel CreateViewModel();

        [SetUp]
        public virtual void Setup()
        {
            ViewModel = CreateViewModel();
        }

        [TearDown]
        public void TearDown()
        {
            if (ViewModel is IDisposable disposable) disposable.Dispose();
        }
    }
}
