using Antlr4.Runtime.Misc;
using ClepsCompiler.CompilerBackend;
using ClepsCompiler.CompilerCore;
using ClepsCompiler.CompilerStructures;
using ClepsCompiler.CompilerTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.SyntaxTreeVisitors
{
    partial class ClepsFunctionBodyGeneratorVisitor
    {
        public override IMethodValue VisitFunctionAssignment_Ex([NotNull] ClepsParser.FunctionAssignmentContext context)
        {
            var oldCurrMethodRegister = CurrMethodGenerator;
            VariableManager variableManager = new VariableManager();
            VariableManagers.Add(variableManager);

            ClepsType returnType = VoidClepsType.GetVoidType();
            if (context.FunctionReturnType != null)
            {
                returnType = Visit(context.FunctionReturnType) as ClepsType;
            }

            List<GenericClepsType> templateParameters = context._FunctionTemplateTypes != null ?
                context._FunctionTemplateTypes.Select(t => new GenericClepsType(TypeManager, t.GetText())).ToList() :
                new List<GenericClepsType>();

            List<ClepsVariable> functionParameters = context._FunctionParameters.Select(p => Visit(p) as ClepsVariable).ToList();
            List<ClepsType> parameterTypes = functionParameters.Select(p => p.VariableType).ToList();

            FunctionClepsType functionType = new FunctionClepsType(TypeManager, templateParameters, parameterTypes, returnType);
            IMethodValue newMethod = null;
            bool generateFunction;

            if (templateParameters.Count != 0)
            {
                if(functionType.TemplateParameters.All(t => TemplateReplacementsToUse.ContainsKey(t)))
                {
                    TemplateReplacementsToUsefunctionType.ReplaceTemplateTypeComponents()
                    List<ClepsType> replacedParameterTypes = parameterTypes.Select(p => (p is GenericClepsType)? TemplateReplacementsToUse[p as GenericClepsType] : p).ToList();
                    ClepsType replacedReturn
                    functionType = new FunctionClepsType(replacedParameterTypes, )
                    generateFunction = true;
                }
                else
                {
                    TemplateManager.CreatePossibleFunctionAssignment(FullyQualifiedClassName, CurrMemberName, null /* only class members supported for now */, context);
                    generateFunction = false;
                }
            }
            else
            {
                generateFunction = true;
            }


            if(generateFunction)
            {
                newMethod = CodeGenerator.CreateNewMethod(functionType);
                CurrMethodGenerator = newMethod;

                CurrMethodGenerator.SetFormalParameterNames(functionParameters.Select(p => p.VariableName).ToList());

                functionParameters.ForEach(variable => {
                    variableManager.AddLocalVariable(variable, CurrMethodGenerator.GetFormalParameterRegister(variable.VariableName));
                });
                Visit(context.statementBlock());
            }
            

            VariableManagers.RemoveAt(VariableManagers.Count - 1);
            CurrMethodGenerator = oldCurrMethodRegister;
            return newMethod;
        }
    }
}
