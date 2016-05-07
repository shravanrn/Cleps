using ClepsCompiler.CompilerTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerCore
{
    class TypeManager
    {
        public ClepsType GetSuperType(List<ClepsType> types)
        {
            if (types.Distinct().Count() == 1)
            {
                return types[0];
            }

            throw new NotImplementedException("Super type of no or multiple types is not supported");
        }
    }
}
