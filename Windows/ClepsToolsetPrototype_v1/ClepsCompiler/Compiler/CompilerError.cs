using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.Compiler
{
    /// <summary>
    /// All errors found during compilation is defined in terms of the following properties
    /// </summary>
    class CompilerError
    {
        public string ErrorSourceFile { get; private set; }
        public long ErrorLineNumber { get; private set; }
        public long ErrorPositionInLine { get; private set; }
        public string ErrorMessage { get; private set; }

        public CompilerError(string errorSourceFile, long errorLineNumber, long errorPositionInLine, string errorMessage)
        {
            ErrorSourceFile = errorSourceFile;
            ErrorLineNumber = errorLineNumber;
            ErrorPositionInLine = errorPositionInLine;
            ErrorMessage = errorMessage;
        }
    }
}
