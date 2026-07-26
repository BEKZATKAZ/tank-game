using NSubstitute;
using NUnit.Framework;
using System;

namespace Tanks.UI.Tests
{
    public abstract class PresenterTestBase<TPresenter, TModel>
        where TPresenter : PresenterBase
    {
        protected TPresenter Presenter { get; private set; }
        protected TModel LatestShownModel { get; private set; }
        protected bool IsHidden { get; private set; }

        private IView<TModel> _view;

        protected abstract TPresenter CreatePresenter();

        [SetUp]
        public virtual void Setup()
        {
            _view = Substitute.For<IView<TModel>>();

            _view.When(x => x.Show(Arg.Any<TModel>()))
                .Do(call =>
                {
                    LatestShownModel = call.Arg<TModel>();
                    IsHidden = false;
                });

            _view.When(x => x.Hide())
                .Do(call => IsHidden = true);

            Presenter = CreatePresenter();
            Presenter.InitializeView(_view);
            IsHidden = true;
        }

        [TearDown]
        public void TearDown()
        {
            if (Presenter is IDisposable disposable) disposable.Dispose();
        }
    }
}
