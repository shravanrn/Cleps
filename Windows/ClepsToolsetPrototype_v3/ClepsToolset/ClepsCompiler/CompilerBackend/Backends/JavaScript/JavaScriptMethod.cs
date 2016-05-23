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
using System.Text.RegularExpressions;

namespace ClepsCompiler.CompilerBackend.Backends.JavaScript
{
    class JavaScriptMethod : JavaScriptValue, IMethodValue
    {
        private StringBuilder methodBody = new StringBuilder();
        private List<string> variablesCreated = new List<string>();
        //public ClepsType ExpressionType { get; private set; }
        private OrderedDictionary<string, string> FormalParameters;

        private int IndentationLevel = 1;

        public override string Expression { get { return GetMethodText(); } }

        public JavaScriptMethod(FunctionClepsType methodType) : base("", methodType)
        {
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
                string varNameUsed = NameGenerator.GetAvailableName(JavaScriptCodeParameters.VARIABLEPREFIX + parameterName, variablesCreated);
                variablesCreated.Add(varNameUsed);
                FormalParameters.Add(parameterName, varNameUsed);
            });
        }

        public IValueRegister GetFormalParameterRegister(string name)
        {
            int index = Array.IndexOf(FormalParameters.Keys.ToArray(), name);
            ClepsType paramType = (ExpressionType as FunctionClepsType).ParameterTypes[index];

            JavaScriptRegister register = new JavaScriptRegister(FormalParameters[name], paramType);
            return register;
        }

        public IValueRegister CreateNewVariable(ClepsVariable variable, IValue initialValue = null)
        {
            string varName = NameGenerator.GetAvailableName(JavaScriptCodeParameters.VARIABLEPREFIX + variable.VariableName, variablesCreated);
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

            AppendFormatLine("{0} = {1};", registerToUse.Expression, valueToUse.Expression.Replace("\n", "\n" + new String('\t', IndentationLevel)));
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
            AppendFormatLine("if({0}[0]) {{", conditionToUse.Expression);
            IndentationLevel++;
        }

        public void CloseBlock()
        {
            IndentationLevel--;
            if(IndentationLevel == 0)
            {
                throw new Exception("Got extra close block call");
            }

            AppendFormatLine("}}");
        }

        private void AppendFormatLine(string format, params object[] args)
        {
            methodBody.Append(new String('\t', IndentationLevel));
            methodBody.AppendFormat(format, args);
            methodBody.AppendLine();
        }

        public string GetMethodText()
        {
            var paramsList = FormalParameters == null? new List<string>() : FormalParameters.Values.ToList();
            return String.Format("function({0}) {{\n{1}}}", String.Join(", ", paramsList), methodBody.ToString());
        }

        public string GetMethodBodyWithoutDeclaration()
        {
            var text = methodBody.ToString();
            text = Regex.Replace(text, "^\t", "");
            text = text.Replace("\n\t", "\n");
            return text;
        }
    }
}
