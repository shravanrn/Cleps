using Antlr4.Runtime.Misc;
using ClepsCompiler.CompilerBackend;
using ClepsCompiler.CompilerCore;
using ClepsCompiler.CompilerStructures;
using ClepsCompiler.CompilerTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.SyntaxTreeVisitors
{
    partial class ClepsFunctionBodyGeneratorVisitor
    {
        public override object VisitFunctionCallOnExpression([NotNull] ClepsParser.FunctionCallOnExpressionContext context)
        {
            IValue target = Visit(context.rightHandExpression()) as IValue;
            return doFunctionCall(context.functionCall(), target, target.ExpressionType, true, false /* allowVoidReturn */);
        }

        public override object VisitFunctionCallStatement([NotNull] ClepsParser.FunctionCallStatementContext context)
        {
            if(context.rightHandExpression() == null)
            {
                throw new NotImplementedException("Function call with target expression is not yet supported");
            }

            IValue target = Visit(context.rightHandExpression()) as IValue;
            IValue functionCall = doFunctionCall(context.functionCall(), target, target.ExpressionType, true, true /* allowVoidReturn */);
            CurrMethodRegister.CreateFunctionCallStatement(functionCall);

            return true;
        }

        //public override object VisitFunctionCallAssignment([NotNull] ClepsParser.FunctionCallAssignmentContext context)
        //{
        //    if (!ClassManager.IsClassBodySet(FullyQualifiedClassName))
        //    {
        //        //This is probably due to some earlier error. Just return something to avoid stalling compilation
        //        return CodeGenerator.CreateByte(0);
        //    }

        //    ClepsType currentType = new BasicClepsType(FullyQualifiedClassName);
        //    return doFunctionCall(context.functionCall(), currentType, !CurrMemberIsStatic, false /* allowVoidReturn */);
        //}

        private IValue doFunctionCall(ClepsParser.FunctionCallContext functionCall, IValue target, ClepsType targetType, bool isMemberFunctionsAccessible, bool allowVoidReturn)
        {
            BasicClepsType dereferencedType = GetDereferencedTypeOrNull(targetType);
            if (dereferencedType == null)
            {
                string errorMessage = String.Format("Could not dereference expression on type {0}", targetType.GetClepsTypeString());
                Status.AddError(new CompilerError(FileName, functionCall.Start.Line, functionCall.Start.Column, errorMessage));
                //just return something to avoid stalling
                return CodeGenerator.CreateByte(0);
            }

            ClepsClass targetClepsClass = ClassManager.GetClass(dereferencedType.GetClepsTypeString());
            string targetFunctionName = functionCall.FunctionName.GetText();
            List<IValue> parameters = functionCall._FunctionParameters.Select(p => Visit(p) as IValue).ToList();

            List<ClepsVariable> functionOverloads;
            if (targetClepsClass.StaticMemberMethods.ContainsKey(targetFunctionName))
            {
                functionOverloads = targetClepsClass.StaticMemberMethods[targetFunctionName];
            }
            else if (isMemberFunctionsAccessible && targetClepsClass.MemberMethods.ContainsKey(targetFunctionName))
            {
                functionOverloads = targetClepsClass.MemberMethods[targetFunctionName];
            }
            else
            {
                string errorMessage = String.Format("Class {0} does not contain a {1}static function called {2}.", targetClepsClass.FullyQualifiedName, isMemberFunctionsAccessible? "member or " : "",targetFunctionName);
                Status.AddError(new CompilerError(FileName, functionCall.Start.Line, functionCall.Start.Column, errorMessage));
                //Just return something to avoid stalling compilation
                return CodeGenerator.CreateByte(0);
            }

            int matchedPosition;
            string fnMatchErrorMessage;

            if (!FunctionOverloadManager.FindMatchingFunctionType(functionOverloads, parameters, out matchedPosition, out fnMatchErrorMessage))
            {
                Status.AddError(new CompilerError(FileName, functionCall.Start.Line, functionCall.Start.Column, fnMatchErrorMessage));
                //Just return something to avoid stalling compilation
                return CodeGenerator.CreateByte(0);
            }

            FunctionClepsType chosenFunctionType = functionOverloads[matchedPosition].VariableType as FunctionClepsType;

            if (!allowVoidReturn && chosenFunctionType.ReturnType == VoidClepsType.GetVoidType())
            {
                string errorMessage = String.Format("Function {0} does not return a value", targetFunctionName);
                Status.AddError(new CompilerError(FileName, functionCall.Start.Line, functionCall.Start.Column, errorMessage));
                //Just return something to avoid stalling compilation
                return CodeGenerator.CreateByte(0);
            }

            IValue returnValue = CodeGenerator.GetFunctionCallReturnValue(targetType.GetClepsTypeString(), targetFunctionName, chosenFunctionType, parameters);
            return returnValue;
        }

    }
}
