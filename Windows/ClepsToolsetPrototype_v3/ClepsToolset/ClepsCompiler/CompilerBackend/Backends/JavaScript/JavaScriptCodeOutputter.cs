using ClepsCompiler.CompilerBackend.Containers;
using ClepsCompiler.CompilerStructures;
using ClepsCompiler.CompilerTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerBackend.Backends.JavaScript
{
    class JavaScriptCodeOutputter
    {
        private Dictionary<string, ClepsClass> ClassesLoaded;
        private Dictionary<string, JavaScriptMethod> ClassInitializers = new Dictionary<string, JavaScriptMethod>();
        private Dictionary<string, JavaScriptMethod> ClassStaticInitializers = new Dictionary<string, JavaScriptMethod>();
        private JavaScriptMethod GlobalInitializer;
        private List<string> NamespacesCreated;
        private string EntryPointClass;
        private string EntryPointFunctionName;

        public JavaScriptCodeOutputter(Dictionary<string, ClepsClass> classesLoaded, 
            Dictionary<string, JavaScriptMethod> classInitializers,
            Dictionary<string, JavaScriptMethod> classStaticInitializers,
            JavaScriptMethod globalInitializer,
            string entryPointClass, 
            string entryPointFunctionName
        )
        {
            ClassesLoaded = classesLoaded;
            ClassInitializers = classInitializers;
            ClassStaticInitializers = classStaticInitializers;
            GlobalInitializer = globalInitializer;
            EntryPointClass = entryPointClass;
            EntryPointFunctionName = entryPointFunctionName;

            NamespacesCreated = new List<string>();
        }

        public void Output(string directoryName, string fileNameWithoutExtension, CompileStatus status)
        {
            StringBuilder output = new StringBuilder();
            InitializeOutput(output);

            foreach (var clepsClass in ClassesLoaded)
            {
                GenerateClass(output, clepsClass.Value);
            }

            output.AppendLine(GlobalInitializer.GetMethodBodyWithoutDeclaration());

            FunctionClepsType voidFuncType = new FunctionClepsType(new List<ClepsType>(), VoidClepsType.GetVoidType());
            foreach (var clepsType in CompilerConstants.SystemSupportedTypes)
            {
                output.AppendFormat("{0}.{1}.{2}();\n", JavaScriptCodeParameters.TOPLEVELNAMESPACE, clepsType.GetClepsTypeString(), JavaScriptCodeParameters.GetMangledFunctionName("classStaticInitializer", voidFuncType));
            }

            if (!String.IsNullOrWhiteSpace(EntryPointClass) && !String.IsNullOrWhiteSpace(EntryPointFunctionName))
            {
                output.AppendFormat("{0}.{1}.{2}();\n", JavaScriptCodeParameters.TOPLEVELNAMESPACE, EntryPointClass, JavaScriptCodeParameters.GetMangledFunctionName("classStaticInitializer", voidFuncType));
                output.AppendFormat("{0}.{1}.{2}();\n", JavaScriptCodeParameters.TOPLEVELNAMESPACE, EntryPointClass, JavaScriptCodeParameters.GetMangledFunctionName(EntryPointFunctionName, voidFuncType));
            }

            var outputFileName = Path.Combine(directoryName, fileNameWithoutExtension + ".js");
            File.WriteAllText(outputFileName, output.ToString());
        }

        private void InitializeOutput(StringBuilder output)
        {
            output.AppendFormat("var {0} = {{}};\n", JavaScriptCodeParameters.TOPLEVELNAMESPACE);
        }

        private void GenerateClass(StringBuilder output, ClepsClass clepsClass)
        {
            FunctionClepsType voidFuncType = new FunctionClepsType(new List<ClepsType>(), VoidClepsType.GetVoidType());

            EnsureNamespaceExists(output, clepsClass);
            output.AppendLine(JavaScriptCodeParameters.TOPLEVELNAMESPACE + "." + clepsClass.FullyQualifiedName + " = function() {");
            {
                clepsClass.MemberVariables.ToList().ForEach(kvp => output.AppendFormat("\tthis.{0} = undefined;\n", kvp.Key));

                output.AppendFormat("\tthis.{0}();\n", JavaScriptCodeParameters.GetMangledFunctionName("classInitializer", voidFuncType));
                output.AppendFormat("\t{0}.{1}.{2}();\n", JavaScriptCodeParameters.TOPLEVELNAMESPACE, clepsClass.FullyQualifiedName, JavaScriptCodeParameters.GetMangledFunctionName("classStaticInitializer", voidFuncType));
            }
            output.AppendLine("};");

            GenerateMethodWithBody(output, clepsClass.FullyQualifiedName, "classInitializer", voidFuncType, false, ClassInitializers[clepsClass.FullyQualifiedName]);
            GenerateMethodWithBody(output, clepsClass.FullyQualifiedName, "classStaticInitializer", voidFuncType, true, ClassStaticInitializers[clepsClass.FullyQualifiedName]);
        }

        private void EnsureNamespaceExists(StringBuilder output, ClepsClass clepsClass)
        {
            string currNamespace = clepsClass.GetPrefixNamespaceAndClass();
            var pieces = currNamespace.Split('.').ToList();

            for (int i = 1; i <= pieces.Count; i++)
            {
                var currNesting = String.Join(".", pieces.Take(i));
                if (!NamespacesCreated.Contains(currNesting))
                {
                    output.AppendFormat("{0}.{1} = {0}.{1} || {{}};\n", JavaScriptCodeParameters.TOPLEVELNAMESPACE, currNesting);
                    NamespacesCreated.Add(currNesting);
                }
            }
        }

        private void GenerateMethodWithBody(StringBuilder output, string fullyQualifiedClassName, string methodName, FunctionClepsType methodType, bool isStatic, JavaScriptMethod method)
        {
            string fullFunctionName = String.Format("{0}.{1}.{2}{3}",
                JavaScriptCodeParameters.TOPLEVELNAMESPACE,
                fullyQualifiedClassName,
                isStatic ? "" : "prototype.",
                JavaScriptCodeParameters.GetMangledFunctionName(methodName, methodType)
            );

            output.AppendFormat("{0} = {1};\n",
                fullFunctionName,
                method.GetMethodText()
            );
        }
    }
}
