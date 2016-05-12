using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerTypes
{
    class BasicStaticClepsType : ClepsType
    {
        protected string RawTypeNameVal;
        public string RawTypeName { get { return RawTypeNameVal + ".static";  } }

        public BasicStaticClepsType(string rawTypeName)
        {
            RawTypeNameVal = rawTypeName;
            IsArrayType = false;
            IsFunctionType = false;
            IsStaticType = true;
        }

        public override string GetClepsTypeString()
        {
            return RawTypeNameVal;
        }

        public override int GetHashCode()
        {
            return RawTypeName.GetHashCode();
        }

        public override bool NotNullObjectEquals(ClepsType obj)
        {
            if (obj.GetType() != typeof(BasicStaticClepsType))
            {
                return false;
            }

            BasicStaticClepsType objToCompare = obj as BasicStaticClepsType;
            return RawTypeName == objToCompare.RawTypeName;
        }
    }
}
