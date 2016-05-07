using ClepsCompiler.CompilerStructures;
using ClepsCompiler.Utils.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClepsCompiler.CompilerTypes;

namespace ClepsCompiler.CompilerBackend.Backends.JavaScript
{
    class JavaScriptMethod : IMethodRegister
    {
        private StringBuilder methodBody = new StringBuilder();
        private List<string> variablesCreated = new List<string>();

        public IValueRegister CreateNewVariable(ClepsVariable variable, IValue initialValue = null)
        {
            string varName = NameGenerator.GetAvailableName(variable.VariableName, variablesCreated);
            variablesCreated.Add(varName);

            if (initialValue == null)
            {
                methodBody.AppendFormat("\tvar {0};\n", varName);
            }
            else
            {
                methodBody.AppendFormat("\tvar {0} = {1};\n", varName, (initialValue as JavaScriptValue).Expression);
            }

            var ret = new JavaScriptRegister(varName, variable.VariableType);
            return ret;
        }

        public void CreateAssignment(IValueRegister targetRegister, IValue value)
        {
            JavaScriptRegister registerToUse = targetRegister as JavaScriptRegister;
            JavaScriptValue valueToUse = value as JavaScriptValue;

            methodBody.AppendFormat("\t{0} = {1};\n", registerToUse.Expression, valueToUse.Expression);
        }

        public void CreateReturnStatement(IValue value)
        {
            JavaScriptValue valueToUse = value as JavaScriptValue;
            methodBody.AppendFormat("\treturn {0};\n", valueToUse == null? "" : valueToUse.Expression);
        }

        public string GetMethodBody()
        {
            return methodBody.ToString();
        }
    }
}
