using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerStructures
{
    /// <summary>
    /// All logs such as errors, warnings etc found during compilation is defined in terms of the following properties
    /// </summary>
    abstract class AbstractCompilerLog
    {
        public string SourceFile { get; private set; }
        public long LineNumber { get; private set; }
        public long PositionInLine { get; private set; }
        public string Message { get; private set; }
        public abstract string LogName { get; }

        public AbstractCompilerLog(string sourceFile, long lineNumber, long positionInLine, string message)
        {
            SourceFile = sourceFile;
            LineNumber = lineNumber;
            PositionInLine = positionInLine;
            Message = message;
        }
    }
}
