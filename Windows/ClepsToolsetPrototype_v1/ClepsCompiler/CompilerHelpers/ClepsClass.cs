using ClepsCompiler.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerHelpers
{
    /// <summary>
    /// Defines the various properties of a cleps class
    /// </summary>
    class ClepsClass
    {
        public string FullyQualifiedName { get; private set; }
        public OrderedDictionary<string, ClepsType> MemberVariables { get; private set; }
        public Dictionary<string, ClepsType> MemberMethods { get; private set; }
        public Dictionary<string, ClepsType> StaticMemberVariables { get; private set; }
        public Dictionary<string, ClepsType> StaticMemberMethods { get; private set; }
        public ClepsType RawLLVMTypeMap { get; private set; }

        public ClepsClass(string name)
        {
            FullyQualifiedName = name;
            MemberVariables = new OrderedDictionary<string, ClepsType>();
            MemberMethods = new Dictionary<string, ClepsType>();
            StaticMemberVariables = new Dictionary<string, ClepsType>();
            StaticMemberMethods = new Dictionary<string, ClepsType>();
        }

        public bool DoesClassContainMember(string memberName)
        {
            return MemberVariables.ContainsKey(memberName) || MemberMethods.ContainsKey(memberName) ||
                StaticMemberVariables.ContainsKey(memberName) || StaticMemberMethods.ContainsKey(memberName);
        }

        public bool DoesClassContainMember(string memberName, bool isStatic)
        {
            if (isStatic)
            {
                return StaticMemberVariables.ContainsKey(memberName) || StaticMemberMethods.ContainsKey(memberName);
            }
            else
            {
                return MemberVariables.ContainsKey(memberName) || MemberMethods.ContainsKey(memberName);
            }
        }

        public void AddNewMember(string memberName, bool isStatic, ClepsType memberType)
        {
            Debug.Assert(!DoesClassContainMember(memberName));

            if (isStatic)
            {
                if (memberType.IsBasicType)
                {
                    StaticMemberVariables.Add(memberName, memberType);
                }
                else
                {
                    StaticMemberMethods.Add(memberName, memberType);
                }
            }
            else
            {
                if (memberType.IsBasicType)
                {
                    MemberVariables.Add(memberName, memberType);
                }
                else
                {
                    MemberMethods.Add(memberName, memberType);
                }
            }
        }

        public void AddRawLLVMTypeMapping(ClepsType rawLLVMTypeMap)
        {
            Debug.Assert(RawLLVMTypeMap == null);
            RawLLVMTypeMap = rawLLVMTypeMap;
        }
    }
}
