namespace AgileObjects.AgileMapper.Members
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Extensions;

    internal class QualifiedMember
    {
        public static readonly QualifiedMember All = new QualifiedMember(new Member[0], null);

        private readonly Member[] _memberChain;
        private readonly QualifiedMemberName _qualifiedName;

        private QualifiedMember(Member member, QualifiedMember parent)
            : this(parent?._memberChain.Concat(member).ToArray() ?? new[] { member })
        {
        }

        private QualifiedMember(Member[] memberChain)
            : this(memberChain, new QualifiedMemberName(memberChain.Select(m => m.MemberName).ToArray()))
        {
        }

        private QualifiedMember(Member[] memberChain, QualifiedMemberName qualifiedName)
        {
            _memberChain = memberChain;
            LeafMember = memberChain.LastOrDefault();
            _qualifiedName = qualifiedName;
        }

        #region Factory Method

        public static QualifiedMember From(Member member)
        {
            return new QualifiedMember(member, null);
        }

        public static QualifiedMember From(Member[] memberChain)
        {
            return new QualifiedMember(memberChain);
        }

        #endregion

        public Member LeafMember { get; }

        public IEnumerable<Member> Members => _memberChain;

        public Type Type => LeafMember.Type;

        public bool IsComplex => LeafMember.IsComplex;

        public bool IsEnumerable => LeafMember.IsEnumerable;

        public Type ElementType => LeafMember.ElementType;

        public QualifiedMember Append(Member childMember)
        {
            return new QualifiedMember(childMember, this);
        }

        public QualifiedMember RelativeTo(int depth)
        {
            if (depth == 0)
            {
                return this;
            }

            var relativeMemberChain = new Member[_memberChain.Length - depth];

            Array.Copy(
                _memberChain,
                _memberChain.Length - relativeMemberChain.Length,
                relativeMemberChain,
                0,
                relativeMemberChain.Length);

            return new QualifiedMember(relativeMemberChain);
        }

        public QualifiedMember WithType(Type runtimeType)
        {
            if (runtimeType == Type)
            {
                return this;
            }

            var newMemberChain = new Member[_memberChain.Length];
            Array.Copy(_memberChain, 0, newMemberChain, 0, newMemberChain.Length - 1);

            newMemberChain[newMemberChain.Length - 1] = LeafMember.WithType(runtimeType);

            return From(newMemberChain);
        }

        public bool Matches(QualifiedMember otherMember)
        {
            return _qualifiedName.Matches(otherMember._qualifiedName);
        }

        public Expression GetAccess(Expression instance)
        {
            return _memberChain
                .Skip(1)
                .Aggregate(instance, GetMemberAccess);
        }

        private static Expression GetMemberAccess(Expression accessSoFar, Member member)
        {
            if (!member.DeclaringType.IsAssignableFrom(accessSoFar.Type))
            {
                accessSoFar = Expression.Convert(accessSoFar, member.DeclaringType);
            }

            return member.GetAccess(accessSoFar);
        }

        public bool Equals(QualifiedMember otherMember)
        {
            if ((this == All) || (otherMember == All))
            {
                return true;
            }

            return (otherMember.Type == Type) &&
                   (otherMember.LeafMember.Name == LeafMember.Name) &&
                   otherMember.LeafMember.DeclaringType.IsAssignableFrom(LeafMember.DeclaringType);
        }
    }
}