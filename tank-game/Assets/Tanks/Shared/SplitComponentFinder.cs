using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Tanks.Shared
{
	public static class SplitComponentFinder
	{
        static SplitComponentFinder()
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

        public static Type[] FindFor(object behaviour, Type abstractionType)
        {
            return FindFor(behaviour, abstractionType, out _);
        }

        public static Type[] FindFor(object behaviour, Type abstractionType, out bool fromCache)
        {
            Type behaviourType = behaviour.GetType();

            if (_componentTypes.TryGetValue(behaviourType, out Type[] result) == false)
            {
                fromCache = false;
                _componentTypes[behaviourType] = result = _baseTypes
                   .Where(x => IsComponentFor(x, behaviourType, abstractionType))
                   .ToArray();
            }
            else fromCache = true;

            return result;
        }

        private static bool IsComponentFor(Type candidate, Type targetType, Type abstractionType)
        {
            return candidate.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == abstractionType &&
                i.GetGenericArguments()[0] == targetType);
        }
    }
}