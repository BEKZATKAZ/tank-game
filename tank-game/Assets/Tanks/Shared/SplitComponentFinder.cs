using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Tanks.Core.Helpers;

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

        public static void ClearCache() => _componentTypes.Clear();

        public static Type[] FindFor(
            object behaviour, Required<Type> abstractionType, bool? isServer = null)
        {
            return FindFor(behaviour, abstractionType, isServer, out _);
        }

        public static Type[] FindFor(
            object behaviour, Required<Type> abstractionType, out bool fromCache)
        {
            return FindFor(behaviour, abstractionType, null, out fromCache);
        }

        public static Type[] FindFor(
            object behaviour, Required<Type> abstractionType, bool? isServer, out bool fromCache)
        {
            Type behaviourType = behaviour.GetType();

            if (_componentTypes.TryGetValue(behaviourType, out Type[] result) == false)
            {
                fromCache = false;

                _componentTypes[behaviourType] = result = _baseTypes
                   .Where(x => IsComponentFor(x, behaviourType, abstractionType))
                   .Where(x => FilterByAssembly(x.Assembly, isServer))
                   .ToArray();
            }
            else fromCache = true;

            return result;
        }

        private static bool IsComponentFor(
            Required<Type> candidate,
            Required<Type> targetType,
            Required<Type> abstractionType)
        {
            return candidate.Value
                .GetInterfaces()
                .Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == abstractionType.Value &&
                    i.GetGenericArguments()[0].IsAssignableFrom(targetType.Value));
        }

        private static bool FilterByAssembly(Required<Assembly> assembly, bool? isServer)
        {
            if (isServer.HasValue == false) return true;

            string expectedName = isServer.Value ? "server" : "client";
            string pattern = $"shared|{expectedName}";

            return Regex.IsMatch(assembly.Value.FullName, pattern, RegexOptions.IgnoreCase);
        }
    }
}
