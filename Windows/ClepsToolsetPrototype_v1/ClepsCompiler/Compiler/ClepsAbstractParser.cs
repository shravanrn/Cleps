using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.Compiler
{
    /// <summary>
    /// Abstract class that all cleps parsers inherit from
    /// </summary>
    /// <typeparam name="T"></typeparam>
    abstract class ClepsAbstractParser<T> : ClepsBaseVisitor<T>
    {
        protected string FileName;

        public T ParseFile(string fileName, IParseTree tree)
        {
            FileName = fileName;
            T ret = Visit(tree);
            return ret;
        }
    }
}
