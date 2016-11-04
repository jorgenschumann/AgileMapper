﻿namespace AgileObjects.AgileMapper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Caching;
    using Extensions;
    using ReadableExpressions.Extensions;

    internal class DerivedTypesCache
    {
        private static readonly Type[] _noTypes = { };
        private readonly ICache<Assembly, IEnumerable<Type>> _typesByAssembly;
        private readonly ICache<Type, ICollection<Type>> _derivedTypesByType;

        public DerivedTypesCache()
        {
            _typesByAssembly = GlobalContext.Instance.Cache.CreateScoped<Assembly, IEnumerable<Type>>();
            _derivedTypesByType = GlobalContext.Instance.Cache.CreateScoped<Type, ICollection<Type>>();
        }

        public ICollection<Type> GetTypesDerivedFrom(Type type)
        {
            if (type.IsSealed())
            {
                return _noTypes;
            }

            return _derivedTypesByType.GetOrAdd(type, GetDerivedTypesForType);
        }

        private ICollection<Type> GetDerivedTypesForType(Type type)
        {
            var assemblyTypes = _typesByAssembly.GetOrAdd(type.GetAssembly(), GetRelevantTypesFromAssembly);
            var derivedTypes = assemblyTypes.Where(t => t.IsDerivedFrom(type)).ToList();

            if (derivedTypes.Count != 0)
            {
                derivedTypes.Sort(DerivedTypeComparer.Instance);
            }

            return derivedTypes;
        }

        private static IEnumerable<Type> GetRelevantTypesFromAssembly(Assembly assembly)
        {
            return GetTypesFromAssembly(assembly)
                .Where(t => t.IsClass() && !t.IsAbstract())
                .ToArray();
        }

        private static IEnumerable<Type> GetTypesFromAssembly(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.WhereNotNull();
            }
        }

        #region Helper Class

        private class DerivedTypeComparer : IComparer<Type>
        {
            public static readonly IComparer<Type> Instance = new DerivedTypeComparer();

            public int Compare(Type x, Type y)
            {
                if (x.IsAssignableFrom(y))
                {
                    return 1;
                }

                return -1;
            }
        }

        #endregion
    }
}