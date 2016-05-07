using ClepsCompiler.CompilerBackend;
using ClepsCompiler.CompilerTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerCore
{
    class FunctionOverloadManager
    {
        public bool FindMatchingFunctionType(List<ClepsType> functionOverloads, List<IValue> parameters, out int matchPostition, out string errorMessage)
        {
            if(functionOverloads.Count > 1)
            {
                throw new NotImplementedException("Function overloads not yet supported");
            }

            var functionOverload = functionOverloads[0] as FunctionClepsType;

            if(parameters.Count != functionOverload.ParameterTypes.Count)
            {
                string parametersString = String.Join(",", parameters.Select(p => p.ExpressionType.GetClepsTypeString()).ToList());
                matchPostition = -1;
                errorMessage = String.Format("No function overload supports parameters of type: ({0})", parametersString);
                return false;
            }
            else
            {
                matchPostition = 0;
                errorMessage = null;
                return true;
            }
        }
    }
}
