using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerTypes
{
    class FunctionClepsType : ClepsType
    {
        public List<ClepsType> ParameterTypes { get; private set; }
        public ClepsType ReturnType { get; private set; }

        public FunctionClepsType(List<ClepsType> parameterTypes, ClepsType returnType)
        {
            ParameterTypes = parameterTypes;
            ReturnType = returnType;
            IsArrayType = false;
            IsFunctionType = true;
            IsStaticType = false;
        }

        public override string GetClepsTypeString()
        {
            return String.Format("fn {0} ({1})", 
                ReturnType.GetClepsTypeString(), 
                String.Join(",", ParameterTypes.Select(p => p.GetClepsTypeString()))
            );
        }

        public override int GetHashCode()
        {
            //Recommended approach in effective java book and java and android docs
            int result = 17;
            result = result * 31 + ReturnType.GetHashCode();
            foreach(var p in ParameterTypes)
            {
                result = result * 31 + p.GetHashCode();
            }

            return result;
        }

        public override bool NotNullObjectEquals(ClepsType obj)
        {
            if (obj.GetType() != typeof(FunctionClepsType))
            {
                return false;
            }

            FunctionClepsType objToCompare = obj as FunctionClepsType;
            return ReturnType == objToCompare.ReturnType && ParameterTypes.SequenceEqual(objToCompare.ParameterTypes);
        }
    }
}
