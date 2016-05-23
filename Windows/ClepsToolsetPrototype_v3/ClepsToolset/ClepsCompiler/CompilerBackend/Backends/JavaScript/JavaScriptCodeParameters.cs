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

        public static readonly Dictionary<char, char> CHARACTERSUBSTITUTIONS = new Dictionary<char, char>()
        {
            //{'|', '\u01C0'}, // Unicode Symbol ǀ 
            {' ', '_' },//'\uFE4D'}, // Unicode Symbol ﹍
            {',', '_' },//'\u00B8'}, // Unicode Symbol ¸
            {'-', '_' },//'\u1508'}, // Unicode Symbol ᔈ
            {'<', '_' },//'\u00AB'}, // Unicode Symbol «
            {'>', '_' },//'\u00BB'}, // Unicode Symbol »
            {'(', '_' },//'\u093F'}, // Unicode Symbol ि
            {')', '_' },//'\u0940'}, // Unicode Symbol ी
            {'.', '_' },//'\uFE4E'}, // Unicode Symbol ﹎ 
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
