using ClepsCompiler.Utils.ClassBehaviors;
using ClepsCompiler.Utils.Types;
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
        public bool HasGenericComponents { get; internal set; }

        public abstract ClepsType ReplaceTemplateTypeComponents(GenericClepsType templateTypeName, ClepsType targetTypeName);
        public abstract SuccessStatus ReplaceWithConcreteType(ClepsType concreteType, Dictionary<GenericClepsType, ClepsType> outReplacementsMade);

        public abstract string GetClepsTypeString();

        public abstract override int GetHashCode();
        public abstract override bool NotNullObjectEquals(ClepsType obj);

        public ClepsType(bool isArrayType, bool isFunctionType, bool isGenericType, bool isPointerType, bool isStaticType, bool isGeneric)
        {
            IsArrayType = isArrayType;
            IsFunctionType = isFunctionType;
            IsGenericType = isGenericType;
            IsStaticType = isStaticType;
            IsPointerType = isPointerType;
            HasGenericComponents = isGeneric;
        }

        public override string ToString()
        {
            return GetClepsTypeString();
        }
    }
}
