using ClepsCompiler.CompilerTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerStructures
{
    class CompilerConstants
    {
        public static readonly ClepsType ClepsByteType = new BasicClepsType("System.Types.Byte");
        public static readonly ClepsType ClepsBoolType = new BasicClepsType("System.Types.Bool");
    }
}
