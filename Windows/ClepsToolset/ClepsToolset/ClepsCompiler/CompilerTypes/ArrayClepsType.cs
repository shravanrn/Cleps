using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerTypes
{
    class ArrayClepsType : ClepsType
    {
        public ClepsType BaseType { get; private set; }
        public long[] Dimensions { get; private set; }

        public ArrayClepsType(ClepsType baseType, long[] dimensions)
        {
            BaseType = baseType;
            Dimensions = dimensions;
            IsFunctionType = false;
            IsArrayType = true;
        }

        public override string GetClepsTypeString()
        {
            return String.Format("{0}[{1}]", BaseType.GetClepsTypeString(), String.Join(",", Dimensions));
        }

        public override int GetHashCode()
        {
            //Recommended approach in effective java book and java and android docs
            int result = 17;
            result = result * 31 + BaseType.GetHashCode();
            foreach (var dim in Dimensions)
            {
                result = result * 31 + dim.GetHashCode();
            }

            return result;
        }

        public override bool NotNullObjectEquals(ClepsType obj)
        {
            if (obj.GetType() != typeof(ArrayClepsType))
            {
                return false;
            }

            ArrayClepsType objToCompare = obj as ArrayClepsType;
            return BaseType == objToCompare.BaseType && Dimensions.SequenceEqual(objToCompare.Dimensions);
        }

        public ClepsType GetTypeForArrayAccess(int numberOfDimsToAccess)
        {
            Debug.Assert(numberOfDimsToAccess <= Dimensions.Length, "Trying to access more array dimensions than what exist");

            var remainingDims = Dimensions.Reverse().Take(Dimensions.Length - numberOfDimsToAccess).Reverse().ToArray();

            if(remainingDims.Length == 0)
            {
                return BaseType;
            }
            else
            {
                return new ArrayClepsType(BaseType, remainingDims);
            }
        }
    }
}
