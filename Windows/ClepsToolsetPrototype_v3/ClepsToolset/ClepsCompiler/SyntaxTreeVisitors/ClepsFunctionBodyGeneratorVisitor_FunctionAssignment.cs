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
        private class TemplateFunction
        {
            public ClepsParser.FunctionAssignmentContext FunctionAssignmentContext { get; private set; }
            public string SourceMemberOfCreation { get; private set; }

            public TemplateFunction(ClepsParser.FunctionAssignmentContext functionAssignmentContext, string sourceMemberOfCreation)
            {
                FunctionAssignmentContext = functionAssignmentContext;
                SourceMemberOfCreation = sourceMemberOfCreation;
            }
        }

        private List<TemplateFunction> TemplateFunctions = new List<TemplateFunction>();

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
                templateParameters = context._FunctionTemplateTypes.Select(t => new GenericClepsType(TypeManager, t.GetText())).ToList() :
                new List<GenericClepsType>();

            List<ClepsVariable> functionParameters = context._FunctionParameters.Select(p => Visit(p) as ClepsVariable).ToList();

            FunctionClepsType functionType = new FunctionClepsType(TypeManager, templateParameters, functionParameters.Select(p => p.VariableType).ToList(), returnType);

            var newMethod = CodeGenerator.CreateNewMethod(functionType);
            CurrMethodGenerator = newMethod;

            CurrMethodGenerator.SetFormalParameterNames(functionParameters.Select(p => p.VariableName).ToList());

            functionParameters.ForEach(variable => {
                variableManager.AddLocalVariable(variable, CurrMethodGenerator.GetFormalParameterRegister(variable.VariableName));
            });

            if (templateParameters.Count == 0)
            {
                Visit(context.statementBlock());
            }
            else
            {
                TemplateFunctions.Add(new TemplateFunction(context, CurrMemberName));
            }

            VariableManagers.RemoveAt(VariableManagers.Count - 1);
            CurrMethodGenerator = oldCurrMethodRegister;
            return newMethod;
        }
    }
}
