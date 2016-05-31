using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClepsCompiler.CompilerStructures;
using ClepsCompiler.CompilerTypes;
using ClepsCompiler.CompilerBackend.Containers;
using ClepsCompiler.CompilerBackend;
using System.IO;

namespace ClepsCompiler.CompilerBackend.Backends.JavaScript
{
    class JavaScriptCodeGenerator : ICodeGenerator
    {
        private Dictionary<string, ClepsClass> ClassesLoaded;
        private Dictionary<string, JavaScriptMethod> ClassInitializers;
        private Dictionary<string, JavaScriptMethod> ClassStaticInitializers;
        private JavaScriptMethod GlobalInitializer;
        private string EntryPointClass;
        private string EntryPointFunctionName;
        private List<string> GlobalNativeCodeSnippets;

        public void Initiate()
        {
            FunctionClepsType voidFuncType = new FunctionClepsType(new List<ClepsType>(), VoidClepsType.GetVoidType());

            ClassesLoaded = new Dictionary<string, ClepsClass>();
            ClassInitializers = new Dictionary<string, JavaScriptMethod>();
            ClassStaticInitializers = new Dictionary<string, JavaScriptMethod>();
            GlobalInitializer = new JavaScriptMethod(voidFuncType);
            EntryPointClass = null;
            EntryPointFunctionName = null;
            GlobalNativeCodeSnippets = new List<string>();
        }

    public void Close() { }

        public byte GetPlatform()
        {
            return 1;
        }

        public void AddEntryPoint(string fullClassName, string functionName)
        {
            EntryPointClass = fullClassName;
            EntryPointFunctionName = functionName;
        }

        public void AddNativeCode(string nativeCode)
        {
            GlobalNativeCodeSnippets.Add(nativeCode);
        }

        public void CreateClass(string className)
        {
            ClassesLoaded.Add(className, null);
            ClassInitializers[className] = new JavaScriptMethod(new FunctionClepsType(new List<ClepsType>(), VoidClepsType.GetVoidType()));
            ClassStaticInitializers[className] = new JavaScriptMethod(new FunctionClepsType(new List<ClepsType>(), VoidClepsType.GetVoidType()));

            //static initializing should occur only once
            ClassStaticInitializers[className].CreateIfStatementBlock(new JavaScriptValue("![" + JavaScriptCodeParameters.TOPLEVELNAMESPACE + "." + className + ".classStaticInitialized]", CompilerConstants.ClepsBoolType));
            ClassStaticInitializers[className].CreateAssignment(new JavaScriptRegister(JavaScriptCodeParameters.TOPLEVELNAMESPACE + "." + className + ".classStaticInitialized", CompilerConstants.ClepsBoolType), new JavaScriptValue("[true]", CompilerConstants.ClepsBoolType));
        }

        public void SetClassBodyAndCreateConstructor(ClepsClass clepsClass)
        {
            ClassesLoaded[clepsClass.FullyQualifiedName] = clepsClass;
        }

        public IMethodValue GetClassInitializerRegister(string className)
        {
            return ClassInitializers[className];
        }

        public IMethodValue GetClassStaticInitializerRegister(string className)
        {
            return ClassStaticInitializers[className];
        }

        public IMethodValue GetGlobalInitializerRegister()
        {
            return GlobalInitializer;
        }

        public IMethodValue CreateNewMethod(FunctionClepsType functionType)
        {
            var methodRegister = new JavaScriptMethod(functionType);
            return methodRegister;
        }

        public IValue CreateByte(byte value)
        {
            return new JavaScriptValue(String.Format("[{0}]", value), CompilerConstants.ClepsByteType);
        }

        public IValue CreateBoolean(bool value)
        {
            return new JavaScriptValue(String.Format("[{0}]", value ? "true" : "false"), CompilerConstants.ClepsBoolType);
        }

        public IValue CreateArray(ClepsType arrayType, List<IValue> arrayElements)
        {
            string elementCode = String.Join(", ", arrayElements.Select(a => (a as JavaScriptValue).Expression).ToList());
            return new JavaScriptValue("[" + elementCode + "]", arrayType);
        }

        public IValue CreateClassInstance(BasicClepsType instanceType, List<IValue> parameters)
        {
            string parameterString = String.Join(", ", parameters.Select(v => (v as JavaScriptValue).Expression).ToList());
            string code = String.Format("new {0}.{1}({2})", JavaScriptCodeParameters.TOPLEVELNAMESPACE, instanceType.GetClepsTypeString(), parameterString);

            JavaScriptValue ret = new JavaScriptValue(code, instanceType);
            return ret;
        }

        public IValue GetThisInstanceValue(BasicClepsType thisInstanceType)
        {
            JavaScriptValue ret = new JavaScriptValue("this", thisInstanceType);
            return ret;
        }

        public IValue GetPtrToValue(IValue value)
        {
            JavaScriptValue valueToUse = value as JavaScriptValue;
            return new JavaScriptValue(String.Format("new clepsPtr({0})", valueToUse.Expression), new PointerClepsType(valueToUse.ExpressionType));
        }

        public IValue GetDereferencedValueFromPtr(IValue value)
        {
            JavaScriptValue valueToUse = value as JavaScriptValue;
            return new JavaScriptValue(String.Format("({0}).obj", valueToUse.Expression), (valueToUse.ExpressionType as PointerClepsType).BaseType);
        }

        public IValue GetRegisterValue(IValueRegister register)
        {
            JavaScriptRegister registerToUse = register as JavaScriptRegister;
            var ret = new JavaScriptValue(registerToUse.Expression, registerToUse.ExpressionType);
            return ret;
        }

        public IValue GetFunctionCallReturnValue(IValue target, BasicClepsType targetType, string targetFunctionName, FunctionClepsType clepsType, List<IValue> parameters)
        {
            string code;

            if (CompilerConstants.SystemSupportedTypes.Contains(targetType) && target != null)
            {
                string fullFunctionName = String.Format("{0}.{1}.prototype.{2}", JavaScriptCodeParameters.TOPLEVELNAMESPACE, targetType.GetClepsTypeString(), JavaScriptCodeParameters.GetMangledFunctionName(targetFunctionName, clepsType));
                string functionTarget = target != null ? (target as JavaScriptValue).Expression : String.Format("{0}.{1}", JavaScriptCodeParameters.TOPLEVELNAMESPACE, targetType.GetClepsTypeString());
                string parameterString = String.Join("", parameters.Select(v => ", " + (v as JavaScriptValue).Expression).ToList());

                code = String.Format("{0}.call({1}{2})", fullFunctionName, functionTarget, parameterString);
            }
            else
            {
                string functionTarget = target != null ? (target as JavaScriptValue).Expression : String.Format("{0}.{1}", JavaScriptCodeParameters.TOPLEVELNAMESPACE, targetType.GetClepsTypeString());
                string fullFunctionName = String.Format("{0}.{1}", functionTarget, JavaScriptCodeParameters.GetMangledFunctionName(targetFunctionName, clepsType));

                string parameterString = String.Join(", ", parameters.Select(v => (v as JavaScriptValue).Expression).ToList());
                code = String.Format("{0}({1})", fullFunctionName, parameterString);
            }

            JavaScriptValue ret = new JavaScriptValue(code, clepsType.ReturnType);
            return ret;
        }



        public IValueRegister GetStaticFieldRegister(string className, string memberName, ClepsType memberType)
        {
            JavaScriptRegister ret;
            if (memberType.IsFunctionType)
            {
                ret = new JavaScriptRegister(String.Format("{0}.{1}.{2}", JavaScriptCodeParameters.TOPLEVELNAMESPACE, className, JavaScriptCodeParameters.GetMangledFunctionName(memberName, memberType as FunctionClepsType)), memberType);
            }
            else
            {
                ret = new JavaScriptRegister(String.Format("{0}.{1}.{2}", JavaScriptCodeParameters.TOPLEVELNAMESPACE, className, memberName), memberType);
            }
            return ret;
        }

        public IValueRegister GetMemberFieldRegisterFromSameClass(string className, string memberName, ClepsType memberType)
        {
            var ret = new JavaScriptRegister("this." + memberName, memberType);
            return ret;
        }

        public IValueRegister GetConstantMemberFieldRegisterForWrite(string className, string memberName, ClepsType memberType)
        {
            JavaScriptRegister ret;
            if (memberType.IsFunctionType)
            {
                ret = new JavaScriptRegister(String.Format("{0}.{1}.prototype.{2}", JavaScriptCodeParameters.TOPLEVELNAMESPACE, className, JavaScriptCodeParameters.GetMangledFunctionName(memberName, memberType as FunctionClepsType)), memberType);
            }
            else
            {
                ret = new JavaScriptRegister(String.Format("{0}.{1}.prototype.{2}", JavaScriptCodeParameters.TOPLEVELNAMESPACE, className, memberName), memberType);
            }
            return ret;
        }

        public IValueRegister GetArrayElementRegister(IValue value, List<IValue> indexes)
        {
            JavaScriptValue valueToUse = value as JavaScriptValue;

            string code = String.Format("{0}{1}", valueToUse.Expression, String.Join("", indexes.Select(i => "[" + (i as JavaScriptValue).Expression + "]")));
            ClepsType returnType = (value.ExpressionType as ArrayClepsType).GetTypeForArrayAccess(indexes.Count);

            JavaScriptRegister ret = new JavaScriptRegister(code, returnType);
            return ret;
        }

        public void Output(string directoryName, string fileNameWithoutExtension, CompileStatus status)
        {
            //the static initializer function has an if statement created so that the initializer runs once. Closing this if block
            ClassStaticInitializers.Values.ToList().ForEach(m => m.CloseBlock());
            var outputter = new JavaScriptCodeOutputter(ClassesLoaded, ClassInitializers, ClassStaticInitializers, GlobalInitializer, EntryPointClass, EntryPointFunctionName, GlobalNativeCodeSnippets);
            outputter.Output(directoryName, fileNameWithoutExtension, status);
        }
    }
}
