using ClepsCompiler.CompilerTypes;
using ClepsCompiler.Utils.ClassBehaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerBackend.Containers
{
    class FunctionContainer : EqualsAndHashCode<FunctionContainer>
    {
        public string ClassName { get; private set; }
        public string FunctionName { get; private set; }
        public ClepsType FunctionType { get; private set; }

        public FunctionContainer(string className, string functionName, ClepsType functionType)
        {
            ClassName = className;
            FunctionName = functionName;
            FunctionType = functionType;
        }

        public override int GetHashCode()
        {
            //Recommended approach in effective java book and java and android docs
            int result = 17;
            result = result * 31 + ClassName.GetHashCode();
            result = result * 31 + FunctionName.GetHashCode();
            result = result * 31 + FunctionType.GetHashCode();
            return result;
        }

        public override bool NotNullObjectEquals(FunctionContainer obj)
        {
            return ClassName == obj.ClassName && FunctionName == obj.FunctionName && FunctionType == obj.FunctionType;
        }
    }
}
