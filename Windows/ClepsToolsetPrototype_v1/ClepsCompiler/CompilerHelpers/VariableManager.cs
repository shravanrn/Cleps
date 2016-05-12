using LLVMSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.CompilerHelpers
{
    /// <summary>
    /// A class that manages all the locally defined variables inside a function of a class
    /// </summary>
    class VariableManager
    {
        private List<Dictionary<string, LLVMRegister>> LocalVariableBlocks = new List<Dictionary<string, LLVMRegister>>();

        public void AddBlock()
        {
            LocalVariableBlocks.Add(new Dictionary<string, LLVMRegister>());
        }

        public void RemoveBlock()
        {
            Debug.Assert(LocalVariableBlocks.Count > 0);
            LocalVariableBlocks.RemoveAt(LocalVariableBlocks.Count - 1);
        }

        public void AddLocalVariable(string variableName, ClepsType variableType, LLVMValueRef llvmValueRef)
        {
            Debug.Assert(LocalVariableBlocks.Count > 0);
            LocalVariableBlocks.Last().Add(variableName, new LLVMRegister(variableType, llvmValueRef));
        }

        public bool IsVariableDefined(string variableName)
        {
            return LocalVariableBlocks.Select(block => block.ContainsKey(variableName)).Any(c => c);
        }

        public LLVMRegister GetVariable(string variableName)
        {
            var localVariablesBlockWithVar = LocalVariableBlocks.Where(b => b.ContainsKey(variableName)).LastOrDefault();
            Debug.Assert(localVariablesBlockWithVar != null);
            LLVMRegister ret = localVariablesBlockWithVar[variableName];
            return ret;
        }

        public void SetLLVMRegister(string variableName, ClepsType variableType, LLVMValueRef llvmValueRef)
        {
            var localVariablesBlockWithVar = LocalVariableBlocks.Where(b => b.ContainsKey(variableName)).LastOrDefault();
            Debug.Assert(localVariablesBlockWithVar != null);
            
            LLVMRegister ret = localVariablesBlockWithVar[variableName];
            Debug.Assert(ret.VariableType == variableType);

            localVariablesBlockWithVar[variableName] = new LLVMRegister(variableType, llvmValueRef);
        }
    }
}
