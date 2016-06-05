using ClepsCompiler.CompilerBackend;
using ClepsCompiler.CompilerStructures;
using ClepsCompiler.CompilerTypes;
using ClepsCompiler.Utils.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerCore
{
    class FunctionOverloadManager
    {
        public static bool FindMatchingFunctionType(TypeManager typeManager, List<ClepsVariable> functionOverloads, List<IValue> parameters, out int matchPosition, out string errorMessage)
        {
            if (functionOverloads.Count > 1)
            {
                throw new NotImplementedException("Function overloads not yet supported");
            }

            ClepsVariable functionOverload = functionOverloads[0];
            FunctionClepsType functionOverloadType = functionOverload.VariableType as FunctionClepsType;

            if (parameters.Count != functionOverloadType.ParameterTypes.Count)
            {
                return ReturnErrorForFindMatchingFunctionType(parameters, out matchPosition, out errorMessage);
            }

            FunctionClepsType functionTypeToTest = functionOverloadType;

            if (functionOverloadType.HasGenericComponents)
            {
                var replacementsMade = new Dictionary<GenericClepsType, ClepsType>();

                SuccessStatus parameterReplaceStatus = functionOverloadType.ParameterTypes.Zip(parameters.Select(p => p.ExpressionType).ToList(), (parameterType, concreteParameterType) =>
                {
                    if (!parameterType.HasGenericComponents)
                    {
                        return typeManager.IsSameOrSubTypeOf(parameterType, concreteParameterType) ? SuccessStatus.Success : SuccessStatus.Failure;
                    }
                    else
                    {
                        return parameterType.ReplaceWithConcreteType(concreteParameterType, replacementsMade);
                    }
                }).Any(s => s == SuccessStatus.Failure) ? SuccessStatus.Failure : SuccessStatus.Success;

                if (parameterReplaceStatus == SuccessStatus.Failure)
                {
                    return ReturnErrorForFindMatchingFunctionType(parameters, out matchPosition, out errorMessage);
                }

                foreach (var replacement in replacementsMade)
                {
                    functionTypeToTest = functionTypeToTest.ReplaceTemplateTypeComponents(replacement.Key, replacement.Value) as FunctionClepsType;
                }

                if (functionTypeToTest.HasGenericComponents)
                {
                    throw new NotImplementedException("Non inferred template types not supported");
                }
            }

            if (functionTypeToTest.ParameterTypes.Zip(parameters.Select(p => p.ExpressionType).ToList(), (targetType, callType) => typeManager.IsSameOrSubTypeOf(targetType, callType)).All(v => v))
            {
                matchPosition = 0;
                errorMessage = null;
                return true;
            }
            else
            {
                return ReturnErrorForFindMatchingFunctionType(parameters, out matchPosition, out errorMessage);
            }
        }

        private static bool ReturnErrorForFindMatchingFunctionType(List<IValue> parameters, out int matchPosition, out string errorMessage)
        {
            string parametersString = String.Join(",", parameters.Select(p => p.ExpressionType.GetClepsTypeString()).ToList());
            matchPosition = -1;
            errorMessage = String.Format("No function overload supports parameters of type: ({0})", parametersString);
            return false;
        }

        public static bool MatchingFunctionTypeExists(List<FunctionClepsType> functionOverloads, FunctionClepsType typeToFind)
        {
            FunctionClepsType matchingTypeIfExists = functionOverloads.Where(o => o == typeToFind).FirstOrDefault();
            return matchingTypeIfExists != null;
        }
    }
}
