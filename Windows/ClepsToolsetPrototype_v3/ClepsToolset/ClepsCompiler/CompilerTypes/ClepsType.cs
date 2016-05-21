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
        public bool IsArrayType { get; internal set; }
        public bool IsFunctionType { get; internal set; }
        public bool IsGenericType { get; internal set; }
        public bool IsStaticType { get; internal set; }
        public bool IsPointerType { get; internal set; }

        public abstract string GetClepsTypeString();

        public abstract override int GetHashCode();
        public abstract override bool NotNullObjectEquals(ClepsType obj);

        public ClepsType(bool isArrayType, bool isFunctionType, bool isGenericType, bool isPointerType, bool isStaticType)
        {
            IsArrayType = isArrayType;
            IsFunctionType = isFunctionType;
            IsGenericType = isGenericType;
            IsStaticType = isStaticType;
            IsPointerType = isPointerType;
        }

        public override string ToString()
        {
            return GetClepsTypeString();
        }
    }
}
