using Antlr4.Runtime.Misc;
using ClepsCompiler.CompilerBackend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.SyntaxTreeVisitors
{
    partial class ClepsFunctionBodyGeneratorVisitor
    {
        public override object VisitIfStatement([NotNull] ClepsParser.IfStatementContext context)
        {
            IValue conditionValue = Visit(context.rightHandExpression()) as IValue;
            CurrMethodRegister.CreateIfStatementBlock(conditionValue);

            Visit(context.statementBlock());

            CurrMethodRegister.CloseBlock();

            return conditionValue;
        }

    }
}
