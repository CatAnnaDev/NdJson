using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace NdJson.Serialization
{
    public static class NdjsonConverterRegistry
    {
        private static readonly ConcurrentDictionary<Type, NdjsonConverter> Explicit = new ConcurrentDictionary<Type, NdjsonConverter>();
        private static readonly ConcurrentDictionary<Type, Type> Generated = new ConcurrentDictionary<Type, Type>();
        private static readonly HashSet<Assembly> ScannedAssemblies = new HashSet<Assembly>();
        private static readonly object ScanLock = new object();

        public static void Register<T>(NdjsonConverter<T> converter)
        {
            if (converter == null)
            {
                throw new ArgumentNullException(nameof(converter));
            }

            Explicit[typeof(T)] = converter;
        }

        public static void Register(Type type, NdjsonConverter converter)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (converter == null)
            {
                throw new ArgumentNullException(nameof(converter));
            }

            Explicit[type] = converter;
        }

        public static void RegisterGenerated(Type targetType, Type converterType)
        {
            if (targetType == null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }

            if (converterType == null)
            {
                throw new ArgumentNullException(nameof(converterType));
            }

            Generated[targetType] = converterType;
        }

        public static bool Unregister(Type type)
        {
            NdjsonConverter removed;
            return Explicit.TryRemove(type, out removed);
        }

        internal static bool TryGetExplicit(Type type, out NdjsonConverter converter)
        {
            return Explicit.TryGetValue(type, out converter);
        }

        internal static bool TryGetGenerated(Type type, out Type converterType)
        {
            if (Generated.TryGetValue(type, out converterType))
            {
                return true;
            }

            ScanAssemblyOf(type);
            return Generated.TryGetValue(type, out converterType);
        }

        private static void ScanAssemblyOf(Type type)
        {
            Assembly assembly;
            try
            {
                assembly = type.GetTypeInfo().Assembly;
            }
            catch (Exception)
            {
                return;
            }

            if (assembly == null)
            {
                return;
            }

            lock (ScanLock)
            {
                if (!ScannedAssemblies.Add(assembly))
                {
                    return;
                }

                try
                {
                    foreach (NdjsonGeneratedConverterAttribute attribute in assembly.GetCustomAttributes<NdjsonGeneratedConverterAttribute>())
                    {
                        if (attribute.TargetType != null && attribute.ConverterType != null)
                        {
                            Generated[attribute.TargetType] = attribute.ConverterType;
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
