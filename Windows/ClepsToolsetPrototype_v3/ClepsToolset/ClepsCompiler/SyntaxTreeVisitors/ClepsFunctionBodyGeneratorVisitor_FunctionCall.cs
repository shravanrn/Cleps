using Antlr4.Runtime;
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
            return doFunctionCall(context.functionCall(), target, target.ExpressionType, false /* allowVoidReturn */);
        }

        public override object VisitFunctionCallStatement([NotNull] ClepsParser.FunctionCallStatementContext context)
        {
            IValue target;
            if (context.rightHandExpression() == null)
            {
                //if no target is specified, pretend this is called on 'this'
                target = CodeGenerator.GetThisInstanceValue(new BasicClepsType(FullyQualifiedClassName));
            }
            else
            {
                target = Visit(context.rightHandExpression()) as IValue;
            }

            IValue functionCall = doFunctionCall(context.functionCall(), target, target.ExpressionType, true /* allowVoidReturn */);
            CurrMethodGenerator.CreateFunctionCallStatement(functionCall);

            return true;
        }

        public override object VisitFunctionCallAssignment([NotNull] ClepsParser.FunctionCallAssignmentContext context)
        {
            BasicClepsType currentType = new BasicClepsType(FullyQualifiedClassName);
            IValue target = CurrMemberIsStatic? null : CodeGenerator.GetThisInstanceValue(currentType);
            return doFunctionCall(context.functionCall(), target, currentType, true /* allowVoidReturn */);
        }

        private IValue doFunctionCall([NotNull] ClepsParser.FunctionCallContext context, IValue target, ClepsType targetType, bool allowVoidReturn)
        {
            string targetFunctionName = context.FunctionName.GetText();
            List<IValue> parameters = context._FunctionParameters.Select(p => Visit(p) as IValue).ToList();
            return doFunctionCall(context, targetFunctionName, parameters, target, targetType, allowVoidReturn);
        }

        private IValue doFunctionCall(ParserRuleContext context, string targetFunctionName, List<IValue> parameters, IValue target, ClepsType targetType, bool allowVoidReturn)
        {
            IValue dereferencedTarget;
            BasicClepsType dereferencedType;
            List<ClepsVariable> functionOverloads;
            bool isStatic;

            if (targetType is FunctionClepsType)
            {
                throw new NotImplementedException("Calling local variable lambda functionsnot yet supported");
            }
            else
            {
                dereferencedTarget = target == null ? null : GetDereferencedRegisterOrNull(target);
                dereferencedType = target == null ? targetType as BasicClepsType : dereferencedTarget.ExpressionType as BasicClepsType;

                if (dereferencedType == null)
                {
                    string errorMessage = String.Format("Could not dereference expression on type {0}", targetType.GetClepsTypeString());
                    Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                    //just return something to avoid stalling
                    return CodeGenerator.CreateByte(0);
                }

                ClepsClass targetClepsClass = ClassManager.GetClass(dereferencedType.GetClepsTypeString());

                if (targetClepsClass.StaticMemberMethods.ContainsKey(targetFunctionName))
                {
                    isStatic = true;
                    functionOverloads = targetClepsClass.StaticMemberMethods[targetFunctionName];
                }
                else if (target != null && targetClepsClass.MemberMethods.ContainsKey(targetFunctionName))
                {
                    isStatic = false;
                    functionOverloads = targetClepsClass.MemberMethods[targetFunctionName];
                }
                else
                {
                    string errorMessage = String.Format("Class {0} does not contain a {1}static function called {2}.", targetClepsClass.FullyQualifiedName, target == null ? "" : "member or ", targetFunctionName);
                    Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                    //Just return something to avoid stalling compilation
                    return CodeGenerator.CreateByte(0);
                }
            }

            int matchedPosition;
            Dictionary<GenericClepsType, ClepsType> replacementsMade;
            FunctionClepsType chosenTargetFunctionType;
            string fnMatchErrorMessage;

            if (!FunctionOverloadManager.FindMatchingFunctionType(TypeManager, functionOverloads, parameters, out matchedPosition, out replacementsMade, out chosenTargetFunctionType, out fnMatchErrorMessage))
            {
                Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, fnMatchErrorMessage));
                //Just return something to avoid stalling compilation
                return CodeGenerator.CreateByte(0);
            }

            FunctionClepsType chosenSourceFunctionType = functionOverloads[matchedPosition].VariableType as FunctionClepsType;

            if(chosenSourceFunctionType.HasGenericComponents)
            {
                TemplateFunction chosenTemplateFunction = TemplateFunctions.Where(t => t.SourceClassOfCreation == dereferencedType.GetClepsTypeString() && t.SourceMemberOfCreation == targetFunctionName).FirstOrDefault();

                if(chosenTemplateFunction == null)
                {
                    string errorMessage = String.Format("Function {0} in Class {1} has never been initialized.", targetFunctionName, dereferencedType.GetClepsTypeString());
                    Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                    //Just return something to avoid stalling compilation
                    return CodeGenerator.CreateByte(0);
                }

                var oldTemplateReplacementsToUse = TemplateReplacementsToUse;
                TemplateReplacementsToUse = replacementsMade;

                Visit(chosenTemplateFunction.FunctionAssignmentContext);

                TemplateReplacementsToUse = oldTemplateReplacementsToUse;
            }

            if (!allowVoidReturn && chosenTargetFunctionType.ReturnType == VoidClepsType.GetVoidType())
            {
                string errorMessage = String.Format("Function {0} does not return a value", targetFunctionName);
                Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                //Just return something to avoid stalling compilation
                return CodeGenerator.CreateByte(0);
            }

            IValue returnValue = CodeGenerator.GetFunctionCallReturnValue(isStatic? null : dereferencedTarget, dereferencedType, targetFunctionName, chosenTargetFunctionType, parameters);
            return returnValue;
        }

    }
}
