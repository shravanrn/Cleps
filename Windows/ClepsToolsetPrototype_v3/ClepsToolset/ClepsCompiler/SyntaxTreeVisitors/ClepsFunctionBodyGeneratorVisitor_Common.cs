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

            if (register.ExpressionType == value.ExpressionType && CompilerConstants.SystemSupportedTypes.Contains(register.ExpressionType))
            {
                CurrMethodGenerator.CreateAssignment(register, value);
            }
            else
            {
                throw new NotImplementedException("assignment for non byte types not supported yet");
            }
        }

        private IValue GetDereferencedRegisterOrNull(IValue target)
        {
            if (target == null)
            {
                return null;
            }
            else if (target.ExpressionType is BasicClepsType)
            {
                return target;
            }
            else
            {
                return null;
            }
        }
    }
}
