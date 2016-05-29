using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;
using ClepsCompiler.CompilerBackend;
using ClepsCompiler.CompilerStructures;

namespace ClepsCompiler.SyntaxTreeVisitors
{
    partial class ClepsFunctionBodyGeneratorVisitor
    {
        public override object VisitBinaryOperatorOnExpression([NotNull] ClepsParser.BinaryOperatorOnExpressionContext context)
        {
            IValue leftValue = Visit(context.LeftExpression) as IValue;
            IValue rightValue = Visit(context.RightExpression) as IValue;
            string operatorString = context.operatorSymbol().GetText();

            IValue ret;

            if (leftValue.ExpressionType == rightValue.ExpressionType)
            {
                List<IValue> parameters = new List<IValue>() { leftValue, rightValue };
                return doFunctionCall(context, operatorString, parameters, null /* target */, leftValue.ExpressionType, false);
            }

            else
            {
                throw new NotImplementedException("Operators for non equal types not supported");
            }

            return ret;
        }
    }
}
