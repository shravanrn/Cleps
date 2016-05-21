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
    partial class ClepsFunctionBodyGeneratorVisitor
    {
        public override object VisitVariable([NotNull] ClepsParser.VariableContext context)
        {
            var ret = context.VariableName.Text;
            return ret;
        }

        public override object VisitVariableAssignment([NotNull] ClepsParser.VariableAssignmentContext context)
        {
            IValueRegister register = GetVariableRegister(context);
            IValue variableValue;
            if (register == null)
            {
                //Error occurred finding the register. Return some value to avoid stopping the compilation
                variableValue = CodeGenerator.CreateByte(0);
            }
            else
            {
                variableValue = CodeGenerator.GetRegisterValue(register);
            }

            return variableValue;
        }

        public override object VisitVariableDeclaration([NotNull] ClepsParser.VariableDeclarationContext context)
        {
            bool isConst = context.CONST() != null;
            ClepsType clepsType = Visit(context.typename()) as ClepsType;
            string name = Visit(context.variable()) as string;

            return new ClepsVariable(name, clepsType, isConst);
        }

        public override object VisitFunctionVariableDeclarationStatement([NotNull] ClepsParser.FunctionVariableDeclarationStatementContext context)
        {
            ClepsVariable variable = Visit(context.variableDeclaration()) as ClepsVariable;

            VariableManager variableManager = VariableManagers.Last();
            if (!variableManager.IsVariableNameAvailable(variable.VariableName))
            {
                string errorMessage = String.Format("Variable {0} is already defined", variable.VariableName);
                Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                //Use a different variable name to avoid stopping the compilation
                string newVariableName = variableManager.GetAvailableVariableName(variable.VariableName);
                variable = new ClepsVariable(newVariableName, variable.VariableType, variable.IsConstant);
            }

            IValue value = null;
            if (context.rightHandExpression() != null)
            {
                value = Visit(context.rightHandExpression()) as IValue;
                if (variable.VariableType != value.ExpressionType)
                {
                    throw new NotImplementedException("Assignment for non identical types not supported yet");
                }
            }

            IValueRegister variableRegister = CurrMethodRegister.CreateNewVariable(variable, value);
            variableManager.AddLocalVariable(variable, variableRegister);

            return variable;
        }

        public override object VisitFunctionVariableAssignmentStatement([NotNull] ClepsParser.FunctionVariableAssignmentStatementContext context)
        {
            IValueRegister register = GetVariableRegister(context.variableAssignment());
            if (register == null)
            {
                return false;
            }

            CreateAssignment(register, context.rightHandExpression());

            return register;
        }

        private IValueRegister GetVariableRegister([NotNull] ClepsParser.VariableAssignmentContext context)
        {
            string variableName = Visit(context.variable()) as string;
            var variableManager = VariableManagers.Last();
            IValueRegister register;

            if (!variableManager.IsVariableAvailable(variableName))
            {
                string errorMessage = String.Format("Variable {0} does not exist", variableName);
                Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                register = null;
            }
            else
            {
                register = variableManager.GetVariableRegister(variableName);
            }

            return register;
        }
    }
}
