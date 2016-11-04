﻿namespace AgileObjects.AgileMapper.Members
{
    using System.Linq.Expressions;
    using ObjectPopulation;

    internal interface IMemberMapperData : IBasicMapperData
    {
        MapperContext MapperContext { get; }

        new ObjectMapperData Parent { get; }

        bool IsForInlineMapping { get; }

        Expression ParentObject { get; }

        ParameterExpression MappingDataObject { get; }

        IQualifiedMember SourceMember { get; }

        Expression SourceObject { get; }

        Expression TargetObject { get; }

        Expression EnumerableIndex { get; }

        ParameterExpression InstanceVariable { get; }

        NestedAccessFinder NestedAccessFinder { get; }
    }
}