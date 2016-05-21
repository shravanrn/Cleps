using Antlr4.Runtime.Misc;
using ClepsCompiler.CompilerBackend;
using ClepsCompiler.CompilerCore;
using ClepsCompiler.CompilerStructures;
using ClepsCompiler.CompilerTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.SyntaxTreeVisitors
{
    partial class ClepsFunctionBodyGeneratorVisitor : ClepsFunctionBodyAnalysisVisitor_Abstract
    {
        //private string CurrMemberName;
        //private bool CurrMemberIsStatic;
        private ClepsType CurrMemberType;
        private IMethodValue CurrMethodRegister;
        private List<VariableManager> VariableManagers = new List<VariableManager>();
        private TypeManager TypeManager;

        public ClepsFunctionBodyGeneratorVisitor(CompileStatus status, ClassManager classManager, ICodeGenerator codeGenerator, TypeManager typeManager) : base(status, classManager, codeGenerator)
        {
            TypeManager = typeManager;
        }

        public override object VisitMemberDeclarationStatement([NotNull] ClepsParser.MemberDeclarationStatementContext context)
        {
            string memberName = context.FieldName.Name.Text;
            bool isStatic = context.STATIC() != null;
            ClepsType memberType = Visit(context.typename()) as ClepsType;
            bool isConst = context.CONST() != null;

            //var oldMemberName = CurrMemberName;
            //var oldMemberIsStatic = CurrMemberIsStatic;
            var oldMemberType = CurrMemberType;

            //CurrMemberName = memberName;
            //CurrMemberIsStatic = isStatic;
            CurrMemberType = memberType;

            if (context.rightHandExpression() != null)
            {
                var expressionValue = Visit(context.rightHandExpression());

                //if(!memberType.IsFunctionType)
                {
                    IValue expressionRegister = expressionValue as IValue;
                    IValueRegister targetRegister;
                    IMethodValue initializerMethod;

                    if (isStatic)
                    {
                        initializerMethod = CodeGenerator.GetClassStaticInitializerRegister(FullyQualifiedClassName);
                        targetRegister = CodeGenerator.GetStaticFieldRegister(FullyQualifiedClassName, memberName, memberType);
                    }
                    else
                    {
                        if (isConst)
                        {
                            initializerMethod = CodeGenerator.GetGlobalInitializerRegister();
                            targetRegister = CodeGenerator.GetMemberFieldRegisterForWriteFromDifferentClass(FullyQualifiedClassName, memberName, memberType);
                        }
                        else
                        {
                            initializerMethod = CodeGenerator.GetClassInitializerRegister(FullyQualifiedClassName);
                            targetRegister = CodeGenerator.GetMemberFieldRegisterFromSameClass(FullyQualifiedClassName, memberName, memberType);
                        }
                    }

                    initializerMethod.CreateAssignment(targetRegister, expressionRegister);
                }
            }

            //CurrMemberName = oldMemberName;
            //CurrMemberIsStatic = oldMemberIsStatic;
            CurrMemberType = oldMemberType;

            return true;
        }

        public override object VisitStatementBlock([NotNull] ClepsParser.StatementBlockContext context)
        {
            VariableManager variableManager = VariableManagers.Last();
            variableManager.AddBlock();
            var ret = VisitChildren(context);
            variableManager.RemoveBlock();
            return ret;
        }
    }
}
