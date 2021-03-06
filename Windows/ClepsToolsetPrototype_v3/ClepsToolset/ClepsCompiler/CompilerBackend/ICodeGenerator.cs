﻿using ClepsCompiler.CompilerStructures;
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

        void AddEntryPoint(string fullClassName, string functionName);
        void AddNativeCode(string nativeCode);

        void CreateClass(string className);
        void SetClassBodyAndCreateConstructor(ClepsClass clepsClass);

        IMethodValue GetClassInitializerRegister(string className);
        IMethodValue GetClassStaticInitializerRegister(string className);
        IMethodValue GetGlobalInitializerRegister();
        IMethodValue CreateNewMethod(FunctionClepsType functionType);

        IValue CreateByte(byte value);
        IValue CreateBoolean(bool value);
        IValue CreateArray(ClepsType arrayType, List<IValue> arrayElements);
        IValue CreateClassInstance(BasicClepsType instanceType, List<IValue> parameters);
        IValue GetThisInstanceValue(BasicClepsType thisInstanceType);
        IValue GetPtrToValue(IValue value);
        IValue GetDereferencedValueFromPtr(IValue value);
        IValue GetRegisterValue(IValueRegister register);
        IValue GetFunctionCallReturnValue(IValue target, BasicClepsType targetType, string targetFunctionName, FunctionClepsType clepsType, List<IValue> parameters);

        IValueRegister GetStaticFieldRegister(string className, string memberName, ClepsType memberType);
        IValueRegister GetMemberFieldRegisterFromSameClass(string className, string memberName, ClepsType memberType);
        IValueRegister GetConstantMemberFieldRegisterForWrite(string className, string memberName, ClepsType memberType);
        IValueRegister GetArrayElementRegister(IValue value, List<IValue> indexes);

        void Output(string directoryName, string fileNameWithoutExtension, CompileStatus compileStatus);
    }
}
