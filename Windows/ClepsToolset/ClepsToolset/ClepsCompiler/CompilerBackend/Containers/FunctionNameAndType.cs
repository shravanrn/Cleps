using ClepsCompiler.CompilerTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerBackend.Containers
{
    class FunctionNameAndType
    {
        public string ClassName { get; private set; }
        public string FunctionName { get; private set; }
        public ClepsType FunctionType { get; private set; }

        public FunctionNameAndType(string className, string functionName, ClepsType functionType)
        {
            ClassName = className;
            FunctionName = functionName;
            FunctionType = functionType;
        }

        public override bool Equals(object obj)
        {
            if (obj is FunctionNameAndType)
            {
                return IsEqual(this, obj as FunctionNameAndType);
            }

            return false;
        }

        private static bool IsEqual(FunctionNameAndType obj1, FunctionNameAndType obj2)
        {
            if (Object.ReferenceEquals(obj1, obj2))
            {
                return true;
            }
            else if (Object.ReferenceEquals(obj1, null) || Object.ReferenceEquals(obj2, null))
            {
                return false;
            }

            return obj1.ClassName == obj2.ClassName && obj1.FunctionName == obj2.FunctionName && obj1.FunctionType == obj2.FunctionType;
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

        public static bool operator ==(FunctionNameAndType c1, FunctionNameAndType c2)
        {
            return IsEqual(c1, c2);
        }

        public static bool operator !=(FunctionNameAndType c1, FunctionNameAndType c2)
        {
            return !IsEqual(c1, c2);
        }
    }
}
