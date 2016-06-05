using Antlr4.Runtime;
using ClepsCompiler.CompilerBackend.Backends.JavaScript;
using ClepsCompiler.CompilerBackend;
using ClepsCompiler.CompilerStructures;
using ClepsCompiler.SyntaxTreeVisitors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerCore
{
    class CompilerDriver
    {
        private CommandLineParameters Args;

        public CompilerDriver(CommandLineParameters args)
        {
            Args = args;
        }

        public CompileStatus CompileFiles()
        {
            ClassManager classManager = new ClassManager();
            CompileStatus status = new CompileStatus(Args.ExitOnFirstError);
            ICodeGenerator codeGenerator = new JavaScriptCodeGenerator();
            TypeManager typeManager = new TypeManager();
            EntryPointManager entryPointManager = new EntryPointManager(status, Args.EntryPointClass);

            try
            {
                codeGenerator.Initiate();

                {
                    ClepsClassNamesCollectorVisitor classSkeletonGenerator = new ClepsClassNamesCollectorVisitor(status, classManager, codeGenerator, typeManager);
                    ParseFilesWithGenerator(classSkeletonGenerator, status);
                }
                {
                    ClepsMemberGeneratorVisitor memberGenerator = new ClepsMemberGeneratorVisitor(status, classManager, codeGenerator, entryPointManager, typeManager);
                    ParseFilesWithGenerator(memberGenerator, status);
                }
                {
                    ClepsFunctionBodyGeneratorVisitor functionBodyGenerator = new ClepsFunctionBodyGeneratorVisitor(status, classManager, codeGenerator, typeManager);
                    ParseFilesWithGenerator(functionBodyGenerator, status);
                }

                string entryClassName = entryPointManager.GetChosenEntryPointOrNull();

                if (!String.IsNullOrWhiteSpace(entryClassName))
                {
                    codeGenerator.AddEntryPoint(entryClassName, EntryPointManager.EntryPointName);
                }

                codeGenerator.Output(Args.OutputDirectory, Args.OutputFileName, status);
            }
            catch (CompilerLogException)
            {
                //Supress compiler errors
            }
            finally
            {
                codeGenerator.Close();
            }

            return status;
        }

        private void ParseFilesWithGenerator(ClepsAbstractVisitor generator, CompileStatus status)
        {
            foreach (string fileName in Args.Files)
            {
                LexerParserErrorHandler lexerParserErrorHandler = new LexerParserErrorHandler(fileName, status);
                var data = File.ReadAllText(fileName);

                AntlrInputStream s = new AntlrInputStream(data);
                ClepsLexer lexer = new ClepsLexer(s);
                CommonTokenStream tokens = new CommonTokenStream(lexer);
                ClepsParser parser = new ClepsParser(tokens);

                parser.RemoveErrorListeners();
                parser.AddErrorListener(lexerParserErrorHandler);
                var parsedFile = parser.compilationUnit();

                generator.ParseFile(fileName, parsedFile);
            }
        }
    }
}
