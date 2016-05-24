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
        public override IMethodValue VisitFunctionAssignment_Ex([NotNull] ClepsParser.FunctionAssignmentContext context)
        {
            var oldCurrMethodRegister = CurrMethodRegister;
            VariableManager variableManager = new VariableManager();
            VariableManagers.Add(variableManager);

            ClepsType returnType = VoidClepsType.GetVoidType();
            if (context.FunctionReturnType != null)
            {
                returnType = Visit(context.FunctionReturnType) as ClepsType;
            }

            List<ClepsVariable> functionParameters = context._FunctionParameters.Select(p => Visit(p) as ClepsVariable).ToList();

            FunctionClepsType functionType = new FunctionClepsType(functionParameters.Select(p => p.VariableType).ToList(), returnType);

            var newMethod = CodeGenerator.CreateNewMethod(functionType);
            CurrMethodRegister = newMethod;

            CurrMethodRegister.SetFormalParameterNames(functionParameters.Select(p => p.VariableName).ToList());

            functionParameters.ForEach(variable => {
                variableManager.AddLocalVariable(variable, CurrMethodRegister.GetFormalParameterRegister(variable.VariableName));
            });

            Visit(context.statementBlock());

            VariableManagers.RemoveAt(VariableManagers.Count - 1);
            CurrMethodRegister = oldCurrMethodRegister;
            return newMethod;
        }
    }
}
