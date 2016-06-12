﻿using Antlr4.Runtime.Misc;
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
                templateParameters = context._FunctionTemplateTypes.Select(t => new GenericClepsType(TypeManager, t.GetText())).ToList() :
                new List<GenericClepsType>();

            List<ClepsVariable> functionParameters = context._FunctionParameters.Select(p => Visit(p) as ClepsVariable).ToList();

            FunctionClepsType functionType = new FunctionClepsType(TypeManager, templateParameters, functionParameters.Select(p => p.VariableType).ToList(), returnType);
            IMethodValue newMethod = null;

            if (templateParameters.Count == 0)
            {
                newMethod = CodeGenerator.CreateNewMethod(functionType);
            CurrMethodGenerator = newMethod;

            CurrMethodGenerator.SetFormalParameterNames(functionParameters.Select(p => p.VariableName).ToList());

            functionParameters.ForEach(variable => {
                variableManager.AddLocalVariable(variable, CurrMethodGenerator.GetFormalParameterRegister(variable.VariableName));
            });
                Visit(context.statementBlock());
            }
            else
            {
                TemplateFunctions.Add(new TemplateFunction(context, FullyQualifiedClassName, CurrMemberName));
            }

            VariableManagers.RemoveAt(VariableManagers.Count - 1);
            CurrMethodGenerator = oldCurrMethodRegister;
            return newMethod;
        }
    }
}
