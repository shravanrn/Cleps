using ClepsCompiler.CompilerCore;
using ClepsCompiler.Utils.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerTypes
{
    class FunctionClepsType : ClepsType
    {
        public List<GenericClepsType> TemplateParameters { get; private set; }
        public List<ClepsType> ParameterTypes { get; private set; }
        public ClepsType ReturnType { get; private set; }
        private TypeManager TypeManager;

        private FunctionClepsType thisFunctionTypeWithStandardTemplates;

        public FunctionClepsType(TypeManager typeManager, List<GenericClepsType> templateParameters, List<ClepsType> parameterTypes, ClepsType returnType) : base(false, true, false, false, false, templateParameters.Count > 0)
        {
            TemplateParameters = templateParameters;
            ParameterTypes = parameterTypes;
            ReturnType = returnType;
            TypeManager = typeManager;

            thisFunctionTypeWithStandardTemplates = this;

            if (TemplateParameters.Count > 0)
            {
                for (int i = 0; i < TemplateParameters.Count; i++)
                {
                    thisFunctionTypeWithStandardTemplates = thisFunctionTypeWithStandardTemplates.ReplaceTemplateTypeComponents(TemplateParameters[i], new GenericClepsType(TypeManager, "T" + i)) as FunctionClepsType;
                }
            }
        }

        public FunctionClepsType(List<ClepsType> parameterTypes, ClepsType returnType) : this(null, new List<GenericClepsType>(), parameterTypes, returnType)
        {
        }

        public override ClepsType ReplaceTemplateTypeComponents(GenericClepsType templateTypeName, ClepsType targetTypeName)
        {
            ClepsType newReturnType = ReturnType.ReplaceTemplateTypeComponents(templateTypeName, targetTypeName);
            List<GenericClepsType> newTemplateParameters = TemplateParameters.Select(t => t.ReplaceTemplateTypeComponents(templateTypeName, targetTypeName))
                .Where(t => t is GenericClepsType)
                .Select(t => t as GenericClepsType)
                .ToList();
            List<ClepsType> newParameterTypes = ParameterTypes.Select(p => p.ReplaceTemplateTypeComponents(templateTypeName, targetTypeName)).ToList();

            var ret = new FunctionClepsType(TypeManager, newTemplateParameters, newParameterTypes, newReturnType);
            return ret;
        }

        public override SuccessStatus ReplaceWithConcreteType(ClepsType concreteType, Dictionary<GenericClepsType, ClepsType> outReplacementsMade)
        {
            if (!concreteType.IsFunctionType)
            {
                return SuccessStatus.Failure;
            }

            if (!(TemplateParameters.Count != 0 || ReturnType.HasGenericComponents || ParameterTypes.Any(p => p.HasGenericComponents)))
            {
                return SuccessStatus.Failure;
            }

            FunctionClepsType concreteTypeToUse = concreteType as FunctionClepsType;

            if (concreteTypeToUse.HasGenericComponents)
            {
                throw new NotImplementedException("Partial replacement of template parameters not supported. Not sure if this even needs to be supported");
            }

            if (ParameterTypes.Count != concreteTypeToUse.ParameterTypes.Count)
            {
                return SuccessStatus.Failure;
            }

            List<ClepsType> parameterAndReturnTypes = ParameterTypes.Union(new ClepsType[] { ReturnType }).ToList();
            List<ClepsType> concreteParameterAndReturnTypes = concreteTypeToUse.ParameterTypes.Union(new ClepsType[] { concreteTypeToUse.ReturnType }).ToList();
            var newReplacementsMade = new Dictionary<GenericClepsType, ClepsType>();

            SuccessStatus parameterAndReturnReplaceStatus = parameterAndReturnTypes.Zip(concreteParameterAndReturnTypes, (parameterType, concreteParameterType) =>
            {
                if (!parameterType.HasGenericComponents)
                {
                    return TypeManager.IsSameOrSubTypeOf(parameterType, concreteParameterType) ? SuccessStatus.Success : SuccessStatus.Failure;
                }
                else
                {
                    return parameterType.ReplaceWithConcreteType(concreteParameterType, newReplacementsMade);
                }
            }).Any(s => s == SuccessStatus.Failure)? SuccessStatus.Failure : SuccessStatus.Success;

            return parameterAndReturnReplaceStatus;
        }

        public override string GetClepsTypeString()
        {
            return String.Format("fn {0}({1})->{2}",
                TemplateParameters.Count > 0? "<" + String.Join(",", TemplateParameters) + ">" : "",
                String.Join(",", ParameterTypes.Select(p => p.GetClepsTypeString())), 
                ReturnType.GetClepsTypeString()
            );
        }

        public override int GetHashCode()
        {
            //Recommended approach in effective java book and java and android docs
            int result = 17;
            result = result * 31 + ReturnType.GetHashCode();

            foreach (var t in TemplateParameters)
            {
                result = result * 31 + t.GetHashCode();
            }
            foreach (var p in ParameterTypes)
            {
                result = result * 31 + p.GetHashCode();
            }

            return result;
        }

        public override bool NotNullObjectEquals(ClepsType obj)
        {
            if (obj.GetType() != typeof(FunctionClepsType))
            {
                return false;
            }

            FunctionClepsType objToCompare = obj as FunctionClepsType;

            if (TemplateParameters.Count != objToCompare.TemplateParameters.Count)
            {
                return false;
            }

            return thisFunctionTypeWithStandardTemplates.ReturnType == objToCompare.thisFunctionTypeWithStandardTemplates.ReturnType
                && thisFunctionTypeWithStandardTemplates.TemplateParameters.Count == objToCompare.thisFunctionTypeWithStandardTemplates.TemplateParameters.Count
                && thisFunctionTypeWithStandardTemplates.ParameterTypes.SequenceEqual(objToCompare.thisFunctionTypeWithStandardTemplates.ParameterTypes);

        }
    }
}
