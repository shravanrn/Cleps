using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerStructures
{
    /// <summary>
    /// All warnings found during compilation
    /// </summary>
    class CompilerWarning : AbstractCompilerLog
    {
        public override string LogName
        {
            get
            {
                return "Warning";
            }
        }

        public CompilerWarning(string sourceFile, long lineNumber, long positionInLine, string message) : base(sourceFile, lineNumber, positionInLine, message)
        {
        }
    }
}
