using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerStructures
{
    class ClassManager
    {
        private Dictionary<string, ClepsClassBuilder> LoadedClassBuilders;
        private Dictionary<string, ClepsClass> LoadedClasses;

        public ClassManager()
        {
            LoadedClassBuilders = new Dictionary<string, ClepsClassBuilder>();
            LoadedClasses = new Dictionary<string, ClepsClass>();
        }

        public bool IsClassBuilderAvailable(string className)
        {
            return LoadedClassBuilders.ContainsKey(className);
        }

        public void AddNewClassDeclaration(string className)
        {
            Debug.Assert(!IsClassBuilderAvailable(className));
            LoadedClassBuilders.Add(className, new ClepsClassBuilder(className));
        }

        public ClepsClassBuilder GetClassBuilder(string className)
        {
            Debug.Assert(IsClassBuilderAvailable(className));
            return LoadedClassBuilders[className];
        }

        public void SetClassDefinition(ClepsClass clepsClass)
        {
            LoadedClasses.Add(clepsClass.FullyQualifiedName, clepsClass);
        }

        public bool IsClassBodySet(string className)
        {
            return LoadedClasses.ContainsKey(className);
        }

        public ClepsClass GetClass(string className)
        {
            return LoadedClasses[className];
        }
    }
}
