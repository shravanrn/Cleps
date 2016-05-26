using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;
using ClepsCompiler.CompilerBackend;
using ClepsCompiler.CompilerTypes;
using ClepsCompiler.CompilerStructures;

namespace ClepsCompiler.SyntaxTreeVisitors
{
    partial class ClepsFunctionBodyGeneratorVisitor
    {
        public override object VisitAddressOfOnExpression([NotNull] ClepsParser.AddressOfOnExpressionContext context)
        {
            IValue target = Visit(context.addressOfOrValueAtTargetExpression()) as IValue;
            IValue ret = CodeGenerator.GetPtrToValue(target);
            return ret;
        }

        public override object VisitValueAtOnExpression([NotNull] ClepsParser.ValueAtOnExpressionContext context)
        {
            IValue target = Visit(context.addressOfOrValueAtTargetExpression()) as IValue;
            if(!(target.ExpressionType is PointerClepsType))
            {
                string errorMessage = String.Format("Cannot derefernce object of type {0}.", target.ExpressionType.GetClepsTypeString());
                Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                //Just return something to avoid stalling compilation
                return target;
            }

            IValue ret = CodeGenerator.GetDereferencedValueFromPtr(target);
            return ret;
        }
    }
}
