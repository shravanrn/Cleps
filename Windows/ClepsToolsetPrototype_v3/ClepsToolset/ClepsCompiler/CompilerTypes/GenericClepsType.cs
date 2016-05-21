using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerTypes
{
    class GenericClepsType : ClepsType
    {
        public string GenericTypeName { get; private set; }

        public GenericClepsType(string genericTypeName) : base(false, false, false, false, false)
        {
            GenericTypeName = genericTypeName;
        }

        public override string GetClepsTypeString()
        {
            return GenericTypeName;
        }

        public override int GetHashCode()
        {
            return GenericTypeName.GetHashCode();
        }

        public override bool NotNullObjectEquals(ClepsType obj)
        {
            if (obj.GetType() != typeof(BasicClepsType))
            {
                return false;
            }

            BasicClepsType objToCompare = obj as BasicClepsType;
            return GenericTypeName == objToCompare.RawTypeName;
        }
    }
}
