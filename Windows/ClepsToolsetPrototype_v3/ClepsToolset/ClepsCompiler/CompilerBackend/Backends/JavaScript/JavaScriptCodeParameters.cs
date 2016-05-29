using ClepsCompiler.CompilerTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerBackend.Backends.JavaScript
{
    class JavaScriptCodeParameters
    {
        public const string TOPLEVELNAMESPACE = "Cleps";
        public const string VARIABLEPREFIX = "_$";

        public static readonly Dictionary<string, string> CHARACTERSUBSTITUTIONS = new Dictionary<string, string>()
        {
            {" ", "$blank$" },
            {",", "$comma$" },
            {"<", "$lt$" },
            {">", "$gt$" },
            {"(", "$oround$" },
            {")", "$cround$" },
            {".", "$dot$" },                    
            {"+", "$plus$" },
            {"-", "$dash$" },
            {"*", "$star$" },
            {"/", "$fslash$" },
        };


        public static string GetMangledFunctionName(string currentFunctionName, FunctionClepsType functionType)
        {
            string ret = String.Format("{0} {1}", currentFunctionName, functionType.GetClepsTypeString());

            foreach(var kvp in CHARACTERSUBSTITUTIONS)
            {
                ret = ret.Replace(kvp.Key, kvp.Value);
            }

            return ret;
        }
    }
}
