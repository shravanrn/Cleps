using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerTypes
{
    class VoidClepsType : ClepsType
    {
        private static VoidClepsType singleton = null;

        private VoidClepsType() : base(false, false, false, false, false)
        {
        }

        public static VoidClepsType GetVoidType()
        {
            if(singleton == null)
            {
                singleton = new VoidClepsType();
            }

            return singleton;
        }

        public override string GetClepsTypeString()
        {
            return "void";
        }

        public override bool NotNullObjectEquals(ClepsType obj)
        {
            if (obj.GetType() != typeof(VoidClepsType))
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
