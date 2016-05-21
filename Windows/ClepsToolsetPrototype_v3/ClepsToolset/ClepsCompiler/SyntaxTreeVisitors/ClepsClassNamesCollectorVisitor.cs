using Antlr4.Runtime.Misc;
using ClepsCompiler.CompilerBackend;
using ClepsCompiler.CompilerCore;
using ClepsCompiler.CompilerStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.SyntaxTreeVisitors
{
    class ClepsClassNamesCollectorVisitor : ClepsAbstractVisitor
    {
        public ClepsClassNamesCollectorVisitor(CompileStatus status, ClassManager classManager, ICodeGenerator codeGenerator) : base(status, classManager, codeGenerator){}

        public override bool VisitClassDeclarationStatements_Ex([NotNull] ClepsParser.ClassDeclarationStatementsContext context)
        {
            //if this qualified name already exists, there is an error
            if (ClassManager.IsClassBuilderAvailable(FullyQualifiedClassName))
            {
                string errorMessage = String.Format("Class {0} has multiple definitions", FullyQualifiedClassName);
                Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                //Don't process this class
                return false;
            }

            ClassManager.AddNewClassDeclaration(FullyQualifiedClassName);
            CodeGenerator.CreateClass(FullyQualifiedClassName);
            return true;
        }
    }
}
