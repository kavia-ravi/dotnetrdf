using VDS.RDF.Nodes;
using VDS.RDF.Query.Builder.Expressions;
using VDS.RDF.Query.Expressions.Conditional;
using VDS.RDF.Query.Expressions.Functions.Sparql.String;
using VDS.RDF.Query.Expressions.Primary;

namespace VDS.RDF.Query.Builder
{
    public sealed class ExpressionBuilder
    {
        private BooleanExpression _expression;
        private readonly INodeFactory _nodeFactory = new NodeFactory();

        public BooleanExpression Expression
        {
            get { return _expression; }
            internal set { _expression = value; }
        }

        public VariableExpression Variable(string variable)
        {
            return new VariableExpression(variable);
        }

        public TypedLiteralExpression<string> Constant(string str)
        {
            return new StringExpression(str);
        }

        public BooleanExpression Not(BooleanExpression innerExpression)
        {
            return new BooleanExpression(new NotExpression(innerExpression.Expression));
        }
    }
}