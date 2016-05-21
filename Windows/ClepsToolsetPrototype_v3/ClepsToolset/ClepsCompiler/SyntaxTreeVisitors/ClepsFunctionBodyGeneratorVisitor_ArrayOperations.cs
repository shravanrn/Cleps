using Antlr4.Runtime.Misc;
using ClepsCompiler.CompilerBackend;
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
        public override object VisitArrayAssignment([NotNull] ClepsParser.ArrayAssignmentContext context)
        {
            var arrayElements = context._ArrayElements.Select(a => Visit(a) as IValue).ToList();
            var elementTypes = arrayElements.Select(a => a.ExpressionType).ToList();
            ClepsType arrayElementType = TypeManager.GetSuperType(elementTypes);
            ClepsType arrayType = new ArrayClepsType(arrayElementType, new long[] { arrayElements.Count });

            IValue arr = CodeGenerator.CreateArray(arrayType, arrayElements);
            return arr;
        }

        public override object VisitArrayAccessOnExpression([NotNull] ClepsParser.ArrayAccessOnExpressionContext context)
        {
            IValue expressionValue = Visit(context.ArrayExpression) as IValue;

            if (!expressionValue.ExpressionType.IsArrayType)
            {
                string errorMessage = String.Format("Expression {0} is not an array and so array access is not possible.", context.ArrayExpression.GetText());
                Status.AddError(new CompilerError(FileName, context.ArrayExpression.Start.Line, context.ArrayExpression.Start.Column, errorMessage));
                //return something to avoid stopping the compilation
                return expressionValue;
            }

            ArrayClepsType expressionType = (expressionValue.ExpressionType as ArrayClepsType);
            List<IValue> indexValues = ArrayIndexAccess(expressionType, context._ArrayIndexExpression);

            IValueRegister register = CodeGenerator.GetArrayElementRegister(expressionValue, indexValues);
            IValue retValue = CodeGenerator.GetRegisterValue(register);
            return retValue;
        }

        public override object VisitFunctionArrayAssignmentStatement([NotNull] ClepsParser.FunctionArrayAssignmentStatementContext context)
        {
            IValue expressionValue = Visit(context.ArrayExpression) as IValue;

            if (!expressionValue.ExpressionType.IsArrayType)
            {
                string errorMessage = String.Format("Expression {0} is not an array and so array access is not possible.", context.ArrayExpression.GetText());
                Status.AddError(new CompilerError(FileName, context.ArrayExpression.Start.Line, context.ArrayExpression.Start.Column, errorMessage));
                //return something to avoid stopping the compilation
                return expressionValue;
            }

            ArrayClepsType expressionType = (expressionValue.ExpressionType as ArrayClepsType);
            List<IValue> indexValues = ArrayIndexAccess(expressionType, context._ArrayIndexExpression);

            IValueRegister register = CodeGenerator.GetArrayElementRegister(expressionValue, indexValues);
            CreateAssignment(register, context.RightExpression);
            return register;
        }

        private List<IValue> ArrayIndexAccess(ArrayClepsType expressionType, IList<ClepsParser.RightHandExpressionContext> arrayIndexExpression)
        {
            var indexValues = arrayIndexExpression.Select(i =>
            {
                var indexValueRet = Visit(i);
                IValue indexValue = indexValueRet as IValue;

                if (indexValue.ExpressionType != CompilerConstants.ClepsByteType)
                {
                    string errorMessage = String.Format("Array access {0} is not a byte type", i.GetText());
                    Status.AddError(new CompilerError(FileName, i.Start.Line, i.Start.Column, errorMessage));
                    //just use the first element access to avoid stalling
                    indexValue = CodeGenerator.CreateByte(0);
                }

                return indexValue;
            }).ToList();

            if (indexValues.Count > expressionType.Dimensions.Length)
            {
                string errorMessage = String.Format("Accessing more dimensions than array has. Array has {0} dimensions, but accessing {1} dimensions", expressionType.Dimensions.Length, indexValues.Count);
                Status.AddError(new CompilerError(FileName, arrayIndexExpression[indexValues.Count - 1].Start.Line, arrayIndexExpression[indexValues.Count - 1].Start.Column, errorMessage));
                //truncate array access to allowed access to avoid stopping the compilation
                indexValues = indexValues.Take(expressionType.Dimensions.Length).ToList();
            }

            return indexValues;
        }
    }
}
