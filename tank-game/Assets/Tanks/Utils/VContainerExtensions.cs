using System;
using System.Linq;
using System.Reflection;
using VContainer;

namespace Tanks.Utils
{
    public static class VContainerExtensions
    {
        public static T CreateInjected<T>(this IObjectResolver resolver) where T : class
        {
            return (T)CreateInjected(resolver, typeof(T));
        }

        public static object CreateInjected(this IObjectResolver resolver, Type type)
        {
            ConstructorInfo ctor = type.GetConstructors()[0];

            object[] args = ctor.GetParameters()
                .Select(p => resolver.Resolve(p.ParameterType))
                .ToArray();

            object instance = Activator.CreateInstance(type, args);
            resolver.Inject(instance);

            return instance;
        }
    }
}
