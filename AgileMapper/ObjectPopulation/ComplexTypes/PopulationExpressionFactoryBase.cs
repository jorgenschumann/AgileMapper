namespace AgileObjects.AgileMapper.ObjectPopulation.ComplexTypes
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Extensions;
    using Members;
    using Members.Population;
    using static CallbackPosition;

    internal abstract class PopulationExpressionFactoryBase
    {
        private readonly ComplexTypeConstructionFactory _constructionFactory;

        protected PopulationExpressionFactoryBase(ComplexTypeConstructionFactory constructionFactory)
        {
            _constructionFactory = constructionFactory;
        }

        public IEnumerable<Expression> GetPopulation(IObjectMappingData mappingData)
        {
            var mapperData = mappingData.MapperData;
            var preCreationCallback = GetCreationCallbackOrNull(Before, mapperData);
            var postCreationCallback = GetCreationCallbackOrNull(After, mapperData);
            var populationsAndCallbacks = GetPopulationsAndCallbacks(mappingData).ToList();

            yield return preCreationCallback;

            if (mapperData.Context.UseLocalVariable)
            {
                var assignCreatedObject = postCreationCallback != null;

                yield return GetLocalVariableInstantiation(assignCreatedObject, populationsAndCallbacks, mappingData);
            }

            yield return postCreationCallback;

            yield return GetObjectRegistrationCallOrNull(mapperData);

            foreach (var population in populationsAndCallbacks)
            {
                yield return population;
            }

            mappingData.MapperKey.AddSourceMemberTypeTesterIfRequired(mappingData);
        }

        private static Expression GetCreationCallbackOrNull(CallbackPosition callbackPosition, IMemberMapperData mapperData)
            => mapperData.MapperContext.UserConfigurations.GetCreationCallbackOrNull(callbackPosition, mapperData);

        private IEnumerable<Expression> GetPopulationsAndCallbacks(IObjectMappingData mappingData)
        {
            foreach (var memberPopulation in MemberPopulationFactory.Default.Create(mappingData))
            {
                if (!memberPopulation.IsSuccessful)
                {
                    yield return memberPopulation.GetPopulation();
                    continue;
                }

                foreach (var expression in GetPopulationExpressionsFor(memberPopulation, mappingData))
                {
                    yield return expression;
                }
            }
        }

        protected abstract IEnumerable<Expression> GetPopulationExpressionsFor(
            IMemberPopulation memberPopulation,
            IObjectMappingData mappingData);

        private Expression GetLocalVariableInstantiation(
            bool assignCreatedObject,
            IList<Expression> memberPopulations,
            IObjectMappingData mappingData)
        {
            var localVariableValue = TargetObjectResolutionFactory.GetObjectResolution(
                GetNewObjectCreation,
                mappingData,
                memberPopulations,
                assignCreatedObject);

            var localVariableAssignment = mappingData.MapperData.LocalVariable.AssignTo(localVariableValue);

            return localVariableAssignment;
        }

        protected virtual Expression GetNewObjectCreation(
            IObjectMappingData mappingData,
            IList<Expression> memberPopulations)
        {
            return _constructionFactory.GetNewObjectCreation(mappingData);
        }

        #region Object Registration

        private static Expression GetObjectRegistrationCallOrNull(ObjectMapperData mapperData)
        {
            if (!mapperData.MappedObjectCachingNeeded || mapperData.TargetTypeWillNotBeMappedAgain)
            {
                return null;
            }

            var registerMethod = typeof(IObjectMappingDataUntyped)
                .GetMethod("Register")
                .MakeGenericMethod(mapperData.SourceType, mapperData.TargetType);

            return Expression.Call(
                mapperData.EntryPointMapperData.MappingDataObject,
                registerMethod,
                mapperData.SourceObject,
                mapperData.TargetInstance);
        }

        #endregion
    }
}