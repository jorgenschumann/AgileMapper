﻿namespace AgileObjects.AgileMapper.UnitTests
{
    using TestClasses;
    using Xunit;
    using Shouldly;

    public class WhenFlatteningObjects
    {
        [Fact]
        public void ShouldIncludeASimpleTypeMember()
        {
            var source = new PublicProperty<string> { Value = "Flatten THIS" };
            var result = Mapper.Flatten(source);

            ((object)result).ShouldNotBeNull();
            ((string)result.Value).ShouldBe("Flatten THIS");
        }

        [Fact]
        public void ShouldIncludeANestedSimpleTypeMember()
        {
            var source = new PublicProperty<PublicField<int>> { Value = new PublicField<int> { Value = 1234 } };
            var result = Mapper.Flatten(source);

            ((object)result).ShouldNotBeNull();
            ((PublicField<int>)result.Value).ShouldNotBeNull();
            ((int)result.Value_Value).ShouldBe(1234);
        }

        [Fact]
        public void ShouldHandleANullComplexTypeMember()
        {
            var source = new PublicProperty<PublicField<int>> { Value = null };
            var result = Mapper.Flatten(source);

            ((object)result).ShouldNotBeNull();
            ((PublicField<int>)result.Value).ShouldBeNull();
        }
    }
}