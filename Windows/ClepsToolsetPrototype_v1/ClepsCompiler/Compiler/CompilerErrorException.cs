using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.Compiler
{
    /// <summary>
    /// A class to that encapsulates compiler error(s) as an exception so that we can quickly exit the compilation process
    /// </summary>
    class CompilerErrorException : Exception
    {
        public List<CompilerError> Errors = new List<CompilerError>();

        public CompilerErrorException(List<CompilerError> errors)
        {
            Errors = errors;
        }
    }
}
