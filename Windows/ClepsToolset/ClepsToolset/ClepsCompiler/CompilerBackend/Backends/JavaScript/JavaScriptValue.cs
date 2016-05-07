using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClepsCompiler.CompilerTypes;

namespace ClepsCompiler.CompilerBackend.Backends.JavaScript
{
    class JavaScriptValue : IValue
    {
        public string Expression { get; private set; }
        public ClepsType ExpressionType { get; private set; }

        public JavaScriptValue(string valueUsed, ClepsType clepsType)
        {
            Expression = valueUsed;
            ExpressionType = clepsType;
        }
    }
}
