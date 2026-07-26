using System;
using System.Linq;
using Tanks.Shared;
using Tanks.Utils;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VContainer.Unity;

namespace Tanks.UI
{
    public interface IDocument { }

    [RequireComponent(typeof(UIDocument))]
    public class UIDocumentExtended : MonoBehaviour, IDocument
    {
        private PresenterBase[] _presenters;

        protected virtual void Awake()
        {
            IObjectResolver resolver = LifetimeScope.Find<LifetimeScope>().Container;
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;

            _presenters = SplitComponentFinder
                .FindFor(this, typeof(IDocumentPresenter<>))
                .Select(x => resolver.CreateInjected(x) as PresenterBase)
                .ToArray();

            foreach (PresenterBase presenter in _presenters)
            {
                IView view = presenter.CreateAndInitializeView();
                view.Initialize(root);
            }
        }

        protected virtual void Update()
        {
            float deltaTime = Time.deltaTime;
            foreach (PresenterBase presenter in _presenters) presenter.OnUpdate(deltaTime);
        }

        protected virtual void LateUpdate()
        {
            float deltaTime = Time.deltaTime;
            foreach (PresenterBase presenter in _presenters) presenter.OnLateUpdate(deltaTime);
        }

        protected virtual void OnDestroy()
        {
            foreach (IDisposable disposable in _presenters.OfType<IDisposable>())
            {
                disposable.Dispose();
            }
        }
    }
}
