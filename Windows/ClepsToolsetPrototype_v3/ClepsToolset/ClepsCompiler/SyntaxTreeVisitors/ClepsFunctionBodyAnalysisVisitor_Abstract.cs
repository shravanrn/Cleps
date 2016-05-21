using ClepsCompiler.CompilerBackend;
using ClepsCompiler.CompilerStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;
using ClepsCompiler.CompilerTypes;
using ClepsCompiler.CompilerCore;

namespace ClepsCompiler.SyntaxTreeVisitors
{
    class ClepsFunctionBodyAnalysisVisitor_Abstract : ClepsAbstractVisitor
    {
        private enum StatementBlockSource
        {
            Function, Other
        }

        private class StatementBlockStatus
        {
            public StatementBlockSource Source;
            public bool ReturnStatementReached = false;

            public StatementBlockStatus(StatementBlockSource source)
            {
                Source = source;
            }
        }

        private List<StatementBlockStatus> CurrentBlockStatus;

        public ClepsFunctionBodyAnalysisVisitor_Abstract(CompileStatus status, ClassManager classManager, ICodeGenerator codeGenerator) : base(status, classManager, codeGenerator){ }


        public virtual IMethodValue VisitFunctionAssignment_Ex([NotNull] ClepsParser.FunctionAssignmentContext context) { return VisitChildren(context) as IMethodValue; }

        public sealed override object VisitFunctionAssignment([NotNull] ClepsParser.FunctionAssignmentContext context)
        {
            var oldCurrentBlockStatus = CurrentBlockStatus;
            CurrentBlockStatus = new List<StatementBlockStatus>() { new StatementBlockStatus(StatementBlockSource.Function) };

            IMethodValue functionType = VisitFunctionAssignment_Ex(context);

            if ((functionType.ExpressionType as FunctionClepsType).ReturnType != VoidClepsType.GetVoidType() && !CurrentBlockStatus.Last().ReturnStatementReached)
            {
                string errorMessage = "Not all pathways have return statements";
                Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
            }

            CurrentBlockStatus = oldCurrentBlockStatus;

            return functionType;
        }

        public virtual bool VisitFunctionStatement_Ex([NotNull] ClepsParser.FunctionStatementContext context) { VisitChildren(context); return true; }

        public sealed override object VisitFunctionStatement([NotNull] ClepsParser.FunctionStatementContext context)
        {
            if (CurrentBlockStatus.Last().ReturnStatementReached)
            {
                string errorMessage = String.Format("Statement is dead code", FullyQualifiedClassName);
                Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                return false;
            }
            else
            {
                var ret = VisitFunctionStatement_Ex(context);
                return ret;
            }
        }

        public virtual bool VisitFunctionReturnStatement_Ex([NotNull] ClepsParser.FunctionReturnStatementContext context) { VisitChildren(context); return true; }

        public sealed override object VisitFunctionReturnStatement([NotNull] ClepsParser.FunctionReturnStatementContext context)
        {
            CurrentBlockStatus.Last().ReturnStatementReached = true;
            var ret = VisitFunctionReturnStatement_Ex(context);
            return ret;
        }
    }
}
