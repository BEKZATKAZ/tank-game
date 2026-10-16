using System;
using System.Reflection;

namespace Tanks.Editor.TestHelpers
{
    public static class ReflectionHelper
    {
        private const BindingFlags Flags = BindingFlags.Instance
            | BindingFlags.NonPublic
            | BindingFlags.Public;

        public static void SetPrivateField(
            this object target,
            string fieldName,
            object value)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            FieldInfo field = FindField(target.GetType(), fieldName);

            if (field == null)
            {
                throw new ArgumentException(
                    $"Field '{fieldName}' not found on type '{target.GetType()}'.",
                    nameof(fieldName));
            }

            field.SetValue(target, value);
        }

        public static T GetPrivateField<T>(
            this object target,
            string fieldName)
        {
            if (target is null) throw new ArgumentNullException(nameof(target));

            FieldInfo field = FindField(target.GetType(), fieldName);

            return field is null
                ? throw new ArgumentException(
                    $"Field '{fieldName}' not found on type '{target.GetType()}'.",
                    nameof(fieldName))
                : (T)field.GetValue(target);
        }

        public static void SetPrivateProperty(
            this object target,
            string propertyName,
            object value)
        {
            if (target is null) throw new ArgumentNullException(nameof(target));

            PropertyInfo property = FindProperty(target.GetType(), propertyName)
                ?? throw new ArgumentException(
                    $"Property '{propertyName}' not found on type '{target.GetType()}'.",
                    nameof(propertyName));

            property.SetValue(target, value, null);
        }

        public static T GetPrivateProperty<T>(
            this object target,
            string propertyName)
        {
            if (target is null) throw new ArgumentNullException(nameof(target));

            PropertyInfo property = FindProperty(target.GetType(), propertyName);

            return property is null
                ? throw new ArgumentException(
                    $"Property '{propertyName}' not found on type '{target.GetType()}'.",
                    nameof(propertyName))
                : (T)property.GetValue(target, null);
        }

        public static object InvokePrivateMethod(
            this object target,
            string methodName,
            params object[] parameters)
        {
            if (target is null) throw new ArgumentNullException(nameof(target));

            MethodInfo method = FindMethod(target.GetType(), methodName);

            return method is null
                ? throw new ArgumentException(
                    $"Method '{methodName}' not found on type '{target.GetType()}'.",
                    nameof(methodName))
                : method.Invoke(target, parameters);
        }

        public static T InvokePrivateMethod<T>(
            this object target,
            string methodName,
            params object[] parameters)
        {
            return (T)InvokePrivateMethod(target, methodName, parameters);
        }

        private static FieldInfo FindField(Type type, string fieldName)
        {
            Type current = type;

            while (current != null && current != typeof(object))
            {
                FieldInfo field = current.GetField(fieldName, Flags | BindingFlags.DeclaredOnly);

                if (field != null) return field;

                current = current.BaseType;
            }

            return null;
        }

        private static PropertyInfo FindProperty(Type type, string propertyName)
        {
            Type current = type;

            while (current != null && current != typeof(object))
            {
                PropertyInfo property = current.GetProperty(
                    propertyName, Flags | BindingFlags.DeclaredOnly);

                if (property != null) return property;

                current = current.BaseType;
            }

            return null;
        }

        private static MethodInfo FindMethod(Type type, string methodName)
        {
            Type current = type;

            while (current != null && current != typeof(object))
            {
                MethodInfo method = current.GetMethod(
                    methodName, Flags | BindingFlags.DeclaredOnly);

                if (method != null) return method;

                current = current.BaseType;
            }

            return null;
        }
    }
}
