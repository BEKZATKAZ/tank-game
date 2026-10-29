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
        public VisualElement Root => GetRootElement();
        public bool IsVisible => Root.style.display != DisplayStyle.None;

        private bool _hasInitialized;
        private VisualElement _previousFocus;

        protected abstract VisualElement GetRootElement();
        protected abstract void Initialize(VisualElement root);
        protected virtual void OnDestroy() { }

        protected virtual void Bind(T viewModel) { }
        protected virtual void Unbind(T viewModel) { }

        public override sealed void Show()
        {
            _hasInitialized = true;

            if (ViewModel != null)
            {
                Unbind(ViewModel);
                if (ViewModel is IDisposable disposable) disposable.Dispose();
            }

            ViewModel = _resolver.Resolve<T>();
            Bind(ViewModel);
            GetRootElement().style.display = DisplayStyle.Flex;
            _previousFocus = Root.panel?.focusController.focusedElement as VisualElement;
            Root.schedule.Execute(FocusFirstControl);
        }

        public override sealed void Hide()
        {
            _hasInitialized = true;
            GetRootElement().style.display = DisplayStyle.None;
            if (CanFocus(_previousFocus))
            {
                _previousFocus.Focus();
            }

            if (ViewModel is null) return;
            Unbind(ViewModel);

            if (ViewModel is IDisposable disposable) disposable.Dispose();
            ViewModel = default;
        }

        protected virtual void OnDisable()
        {
            if (_hasInitialized) Hide();
        }

        protected virtual void OnEnable()
        {
            // Stop too early initialization
            if (didStart == false) return;

            if (ShowOnPlay) Show();
            else Hide();
        }

        protected virtual void Update()
        {
            if (_hasInitialized && ViewModel != null && Root.panel != null)
            {
                var focused = Root.panel.focusController.focusedElement as VisualElement;
                if (CanFocus(focused) == false)
                {
                    FocusFirstControl();
                }
            }

            if (_hasInitialized) return;
            if (_resolver is null) return;

            if (ShowOnPlay) Show();
            else Hide();
        }

        protected virtual void FocusFirstControl()
        {
            if (Root.panel == null) return;

            VisualElement first = null;
            Root.Query<VisualElement>().ForEach(element =>
            {
                if (first != null || CanFocus(element) == false) return;
                first = element;
            });
            first?.Focus();
        }

        private static bool CanFocus(VisualElement element)
        {
            if (element?.panel == null || element.focusable == false || element.tabIndex < 0) return false;
            if (element.enabledInHierarchy == false || element.resolvedStyle.visibility != Visibility.Visible) return false;

            for (VisualElement parent = element; parent != null; parent = parent.parent)
            {
                if (parent.resolvedStyle.display == DisplayStyle.None) return false;
            }

            return true;
        }

        private void Awake()
        {
            Initialize(GetComponent<UIDocument>().rootVisualElement);
        }
    }
}
