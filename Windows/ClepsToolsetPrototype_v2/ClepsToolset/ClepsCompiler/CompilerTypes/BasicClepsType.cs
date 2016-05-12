using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerTypes
{
    class BasicClepsType : ClepsType
    {
        public BasicStaticClepsType StaticType { get; private set; }
        public string RawTypeName { get; private set; }

        public BasicClepsType(string rawTypeName)
        {
            StaticType = new BasicStaticClepsType(rawTypeName);
            RawTypeName = rawTypeName;
            IsArrayType = false;
            IsFunctionType = false;
            IsStaticType = false;
        }

        public override string GetClepsTypeString()
        {
            return RawTypeName;
        }

        public override int GetHashCode()
        {
            return RawTypeName.GetHashCode();
        }

        public override bool NotNullObjectEquals(ClepsType obj)
        {
            if (obj.GetType() != typeof(BasicClepsType))
            {
                return false;
            }

            BasicClepsType objToCompare = obj as BasicClepsType;
            return RawTypeName == objToCompare.RawTypeName;
        }
    }
}
