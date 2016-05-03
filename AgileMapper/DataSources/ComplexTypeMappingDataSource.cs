﻿namespace AgileObjects.AgileMapper.DataSources
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Api.Configuration;
    using Members;
    using ObjectPopulation;

    internal class ComplexTypeMappingDataSource : IDataSource
    {
        public ComplexTypeMappingDataSource(Member complexTypeMember, IObjectMappingContext omc)
        {
            Value = omc.GetMapCall(complexTypeMember);
        }

        public Expression GetConditionOrNull(IConfigurationContext context) => null;

        public IEnumerable<Expression> NestedSourceMemberAccesses => Enumerable.Empty<Expression>();

        public Expression Value { get; }
    }
}