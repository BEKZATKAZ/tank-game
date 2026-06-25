using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Tanks.Shared
{
	public static class NetworkComponentFinder
	{
        static NetworkComponentFinder()
        {
            _baseTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(x =>
                {
                    try { return x.GetTypes(); }
                    catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
                })
                .Where(x => x.IsAbstract == false && x.IsInterface == false)
                .ToArray();
        }

        // Cache to minimize reflection on each call
        private static readonly Type[] _baseTypes;
        private static readonly Dictionary<Type, Type[]> _componentTypes = new();

        public static Type[] FindFor(INetworkBehaviourExtended behaviour)
        {
            return FindFor(behaviour, out _);
        }

        public static Type[] FindFor(INetworkBehaviourExtended behaviour, out bool fromCache)
        {
            Type behaviourType = behaviour.GetType();

            if (_componentTypes.TryGetValue(behaviourType, out Type[] result) == false)
            {
                fromCache = false;
                _componentTypes[behaviourType] = result = _baseTypes
                   .Where(x => IsNetworkComponentFor(x, behaviourType))
                   .ToArray();
            }
            else fromCache = true;

            return result;
        }

        private static bool IsNetworkComponentFor(Type candidate, Type targetType)
        {
            return candidate.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(INetworkComponent<>) &&
                i.GetGenericArguments()[0] == targetType);
        }
    }
}