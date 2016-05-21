using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerStructures
{
    /// <summary>
    /// A class to that encapsulates compiler logs such as error(s) as an exception so that we can quickly exit the compilation process
    /// </summary>
    class CompilerLogException : Exception
    {
        public List<CompilerWarning> Warnings = new List<CompilerWarning>();
        public List<CompilerError> Errors = new List<CompilerError>();

        public CompilerLogException(List<CompilerWarning> warnings, List<CompilerError> errors)
        {
            Warnings = warnings;
            Errors = errors;
        }
    }
}
