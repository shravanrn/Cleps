using ClepsCompiler.CompilerTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerBackend.Backends.JavaScript
{
    class JavaScriptRegister : IValueRegister
    {
        public string Expression { get; private set; }
        public ClepsType ExpressionType { get; private set; }

        public JavaScriptRegister(string valueUsed, ClepsType clepsType)
        {
            Expression = valueUsed;
            ExpressionType = clepsType;
        }
    }
}
