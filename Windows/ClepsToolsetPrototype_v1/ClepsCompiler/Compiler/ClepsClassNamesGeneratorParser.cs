using LLVMSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;
using ClepsCompiler.CompilerHelpers;
using System.Diagnostics;

namespace ClepsCompiler.Compiler
{
    /// <summary>
    /// This parser is used to generate a list of classes defined in the code
    /// </summary>
    class ClepsClassNamesGeneratorParser : ClepsAbstractParser<int>
    {
        private ClassManager ClassManager;
        private CompileStatus Status;
        private LLVMContextRef Context;
        private LLVMModuleRef Module;
        private LLVMBuilderRef Builder;
        private Dictionary<string, LLVMTypeRef> ClassSkeletons;

        private List<string> CurrentNamespaceAndClass;

        public ClepsClassNamesGeneratorParser(ClassManager classManager, CompileStatus status, LLVMContextRef context, LLVMModuleRef module, LLVMBuilderRef builder, out Dictionary<string, LLVMTypeRef> classSkeletons)
        {
            ClassManager = classManager;
            Status = status;
            Context = context;
            Module = module;
            Builder = builder;
            classSkeletons = new Dictionary<string, LLVMTypeRef>();
            ClassSkeletons = classSkeletons;
        }

        public override int VisitCompilationUnit([NotNull] ClepsParser.CompilationUnitContext context)
        {
            CurrentNamespaceAndClass = new List<String>();
            var ret = VisitChildren(context);
            return ret;
        }

        public override int VisitNamespaceBlockStatement([NotNull] ClepsParser.NamespaceBlockStatementContext context)
        {
            CurrentNamespaceAndClass.Add(context.NamespaceName.GetText());
            var ret = VisitChildren(context);
            CurrentNamespaceAndClass.RemoveAt(CurrentNamespaceAndClass.Count - 1);
            return ret;
        }

        public override int VisitClassDeclarationStatements([NotNull] ClepsParser.ClassDeclarationStatementsContext context)
        {
            if(context.ClassName == null)
            {
                //Some antlr parsing exception has occurred. Just exit.
                return -1;
            }

            CurrentNamespaceAndClass.Add(context.ClassName.GetText());

            string className = String.Join(".", CurrentNamespaceAndClass);
            
            //if this qualified name already exists, there is an error
            if(ClassManager.IsClassLoaded(className))
            {
                string errorMessage = String.Format("Class {0} has multiple definitions", className);
                Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                //Don't process this class
                return -1;
            }

            LLVMTypeRef structType = LLVM.StructCreateNamed(Context, className);
            ClassSkeletons[className] = structType;
            ClassManager.AddNewClass(className);

            CurrentNamespaceAndClass.RemoveAt(CurrentNamespaceAndClass.Count - 1);
            return 0;
        }
    }
}
