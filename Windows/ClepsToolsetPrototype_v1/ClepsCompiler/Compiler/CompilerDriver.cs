using Antlr4.Runtime;
using ClepsCompiler.CompilerHelpers;
using LLVMSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.Compiler
{
    /// <summary>
    /// This is the main driver/controller of the compilation process
    /// </summary>
    class CompilerDriver
    {
        private List<string> Files;
        private string OutputFileName;

        public CompilerDriver(List<string> files, string outputFileName)
        {
            Files = files;
            OutputFileName = outputFileName;
        }

        public CompileStatus CompileFiles()
        {
            ClassManager classManager = new ClassManager();
            CompileStatus status = new CompileStatus(false /* exit on first error */);

            LLVMContextRef context = LLVM.ContextCreate();
            LLVMModuleRef module = LLVM.ModuleCreateWithNameInContext(OutputFileName, context);
            LLVMBuilderRef builder = LLVM.CreateBuilderInContext(context);

            try
            {
                //Byte code is generated in multiple passes so that all member variables and functions are stubbed out before they are referred to in function bodies
                //This allows functions on the top of a file to call functions on the bottom of a file as well

                Dictionary<string, LLVMTypeRef> classSkeletons;

                {
                    ClepsClassNamesGeneratorParser classSkeletonGenerator = new ClepsClassNamesGeneratorParser(classManager, status, context, module, builder, out classSkeletons);
                    ParseFilesWithGenerator(classSkeletonGenerator, status);
                }

                ClepsLLVMTypeConvertor clepsLLVMTypeConvertor = new ClepsLLVMTypeConvertor(classSkeletons, context);

                {
                    ClepsMemberGeneratorParser memberGenerator = new ClepsMemberGeneratorParser(classManager, status, context, module, builder, clepsLLVMTypeConvertor);
                    ParseFilesWithGenerator(memberGenerator, status);
                }

                {
                    ClepsFunctionBodyGeneratorParser functionBodyGenerator = new ClepsFunctionBodyGeneratorParser(classManager, status, context, module, builder, clepsLLVMTypeConvertor);
                    ParseFilesWithGenerator(functionBodyGenerator, status);
                }

                AddEntryPoint(classManager, status, context, module, builder);

                VerifyModule(module, status);
                PrintModuleToFile(module, status);
            }
            catch (CompilerErrorException)
            {
                //Supress compiler errors
            }
            finally
            {
                LLVM.DisposeBuilder(builder);
                LLVM.DisposeModule(module);
                LLVM.ContextDispose(context);
            }

            return status;
        }

        private void ParseFilesWithGenerator<T>(ClepsAbstractParser<T> generator, CompileStatus status)
        {
            foreach (string file in Files)
            {
                LexerParserErrorHandler lexerParserErrorHandler = new LexerParserErrorHandler(file, status);
                var data = File.ReadAllText(file);

                AntlrInputStream s = new AntlrInputStream(data);
                ClepsLexer lexer = new ClepsLexer(s);
                CommonTokenStream tokens = new CommonTokenStream(lexer);
                ClepsParser parser = new ClepsParser(tokens);

                parser.RemoveErrorListeners();
                parser.AddErrorListener(lexerParserErrorHandler);
                var parsedFile = parser.compilationUnit();

                generator.ParseFile(file, parsedFile);
            }
        }

        private void AddEntryPoint(ClassManager classManager, CompileStatus status, LLVMContextRef context, LLVMModuleRef module, LLVMBuilderRef builder)
        {
            LLVMTypeRef functionType = LLVM.FunctionType(LLVM.Int32TypeInContext(context), new LLVMTypeRef[] { }, false);
            LLVMValueRef functionValue = LLVM.AddFunction(module, "main", functionType);
            LLVMBasicBlockRef blockValue = LLVM.AppendBasicBlockInContext(context, functionValue, "entry");
            LLVM.PositionBuilderAtEnd(builder, blockValue);

            LLVMValueRef intRet = LLVM.ConstInt(LLVM.Int32Type(), 0, false);

            if (classManager.MainFunctionFullNames.Count < 1)
            {
                status.AddError(new CompilerError("", 0, 0, "No main functions found in the program"));
            }
            else if (classManager.MainFunctionFullNames.Count > 1)
            {
                status.AddError(new CompilerError("", 0, 0, "Multiple main functions found in the program: " + String.Join(",", classManager.MainFunctionFullNames)));
            }
            else
            {
                LLVMValueRef functionToCall = LLVM.GetNamedFunction(module, classManager.MainFunctionFullNames.First());
                LLVMValueRef intOrIntMappedTypeRet = LLVM.BuildCall(builder, functionToCall, new LLVMValueRef[0], "entryPointCall");
                LLVMTypeRef returnType = LLVM.TypeOf(intOrIntMappedTypeRet);

                if (returnType.TypeKind == LLVMTypeKind.LLVMIntegerTypeKind && returnType.GetIntTypeWidth() == 32)
                {
                    intRet = intOrIntMappedTypeRet;
                }
                else
                {
                    LLVMValueRef intMappedTypeRetPtr = LLVM.BuildAlloca(builder, LLVM.TypeOf(intOrIntMappedTypeRet), "intMappedType");
                    LLVM.BuildStore(builder, intOrIntMappedTypeRet, intMappedTypeRetPtr);
                    //Extract the first field to get the int value from the mapped type
                    //See rawtypemap for more details
                    LLVMValueRef fieldPtr = LLVM.BuildStructGEP(builder, intMappedTypeRetPtr, 0, "returnIntFieldPtr");
                    intRet = LLVM.BuildLoad(builder, fieldPtr, "returnValue");
                }
            }

            LLVM.BuildRet(builder, intRet);
        }

        private void VerifyModule(LLVMModuleRef module, CompileStatus status)
        {
            IntPtr llvmErrorMessagePtr;
            //VerifyModule returns an inverted result...
            LLVMBool llvmFailure = LLVM.VerifyModule(module, LLVMVerifierFailureAction.LLVMReturnStatusAction, out llvmErrorMessagePtr);
            string errorMessage = Marshal.PtrToStringAnsi(llvmErrorMessagePtr);
            LLVM.DisposeMessage(llvmErrorMessagePtr);

            if (llvmFailure)
            {
                status.AddError(new CompilerError(OutputFileName, 0, 0, "Module Verification failed : " + errorMessage));
            }
        }

        private void PrintModuleToFile(LLVMModuleRef module, CompileStatus status)
        {
            IntPtr llvmErrorMessagePtr;
            LLVMBool llvmFailure = LLVM.PrintModuleToFile(module, OutputFileName, out llvmErrorMessagePtr);
            string errorMessage = Marshal.PtrToStringAnsi(llvmErrorMessagePtr);
            LLVM.DisposeMessage(llvmErrorMessagePtr);

            if (llvmFailure)
            {
                status.AddError(new CompilerError(OutputFileName, 0, 0, "Module Output failed : " + errorMessage));
            }
        }
    }
}
