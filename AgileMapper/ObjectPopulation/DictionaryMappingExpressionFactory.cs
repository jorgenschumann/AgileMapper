namespace AgileObjects.AgileMapper.ObjectPopulation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using ComplexTypes;
    using DataSources;
    using Enumerables.Dictionaries;
    using Extensions;
    using Members;
    using NetStandardPolyfills;
    using ReadableExpressions;

    internal class DictionaryMappingExpressionFactory : MappingExpressionFactoryBase
    {
        public static readonly MappingExpressionFactoryBase Instance = new DictionaryMappingExpressionFactory();

        private readonly MemberPopulationFactory _memberPopulationFactory;

        private DictionaryMappingExpressionFactory()
        {
            _memberPopulationFactory = new MemberPopulationFactory(GetAllTargetMembers);
        }

        #region Target Member Generation

        private static IEnumerable<QualifiedMember> GetAllTargetMembers(ObjectMapperData mapperData)
        {
            var targetMembersFromSource = EnumerateTargetMembers(mapperData).ToArray();

            var configuredDataSourceFactories = mapperData.MapperContext
                .UserConfigurations
                .QueryDataSourceFactories<ConfiguredDictionaryDataSourceFactory>()
                .Where(dsf => dsf.IsFor(mapperData))
                .ToArray();

            if (configuredDataSourceFactories.None())
            {
                return targetMembersFromSource;
            }

            var configuredCustomTargetMembers =
                GetConfiguredTargetMembers(configuredDataSourceFactories, targetMembersFromSource);

            var allTargetMembers = targetMembersFromSource.Append(configuredCustomTargetMembers);

            return allTargetMembers;
        }

        private static IEnumerable<DictionaryTargetMember> EnumerateTargetMembers(ObjectMapperData mapperData)
        {
            var targetDictionaryMember = (DictionaryTargetMember)mapperData.TargetMember;
            var sourceMembers = GlobalContext.Instance.MemberCache.GetSourceMembers(mapperData.SourceType);

            foreach (var sourceMember in sourceMembers)
            {
                var entryTargetMember = targetDictionaryMember.Append(sourceMember.DeclaringType, sourceMember.Name);

                var entryMapperData = new ChildMemberMapperData(entryTargetMember, mapperData);
                var configuredKey = GetCustomKeyOrNull(entryMapperData);

                if (configuredKey != null)
                {
                    entryTargetMember.SetCustomKey(configuredKey);
                }

                if (!sourceMember.IsSimple)
                {
                    entryTargetMember = entryTargetMember.WithTypeOf(sourceMember);
                }

                yield return entryTargetMember;
            }
        }

        private static string GetCustomKeyOrNull(IMemberMapperData entryMapperData)
        {
            var dictionaries = entryMapperData.MapperContext.UserConfigurations.Dictionaries;
            var configuredFullKey = dictionaries.GetFullKeyValueOrNull(entryMapperData);

            return configuredFullKey ?? dictionaries.GetMemberKeyOrNull(entryMapperData);
        }

        private static DictionaryTargetMember[] GetConfiguredTargetMembers(
            IEnumerable<ConfiguredDictionaryDataSourceFactory> configuredDataSourceFactories,
            IList<DictionaryTargetMember> targetMembersFromSource)
        {
            return configuredDataSourceFactories
                .GroupBy(dsf => dsf.TargetDictionaryEntryMember.Name)
                .Select(group =>
                {
                    var factory = group.First();
                    var targetMember = factory.TargetDictionaryEntryMember;

                    targetMember.IsCustom = targetMembersFromSource.None(
                        sourceMember => sourceMember.RegistrationName == targetMember.Name);

                    return targetMember.IsCustom ? targetMember : null;
                })
                .WhereNotNull()
                .ToArray();
        }

        #endregion

        public override bool IsFor(IObjectMappingData mappingData)
        {
            if (mappingData.MapperData.TargetMember.IsDictionary)
            {
                return true;
            }

            if (mappingData.IsRoot)
            {
                return false;
            }

            if (!(mappingData.MapperData.TargetMember is DictionaryTargetMember dictionaryMember))
            {
                return false;
            }

            if (dictionaryMember.HasSimpleEntries)
            {
                return true;
            }

            return dictionaryMember.HasObjectEntries && !mappingData.IsStandalone();
        }

        protected override bool TargetCannotBeMapped(IObjectMappingData mappingData, out Expression nullMappingBlock)
        {
            if (mappingData.MapperKey.MappingTypes.SourceType.IsDictionary())
            {
                nullMappingBlock = null;
                return false;
            }

            var targetMember = (DictionaryTargetMember)mappingData.MapperData.TargetMember;

            if ((targetMember.KeyType == typeof(string)) || (targetMember.KeyType == typeof(object)))
            {
                nullMappingBlock = null;
                return false;
            }

            nullMappingBlock = Expression.Block(
                ReadableExpression.Comment("Only string- or object-keyed Dictionaries are supported"),
                mappingData.MapperData.GetFallbackCollectionValue());

            return true;
        }

        protected override IEnumerable<Expression> GetShortCircuitReturns(GotoExpression returnNull, IObjectMappingData mappingData)
            => Enumerable<Expression>.Empty;

        protected override Expression GetDerivedTypeMappings(IObjectMappingData mappingData)
            => Constants.EmptyExpression;

        protected override IEnumerable<Expression> GetObjectPopulation(IObjectMappingData mappingData)
        {
            var mapperData = mappingData.MapperData;

            if (!mapperData.TargetMember.IsDictionary)
            {
                yield return GetDictionaryPopulation(mappingData);
                yield break;
            }

            Func<DictionarySourceMember, IObjectMappingData, Expression> assignmentFactory;

            if (SourceMemberIsDictionary(mapperData, out var sourceDictionaryMember))
            {
                if (UseDictionaryCloneConstructor(sourceDictionaryMember, mapperData, out var cloneConstructor))
                {
                    yield return GetClonedDictionaryAssignment(mapperData, cloneConstructor);
                    yield break;
                }

                assignmentFactory = GetMappedDictionaryAssignment;
            }
            else
            {
                assignmentFactory = (dsm, md) => GetParameterlessDictionaryAssignment(md);
            }

            var population = GetDictionaryPopulation(mappingData);
            var assignment = assignmentFactory.Invoke(sourceDictionaryMember, mappingData);

            yield return assignment;
            yield return population;
        }

        private static bool SourceMemberIsDictionary(
            IMemberMapperData mapperData,
            out DictionarySourceMember sourceDictionaryMember)
        {
            sourceDictionaryMember = mapperData.GetDictionarySourceMemberOrNull();
            return sourceDictionaryMember != null;
        }

        private static bool UseDictionaryCloneConstructor(
            IQualifiedMember sourceDictionaryMember,
            IBasicMapperData mapperData,
            out ConstructorInfo cloneConstructor)
        {
            cloneConstructor = null;

            return mapperData.TargetMember.ElementType.IsSimple() &&
                  (sourceDictionaryMember.Type == mapperData.TargetType) &&
                 ((cloneConstructor = GetDictionaryCloneConstructor(mapperData)) != null);
        }

        private static ConstructorInfo GetDictionaryCloneConstructor(IBasicMapperData mapperData)
        {
            var dictionaryTypes = mapperData.TargetType.GetDictionaryTypes();
            var dictionaryInterfaceType = typeof(IDictionary<,>).MakeGenericType(dictionaryTypes.Key, dictionaryTypes.Value);

            var comparerProperty = mapperData.SourceType.GetPublicInstanceProperty("Comparer");

            return FindDictionaryConstructor(
                mapperData.TargetType,
                dictionaryInterfaceType,
               (comparerProperty != null) ? 2 : 1);
        }

        private static Expression GetClonedDictionaryAssignment(IMemberMapperData mapperData, ConstructorInfo cloneConstructor)
        {
            Expression cloneDictionary;

            if (cloneConstructor.GetParameters().Length == 1)
            {
                cloneDictionary = Expression.New(cloneConstructor, mapperData.SourceObject);
            }
            else
            {
                var comparer = Expression.Property(mapperData.SourceObject, "Comparer");
                cloneDictionary = Expression.New(cloneConstructor, mapperData.SourceObject, comparer);
            }

            var assignment = mapperData.TargetInstance.AssignTo(cloneDictionary);

            return assignment;
        }

        private static ConstructorInfo FindDictionaryConstructor(
            Type dictionaryType,
            Type firstParameterType,
            int numberOfParameters)
        {
            if (dictionaryType.IsInterface())
            {
                dictionaryType = GetConcreteDictionaryType(dictionaryType);
            }

            return dictionaryType
                .GetPublicInstanceConstructors()
                .Select(ctor => new { Ctor = ctor, Parameters = ctor.GetParameters() })
                .First(ctor =>
                    (ctor.Parameters.Length == numberOfParameters) &&
                    (ctor.Parameters[0].ParameterType == firstParameterType))
                .Ctor;
        }

        private static Type GetConcreteDictionaryType(Type dictionaryInterfaceType)
        {
            var types = dictionaryInterfaceType.GetDictionaryTypes();

            return typeof(Dictionary<,>).MakeGenericType(types.Key, types.Value);
        }

        private static Expression GetMappedDictionaryAssignment(
            DictionarySourceMember sourceDictionaryMember,
            IObjectMappingData mappingData)
        {
            var mapperData = mappingData.MapperData;

            if (UseParameterlessConstructor(sourceDictionaryMember, mapperData))
            {
                return GetParameterlessDictionaryAssignment(mappingData);
            }

            var comparerProperty = mapperData.SourceObject.Type.GetPublicInstanceProperty("Comparer");

            Expression dictionaryConstruction;

            if (comparerProperty == null)
            {
                if (mapperData.TargetType.IsInterface())
                {
                    dictionaryConstruction = Expression.New(GetConcreteDictionaryType(mapperData.TargetType));
                }
                else
                {
                    dictionaryConstruction = mapperData
                        .MapperContext
                        .ConstructionFactory
                        .GetNewObjectCreation(mappingData);
                }
            }
            else
            {
                var comparer = Expression.Property(mapperData.SourceObject, comparerProperty);

                var constructor = FindDictionaryConstructor(
                    mapperData.TargetType,
                    comparer.Type,
                    numberOfParameters: 1);

                dictionaryConstruction = Expression.New(constructor, comparer);
            }

            return GetDictionaryAssignment(dictionaryConstruction, mappingData);
        }

        private static bool UseParameterlessConstructor(
            DictionarySourceMember sourceDictionaryMember,
            IBasicMapperData mapperData)
        {
            if (sourceDictionaryMember.Type.IsInterface())
            {
                return true;
            }

            return sourceDictionaryMember.ValueType != mapperData.TargetMember.ElementType;
        }

        private static Expression GetParameterlessDictionaryAssignment(IObjectMappingData mappingData)
        {
            var valueType = mappingData.MapperData.EnumerablePopulationBuilder.TargetTypeHelper.ElementType;
            var newDictionary = mappingData.MapperData.TargetType.GetEmptyInstanceCreation(valueType);

            return GetDictionaryAssignment(newDictionary, mappingData);
        }

        private static Expression GetDictionaryAssignment(Expression value, IObjectMappingData mappingData)
        {
            var mapperData = mappingData.MapperData;

            if (mapperData.TargetMember.IsReadOnly)
            {
                return null;
            }

            var valueResolution = TargetObjectResolutionFactory.GetObjectResolution(
                value,
                mappingData,
                mapperData.HasMapperFuncs);

            return mapperData.TargetInstance.AssignTo(valueResolution);
        }

        private Expression GetDictionaryPopulation(IObjectMappingData mappingData)
        {
            var mapperData = mappingData.MapperData;

            if (mapperData.SourceMember.IsEnumerable)
            {
                return GetEnumerableToDictionaryMapping(mappingData);
            }

            var memberPopulations = _memberPopulationFactory
                .Create(mappingData)
                .Select(memberPopulation => memberPopulation.GetPopulation())
                .ToArray();

            if (memberPopulations.HasOne())
            {
                return memberPopulations[0];
            }

            var memberPopulationBlock = Expression.Block(memberPopulations);

            return memberPopulationBlock;
        }

        private static Expression GetEnumerableToDictionaryMapping(IObjectMappingData mappingData)
        {
            var builder = new DictionaryPopulationBuilder(mappingData.MapperData.EnumerablePopulationBuilder);

            if (builder.HasSourceEnumerable)
            {
                builder.AssignSourceVariableFromSourceObject();
            }

            builder.AddItems(mappingData);

            return builder;
        }

        protected override Expression GetReturnValue(ObjectMapperData mapperData)
            => mapperData.TargetInstance;
    }
}