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

            if(leftValue.ExpressionType == rightValue.ExpressionType && leftValue.ExpressionType == CompilerConstants.ClepsByteType)
            {
                if (operatorString == "+")
                {
                    ret = CodeGenerator.PerformWrappedAddition(leftValue, rightValue);
                }
                else
                {
                    string errorMessage = String.Format("Operator {0} is not supported on arguments of type ({1}, {2})", operatorString, leftValue.ExpressionType.GetClepsTypeString(), rightValue.ExpressionType.GetClepsTypeString());
                    Status.AddError(new CompilerError(FileName, context.operatorSymbol().Start.Line, context.operatorSymbol().Start.Column, errorMessage));
                    //just use the first element access to avoid stalling
                    ret = leftValue;
                }
            }
            else
            {
                throw new NotImplementedException("Operators not supported on other types");
            }

            return ret;
        }
    }
}
