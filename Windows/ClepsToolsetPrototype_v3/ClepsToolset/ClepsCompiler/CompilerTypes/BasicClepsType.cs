﻿using ClepsCompiler.Utils.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerTypes
{
    class BasicClepsType : ClepsType
    {
        public string RawTypeName { get; private set; }

        public BasicClepsType(string rawTypeName) : base(false, false, false, false, false, false)
        {
            RawTypeName = rawTypeName;
        }

        public override ClepsType ReplaceTemplateTypeComponents(GenericClepsType templateTypeName, ClepsType targetTypeName)
        {
            return this;
        }

        public override SuccessStatus ReplaceWithConcreteType(ClepsType concreteType, Dictionary<GenericClepsType, ClepsType> outReplacementsMade)
        {
            return SuccessStatus.Failure;
        }

        public override string GetClepsTypeString()
        {
            return RawTypeName;
        }

        public override int GetHashCode()
        {
            return RawTypeName.GetHashCode();
        }

        public override bool NotNullObjectEquals(ClepsType obj)
        {
            if (obj.GetType() != typeof(BasicClepsType))
            {
                return false;
            }

            BasicClepsType objToCompare = obj as BasicClepsType;
            return RawTypeName == objToCompare.RawTypeName;
        }
    }
}
