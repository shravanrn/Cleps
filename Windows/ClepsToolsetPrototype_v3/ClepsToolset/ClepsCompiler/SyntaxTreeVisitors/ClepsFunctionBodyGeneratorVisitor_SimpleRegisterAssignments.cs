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
        public override object VisitNumericAssignments([NotNull] ClepsParser.NumericAssignmentsContext context)
        {
            IValue val;
            string numericValue = context.numeric().NumericValue.Text;
            string numericType = context.numeric().NumericType?.Text;

            if (numericType == "b")
            {
                byte byteVal;
                if (!byte.TryParse(numericValue, out byteVal))
                {
                    string errorMessage = String.Format("Value {0} is outside the range of values allowed for a byte. Allowed range is 0 to 255", numericValue);
                    Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                    //Use a different value to avoid stopping the compilation
                    byteVal = 0;
                }

                val = CodeGenerator.CreateByte(byteVal);
            }
            else
            {
                throw new NotImplementedException("Other numeric types not supported yet");
            }

            return val;
        }

        public override object VisitBooleanAssignments([NotNull] ClepsParser.BooleanAssignmentsContext context)
        {
            return CodeGenerator.CreateBoolean(context.TRUE() != null);
        }

        public override object VisitClassInstanceAssignment([NotNull] ClepsParser.ClassInstanceAssignmentContext context)
        {
            ClepsType typeName = Visit(context.typename()) as ClepsType;
            List<IValue> functionParams = context._FunctionParameters.Select(p => Visit(p) as IValue).ToList();

            IValue instance;
            if (
                (typeName == CompilerConstants.ClepsByteType || typeName == CompilerConstants.ClepsBoolType) &&
                (functionParams.Count == 1 && functionParams[0].ExpressionType == typeName)
            )
            {
                instance = functionParams[0];
            }
            else
            {
                instance = CodeGenerator.CreateClassInstance(typeName as BasicClepsType, functionParams);
            }

            return instance;
        }
    }
}
