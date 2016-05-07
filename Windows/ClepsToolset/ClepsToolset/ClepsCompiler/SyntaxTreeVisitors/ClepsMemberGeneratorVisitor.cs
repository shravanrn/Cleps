using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using ClepsCompiler.CompilerBackend;
using ClepsCompiler.CompilerStructures;
using ClepsCompiler.CompilerTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.SyntaxTreeVisitors
{
    class ClepsMemberGeneratorVisitor : ClepsAbstractVisitor
    {
        public ClepsMemberGeneratorVisitor(CompileStatus status, ClassManager classManager, ICodeGenerator codeGenerator) : base(status, classManager, codeGenerator){ }

        public override object VisitClassDeclarationStatements_Ex([NotNull] ClepsParser.ClassDeclarationStatementsContext context)
        {

            if (!ClassManager.IsClassBuilderAvailable(FullyQualifiedClassName))
            {
                //if the class was not found in the loaded class stage, then this is probably due to an earlier parsing error, just stop processing this class
                return false;
            }
            
            if (ClassManager.IsClassBodySet(FullyQualifiedClassName))
            {
                //class body is already set
                //this branch is hit, if the user has an error in code where multiple classes have the same name
                //gracefully exit
                return false;
            }

            var ret = Visit(context.classBodyStatements());


            ClepsClassBuilder classBuilder = ClassManager.GetClassBuilder(FullyQualifiedClassName);
            ClepsClass classDetails = classBuilder.BuildClass();
            ClassManager.SetClassDefinition(classDetails);
            CodeGenerator.SetClassBodyAndCreateConstructor(classDetails);

            return ret;
        }

        public override object VisitMemberDeclarationStatement([NotNull] ClepsParser.MemberDeclarationStatementContext context)
        {
            string memberName = context.FieldName.Name.Text;

            ClepsClassBuilder classBuilder = ClassManager.GetClassBuilder(FullyQualifiedClassName);

            if (classBuilder == null)
            {
                //This is probably because of some earlier error. Return gracefully
                return false;
            }

            if (classBuilder.DoesClassContainMember(memberName))
            {
                string errorMessage = String.Format("Class {0} has multiple definitions of member {1}", FullyQualifiedClassName, memberName);
                Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                //Don't process this member
                return false;
            }

            bool isStatic = context.STATIC() != null;
            ClepsType memberType = Visit(context.typename()) as ClepsType;

            if(memberType.IsFunctionType)
            {
                CodeGenerator.CreateMethod(FullyQualifiedClassName, isStatic, memberType, memberName);
            }

            classBuilder.AddNewMember(isStatic, memberType, memberName);
            return true;
        }
    }
}
