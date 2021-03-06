﻿using ClepsCompiler.CompilerCore;
using ClepsCompiler.CompilerStructures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler
{
    class Program
    {
        static int Main(string[] args)
        {
            CommandLineParameters programParams = ParseParameters(args);
            if (!ValidateFilesExists(programParams.Files))
            {
                return -2;
            }

            CompilerDriver driver = new CompilerDriver(programParams);
            CompileStatus status = driver.CompileFiles();

            if (status.Success)
            {
                Console.WriteLine("Built successfully");
            }
            else
            {
                Console.WriteLine("Error building");
            }

            Console.WriteLine(String.Join("\n",
                status.Logs.Select(e => String.Format("{0} - File: {1} Line:{2} Column:{3} {4}", e.LogName, e.SourceFile, e.LineNumber, e.PositionInLine, e.Message)).ToList()
            ));

            Console.ReadLine();
            return 0;
        }

        private static CommandLineParameters ParseParameters(string[] args)
        {
            CommandLineParameters programParams = new CommandLineParameters { Files = new List<string>(args), OutputDirectory = ".", OutputFileName = "outputFile", ExitOnFirstError = false, EntryPointClass = null };
            if (programParams.Files.Count == 0)
            {
                {
                    string testFileName = @"..\..\..\ClepsCode\CoreLibrary.cleps";

                    if (File.Exists(testFileName))
                    {
                        programParams.Files.Add(testFileName);
                    }
                }
                {
                    string testFileName = @"..\..\..\ClepsCode\TestProgram.cleps";

                    if (File.Exists(testFileName))
                    {
                        programParams.Files.Add(testFileName);
                    }
                }
            }

            return programParams;
        }


        private static bool ValidateFilesExists(List<string> files)
        {
            if (files.Count == 0)
            {
                Console.WriteLine("Files to compile are not specified");
                return false;
            }

            var missingFiles = files.Where(f => !File.Exists(f));
            if (missingFiles.Any())
            {
                Console.WriteLine("Could not find files : {0}", String.Join(", ", missingFiles));
                return false;
            }

            return true;
        }
    }
}
