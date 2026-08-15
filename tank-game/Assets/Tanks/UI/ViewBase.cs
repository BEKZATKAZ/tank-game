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
    public abstract class ViewBase : MonoBehaviour, IView
    {
        [SerializeField] private bool _showOnPlay = true;

        public bool ShowOnPlay => _showOnPlay;

        public abstract void Show();
        public abstract void Hide();
    }

    [RequireComponent(typeof(UIDocument))]
    public abstract class ViewBase<T> : ViewBase, IView<T> where T : IViewModel
    {
        [Inject] private readonly IObjectResolver _resolver;

        public T ViewModel { get; private set; }
        public bool IsVisible => GetRootElement().style.display != DisplayStyle.None;

        protected abstract VisualElement GetRootElement();
        protected abstract void Initialize(VisualElement root);

        protected virtual void Bind(T viewModel) { }
        protected virtual void Unbind(T viewModel) { }

        public override sealed void Show()
        {
            if (ViewModel != null)
            {
                Unbind(ViewModel);
                if (ViewModel is IDisposable disposable) disposable.Dispose();
            }

            ViewModel = _resolver.Resolve<T>();
            Bind(ViewModel);
            GetRootElement().style.display = DisplayStyle.Flex;
        }

        public override sealed void Hide()
        {
            GetRootElement().style.display = DisplayStyle.None;

            if (ViewModel is null) return;
            Unbind(ViewModel);

            if (ViewModel is IDisposable disposable) disposable.Dispose();
            ViewModel = default;
        }

        protected virtual void OnDisable()
        {
            Hide();
        }

        protected virtual void OnEnable()
        {
            // Stop too early initialization
            if (didStart == false) return;

            if (ShowOnPlay) Show();
            else Hide();
        }

        protected virtual void Start()
        {
            if (ShowOnPlay) Show();
            else Hide();
        }

        private void Awake()
        {
            Initialize(GetComponent<UIDocument>().rootVisualElement);
        }
    }
}
