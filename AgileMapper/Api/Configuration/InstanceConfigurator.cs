namespace AgileObjects.AgileMapper.Api.Configuration
{
    using System;
    using System.Linq.Expressions;
    using AgileMapper.Configuration;
    using Members;

    /// <summary>
    /// Provides options to configure mappings of the type specified by the type argument.
    /// </summary>
    /// <typeparam name="TObject">The type of object to which the configuration should apply.</typeparam>
    public class InstanceConfigurator<TObject> where TObject : class
    {
        private readonly MappingConfigInfo _configInfo;

        internal InstanceConfigurator(MapperContext mapperContext)
        {
            _configInfo = MappingConfigInfo.AllRuleSetsSourceTypesAndTargetTypes(mapperContext);
        }

        /// <summary>
        /// Use the given <paramref name="idExpression"/> to uniquely identify instances of the type being configured.
        /// </summary>
        /// <typeparam name="TId">
        /// The type of the expression to use to uniquely identify instances of the type being configured.
        /// </typeparam>
        /// <param name="idExpression">
        /// The expression to use to uniquely identify instances of the type being configured.
        /// </param>
        public void IdentifyUsing<TId>(Expression<Func<TObject, TId>> idExpression)
        {
            _configInfo.MapperContext.UserConfigurations.Identifiers.Add(typeof(TObject), idExpression);
        }

        /// <summary>
        /// Use the given <paramref name="factory"/> expression to create instances of the type being configured.
        /// The factory expression is passed a context object containing the current mapping's source and target 
        /// objects in untyped properties.
        /// </summary>
        /// <param name="factory">
        /// The factory expression to use to create instances of the type being configured.
        /// </param>
        public void CreateUsing(Expression<Func<IMappingData<object, object>, TObject>> factory)
            => new FactorySpecifier<object, object, TObject>(_configInfo).Using(factory);

        /// <summary>
        /// Use the given <paramref name="factory"/> function to create instances of the type being configured.
        /// The following factory function signatures are supported:
        /// <para>
        /// Func&lt;TObject&gt; - parameterless.
        /// </para>
        /// <para>
        /// Func&lt;IMappingData&lt;object, object&gt;, TObject&gt; - taking a context object containing the 
        /// current mapping's source and target objects in untyped properties.
        /// </para>
        /// <para>
        /// Func&lt;object, object, TObject&gt; - taking the source and target objects as untyped arguments.
        /// </para>
        /// <para>
        /// Func&lt;object, object, int?, TObject&gt; - taking the source and target objects as untyped 
        /// arguments and the current enumerable index, if applicable.
        /// </para>
        /// </summary>
        /// <param name="factory">
        /// The factory function to use to create instances of the type being configured.
        /// </param>
        public void CreateUsing<TFactory>(TFactory factory) where TFactory : class
            => new FactorySpecifier<object, object, TObject>(_configInfo).Using(factory);
    }
}