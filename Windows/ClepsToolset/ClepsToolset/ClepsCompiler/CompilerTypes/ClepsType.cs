using ClepsCompiler.Utils.ClassBehaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerTypes
{
    abstract class ClepsType : EqualsAndHashCode<ClepsType>
    {
        public bool IsFunctionType { get; internal set; }
        public bool IsArrayType { get; internal set; }

        public abstract string GetClepsTypeString();

        public abstract override int GetHashCode();
        public abstract override bool NotNullObjectEquals(ClepsType obj);

        public override string ToString()
        {
            return GetClepsTypeString();
        }
    }
}
