using ClepsCompiler.CompilerStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClepsCompiler.CompilerTypes;

namespace ClepsCompiler.CompilerBackend
{
    interface ICodeGenerator
    {
        void CreateClass(string className);
        void Initiate();
        void Close();
        void SetClassBodyAndCreateConstructor(ClepsClass clepsClass);
        IMethodRegister CreateMethod(string className, bool isStatic, ClepsType functionType, string functionName);
        IMethodRegister GetMethodRegister(string className, bool isStatic, ClepsType functionType, string functionName);

        IValue CreateByte(byte value);
        IValue CreateArray(ClepsType arrayType, List<IValue> arrayElements);
        IValue GetRegisterValue(IValueRegister register);
        IValue GetFunctionCallReturnValue(string fullyQualifiedClassName, string targetFunctionName, FunctionClepsType clepsType, List<IValue> parameters);

        IValueRegister GetArrayElementRegister(IValue value, List<IValue> indexes);

        void Output(string directoryName, string fileNameWithoutExtension, CompileStatus compileStatus);
    }
}
