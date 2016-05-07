using ClepsCompiler.CompilerBackend;
using ClepsCompiler.CompilerStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;

namespace ClepsCompiler.SyntaxTreeVisitors
{
    class ClepsFunctionBodyAnalysisVisitor_Partial : ClepsAbstractVisitor
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

        public ClepsFunctionBodyAnalysisVisitor_Partial(CompileStatus status, ClassManager classManager, ICodeGenerator codeGenerator) : base(status, classManager, codeGenerator){ }

        public virtual object VisitFunctionAssignment_Ex([NotNull] ClepsParser.FunctionAssignmentContext context)
        {
            return VisitChildren(context);
        }

        public sealed override object VisitFunctionAssignment([NotNull] ClepsParser.FunctionAssignmentContext context)
        {
            var oldCurrentBlockStatus = CurrentBlockStatus;
            CurrentBlockStatus = new List<StatementBlockStatus>() { new StatementBlockStatus(StatementBlockSource.Function) };

            VisitFunctionAssignment_Ex(context);

            if(!CurrentBlockStatus.Last().ReturnStatementReached)
            {
                string errorMessage = "Not all pathways have return statements";
                Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
            }

            CurrentBlockStatus = oldCurrentBlockStatus;

            return true;
        }

        public virtual object VisitFunctionStatement_Ex([NotNull] ClepsParser.FunctionStatementContext context)
        {
            return VisitChildren(context);
        }

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
                VisitFunctionStatement_Ex(context);
                return true;
            }
        }

        public virtual object VisitFunctionReturnStatement_Ex([NotNull] ClepsParser.FunctionReturnStatementContext context)
        {
            return VisitChildren(context);
        }

        public sealed override object VisitFunctionReturnStatement([NotNull] ClepsParser.FunctionReturnStatementContext context)
        {
            CurrentBlockStatus.Last().ReturnStatementReached = true;
            VisitFunctionReturnStatement_Ex(context);
            return true;
        }

    }
}
