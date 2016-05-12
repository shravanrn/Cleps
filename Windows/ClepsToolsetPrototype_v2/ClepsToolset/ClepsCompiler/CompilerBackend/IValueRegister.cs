using ClepsCompiler.CompilerTypes;
using ClepsCompiler.Utils.ClassBehaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerBackend
{
    interface IValueRegister
    {
        ClepsType ExpressionType { get; }
    }
}
