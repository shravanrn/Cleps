using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerTypes
{
    class PointerClepsType : ClepsType
    {
        public ClepsType BaseType { get; private set; }

        public PointerClepsType(ClepsType baseType)
        {
            BaseType = baseType;
            IsFunctionType = false;
            IsArrayType = false;
        }

        public override string GetClepsTypeString()
        {
            return BaseType.GetClepsTypeString() + "*";
        }

        public override int GetHashCode()
        {
            //Need it to be different from the basetype hashcode to easiy distinguish between say an Int32 and Int32*
            return BaseType.GetHashCode() + 2;
        }

        public override bool NotNullObjectEquals(ClepsType obj)
        {
            if (obj.GetType() != typeof(PointerClepsType))
            {
                return false;
            }

            PointerClepsType objToCompare = obj as PointerClepsType;
            return BaseType == objToCompare.BaseType;
        }
    }
}
