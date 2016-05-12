using ClepsCompiler.CompilerStructures;
using System.Collections.Generic;
using ClepsCompiler.CompilerTypes;

namespace ClepsCompiler.CompilerBackend
{
    interface IMethodRegister
    {
        void AddNativeCode(string nativeCode);

        void SetFormalParameterNames(List<string> formalParameters);

        IValueRegister GetFormalParameterRegister(string name);
        IValueRegister CreateNewVariable(ClepsVariable variable, IValue initialValue = null);

        void CreateAssignment(IValueRegister targetRegister, IValue value);
        void CreateReturnStatement(IValue value);
        void CreateFunctionCallStatement(IValue value);

        void CreateIfStatementBlock(IValue condition);
        void CloseBlock();
    }
}