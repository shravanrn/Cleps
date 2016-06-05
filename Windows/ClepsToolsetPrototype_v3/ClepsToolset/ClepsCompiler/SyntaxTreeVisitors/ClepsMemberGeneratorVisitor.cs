using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using ClepsCompiler.CompilerBackend;
using ClepsCompiler.CompilerCore;
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
        private EntryPointManager EntryPointManager;

        public ClepsMemberGeneratorVisitor(CompileStatus status, ClassManager classManager, ICodeGenerator codeGenerator, EntryPointManager entryPointManager, TypeManager typeManager) : base(status, classManager, codeGenerator, typeManager)
        {
            EntryPointManager = entryPointManager;
        }

        public override bool VisitClassDeclarationStatements_Ex([NotNull] ClepsParser.ClassDeclarationStatementsContext context)
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

            Visit(context.classBodyStatements());


            ClepsClassBuilder classBuilder = ClassManager.GetClassBuilder(FullyQualifiedClassName);
            ClepsClass classDetails = classBuilder.BuildClass();
            ClassManager.SetClassDefinition(classDetails);
            CodeGenerator.SetClassBodyAndCreateConstructor(classDetails);

            return true;
        }

        public override object VisitMemberDeclarationStatement([NotNull] ClepsParser.MemberDeclarationStatementContext context)
        {
            string memberName;

            if (context.OPERATOR() == null)
            {
                memberName = context.FieldName.Name.Text;
            }
            else
            {
                memberName = context.OperatorName.GetText();
            }

            ClepsClassBuilder classBuilder = ClassManager.GetClassBuilder(FullyQualifiedClassName);

            if (classBuilder == null)
            {
                //This is probably because of some earlier error. Return gracefully
                return false;
            }

            bool isStatic = context.STATIC() != null;
            ClepsType memberType = Visit(context.typename()) as ClepsType;

            string cantAddReason;
            if (!classBuilder.CanAddMemberToClass(memberName, memberType, isStatic, out cantAddReason))
            {
                Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, cantAddReason));
                //Don't process this member
                return false;
            }

            bool isConst = context.CONST() != null;
            classBuilder.AddNewMember(isStatic, new ClepsVariable(memberName, memberType, isConst));

            EntryPointManager.NewMemberSeen(FullyQualifiedClassName, memberName, memberType, isStatic);
            
            return true;
        }
    }
}
