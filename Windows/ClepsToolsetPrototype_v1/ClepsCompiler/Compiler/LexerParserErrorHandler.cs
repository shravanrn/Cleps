using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.Compiler
{
    /// <summary>
    /// An error handler for the antlr lexer parser that processes the cleps code
    /// </summary>
    class LexerParserErrorHandler : BaseErrorListener
    {
        public CompileStatus CompileStatus;
        public string FileName;

        public LexerParserErrorHandler(string fileName, CompileStatus compileStatus)
        {
            FileName = fileName;
            CompileStatus = compileStatus;
        }

        public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            CompileStatus.AddError(new CompilerError(FileName, line, charPositionInLine, msg));
        }
    }
}
