using UnityEngine.UIElements;

namespace Tanks.UI
{
    public interface IView
    {
        void Initialize(VisualElement root);
    }

    public interface IView<T> : IView
    {
        void Show(T data);
        void Hide();
    }
}