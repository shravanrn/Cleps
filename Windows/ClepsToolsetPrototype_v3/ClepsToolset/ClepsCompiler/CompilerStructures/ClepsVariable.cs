using ClepsCompiler.CompilerTypes;
using ClepsCompiler.Utils.ClassBehaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerStructures
{
    class ClepsVariable : EqualsAndHashCode<ClepsVariable>
    {
        public string VariableName { get; private set; }
        public ClepsType VariableType { get; private set; }
        public bool IsConstant { get; private set; }

        public ClepsVariable(string variableName, ClepsType variableType, bool isConstant)
        {
            VariableName = variableName;
            VariableType = variableType;
            IsConstant = isConstant;
        }

        public override int GetHashCode()
        {
            return GetHashCodeFor(VariableName, VariableType);
        }

        public override bool NotNullObjectEquals(ClepsVariable obj)
        {
            return VariableName == obj.VariableName && VariableType == obj.VariableType;
        }
    }
}
