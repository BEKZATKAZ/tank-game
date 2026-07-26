using System.Linq;
using Tanks.Shared;
using Tanks.Utils;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VContainer.Unity;

namespace Tanks.UI
{
    public interface IDocumentController { }
    public interface IDocumentController<T> : IDocumentController where T : IDocument { }
    public interface IDocument { }

    public interface IDocumentInitialize
    {
        void Initialize(VisualElement root);
    }

    public interface IDocumentUpdate
    {
        void OnUpdate(float deltaTime);
    }

    public interface IDocumentLateUpdate
    {
        void OnLateUpdate(float deltaTime);
    }

    public interface IDocumentDestroy
    {
        void OnDestroy();
    }

    [RequireComponent(typeof(UIDocument))]
    public class UIDocumentExtended : MonoBehaviour, IDocument
    {
        private IDocumentUpdate[] _updates;
        private IDocumentLateUpdate[] _lateUpdates;
        private IDocumentDestroy[] _destroys;

        protected virtual void Awake()
        {
            IObjectResolver resolver = LifetimeScope.Find<LifetimeScope>().Container;
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;

            IDocumentController[] controllers = SplitComponentFinder
                .FindFor(this, typeof(IDocumentController<>))
                .Select(x => resolver.CreateInjected(x) as IDocumentController)
                .ToArray();

            _updates = controllers.OfType<IDocumentUpdate>().ToArray();
            _lateUpdates = controllers.OfType<IDocumentLateUpdate>().ToArray();
            _destroys = controllers.OfType<IDocumentDestroy>().ToArray();

            foreach (var initialize in controllers.OfType<IDocumentInitialize>())
            {
                initialize.Initialize(root);
            }
        }

        protected virtual void Update()
        {
            float deltaTime = Time.deltaTime;
            for (int i = 0; i < _updates.Length; i++) _updates[i].OnUpdate(deltaTime);
        }

        protected virtual void LateUpdate()
        {
            float deltaTime = Time.deltaTime;
            for (int i = 0; i < _lateUpdates.Length; i++) _lateUpdates[i].OnLateUpdate(deltaTime);
        }

        protected virtual void OnDestroy()
        {
            for (int i = 0; i < _destroys.Length; i++) _destroys[i].OnDestroy();
        }
    }
}
