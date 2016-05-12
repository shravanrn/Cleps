using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler
{
    class CommandLineParameters
    {
        public List<string> Files { get; internal set; }
        public string OutputDirectory { get; internal set; }
        public string OutputFileName { get; internal set; }
        public bool ExitOnFirstError { get; internal set; }
    }
}
