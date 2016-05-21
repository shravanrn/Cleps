using ClepsCompiler.CompilerStructures;
using ClepsCompiler.CompilerTypes;
using ClepsCompiler.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerCore
{
    class ClepsClassBuilder
    {
        public string FullyQualifiedName { get; private set; }
        public OrderedDictionary<string, ClepsVariable> MemberVariables { get; private set; }
        public Dictionary<string, List<ClepsVariable>> MemberMethods { get; private set; }
        public OrderedDictionary<string, ClepsVariable> StaticMemberVariables { get; private set; }
        public Dictionary<string, List<ClepsVariable>> StaticMemberMethods { get; private set; }

        public ClepsClassBuilder(string name)
        {
            FullyQualifiedName = name;
            MemberVariables = new OrderedDictionary<string, ClepsVariable>();
            MemberMethods = new Dictionary<string, List<ClepsVariable>>();
            StaticMemberVariables = new OrderedDictionary<string, ClepsVariable>();
            StaticMemberMethods = new Dictionary<string, List<ClepsVariable>>();
        }

        public ClepsClass BuildClass()
        {
            return new ClepsClass(FullyQualifiedName, MemberVariables, MemberMethods,
                StaticMemberVariables, StaticMemberMethods);
        }

        public bool CanAddMemberToClass(string memberName, ClepsType memberType, bool isStatic, out string cantAddReason)
        {
            if (memberType.IsFunctionType)
            {
                return CanAddFunctionToClass(memberName, memberType, isStatic, out cantAddReason);
            }
            else
            {
                return CanAddVariableToClass(memberName, memberType, isStatic, out cantAddReason);
            }            
        }

        private bool CanAddVariableToClass(string memberName, ClepsType memberType, bool isStatic, out string cantAddReason)
        {
            //variable rules are simple. No other member can have the same name
            var ret =
                    !MemberVariables.ContainsKey(memberName) &&
                    !MemberMethods.ContainsKey(memberName) &&
                    !StaticMemberVariables.ContainsKey(memberName) &&
                    !StaticMemberMethods.ContainsKey(memberName);
            cantAddReason = ret ? null : String.Format("Class {0} has multiple definitions of member {1}", FullyQualifiedName, memberName);
            return ret;
        }

        private bool CanAddFunctionToClass(string memberName, ClepsType memberType, bool isStatic, out string cantAddReason)
        {
            //for a new member function, no variables, or static functions can have the same name
            //for a new static function, no variables, or member functions can have the same name
            if (
                (isStatic && (
                    MemberVariables.ContainsKey(memberName) ||
                    MemberMethods.ContainsKey(memberName) ||
                    StaticMemberVariables.ContainsKey(memberName))
                )
                ||
                (!isStatic && (
                    MemberVariables.ContainsKey(memberName) ||
                    StaticMemberVariables.ContainsKey(memberName) ||
                    StaticMemberMethods.ContainsKey(memberName))
                )
            )
            {
                cantAddReason = String.Format("Class {0} has multiple definitions of member {1}", FullyQualifiedName, memberName);
                return false;
            }

            //the name is used by a function in the same class. Check if we can overload the function

            FunctionClepsType methodMemberType = memberType as FunctionClepsType;
            List<ClepsVariable> methods;

            if (isStatic)
            {
                methods = StaticMemberMethods.ContainsKey(memberName) ? StaticMemberMethods[memberName] : new List<ClepsVariable>();
            }
            else
            {
                methods = MemberMethods.ContainsKey(memberName) ? MemberMethods[memberName] : new List<ClepsVariable>();
            }

            if (FunctionOverloadManager.MatchingFunctionTypeExists(methods.Select(m => m.VariableType as FunctionClepsType).ToList(), methodMemberType))
            {
                cantAddReason = String.Format("Class {0} already has a function {1}{2}.", FullyQualifiedName, memberName, methodMemberType.GetClepsTypeString());
                return false;
            }
            else
            {
                cantAddReason = null;
                return true;
            }
        }

        public void AddNewMember(bool isStatic, ClepsVariable member)
        {
            string cantAddReason;
            Debug.Assert(CanAddMemberToClass(member.VariableName, member.VariableType, isStatic, out cantAddReason));

            OrderedDictionary<string, ClepsVariable> variables;
            Dictionary<string, List<ClepsVariable>> methods;

            if (isStatic)
            {
                variables = StaticMemberVariables;
                methods = StaticMemberMethods;
            }
            else
            {
                variables = MemberVariables;
                methods = MemberMethods;
            }

            if (member.VariableType.IsFunctionType)
            {
                List<ClepsVariable> functions;
                if(methods.ContainsKey(member.VariableName))
                {
                    functions = methods[member.VariableName];
                }
                else
                {
                    functions = new List<ClepsVariable>();
                    methods[member.VariableName] = functions;
                }

                functions.Add(member);
            }
            else
            {
                variables.Add(member.VariableName, member);
            }           
        }
    }
}
