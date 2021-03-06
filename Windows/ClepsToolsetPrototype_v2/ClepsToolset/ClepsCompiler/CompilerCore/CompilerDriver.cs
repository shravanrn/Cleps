﻿using Antlr4.Runtime;
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
            FunctionOverloadManager functionOverloadManager = new FunctionOverloadManager();

            try
            {
                codeGenerator.Initiate();

                {
                    ClepsClassNamesCollectorVisitor classSkeletonGenerator = new ClepsClassNamesCollectorVisitor(status, classManager, codeGenerator);
                    ParseFilesWithGenerator(classSkeletonGenerator, status);
                }
                {
                    ClepsMemberGeneratorVisitor memberGenerator = new ClepsMemberGeneratorVisitor(status, classManager, codeGenerator);
                    ParseFilesWithGenerator(memberGenerator, status);
                }
                //{
                //    ClepsCodeAnalysisGeneratorParser codeAnalysisGenerator = new ClepsCodeAnalysisGeneratorParser(status, classManager, codeGenerator);
                //    ParseFilesWithGenerator(codeAnalysisGenerator, status);
                //}
                {
                    ClepsFunctionBodyGeneratorVisitor functionBodyGenerator = new ClepsFunctionBodyGeneratorVisitor(status, classManager, codeGenerator, typeManager, functionOverloadManager);
                    ParseFilesWithGenerator(functionBodyGenerator, status);
                }
                codeGenerator.Output(Args.OutputDirectory, Args.OutputFileName, status);
            }
            catch (CompilerErrorException)
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
