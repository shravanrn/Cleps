using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerTypes
{
    class VoidType : ClepsType
    {
        private static VoidType singleton = null;

        private VoidType()
        {
            IsFunctionType = false;
            IsArrayType = false;
        }

        public static VoidType GetVoidType()
        {
            if(singleton == null)
            {
                singleton = new VoidType();
            }

            return singleton;
        }

        public override string GetClepsTypeString()
        {
            return "void";
        }

        public override bool NotNullObjectEquals(ClepsType obj)
        {
            if (obj.GetType() != typeof(VoidType))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return 1;
        }
    }
}
