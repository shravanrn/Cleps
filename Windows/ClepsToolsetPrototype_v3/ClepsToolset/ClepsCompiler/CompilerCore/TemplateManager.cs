using ClepsCompiler.CompilerTypes;
using ClepsCompiler.Utils.ClassBehaviors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerCore
{
    class TemplateManager<T>
    {
        private class VariableKey : EqualsAndHashCode<VariableKey>
        {
            public string ClepsClass;
            public string MemberName;
            public string LocationInMember;

            public override int GetHashCode()
            {
                //Recommended approach in effective java book and java and android docs
                int result = 17;
                result = result * 31 + ClepsClass.GetHashCode();
                result = result * 31 + MemberName.GetHashCode();
                result = result * 31 + LocationInMember.GetHashCode();
                return result;
            }

            public override bool NotNullObjectEquals(VariableKey obj)
            {
                return ClepsClass == obj.ClepsClass && MemberName == obj.MemberName && LocationInMember == obj.LocationInMember;
            }
        }

        private class TemplateInstantiation : EqualsAndHashCode<TemplateInstantiation>
        {
            public readonly Dictionary<GenericClepsType, ClepsType> ConcreteInstantiations;

            public TemplateInstantiation(Dictionary<GenericClepsType, ClepsType> concreteInstantiations)
            {
                ConcreteInstantiations = concreteInstantiations;
            }

            public override int GetHashCode()
            {
                //fix
                return ConcreteInstantiations.Count;
            }

            public override bool NotNullObjectEquals(TemplateInstantiation obj)
            {
                return ConcreteInstantiations.Keys.Count == obj.ConcreteInstantiations.Keys.Count &&
                    ConcreteInstantiations.Keys.All(key => obj.ConcreteInstantiations.ContainsKey(key) && Object.Equals(ConcreteInstantiations[key], obj.ConcreteInstantiations[key]));
            }
        }

        private Dictionary<VariableKey, List<T>> VariableInstances = new Dictionary<VariableKey, List<T>>();
        private Dictionary<T, List<TemplateInstantiation>> InstanceInitializations = new Dictionary<T, List<TemplateInstantiation>>();

        public void AddTemplateFunctionVariable(string clepsClass, string memberName, string locationInMember)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(clepsClass));
            Debug.Assert(!String.IsNullOrWhiteSpace(memberName));

            var key = new VariableKey() { ClepsClass = clepsClass, MemberName = memberName, LocationInMember = locationInMember ?? "" };
            VariableInstances.Add(key, new List<T>());
        }

        public void CreatePossibleFunctionAssignment(string clepsClass, string memberName, string locationInMember, T assignment)
        {
            var key = new VariableKey() { ClepsClass = clepsClass, MemberName = memberName, LocationInMember = locationInMember ?? "" };
            VariableInstances[key].Add(assignment);
        }

        public List<T> GetFunctionInitializationsForVariable(string clepsClass, string memberName, string locationInMember)
        {
            var key = new VariableKey() { ClepsClass = clepsClass, MemberName = memberName, LocationInMember = locationInMember ?? "" };
            return VariableInstances[key];
        }

        public FuncRet CallIfNotInstantiated<FuncRet>(T templateFunction, Dictionary<GenericClepsType, ClepsType> templateInitializations, Func<T, FuncRet> funcToCall)
        {
            if(InstanceInitializations[templateFunction].Contains(new TemplateInstantiation(templateInitializations)))
            {
                return funcToCall(templateFunction);
            }

            return default(FuncRet);
        }
    }

}
