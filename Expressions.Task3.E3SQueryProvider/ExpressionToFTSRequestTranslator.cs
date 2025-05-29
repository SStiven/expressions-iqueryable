using System;
using System.Linq;
using System.Linq.Expressions;
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
            switch (node.NodeType)
            {
                case ExpressionType.Equal:
                    if (node.Left.NodeType != ExpressionType.MemberAccess)
                        throw new NotSupportedException($"Left operand should be property or field: {node.NodeType}");

                    if (node.Right.NodeType != ExpressionType.Constant)
                        throw new NotSupportedException($"Right operand should be constant: {node.NodeType}");

                    Visit(node.Left);
                    _resultStringBuilder.Append("(");
                    Visit(node.Right);
                    _resultStringBuilder.Append(")");
                    break;

                default:
                    throw new NotSupportedException($"Operation '{node.NodeType}' is not supported");
            }
            ;

            return node;
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
