using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerStructures
{
    /// <summary>
    /// A class that maintains properties of the current compilation state about the outcome of the compilation process
    /// </summary>
    class CompileStatus
    {
        private bool ExitOnError;

        public bool Success { get; private set; }
        public List<AbstractCompilerLog> Logs = new List<AbstractCompilerLog>();
        public List<CompilerWarning> Warnings = new List<CompilerWarning>();
        public List<CompilerError> Errors = new List<CompilerError>();

        public CompileStatus(bool exitOnError)
        {
            ExitOnError = exitOnError;
            Success = true;
        }

        public void AddWarning(CompilerWarning w)
        {
            Warnings.Add(w);
            Logs.Add(w);
        }

        public void AddError(CompilerError e)
        {
            Errors.Add(e);
            Logs.Add(e);
            Success = false;
            if (ExitOnError)
            {
                ThrowIfError();
            }
        }

        public void ThrowIfError()
        {
            CompilerLogException ex = new CompilerLogException(Warnings, Errors);
            throw ex;
        }
    }
}
