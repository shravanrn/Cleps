using Antlr4.Runtime.Misc;
using ClepsCompiler.CompilerBackend;
using ClepsCompiler.CompilerStructures;
using ClepsCompiler.CompilerTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Tree;

namespace ClepsCompiler.SyntaxTreeVisitors
{
    partial class ClepsFunctionBodyGeneratorVisitor
    {
        public override bool VisitFunctionReturnStatement_Ex([NotNull] ClepsParser.FunctionReturnStatementContext context)
        {
            IValue returnValue = null;
            if (context.rightHandExpression() != null)
            {
                returnValue = Visit(context.rightHandExpression()) as IValue;
            }

            var currFunctionType = CurrMethodGenerator.ExpressionType as FunctionClepsType;

            if (currFunctionType.ReturnType != returnValue.ExpressionType)
            {
                string errorMessage = String.Format("Expected return of {0}. Returning type {1} instead.", currFunctionType.ReturnType.GetClepsTypeString(), returnValue.ExpressionType.GetClepsTypeString());
                Status.AddError(new CompilerError(FileName, context.rightHandExpression().Start.Line, context.rightHandExpression().Start.Column, errorMessage));
            }

            CurrMethodGenerator.CreateReturnStatement(returnValue);

            return true;
        }

        public override object VisitNativeGlobalStatement([NotNull] ClepsParser.NativeGlobalStatementContext context)
        {
            var code = Visit(context.nativeStatement()) as string;
            if (code != null)
            {
                CodeGenerator.AddNativeCode(code);
            }

            return true;
        }

        public override object VisitNativeFunctionStatement([NotNull] ClepsParser.NativeFunctionStatementContext context)
        {
            var code = Visit(context.nativeStatement()) as string;
            if (code != null)
            {
                CurrMethodGenerator.AddNativeCode(code);
            }

            return true;
        }

        public override object VisitNativeStatement([NotNull] ClepsParser.NativeStatementContext context)
        {
            var startIndex = context.NativeOpen.StopIndex + 1;
            var endIndex = context.NativeClose.StartIndex - 1;
            var length = endIndex - startIndex + 1;
            var nativeCode = System.IO.File.ReadAllText(FileName).Substring(startIndex, length) + "\n";
            byte platformId;

            if (!byte.TryParse(context.PlatFormTarget.Text, out platformId))
            {
                string errorMessage = String.Format("Unable to parse '{0}' to a numeric platform id.", context.PlatFormTarget.Text);
                Status.AddError(new CompilerError(FileName, context.PlatFormTarget.Line, context.PlatFormTarget.Column, errorMessage));
                platformId = 0;
            }

            if (platformId == CodeGenerator.GetPlatform())
            {
                return nativeCode;
            }
            else
            {
                return null;
            }
        }
    }
}
