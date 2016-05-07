using ClepsCompiler.CompilerTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerBackend
{
    interface IValue
    {
        ClepsType ExpressionType { get; }
    }
}
