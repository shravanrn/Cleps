using ClepsCompiler.CompilerTypes;
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
    class ClepsClass
    {
        public string FullyQualifiedName { get; private set; }
        // TODO Change to Read only Dictionary
        public OrderedDictionary<string, ClepsType> MemberVariables { get; private set; }
        public Dictionary<string, ClepsType> MemberMethods { get; private set; }
        public Dictionary<string, ClepsType> StaticMemberVariables { get; private set; }
        public Dictionary<string, ClepsType> StaticMemberMethods { get; private set; }

        public ClepsClass
        (
            string name,
            OrderedDictionary<string, ClepsType> memberVariables,
            Dictionary<string, ClepsType> memberMethods,
            Dictionary<string, ClepsType> staticMemberVariables,
            Dictionary<string, ClepsType> staticMemberMethods
        )
        {
            FullyQualifiedName = name;
            MemberVariables = memberVariables;
            MemberMethods = memberMethods;
            StaticMemberVariables = staticMemberVariables;
            StaticMemberMethods = staticMemberMethods;
        }

        public ClepsType GetClepsType()
        {
            return new BasicClepsType(FullyQualifiedName);
        }

        public string GetNamespace()
        {
            var pieces = FullyQualifiedName.Split('.').ToList();
            var namespacePieces = pieces.Take(pieces.Count - 1);
            return String.Join(".", namespacePieces);
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(ClepsClass) || Object.ReferenceEquals(null, obj))
            {
                return false;
            }

            ClepsClass objToCompare = obj as ClepsClass;
            return FullyQualifiedName == objToCompare.FullyQualifiedName &&
                AreDictionariesEqual(MemberVariables, objToCompare.MemberVariables) &&
                AreDictionariesEqual(MemberMethods, objToCompare.MemberMethods) &&
                AreDictionariesEqual(StaticMemberVariables, objToCompare.StaticMemberVariables) &&
                AreDictionariesEqual(StaticMemberMethods, objToCompare.StaticMemberMethods);
        }

        public override int GetHashCode()
        {
            return FullyQualifiedName.GetHashCode();
        }

        public override string ToString()
        {
            return FullyQualifiedName;
        }

        private bool AreDictionariesEqual(IDictionary<string, ClepsType> dictionary1, IDictionary<string, ClepsType> dictionary2)
        {
            return dictionary1.Keys.Count == dictionary2.Keys.Count &&
                dictionary1.Keys.All(key => dictionary2.ContainsKey(key) && Object.Equals(dictionary1[key], dictionary2[key]));
        }
    }
}