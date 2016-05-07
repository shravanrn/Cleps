using ClepsCompiler.CompilerStructures;
using System.Collections.Generic;
using ClepsCompiler.CompilerTypes;

namespace ClepsCompiler.CompilerBackend
{
    interface IMethodRegister
    {
        IValueRegister CreateNewVariable(ClepsVariable variable, IValue initialValue = null);
        void CreateAssignment(IValueRegister targetRegister, IValue value);
        void CreateReturnStatement(IValue value);
    }
}