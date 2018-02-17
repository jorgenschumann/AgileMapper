﻿namespace AgileObjects.AgileMapper.UnitTests.Orms.Ef5.SimpleTypeConversion
{
    using System.Threading.Tasks;
    using Infrastructure;
    using Orms.SimpleTypeConversion;
    using Xunit;

    public class WhenConvertingToInts : WhenConvertingToInts<Ef5TestDbContext>

    {
        public WhenConvertingToInts(InMemoryEf5TestContext context)
            : base(context)
        {
        }

        [Fact]
        public Task ShouldErrorProjectingAParseableString()
            => RunShouldErrorProjectingAParseableStringToAnInt();

        [Fact]
        public Task ShouldErrorProjectingANullString()
            => RunShouldErrorProjectingANullStringToAnInt();

        [Fact]
        public Task ShouldErrorProjectingAnUnparseableString()
            => RunShouldErrorProjectingAnUnparseableStringToAnInt();
    }
}