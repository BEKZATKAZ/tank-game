using System;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Tanks.UI
{
    public interface IView
    {
        void Hide();
        void Show();
    }

    public interface IView<T> : IView where T : IViewModel
    {

    }

    [RequireComponent(typeof(UIDocument))]
    public abstract class ViewBase<T> : MonoBehaviour, IView<T> where T : IViewModel
    {
        [SerializeField] private bool _showOnPlay = true;

        [Inject] private readonly IObjectResolver _resolver;

        public T ViewModel { get; private set; }

        protected abstract VisualElement GetRootElement();
        protected abstract void Initialize(VisualElement root);

        protected virtual void Bind(T viewModel) { }
        protected virtual void Unbind(T viewModel) { }

        public void Show()
        {
            ViewModel = _resolver.Resolve<T>();
            Bind(ViewModel);
            GetRootElement().style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            GetRootElement().style.display = DisplayStyle.None;
            if (ViewModel is null) return;

            Unbind(ViewModel);
            if (ViewModel is IDisposable disposable) disposable.Dispose();
        }

        protected virtual void OnDisable()
        {
            Hide();
        }

        private void Start()
        {
            Initialize(GetComponent<UIDocument>().rootVisualElement);
            if (_showOnPlay) Show();
        }
    }
}