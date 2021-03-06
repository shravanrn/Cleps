﻿using System;
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
        private Dictionary<string, ClepsClass> classesLoaded = new Dictionary<string, ClepsClass>();
        private Dictionary<string, List<FunctionNameAndType>> staticMethods = new Dictionary<string, List<FunctionNameAndType>>();
        private Dictionary<string, List<FunctionNameAndType>> memberMethods = new Dictionary<string, List<FunctionNameAndType>>();
        private Dictionary<FunctionNameAndType, JavaScriptMethod> methodBodies = new Dictionary<FunctionNameAndType, JavaScriptMethod>();
        private const string TOPLEVELNAMESPACE = "Cleps";
        private List<string> namespacesCreated = new List<string>();

        public void Initiate() {}

        public void Close() {}

        public byte GetPlatform()
        {
            return 0;
        }

        public void CreateClass(string className)
        {
            if (!classesLoaded.ContainsKey(className))
            {
                classesLoaded.Add(className, null);
                staticMethods[className + ".static"] = new List<FunctionNameAndType>();
                memberMethods[className] = new List<FunctionNameAndType>();
            }
        }

        public void SetClassBodyAndCreateConstructor(ClepsClass clepsClass)
        {
            if(!classesLoaded.ContainsKey(clepsClass.FullyQualifiedName))
            {
                throw new ArgumentException(String.Format("Class {0} not loaded", clepsClass.FullyQualifiedName));
            }

            if(classesLoaded[clepsClass.FullyQualifiedName] != null)
            {
                throw new ArgumentException(String.Format("Body for class {0} already set", clepsClass.FullyQualifiedName));
            }

            classesLoaded[clepsClass.FullyQualifiedName] = clepsClass;
        }


        public IMethodRegister CreateMethod(string className, bool isStatic, ClepsType functionType, string functionName)
        {
            if (!classesLoaded.ContainsKey(className))
            {
                throw new ArgumentException(String.Format("Class {0} not loaded", className));
            }

            if (!functionType.IsFunctionType)
            {
                throw new ArgumentException("Expected function type. Got " + functionType.GetClepsTypeString());
            }

            var classNameToUse = isStatic ? className + ".static" : className;
            var functionNameAndType = new FunctionNameAndType(classNameToUse, functionName, functionType);
            var methodList = isStatic ? staticMethods[classNameToUse] : memberMethods[classNameToUse];

            if (methodList.Contains(functionNameAndType))
            {
                throw new ArgumentException(String.Format("Function {0} {1} {2} for class {3} already exists", isStatic ? "static" : "", functionType, functionName, className));
            }

            methodList.Add(functionNameAndType);
            var methodRegister = new JavaScriptMethod(functionType as FunctionClepsType);
            methodBodies.Add(functionNameAndType, methodRegister);

            return methodRegister;
        }

        public IMethodRegister GetMethodRegister(string className, bool isStatic, ClepsType functionType, string functionName)
        {
            string classNameToUse = isStatic ? className + ".static" : className;
            FunctionNameAndType methodRegisterKey = new FunctionNameAndType(classNameToUse, functionName, functionType);
            return methodBodies[methodRegisterKey];
        }

        public IValue CreateByte(byte value)
        {
            return new JavaScriptValue(value.ToString(), CompilerConstants.ClepsByteType);
        }

        public IValue CreateBoolean(bool value)
        {
            return new JavaScriptValue(value? "true" : "false", CompilerConstants.ClepsBoolType);
        }

        public IValue CreateArray(ClepsType arrayType, List<IValue> arrayElements)
        {
            string elementCode = String.Join(", ", arrayElements.Select(a => (a as JavaScriptValue).Expression).ToList());
            return new JavaScriptValue("[" + elementCode + "]", arrayType);
        }

        public IValue CreateClassInstance(BasicClepsType instanceType, List<IValue> parameters)
        {
            string parameterString = String.Join(", ", parameters.Select(v => (v as JavaScriptValue).Expression).ToList());
            string code = String.Format("new {0}({1})", instanceType.GetClepsTypeString(), parameterString);

            JavaScriptValue ret = new JavaScriptValue(code, instanceType);
            return ret;
        }

        public IValue GetRegisterValue(IValueRegister register)
        {
            JavaScriptRegister registerToUse = register as JavaScriptRegister;

            var ret = new JavaScriptValue(registerToUse.Expression, registerToUse.ExpressionType);
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

        public IValue GetFunctionCallReturnValue(string fullyQualifiedClassName, string targetFunctionName, FunctionClepsType clepsType, List<IValue> parameters)
        {
            string parameterString = String.Join(", ", parameters.Select(v => (v as JavaScriptValue).Expression).ToList());
            string code = String.Format("{0}.{1}.{2}({3})", TOPLEVELNAMESPACE, fullyQualifiedClassName, targetFunctionName, parameterString);

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

        public IValue GetClassStaticInstance(BasicStaticClepsType clepsClass)
        {
            JavaScriptValue ret = new JavaScriptValue(TOPLEVELNAMESPACE + "." + clepsClass.RawTypeName + "Inst", clepsClass);
            return ret;
        }

        public void Output(string directoryName, string fileNameWithoutExtension, CompileStatus status)
        {
            StringBuilder output = new StringBuilder();
            InitializeOutput(output);

            foreach(var clepsClass in classesLoaded)
            {
                GenerateClass(output, clepsClass.Value);
            }

            var outputFileName = Path.Combine(directoryName, fileNameWithoutExtension + ".js");
            File.WriteAllText(outputFileName, output.ToString());
        }

        #region OutputHelpers

        private void InitializeOutput(StringBuilder output)
        {
            output.AppendFormat("var {0} = {{}};\n", TOPLEVELNAMESPACE);
        }

        private void GenerateClass(StringBuilder output, ClepsClass clepsClass)
        {
            EnsureNamespaceExists(output, clepsClass);
            //output.AppendLine(TOPLEVELNAMESPACE + "." + clepsClass.FullyQualifiedName + " = function() {");
            //{
            //    output.AppendLine("\tfunction newInst(curr) {");
            //    {
            //        clepsClass.MemberVariables.ToList().ForEach(kvp => output.AppendFormat("\t\tcurr.{0} = {1};\n", kvp.Key, GetTypeInstance(kvp.Value)));
            //        output.AppendLine("\t}");
            //    }
            //    output.AppendLine("\tnewInst(this);");
            //}
            //output.AppendLine("};");
            //clepsClass.MemberMethods.ToList().ForEach(kvp => output.AppendFormat("{0}.{1}.prototype.{2} = {3};\n", TOPLEVELNAMESPACE, clepsClass.FullyQualifiedName, kvp.Key, methodBodies[new FunctionNameAndType(clepsClass.FullyQualifiedName, kvp.Key, kvp.Value)].GetMethodBody()));
            //clepsClass.StaticMemberVariables.ToList().ForEach(kvp => output.AppendFormat("{0}.{1}.{2} = undefined;\n", TOPLEVELNAMESPACE, clepsClass.FullyQualifiedName, kvp.Key));
            //clepsClass.StaticMemberMethods.ToList().ForEach(kvp => output.AppendFormat("{0}.{1}.{2} = {3};\n", TOPLEVELNAMESPACE, clepsClass.FullyQualifiedName, kvp.Key, methodBodies[new FunctionNameAndType(clepsClass.FullyQualifiedName, kvp.Key, kvp.Value)].GetMethodBody()));
            GenerateJavaScriptClass(output, clepsClass.FullyQualifiedName, clepsClass.MemberVariables, clepsClass.MemberMethods, true /* staticInstCheck */);
            GenerateJavaScriptClass(output, clepsClass.FullyQualifiedName + ".static", clepsClass.StaticMemberVariables, clepsClass.StaticMemberMethods, false /* staticInstCheck */);
        }

        private void GenerateJavaScriptClass(StringBuilder output, string fullyQualifiedName, IDictionary<string, ClepsType> fields, IDictionary<string, ClepsType> methods, bool staticInstCheck)
        {
            output.AppendLine(TOPLEVELNAMESPACE + "." + fullyQualifiedName + " = function() {");
            {
                output.AppendLine("\tfunction newInst(curr) {");
                {
                    if(staticInstCheck)
                    {
                        output.AppendFormat("\t\tif(!{0}.{1}.staticInst){{\n", TOPLEVELNAMESPACE, fullyQualifiedName);
                        output.AppendFormat("\t\t\t{0}.{1}.staticInst = new {0}.{1}.static();\n", TOPLEVELNAMESPACE, fullyQualifiedName);
                        output.AppendFormat("\t\t}}\n");
                    }
                    fields.ToList().ForEach(kvp => output.AppendFormat("\t\tcurr.{0} = {1};\n", kvp.Key, GetTypeInstance(kvp.Value)));
                    output.AppendLine("\t}");
                }
                output.AppendLine("\tnewInst(this);");
            }
            output.AppendLine("};");
            methods.ToList().ForEach(kvp => output.AppendFormat("{0}.{1}.prototype.{2} = {3};\n", TOPLEVELNAMESPACE, fullyQualifiedName, kvp.Key, methodBodies[new FunctionNameAndType(fullyQualifiedName, kvp.Key, kvp.Value)].GetMethodBody()));
        }

        private void EnsureNamespaceExists(StringBuilder output, ClepsClass clepsClass)
        {
            string currNamespace = clepsClass.GetNamespace();
            var pieces = currNamespace.Split('.').ToList();

            for(int i = 1; i <= pieces.Count; i++)
            {
                var currNesting = String.Join(".", pieces.Take(i));
                if (!namespacesCreated.Contains(currNesting))
                {
                    output.AppendFormat("{0}.{1} = {0}.{1} || {{}};\n", TOPLEVELNAMESPACE, currNesting);
                    namespacesCreated.Add(currNesting);
                }
            }
        }

        private string GetTypeInstance(ClepsType clepsType)
        {
            string clepsName = clepsType.GetClepsTypeString();
            string ret = "";

            if (clepsName.StartsWith("System.RawTypes."))
            {
                if (clepsName == "System.RawTypes.Byte")
                {
                    ret = "0";
                }
                else
                {
                    throw new NotImplementedException("Rawtype " + clepsName + " not supported");
                }
            }
            else
            {
                ret = String.Format("new {0}.{1}()", TOPLEVELNAMESPACE, clepsName);
            }

            return ret;
        }

        #endregion OutputHelpers
    }
}
