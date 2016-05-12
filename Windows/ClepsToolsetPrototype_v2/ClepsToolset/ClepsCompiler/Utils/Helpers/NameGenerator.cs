using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.Utils.Helpers
{
    class NameGenerator
    {
        public static string GetAvailableName(string prefix, List<string> usedNames)
        {
            var ret = prefix;
            if (!usedNames.Contains(prefix))
            {
                ret = prefix;
            }
            else
            {
                bool newNameFound = false;
                for (ulong i = 0; i < ulong.MaxValue; i++)
                {
                    var newName = prefix + "_" + i;
                    if (!usedNames.Contains(newName))
                    {
                        newNameFound = true;
                        ret = newName;
                    }
                }

                if (!newNameFound)
                {
                    throw new Exception("Unable to find an available name with prefix " + prefix);
                }
            }

            return ret;
        }
    }
}
