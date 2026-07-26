using System;

namespace Tanks.UI
{
    public interface IDocumentPresenter { }
    public interface IDocumentPresenter<T> : IDocumentPresenter where T : IDocument { }

    public abstract class PresenterBase : IDocumentPresenter
    {
        public abstract void InitializeView(IView view);
        public abstract IView CreateAndInitializeView();

        public virtual void OnUpdate(float deltaTime) { }
        public virtual void OnLateUpdate(float deltaTime) { }
    }

    public abstract class PresenterBase<TDocument, TView, TModel> :
        PresenterBase,
        IDocumentPresenter<TDocument>
        where TDocument : IDocument
        where TView : IView<TModel>
    {
        public IView<TModel> View { get; private set; }

        public override sealed IView CreateAndInitializeView()
        {
            return View = Activator.CreateInstance<TView>();
        }

        public override sealed void InitializeView(IView view)
        {
            View = view as IView<TModel>;
        }
    }
}
