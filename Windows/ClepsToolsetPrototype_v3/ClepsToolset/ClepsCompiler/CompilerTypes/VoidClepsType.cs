using ClepsCompiler.Utils.Types;
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

        private VoidClepsType() : base(false, false, false, false, false, false)
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

        public override ClepsType ReplaceTemplateTypeComponents(GenericClepsType templateTypeName, ClepsType targetTypeName)
        {
            return this;
        }

        public override SuccessStatus ReplaceWithConcreteType(ClepsType concreteType, Dictionary<GenericClepsType, ClepsType> outReplacementsMade)
        {
            return SuccessStatus.Failure;
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
