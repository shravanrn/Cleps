using ClepsCompiler.CompilerStructures;
using ClepsCompiler.Utils.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClepsCompiler.CompilerTypes;
using ClepsCompiler.Utils.Collections;
using System.Collections.Specialized;

namespace ClepsCompiler.CompilerBackend.Backends.JavaScript
{
    class JavaScriptMethod : IMethodRegister
    {
        private StringBuilder methodBody = new StringBuilder();
        private List<string> variablesCreated = new List<string>();
        private FunctionClepsType MethodType;
        private OrderedDictionary<string, string> FormalParameters;

        private int indentationLevel = 1;

        public JavaScriptMethod(FunctionClepsType methodType)
        {
            MethodType = methodType;
        }

        public void AddNativeCode(string nativeCode)
        {
            methodBody.Append(nativeCode);
        }

        public void SetFormalParameterNames(List<string> formalParameters)
        {
            FormalParameters = new OrderedDictionary<string, string>();

            formalParameters.ForEach(parameterName =>
            {
                string varNameUsed = NameGenerator.GetAvailableName(parameterName, variablesCreated);
                variablesCreated.Add(varNameUsed);
                FormalParameters.Add(parameterName, varNameUsed);
            });
        }

        public IValueRegister GetFormalParameterRegister(string name)
        {
            int index = Array.IndexOf(FormalParameters.Keys.ToArray(), name);
            ClepsType paramType = MethodType.ParameterTypes[index];

            JavaScriptRegister register = new JavaScriptRegister(FormalParameters[name], paramType);
            return register;
        }

        public IValueRegister CreateNewVariable(ClepsVariable variable, IValue initialValue = null)
        {
            string varName = NameGenerator.GetAvailableName(variable.VariableName, variablesCreated);
            variablesCreated.Add(varName);

            if (initialValue == null)
            {
                AppendFormatLine("var {0};", varName);
            }
            else
            {
                AppendFormatLine("var {0} = {1};", varName, (initialValue as JavaScriptValue).Expression);
            }

            var ret = new JavaScriptRegister(varName, variable.VariableType);
            return ret;
        }

        public void CreateAssignment(IValueRegister targetRegister, IValue value)
        {
            JavaScriptRegister registerToUse = targetRegister as JavaScriptRegister;
            JavaScriptValue valueToUse = value as JavaScriptValue;

            AppendFormatLine("{0} = {1};", registerToUse.Expression, valueToUse.Expression);
        }

        public void CreateReturnStatement(IValue value)
        {
            JavaScriptValue valueToUse = value as JavaScriptValue;
            AppendFormatLine("return {0};", valueToUse == null ? "" : valueToUse.Expression);
        }

        public void CreateFunctionCallStatement(IValue value)
        {
            JavaScriptValue valueToUse = value as JavaScriptValue;
            AppendFormatLine("{0};", valueToUse.Expression);
        }

        public void CreateIfStatementBlock(IValue condition)
        {
            JavaScriptValue conditionToUse = condition as JavaScriptValue;
            AppendFormatLine("if({0}) {{", conditionToUse.Expression);
            indentationLevel++;
        }

        public void CloseBlock()
        {
            indentationLevel--;
            if(indentationLevel == 0)
            {
                throw new Exception("Got extra close block call");
            }

            AppendFormatLine("}}");
        }

        private void AppendFormatLine(string format, params object[] args)
        {
            methodBody.Append(new String('\t', indentationLevel));
            methodBody.AppendFormat(format, args);
            methodBody.AppendLine();
        }

        public string GetMethodBody()
        {
            var paramsList = FormalParameters == null? new List<string>() : FormalParameters.Values.ToList();
            return String.Format("function({0}) {{\n{1}}}", String.Join(", ", paramsList), methodBody.ToString());
        }
    }
}
