using ClepsCompiler.CompilerTypes;
using ClepsCompiler.Utils.ClassBehaviors;
using ClepsCompiler.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ClepsCompiler.CompilerStructures
{
    /// <summary>
    /// Defines the various properties of a cleps class
    /// </summary>
    class ClepsClass : EqualsAndHashCode<ClepsClass>
    {
        public string FullyQualifiedName { get; private set; }

        public OrderedDictionary<string, ClepsVariable> MemberVariables { get; private set; }
        public Dictionary<string, List<ClepsVariable>> MemberMethods { get; private set; }
        public OrderedDictionary<string, ClepsVariable> StaticMemberVariables { get; private set; }
        public Dictionary<string, List<ClepsVariable>> StaticMemberMethods { get; private set; }

        public ClepsClass
        (
            string fullyQualifiedName,
            OrderedDictionary<string, ClepsVariable> memberVariables,
            Dictionary<string, List<ClepsVariable>> memberMethods,
            OrderedDictionary<string, ClepsVariable> staticMemberVariables,
            Dictionary<string, List<ClepsVariable>> staticMemberMethods
        )
        {
            FullyQualifiedName = fullyQualifiedName;
            MemberVariables = memberVariables;
            MemberMethods = memberMethods;
            StaticMemberVariables = staticMemberVariables;
            StaticMemberMethods = staticMemberMethods;
        }

        public BasicClepsType GetClepsType()
        {
            return new BasicClepsType(FullyQualifiedName);
        }

        public string GetPrefixNamespaceAndClass()
        {
            var pieces = FullyQualifiedName.Split('.').ToList();
            var namespacePieces = pieces.Take(pieces.Count - 1);
            return String.Join(".", namespacePieces);
        }

        public override bool NotNullObjectEquals(ClepsClass obj)
        {
            return FullyQualifiedName == obj.FullyQualifiedName &&
                AreDictionariesEqual(MemberVariables, obj.MemberVariables) &&
                AreDictionariesEqual(MemberMethods, obj.MemberMethods) &&
                AreDictionariesEqual(StaticMemberVariables, obj.StaticMemberVariables) &&
                AreDictionariesEqual(StaticMemberMethods, obj.StaticMemberMethods);
        }

        public override int GetHashCode()
        {
            return FullyQualifiedName.GetHashCode();
        }

        public override string ToString()
        {
            return FullyQualifiedName;
        }

        private bool AreDictionariesEqual<T>(IDictionary<string, T> dictionary1, IDictionary<string, T> dictionary2)
        {
            return dictionary1.Keys.Count == dictionary2.Keys.Count &&
                dictionary1.Keys.All(key => dictionary2.ContainsKey(key) && Object.Equals(dictionary1[key], dictionary2[key]));
        }
    }
}