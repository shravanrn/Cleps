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
        private void CreateAssignment(IValueRegister register, ClepsParser.RightHandExpressionContext rightHandExpression)
        {
            IValue value = Visit(rightHandExpression) as IValue;

            if (register.ExpressionType == value.ExpressionType && register.ExpressionType == CompilerConstants.ClepsByteType)
            {
                CurrMethodRegister.CreateAssignment(register, value);
            }
            else
            {
                throw new NotImplementedException("assignment for non byte types not supported yet");
            }
        }

        private BasicClepsType GetDereferencedTypeOrNull(ClepsType targetType)
        {
            if (targetType == null)
            {
                return null;
            }
            else if (targetType is BasicClepsType)
            {
                return targetType as BasicClepsType;
            }
            else if (targetType is PointerClepsType)
            {
                return GetDereferencedTypeOrNull((targetType as PointerClepsType).BaseType);
            }
            else
            {
                return null;
            }
        }
    }
}
