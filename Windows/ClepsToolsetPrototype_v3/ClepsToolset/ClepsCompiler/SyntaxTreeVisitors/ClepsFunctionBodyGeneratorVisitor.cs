using Antlr4.Runtime;
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
        private string CurrMemberName;
        private bool CurrMemberIsStatic;
        private ClepsType CurrMemberType;
        private IMethodValue CurrMethodGenerator;
        private List<VariableManager> VariableManagers = new List<VariableManager>();
        private TemplateManager<ParserRuleContext> TemplateManager = new TemplateManager<ParserRuleContext>();

        public ClepsFunctionBodyGeneratorVisitor(CompileStatus status, ClassManager classManager, ICodeGenerator codeGenerator, TypeManager typeManager) : base(status, classManager, codeGenerator, typeManager)
        {
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

            bool isStatic = context.STATIC() != null;
            ClepsType memberType = Visit(context.typename()) as ClepsType;
            bool isConst = context.CONST() != null;

            var oldMemberName = CurrMemberName;
            var oldMemberIsStatic = CurrMemberIsStatic;
            var oldMemberType = CurrMemberType;

            CurrMemberName = memberName;
            CurrMemberIsStatic = isStatic;
            CurrMemberType = memberType;

            if (context.rightHandExpression() != null)
            {
                var expressionValue = Visit(context.rightHandExpression());

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
                        targetRegister = CodeGenerator.GetConstantMemberFieldRegisterForWrite(FullyQualifiedClassName, memberName, memberType);
                    }
                    else
                    {
                        initializerMethod = CodeGenerator.GetClassInitializerRegister(FullyQualifiedClassName);
                        targetRegister = CodeGenerator.GetMemberFieldRegisterFromSameClass(FullyQualifiedClassName, memberName, memberType);
                    }
                }

                initializerMethod.CreateAssignment(targetRegister, expressionRegister);
            }

            CurrMemberName = oldMemberName;
            CurrMemberIsStatic = oldMemberIsStatic;
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
