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
        void Initiate();
        void Close();
        byte GetPlatform();

        void CreateClass(string className);
        void SetClassBodyAndCreateConstructor(ClepsClass clepsClass);

        IMethodRegister CreateMethod(string className, bool isStatic, ClepsType functionType, string functionName);
        IMethodRegister GetMethodRegister(string className, bool isStatic, ClepsType functionType, string functionName);

        IValue CreateByte(byte value);
        IValue CreateBoolean(bool value);
        IValue CreateArray(ClepsType arrayType, List<IValue> arrayElements);
        IValue CreateClassInstance(BasicClepsType instanceType, List<IValue> parameters);
        IValue GetRegisterValue(IValueRegister register);
        IValue GetFunctionCallReturnValue(string fullyQualifiedClassName, string targetFunctionName, FunctionClepsType clepsType, List<IValue> parameters);
        IValue GetAreByteValuesEqual(IValue leftValue, IValue rightValue);
        IValue GetClassStaticInstance(BasicStaticClepsType clepsClass);

        IValueRegister GetArrayElementRegister(IValue value, List<IValue> indexes);

        void Output(string directoryName, string fileNameWithoutExtension, CompileStatus compileStatus);
    }
}
