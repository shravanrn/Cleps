using ClepsCompiler.CompilerBackend;
using ClepsCompiler.CompilerStructures;
using ClepsCompiler.CompilerTypes;
using ClepsCompiler.Utils.ClassBehaviors;
using ClepsCompiler.Utils.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerCore
{
    class VariableManager
    {
        private class VariableAndRegister : EqualsAndHashCode<VariableAndRegister>
        {
            public ClepsVariable Variable;
            public IValueRegister Register;

            public override int GetHashCode()
            {
                return GetHashCodeFor(Variable, Register);
            }

            public override bool NotNullObjectEquals(VariableAndRegister obj)
            {
                return Variable == obj.Variable && Register == obj.Register;
            }
        }
        
        //Start with a default block which holds the formal parameters
        private List<Dictionary<string, VariableAndRegister>> LocalVariableBlocks = new List<Dictionary<string, VariableAndRegister>>()
        {
            new Dictionary<string, VariableAndRegister>()
        };

        public void AddBlock()
        {
            LocalVariableBlocks.Add(new Dictionary<string, VariableAndRegister>());
        }

        public void RemoveBlock()
        {
            Debug.Assert(LocalVariableBlocks.Count > 0);
            LocalVariableBlocks.RemoveAt(LocalVariableBlocks.Count - 1);
        }

        public void AddLocalVariable(ClepsVariable variable, IValueRegister variableRegister)
        {
            Debug.Assert(LocalVariableBlocks.Count > 0 && IsVariableNameAvailable(variable.VariableName));
            LocalVariableBlocks.Last().Add(variable.VariableName, new VariableAndRegister() { Variable = variable, Register = variableRegister });
        }

        public bool IsVariableNameAvailable(string variableName)
        {
            return !LocalVariableBlocks.Select(block => block.ContainsKey(variableName)).Any(c => c);
        }
        public string GetAvailableVariableName(string prefix)
        {
            return NameGenerator.GetAvailableName(prefix, LocalVariableBlocks.SelectMany(block => block.Keys).ToList());
        }

        public bool IsVariableAvailable(string variableName)
        {
            var localVariablesBlockWithVar = LocalVariableBlocks.Where(b => b.ContainsKey(variableName)).LastOrDefault();
            return localVariablesBlockWithVar != null;
        }

        public ClepsVariable GetVariable(string variableName)
        {
            var localVariablesBlockWithVar = LocalVariableBlocks.Where(b => b.ContainsKey(variableName)).LastOrDefault();
            Debug.Assert(localVariablesBlockWithVar != null);
            var ret = localVariablesBlockWithVar[variableName].Variable;
            return ret;
        }

        public IValueRegister GetVariableRegister(string variableName)
        {
            var localVariablesBlockWithVar = LocalVariableBlocks.Where(b => b.ContainsKey(variableName)).LastOrDefault();
            Debug.Assert(localVariablesBlockWithVar != null);
            var ret = localVariablesBlockWithVar[variableName].Register;
            return ret;
        }
    }
}
