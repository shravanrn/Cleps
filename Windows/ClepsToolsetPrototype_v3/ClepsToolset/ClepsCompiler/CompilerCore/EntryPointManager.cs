using ClepsCompiler.CompilerStructures;
using ClepsCompiler.CompilerTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerCore
{
    class EntryPointManager
    {
        private CompileStatus Status;
        private string SpecifiedEntryPointClass = null;
        private List<string> EntryPoints = new List<string>();

        public const string EntryPointName = "Main";
        private readonly ClepsType EntryPointType = new FunctionClepsType(new List<ClepsType>(), VoidClepsType.GetVoidType());

        public EntryPointManager(CompileStatus status, string specifiedEntryPointClass = null)
        {
            Status = status;
            SpecifiedEntryPointClass = specifiedEntryPointClass;
        }

        public void NewMemberSeen(string fullClassName, string memberName, ClepsType memberType, bool isStatic)
        {
            if (memberName == "Main" && memberType.IsFunctionType && isStatic)
            {
                EntryPoints.Add(fullClassName);
            }
        }

        public string GetChosenEntryPointOrNull()
        {
            if (EntryPoints.Count == 0)
            {
                string errorMessage = String.Format("No program entry points found. Make sure you have a static function called static function called {0} with signature {1}", EntryPointName, EntryPointType.GetClepsTypeString());
                Status.AddError(new CompilerError("", 0, 0, errorMessage));
                return null;
            }
            else
            {
                if (SpecifiedEntryPointClass == null)
                {
                    if (EntryPoints.Count == 1)
                    {
                        return EntryPoints[0];
                    }
                    else
                    {
                        string errorMessage = String.Format("Multiple program entry points found. \n{0}.\n. Specify which one you want to use as a compiler parameter.", String.Join(", ", EntryPoints));
                        Status.AddError(new CompilerError("", 0, 0, errorMessage));
                        return null;
                    }
                }
                else
                {
                    if (EntryPoints.Contains(SpecifiedEntryPointClass))
                    {
                        return SpecifiedEntryPointClass;
                    }
                    else
                    {
                        string errorMessage = String.Format("Program entry point not found in {0}. Either add a program entry point to {0} or change the specified program entry point.", SpecifiedEntryPointClass);
                        Status.AddError(new CompilerError("", 0, 0, errorMessage));
                        return null;
                    }
                }
            }
        }
    }
}
