using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;
using ClepsCompiler.CompilerStructures;
using ClepsCompiler.CompilerTypes;
using ClepsCompiler.CompilerBackend;
using Antlr4.Runtime;

namespace ClepsCompiler.SyntaxTreeVisitors
{
    partial class ClepsFunctionBodyGeneratorVisitor
    {
        //public override object VisitFieldOrClassAssignment([NotNull] ClepsParser.FieldOrClassAssignmentContext context)
        //{
        //    List<string> namespaceClassAndFieldHierarchy = context._ClassHierarchy.Select(h => h.GetText()).ToList();
        //    string classNameToTest = String.Join(".", namespaceClassAndFieldHierarchy);

        //    if (!ClassManager.IsClassBodySet(classNameToTest))
        //    {
        //        string errorMessage = String.Format("Could not find class {0}.", classNameToTest);
        //        Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
        //        //just return something to avoid stalling
        //        return ClassManager.GetClass(CompilerConstants.ClepsByteType.GetClepsTypeString());
        //    }

        //    return ClassManager.GetClass(classNameToTest);
        //}

            //bool classFound = false;
            //int i;

            //for (i = namespaceClassAndFieldHierarchy.Count; i >= 1; i--)
            //{
            //    string classNameToTest = String.Join(".", namespaceClassAndFieldHierarchy.Take(i).ToList());
            //    if (ClassManager.IsClassBodySet(classNameToTest))
            //    {
            //        classFound = true;
            //        break;
            //    }
            //}

            //if (!classFound)
            //{
            //    string errorMessage = String.Format("Could not find class or field {0}", String.Join(".", namespaceClassAndFieldHierarchy));
            //    Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
            //    //just return something to avoid stalling
            //    return CodeGenerator.CreateByte(0);
            //}

            //string fullClassName = String.Join(".", namespaceClassAndFieldHierarchy.Take(i).ToList());
            //ClepsClass currentClass = ClassManager.GetClass(fullClassName);
            ////BasicStaticClepsType currentType = new BasicStaticClepsType(fullClassName);
            //var fieldAccesses = namespaceClassAndFieldHierarchy.Skip(i).ToList();

            //if (fieldAccesses.Count == 0)
            //{
            //    throw new NotImplementedException("Returning static classes is not supported yet");
            //}
            //else

            //return ret;
        //}

        //private IValue GetStaticFieldOnClass(ParserRuleContext context, ClepsType targetType, string fieldName)
        //{
        //    BasicClepsType dereferencedType = GetDereferencedTypeOrNull(targetType);

        //    if(dereferencedType == null)
        //    {
        //        string errorMessage = String.Format("Could not dereference expression on type {0}", targetType.GetClepsTypeString());
        //        Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
        //        //just return something to avoid stalling
        //        return CodeGenerator.CreateByte(0);
        //    }

        //    ClepsClass targetClass = ClassManager.GetClass(dereferencedType.RawTypeName);

        //    if (targetClass.StaticMemberVariables.ContainsKey(fieldName))
        //    {
        //        IValueRegister fieldRegister = CodeGenerator.GetStaticFieldRegister(targetClass.FullyQualifiedName, fieldName, targetClass.StaticMemberVariables[fieldName].VariableType);
        //        return CodeGenerator.GetRegisterValue(fieldRegister);
        //    }
        //    else
        //    {
        //        string errorMessage = String.Format("Could not find field {0} on type {1}", fieldName, targetType.GetClepsTypeString());
        //        Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
        //        //just return something to avoid stalling
        //        return CodeGenerator.CreateByte(0);
        //    }
        //}
    }
}
