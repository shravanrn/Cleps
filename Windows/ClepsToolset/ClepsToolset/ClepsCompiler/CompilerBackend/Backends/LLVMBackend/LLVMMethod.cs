using LLVMSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClepsCompiler.CompilerStructures;

namespace ClepsCompiler.CompilerBackend.Backends.LLVMBackend
{
    class LLVMMethod : IMethodRegister
    {
        public LLVMBasicBlockRef Block { get; private set; }

        public LLVMMethod(LLVMBasicBlockRef block)
        {
            Block = block;
        }

        public IValueRegister CreateNewVariable(string suggestedName)
        {
            throw new NotImplementedException();
        }

        public IValueRegister CreateNewVariable(ClepsVariable variable, IValue initialValue = null)
        {
            throw new NotImplementedException();
        }

        public void CreateAssignment(IValueRegister register, IValueRegister value)
        {
            throw new NotImplementedException();
        }

        public void CreateAssignment(IValueRegister register, IValue value)
        {
            throw new NotImplementedException();
        }

        public void CreateReturnStatement(IValue value)
        {
            throw new NotImplementedException();
        }
    }
}
