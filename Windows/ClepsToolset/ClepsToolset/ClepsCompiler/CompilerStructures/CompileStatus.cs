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
        public List<CompilerError> Errors = new List<CompilerError>();

        public CompileStatus(bool exitOnError)
        {
            ExitOnError = exitOnError;
            Success = true;
        }

        public void AddError(CompilerError e)
        {
            Errors.Add(e);
            Success = false;
            if (ExitOnError)
            {
                ThrowIfError();
            }
        }

        public void ThrowIfError()
        {
            CompilerErrorException ex = new CompilerErrorException(Errors);
            throw ex;
        }
    }
}
