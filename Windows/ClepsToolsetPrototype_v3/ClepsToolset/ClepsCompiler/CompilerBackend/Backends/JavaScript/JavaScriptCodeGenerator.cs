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
        private Dictionary<FunctionContainer, JavaScriptMethod> MethodBodies;
        private Dictionary<string, JavaScriptMethod> ClassInitializers;
        private Dictionary<string, JavaScriptMethod> ClassStaticInitializers;
        private JavaScriptMethod GlobalInitializer;
        private List<string> NamespacesCreated;

        public void Initiate()
        {
            FunctionClepsType voidFuncType = new FunctionClepsType(new List<ClepsType>(), VoidClepsType.GetVoidType());

            ClassesLoaded = new Dictionary<string, ClepsClass>();
            MethodBodies = new Dictionary<FunctionContainer, JavaScriptMethod>();
            ClassInitializers = new Dictionary<string, JavaScriptMethod>();
            ClassStaticInitializers = new Dictionary<string, JavaScriptMethod>();
            GlobalInitializer = new JavaScriptMethod(voidFuncType);
            NamespacesCreated = new List<string>();
        }

        public void Close() { }

        public byte GetPlatform()
        {
            return 1;
        }

        public void CreateClass(string className)
        {
            ClassesLoaded.Add(className, null);
            ClassInitializers[className] = new JavaScriptMethod(new FunctionClepsType(new List<ClepsType>(), VoidClepsType.GetVoidType()));
            ClassStaticInitializers[className] = new JavaScriptMethod(new FunctionClepsType(new List<ClepsType>(), VoidClepsType.GetVoidType()));

            //static initializing should occur only once
            ClassStaticInitializers[className].CreateIfStatementBlock(new JavaScriptValue("!" + JavaScriptCodeParameters.TOPLEVELNAMESPACE + "." + className + ".classStaticInitialized", CompilerConstants.ClepsBoolType));
            ClassStaticInitializers[className].CreateAssignment(new JavaScriptRegister(JavaScriptCodeParameters.TOPLEVELNAMESPACE + "." + className + ".classStaticInitialized", CompilerConstants.ClepsBoolType), new JavaScriptValue("true", CompilerConstants.ClepsBoolType));
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

        //public IMethodValue CreateMethod(string className, bool isStatic, FunctionClepsType functionType, string functionName)
        //{
        //    var functionContainer = new FunctionContainer(className, functionName, functionType);
        //    var methodRegister = new JavaScriptMethod(functionType);

        //    MethodBodies.Add(functionContainer, methodRegister);
        //    return methodRegister;
        //}

        //public IMethodValue GetMethodRegister(string className, bool isStatic, FunctionClepsType functionType, string functionName)
        //{
        //    FunctionContainer methodRegisterKey = new FunctionContainer(className, functionName, functionType);
        //    return MethodBodies[methodRegisterKey];
        //}

        public IValue CreateByte(byte value)
        {
            return new JavaScriptValue(value.ToString(), CompilerConstants.ClepsByteType);
        }

        public IValue CreateBoolean(bool value)
        {
            return new JavaScriptValue(value ? "true" : "false", CompilerConstants.ClepsBoolType);
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

        public IValue GetRegisterValue(IValueRegister register)
        {
            JavaScriptRegister registerToUse = register as JavaScriptRegister;

            var ret = new JavaScriptValue(registerToUse.Expression, registerToUse.ExpressionType);
            return ret;
        }

        public IValue GetFunctionCallReturnValue(string fullyQualifiedClassName, string targetFunctionName, FunctionClepsType clepsType, List<IValue> parameters)
        {
            string fullFunctionName = String.Format("{0}.{1}.{2}", JavaScriptCodeParameters.TOPLEVELNAMESPACE, fullyQualifiedClassName, JavaScriptCodeParameters.GetMangledFunctionName(targetFunctionName, clepsType));

            string parameterString = String.Join(", ", parameters.Select(v => (v as JavaScriptValue).Expression).ToList());
            string code = String.Format("{0}({1})", fullFunctionName, parameterString);

            JavaScriptValue ret = new JavaScriptValue(code, clepsType.ReturnType);
            return ret;
        }

        public IValue GetAreByteValuesEqual(IValue leftValue, IValue rightValue)
        {
            JavaScriptValue leftValueToUse = leftValue as JavaScriptValue;
            JavaScriptValue rightValueToUse = rightValue as JavaScriptValue;

            string code = String.Format("({0} == {1})", leftValueToUse.Expression, rightValueToUse.Expression);

            JavaScriptValue ret = new JavaScriptValue(code, CompilerConstants.ClepsBoolType);
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

        public IValueRegister GetMemberFieldRegisterForWriteFromDifferentClass(string className, string memberName, ClepsType memberType)
        {
            JavaScriptRegister ret;
            if (memberType.IsFunctionType)
            {
                ret = new JavaScriptRegister(String.Format("{0}.{1}.prototype.{2}", JavaScriptCodeParameters.TOPLEVELNAMESPACE, className, JavaScriptCodeParameters.GetMangledFunctionName(memberName, memberType as FunctionClepsType)), memberType);
            }
            else
            {
                ret = new JavaScriptRegister(String.Format("{0}.{1}.{2}", JavaScriptCodeParameters.TOPLEVELNAMESPACE, className, memberName), memberType);
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
            var outputter = new JavaScriptCodeOutputter(ClassesLoaded, MethodBodies, ClassInitializers, ClassStaticInitializers, GlobalInitializer, NamespacesCreated);
            outputter.Output(directoryName, fileNameWithoutExtension, status);
        }
    }
}
