using ClepsCompiler.CompilerCore;
using ClepsCompiler.Utils.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerTypes
{
    class GenericClepsType : ClepsType
    {
        public string GenericTypeName { get; private set; }
        private TypeManager TypeManager;

        public GenericClepsType(TypeManager typeManager, string genericTypeName) : base(false, false, false, false, false, true)
        {
            TypeManager = typeManager;
            GenericTypeName = genericTypeName;
        }

        public override ClepsType ReplaceTemplateTypeComponents(GenericClepsType templateTypeName, ClepsType targetTypeName)
        {
            if (this == templateTypeName)
            {
                return targetTypeName;
            }

            return this;
        }

        public override SuccessStatus ReplaceWithConcreteType(ClepsType concreteType, Dictionary<GenericClepsType, ClepsType> outReplacementsMade)
        {
            var newReplacementType = concreteType;

            if (outReplacementsMade.ContainsKey(this))
            {
                var existingReplacementType = outReplacementsMade[this];

                if (TypeManager.IsSameOrSubTypeOf(existingReplacementType, newReplacementType))
                {
                    return SuccessStatus.Success;
                }
                else if (TypeManager.IsSameOrSubTypeOf(newReplacementType, existingReplacementType))
                {
                    outReplacementsMade[this] = newReplacementType;
                    return SuccessStatus.Success;
                }

                return SuccessStatus.Failure;
            }
            else
            {
                outReplacementsMade[this] = newReplacementType;
                return SuccessStatus.Success;
            }
        }

        public override string GetClepsTypeString()
        {
            return GenericTypeName;
        }

        public override int GetHashCode()
        {
            return GenericTypeName.GetHashCode();
        }

        public override bool NotNullObjectEquals(ClepsType obj)
        {
            if (obj.GetType() != typeof(GenericClepsType))
            {
                return false;
            }

            GenericClepsType objToCompare = obj as GenericClepsType;
            return GenericTypeName == objToCompare.GenericTypeName;
        }
    }
}
