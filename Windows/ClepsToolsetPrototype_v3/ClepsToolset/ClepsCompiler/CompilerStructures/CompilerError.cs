using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerStructures
{
    /// <summary>
    /// All errors found during compilation
    /// </summary>
    class CompilerError : AbstractCompilerLog
    {
        public override string LogName
        {
            get
            {
                return "Error";
            }
        }

        public CompilerError(string sourceFile, long lineNumber, long positionInLine, string message) : base(sourceFile, lineNumber, positionInLine, message)
        {
        }
    }
}
