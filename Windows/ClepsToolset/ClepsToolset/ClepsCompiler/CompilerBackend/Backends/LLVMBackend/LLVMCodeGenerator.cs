using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClepsCompiler.CompilerStructures;
using ClepsCompiler.CompilerTypes;
using LLVMSharp;
using System.Runtime.InteropServices;
using System.IO;

namespace ClepsCompiler.CompilerBackend.Backends.LLVMBackend
{
    class LLVMCodeGenerator : ICodeGenerator
    {
        private Dictionary<string, LLVMTypeRef> ClassSkeletons;
        private LLVMContextRef Context;
        private LLVMModuleRef Module;
        private LLVMBuilderRef Builder;

        public void Initiate()
        {
            ClassSkeletons = new Dictionary<string, LLVMTypeRef>();
            Context = LLVM.ContextCreate();
            Module = LLVM.ModuleCreateWithNameInContext("ADefaultModuleId", Context);
            Builder = LLVM.CreateBuilderInContext(Context);
        }

        public void Close()
        {
            try {
                LLVM.DisposeBuilder(Builder);
            } catch { }

            try {
                LLVM.DisposeModule(Module);
            } catch { }

            try {
                LLVM.ContextDispose(Context);
            } catch { }
        }

        public void CreateClass(string className)
        {
            LLVMTypeRef structType = LLVM.StructCreateNamed(Context, className);
            ClassSkeletons[className] = structType;
        }

        public void SetClassBodyAndCreateConstructor(ClepsClass clepsClass)
        {
            var memberTypes = clepsClass.MemberVariables.Values.Select(c => GetLLVMType(c)).ToArray();
            LLVM.StructSetBody(ClassSkeletons[clepsClass.FullyQualifiedName], memberTypes, false /* packed */);
        }

        public IMethodRegister CreateMethod(string className, bool isStatic, ClepsType functionType, string functionName)
        {
            string fullFunctionName = className + "." + functionName;
            FunctionClepsType functionClepsType = functionType as FunctionClepsType;
            LLVMTypeRef returnLLVMType = GetLLVMType(functionClepsType.ReturnType);
            LLVMTypeRef[] parameterLLVMTypes = functionClepsType.ParameterTypes.Select(c => GetLLVMType(c)).ToArray();

            LLVMTypeRef functionLLVMType = LLVM.FunctionType(returnLLVMType, parameterLLVMTypes, false /* var args */);
            LLVMValueRef functionRef = LLVM.AddFunction(Module, fullFunctionName, functionLLVMType);
            LLVMBasicBlockRef block = LLVM.AppendBasicBlockInContext(Context, functionRef, "entry");
            LLVM.PositionBuilderAtEnd(Builder, block);

            var ret = new LLVMMethod(block);
            return ret;
        }

        public void Output(string directoryName, string fileNameWithoutExtension, CompileStatus status)
        {
            var outputFileName = Path.Combine(directoryName, fileNameWithoutExtension + ".js");
            IntPtr llvmErrorMessagePtr;
            LLVMBool llvmFailure = LLVM.PrintModuleToFile(Module, outputFileName, out llvmErrorMessagePtr);
            string errorMessage = Marshal.PtrToStringAnsi(llvmErrorMessagePtr);
            LLVM.DisposeMessage(llvmErrorMessagePtr);

            if (llvmFailure)
            {
                status.AddError(new CompilerError(outputFileName, 0, 0, "Module Output failed : " + errorMessage));
            }
        }

        private LLVMTypeRef GetLLVMType(ClepsType clepsType)
        {
            string clepsName = clepsType.GetClepsTypeString();
            LLVMTypeRef llvmType;

            if (clepsName.StartsWith("System.RawTypes."))
            {
                if (clepsName == "System.RawTypes.Byte")
                {
                    llvmType = LLVM.Int1TypeInContext(Context);
                }
                else
                {
                    throw new NotImplementedException("Rawtype " + clepsName + " not supported");
                }
            }
            else
            {
                llvmType = ClassSkeletons[clepsName];
            }

            return llvmType;
        }

        public IMethodRegister GetMethodRegister(string className, bool isStatic, ClepsType functionType, string functionName)
        {
            throw new NotImplementedException();
        }

        public IValue CreateByte(byte value)
        {
            throw new NotImplementedException();
        }

        public IValue CreateArray(ClepsType arrayType, List<IValue> arrayElements)
        {
            throw new NotImplementedException();
        }

        public IValue GetRegisterValue(IValueRegister register)
        {
            throw new NotImplementedException();
        }

        public IValue GetFunctionCallReturnValue(string fullyQualifiedClassName, string targetFunctionName, FunctionClepsType clepsType, List<IValue> parameters)
        {
            throw new NotImplementedException();
        }

        public IValueRegister GetArrayElementRegister(IValue value, List<IValue> indexes)
        {
            throw new NotImplementedException();
        }
    }
}
