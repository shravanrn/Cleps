using LLVMSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClepsCompiler.CompilerStructures;
using ClepsCompiler.CompilerTypes;

namespace ClepsCompiler.CompilerBackend.Backends.LLVMBackend
{
    class LLVMMethod : IMethodRegister
    {
        public LLVMBasicBlockRef Block { get; private set; }

        public LLVMMethod(LLVMBasicBlockRef block)
        {
            Block = block;
        }

        public void SetMethodType(FunctionClepsType methodType)
        {
            throw new NotImplementedException();
        }

        public void SetFormalParameterNames(List<string> formalParameters)
        {
            throw new NotImplementedException();
        }

        public IValueRegister GetFormalParameterRegister(string name)
        {
            throw new NotImplementedException();
        }

        public IValueRegister CreateNewVariable(ClepsVariable variable, IValue initialValue = null)
        {
            throw new NotImplementedException();
        }

        public void CreateAssignment(IValueRegister targetRegister, IValue value)
        {
            throw new NotImplementedException();
        }

        public void CreateReturnStatement(IValue value)
        {
            throw new NotImplementedException();
        }
    }
}
