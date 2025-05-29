using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata;
using System.Text;

namespace Expressions.Task3.E3SQueryProvider
{
    public class ExpressionToFtsRequestTranslator : ExpressionVisitor
    {
        readonly StringBuilder _resultStringBuilder;

        public ExpressionToFtsRequestTranslator()
        {
            _resultStringBuilder = new StringBuilder();
        }

        public string Translate(Expression exp)
        {
            Visit(exp);

            return _resultStringBuilder.ToString();
        }

        #region protected methods

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Queryable)
                && node.Method.Name == "Where")
            {
                var predicate = node.Arguments[1];
                Visit(predicate);

                return node;
            }

            if (node.Method.Name == nameof(string.Equals)
                && node.Object is MemberExpression equalsMember
                && node.Arguments.Count == 1
                && node.Arguments[0] is ConstantExpression equalsConstant)
            {
                _resultStringBuilder
                    .Append(equalsMember.Member.Name)
                    .Append(":(")
                    .Append(equalsConstant.Value)
                    .Append(")");
                return node;
            }

            if (node.Method.Name == nameof(string.StartsWith)
                && node.Object is MemberExpression startWithMember
                && node.Arguments.Count == 1
                && node.Arguments[0] is ConstantExpression startWithConstant)
            {
                _resultStringBuilder
                    .Append(startWithMember.Member.Name)
                    .Append(":(")
                    .Append(startWithConstant.Value)
                    .Append("*)");
                return node;
            }

            if (node.Method.Name == nameof(string.Contains)
                && node.Object is MemberExpression containsMember
                && node.Arguments.Count == 1
                && node.Arguments[0] is ConstantExpression containsConstant)
            {
                _resultStringBuilder
                    .Append(containsMember.Member.Name)
                    .Append(":(*")
                    .Append(containsConstant.Value)
                    .Append("*)");
                return node;
            }

            if (node.Method.Name == nameof(string.EndsWith)
                && node.Object is MemberExpression endsWithMember
                && node.Arguments.Count == 1
                && node.Arguments[0] is ConstantExpression endsWithConstant)
            {
                _resultStringBuilder
                    .Append(endsWithMember.Member.Name)
                    .Append(":(*")
                    .Append(endsWithConstant.Value)
                    .Append(")");
                return node;
            }

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.AndAlso)
            {
                Visit(node.Left);
                _resultStringBuilder.Append(" && ");
                Visit(node.Right);
                return node;
            }

            if (node.NodeType != ExpressionType.Equal)
            {
                throw new NotSupportedException($"Unsupported binary op: {node.NodeType}");
            }

            var (member, constant) = ExtractMemberAndConstant(node);

            _resultStringBuilder
                .Append(member.Member.Name)
                .Append(":(")
                .Append(constant.Value)
                .Append(")");

            return node;
        }

        private (MemberExpression member, ConstantExpression constant) ExtractMemberAndConstant(BinaryExpression node)
        {
            if (node.Left is MemberExpression m1 && node.Right is ConstantExpression c1)
                return (m1, c1);

            if (node.Right is MemberExpression m2 && node.Left is ConstantExpression c2)
                return (m2, c2);

            throw new NotSupportedException("Equality must be between a field and a constant");
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            _resultStringBuilder.Append(node.Member.Name).Append(":");

            return base.VisitMember(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            _resultStringBuilder.Append(node.Value);

            return node;
        }

        #endregion
    }
}
