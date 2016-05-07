using ClepsCompiler.CompilerTypes;
using ClepsCompiler.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerStructures
{
    class ClepsClassBuilder
    {
        public string FullyQualifiedName { get; private set; }
        public OrderedDictionary<string, ClepsType> MemberVariables { get; private set; }
        public Dictionary<string, ClepsType> MemberMethods { get; private set; }
        public Dictionary<string, ClepsType> StaticMemberVariables { get; private set; }
        public Dictionary<string, ClepsType> StaticMemberMethods { get; private set; }

        public ClepsClassBuilder(string name)
        {
            FullyQualifiedName = name;
            MemberVariables = new OrderedDictionary<string, ClepsType>();
            MemberMethods = new Dictionary<string, ClepsType>();
            StaticMemberVariables = new Dictionary<string, ClepsType>();
            StaticMemberMethods = new Dictionary<string, ClepsType>();
        }

        public ClepsClass BuildClass()
        {
            return new ClepsClass(FullyQualifiedName, MemberVariables, MemberMethods,
                StaticMemberVariables, StaticMemberMethods);
        }

        public bool DoesClassContainMember(string memberName)
        {
            return MemberVariables.ContainsKey(memberName) || MemberMethods.ContainsKey(memberName) ||
                StaticMemberVariables.ContainsKey(memberName) || StaticMemberMethods.ContainsKey(memberName);
        }

        public void AddNewMember(bool isStatic, ClepsType memberType, string memberName)
        {
            Debug.Assert(!DoesClassContainMember(memberName));

            if (isStatic)
            {
                if (memberType.IsFunctionType)
                {
                    StaticMemberMethods.Add(memberName, memberType);
                }
                else
                {
                    StaticMemberVariables.Add(memberName, memberType);
                }
            }
            else
            {
                if (memberType.IsFunctionType)
                {
                    MemberMethods.Add(memberName, memberType);
                }
                else
                {
                    MemberVariables.Add(memberName, memberType);
                }
            }
        }
    }
}
