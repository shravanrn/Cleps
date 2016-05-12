using Antlr4.Runtime.Misc;
using ClepsCompiler.CompilerBackend;
using ClepsCompiler.CompilerCore;
using ClepsCompiler.CompilerStructures;
using ClepsCompiler.CompilerTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.SyntaxTreeVisitors
{
    class ClepsFunctionBodyGeneratorVisitor : ClepsFunctionBodyAnalysisVisitor_Partial
    {
        private string CurrMemberName;
        private bool CurrMemberIsStatic;
        private ClepsType CurrMemberType;
        private List<VariableManager> VariableManagers = new List<VariableManager>();
        private TypeManager TypeManager;
        private FunctionOverloadManager FunctionOverloadManager;

        public ClepsFunctionBodyGeneratorVisitor(CompileStatus status, ClassManager classManager, ICodeGenerator codeGenerator, TypeManager typeManager, FunctionOverloadManager functionOverloadManager) : base(status, classManager, codeGenerator)
        {
            TypeManager = typeManager;
            FunctionOverloadManager = functionOverloadManager;
        }

        public override object VisitMemberDeclarationStatement([NotNull] ClepsParser.MemberDeclarationStatementContext context)
        {
            string memberName = context.FieldName.Name.Text;
            bool isStatic = context.STATIC() != null;
            ClepsType memberType = Visit(context.typename()) as ClepsType;
            
            var oldMemberName = CurrMemberName;
            var oldMemberIsStatic = CurrMemberIsStatic;
            var oldMemberType = CurrMemberType;

            CurrMemberName = memberName;
            CurrMemberIsStatic = isStatic;
            CurrMemberType = memberType;

            if (context.rightHandExpression() != null)
            {
                Visit(context.rightHandExpression());
            }

            CurrMemberName = oldMemberName;
            CurrMemberIsStatic = oldMemberIsStatic;
            CurrMemberType = oldMemberType;

            return true;
        }

        public override object VisitFunctionAssignment_Ex([NotNull] ClepsParser.FunctionAssignmentContext context)
        {
            VariableManager variableManager = new VariableManager();
            VariableManagers.Add(variableManager);

            ClepsType returnType = VoidType.GetVoidType();
            if(context.FunctionReturnType != null)
            {
                returnType = Visit(context.FunctionReturnType) as ClepsType;
            }

            List<ClepsType> parameterTypes = context._FunctionParameterTypes.Select(t => Visit(context.FunctionReturnType) as ClepsType).ToList();
            FunctionClepsType functionType = new FunctionClepsType(parameterTypes, returnType);

            IMethodRegister methodRegister = CodeGenerator.GetMethodRegister(FullyQualifiedClassName, CurrMemberIsStatic, CurrMemberType, CurrMemberName);
            var formalParameterNames = context._FormalParameters.Select(p => Visit(p) as string).ToList();
            methodRegister.SetFormalParameterNames(formalParameterNames);

            formalParameterNames.Zip(parameterTypes, (name, clepsType) => new ClepsVariable(name, clepsType))
                .ToList().ForEach(variable =>
                {
                    variableManager.AddLocalVariable(variable, methodRegister.GetFormalParameterRegister(variable.VariableName));
                });

            Visit(context.statementBlock());
            VariableManagers.RemoveAt(VariableManagers.Count - 1);
            return functionType;
        }

        public override object VisitStatementBlock([NotNull] ClepsParser.StatementBlockContext context)
        {
            VariableManager variableManager = VariableManagers.Last();
            variableManager.AddBlock();
            var ret = VisitChildren(context);
            variableManager.RemoveBlock();
            return ret;
        }

        public override object VisitVariable([NotNull] ClepsParser.VariableContext context)
        {
            var ret = context.VariableName.Text;
            return ret;
        }

        private IValueRegister GetVariableRegister([NotNull] ClepsParser.VariableAssignmentContext context)
        {
            string variableName = Visit(context.variable()) as string;
            var variableManager = VariableManagers.Last();
            IValueRegister register;

            if (!variableManager.IsVariableAvailable(variableName))
            {
                string errorMessage = String.Format("Variable {0} does not exist", variableName);
                Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                register = null;
            }
            else
            {
                register = variableManager.GetVariableRegister(variableName);
            }

            return register;
        }

        #region FunctionStatements
        public override object VisitFunctionVariableDeclarationStatement([NotNull] ClepsParser.FunctionVariableDeclarationStatementContext context)
        {
            IMethodRegister methodRegister = CodeGenerator.GetMethodRegister(FullyQualifiedClassName, CurrMemberIsStatic, CurrMemberType, CurrMemberName);

            ClepsType variableType = Visit(context.typename()) as ClepsType;
            string variableName = Visit(context.variable()) as string;

            VariableManager variableManager = VariableManagers.Last();
            if(!variableManager.IsVariableNameAvailable(variableName))
            {
                string errorMessage = String.Format("Variable {0} is already defined", variableName);
                Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                //Use a different variable name to avoid stopping the compilation
                variableName = variableManager.GetAvailableVariableName(variableName);
            }

            ClepsVariable variable = new ClepsVariable(variableName, variableType);
            IValue value = null;
            if (context.rightHandExpression() != null)
            {
                value = Visit(context.rightHandExpression()) as IValue;
                if (variable.VariableType != value.ExpressionType)
                {
                    throw new NotImplementedException("Assignment for non identical types not supported yet");
                }
            }

            IValueRegister variableRegister = methodRegister.CreateNewVariable(variable, value);
            variableManager.AddLocalVariable(variable, variableRegister);

            return variable;
        }

        public override object VisitFunctionVariableAssignmentStatement([NotNull] ClepsParser.FunctionVariableAssignmentStatementContext context)
        {
            IValueRegister register = GetVariableRegister(context.variableAssignment());
            if(register == null)
            {
                return false;
            }

            CreateAssignment(register, context.rightHandExpression());

            return register;
        }

        public override object VisitFunctionArrayAssignmentStatement([NotNull] ClepsParser.FunctionArrayAssignmentStatementContext context)
        {
            IValue expressionValue = Visit(context.ArrayExpression) as IValue;

            if (!expressionValue.ExpressionType.IsArrayType)
            {
                string errorMessage = String.Format("Expression {0} is not an array and so array access is not possible.", context.ArrayExpression.GetText());
                Status.AddError(new CompilerError(FileName, context.ArrayExpression.Start.Line, context.ArrayExpression.Start.Column, errorMessage));
                //return something to avoid stopping the compilation
                return expressionValue;
            }

            ArrayClepsType expressionType = (expressionValue.ExpressionType as ArrayClepsType);
            List<IValue> indexValues = ArrayIndexAccess(expressionType, context._ArrayIndexExpression);

            IValueRegister register = CodeGenerator.GetArrayElementRegister(expressionValue, indexValues);
            CreateAssignment(register, context.RightExpression);
            return register;
        }

        private void CreateAssignment(IValueRegister register, ClepsParser.RightHandExpressionContext rightHandExpression)
        {
            IValue value = Visit(rightHandExpression) as IValue;

            if (register.ExpressionType == value.ExpressionType && register.ExpressionType == CompilerConstants.ClepsByteType)
            {
                IMethodRegister methodRegister = CodeGenerator.GetMethodRegister(FullyQualifiedClassName, CurrMemberIsStatic, CurrMemberType, CurrMemberName);
                methodRegister.CreateAssignment(register, value);
            }
            else
            {
                throw new NotImplementedException("assignment for non byte types not supported yet");
            }
        }

        public override object VisitFunctionReturnStatement_Ex([NotNull] ClepsParser.FunctionReturnStatementContext context)
        {
            IValue returnValue = null;
            if (context.rightHandExpression() != null)
            {
                returnValue = Visit(context.rightHandExpression()) as IValue;
            }

            var currFunctionType = CurrMemberType as FunctionClepsType;
            
            if(currFunctionType.ReturnType != returnValue.ExpressionType)
            {
                string errorMessage = String.Format("Expected return of {0}. Returning type {1} instead.", currFunctionType.ReturnType.GetClepsTypeString(), returnValue.ExpressionType.GetClepsTypeString());
                Status.AddError(new CompilerError(FileName, context.rightHandExpression().Start.Line, context.rightHandExpression().Start.Column, errorMessage));
            }

            IMethodRegister methodRegister = CodeGenerator.GetMethodRegister(FullyQualifiedClassName, CurrMemberIsStatic, CurrMemberType, CurrMemberName);
            methodRegister.CreateReturnStatement(returnValue);

            return returnValue;
        }

        public override object VisitIfStatement([NotNull] ClepsParser.IfStatementContext context)
        {
            IValue conditionValue = Visit(context.rightHandExpression()) as IValue;
            IMethodRegister methodRegister = CodeGenerator.GetMethodRegister(FullyQualifiedClassName, CurrMemberIsStatic, CurrMemberType, CurrMemberName);
            methodRegister.CreateIfStatementBlock(conditionValue);

            Visit(context.statementBlock());

            methodRegister.CloseBlock();

            return conditionValue;
        }

        public override object VisitNativeStatement([NotNull] ClepsParser.NativeStatementContext context)
        {
            IMethodRegister methodRegister = CodeGenerator.GetMethodRegister(FullyQualifiedClassName, CurrMemberIsStatic, CurrMemberType, CurrMemberName);
            var startIndex = context.NativeOpen.StopIndex + 1;
            var endIndex = context.NativeClose.StartIndex - 1;
            var length = endIndex - startIndex + 1;
            var nativeCode = System.IO.File.ReadAllText(FileName).Substring(startIndex, length) + "\n";

            methodRegister.AddNativeCode(nativeCode);
            return true;
        }

        public override object VisitFunctionCallStatement([NotNull] ClepsParser.FunctionCallStatementContext context)
        {
            IValue expressionValue = Visit(context.rightHandExpression()) as IValue;
            bool isMembersAccessible = !expressionValue.ExpressionType.IsStaticType;

            IValue ret = doFunctionCall(context.functionCall(), expressionValue.ExpressionType, isMembersAccessible, true /* allowVoidReturn */);
            IMethodRegister methodRegister = CodeGenerator.GetMethodRegister(FullyQualifiedClassName, CurrMemberIsStatic, CurrMemberType, CurrMemberName);
            methodRegister.CreateFunctionCallStatement(ret);

            return ret;
        }

        #endregion FunctionStatements

        #region Expression

        public override object VisitArrayAccessOnExpression([NotNull] ClepsParser.ArrayAccessOnExpressionContext context)
        {
            IValue expressionValue = Visit(context.ArrayExpression) as IValue;

            if (!expressionValue.ExpressionType.IsArrayType)
            {
                string errorMessage = String.Format("Expression {0} is not an array and so array access is not possible.", context.ArrayExpression.GetText());
                Status.AddError(new CompilerError(FileName, context.ArrayExpression.Start.Line, context.ArrayExpression.Start.Column, errorMessage));
                //return something to avoid stopping the compilation
                return expressionValue;
            }

            ArrayClepsType expressionType = (expressionValue.ExpressionType as ArrayClepsType);
            List<IValue> indexValues = ArrayIndexAccess(expressionType, context._ArrayIndexExpression);

            IValueRegister register = CodeGenerator.GetArrayElementRegister(expressionValue, indexValues);
            IValue retValue = CodeGenerator.GetRegisterValue(register);
            return retValue;
        }

        private List<IValue> ArrayIndexAccess(ArrayClepsType expressionType, IList<ClepsParser.RightHandExpressionContext> arrayIndexExpression)
        {
            var indexValues = arrayIndexExpression.Select(i => {
                var indexValueRet = Visit(i);
                IValue indexValue = indexValueRet as IValue;

                if(indexValue.ExpressionType != CompilerConstants.ClepsByteType)
                {
                    string errorMessage = String.Format("Array access {0} is not a byte type", i.GetText());
                    Status.AddError(new CompilerError(FileName, i.Start.Line, i.Start.Column, errorMessage));
                    //just use the first element access to avoid stalling
                    indexValue = CodeGenerator.CreateByte(0);
                }

                return indexValue;
            }).ToList();

            if (indexValues.Count > expressionType.Dimensions.Length)
            {
                string errorMessage = String.Format("Accessing more dimensions than array has. Array has {0} dimensions, but accessing {1} dimensions", expressionType.Dimensions.Length, indexValues.Count);
                Status.AddError(new CompilerError(FileName, arrayIndexExpression[indexValues.Count - 1].Start.Line, arrayIndexExpression[indexValues.Count - 1].Start.Column, errorMessage));
                //truncate array access to allowed access to avoid stopping the compilation
                indexValues = indexValues.Take(expressionType.Dimensions.Length).ToList();
            }

            return indexValues;
        }

        public override object VisitBinaryOperatorOnExpression([NotNull] ClepsParser.BinaryOperatorOnExpressionContext context)
        {
            IValue leftValue = Visit(context.LeftExpression) as IValue;
            IValue rightValue = Visit(context.RightExpression) as IValue;

            if (context.operatorSymbol().GetText() != "==" || leftValue.ExpressionType != CompilerConstants.ClepsByteType || rightValue.ExpressionType != CompilerConstants.ClepsByteType)
            {
                throw new NotImplementedException("This kind of binary operation on these types is not yet implemented");
            }

            return CodeGenerator.GetAreByteValuesEqual(leftValue, rightValue);
        }

        public override object VisitFunctionCallOnExpression([NotNull] ClepsParser.FunctionCallOnExpressionContext context)
        {
            IValue expressionValue = Visit(context.rightHandExpression()) as IValue;
            bool isMembersAccessible = !expressionValue.ExpressionType.IsStaticType;

            return doFunctionCall(context.functionCall(), expressionValue.ExpressionType, isMembersAccessible, false /* allowVoidReturn */);
        }

        private IValue doFunctionCall(ClepsParser.FunctionCallContext functionCall, ClepsType targetType, bool isMemberFunctionsAccessible, bool allowVoidReturn)
        {
            ClepsClass targetClepsClass;
            
            if(targetType is BasicClepsType)
            {
                targetClepsClass = ClassManager.GetClass(targetType.GetClepsTypeString());
            }
            else if(targetType is BasicStaticClepsType)
            {
                targetClepsClass = ClassManager.GetClass(targetType.GetClepsTypeString());
                isMemberFunctionsAccessible = false;
            }
            else
            {
                throw new NotImplementedException("Function calls on non basic types not supported");
            }

            string targetFunctionName = functionCall.FunctionName.GetText();
            List<IValue> parameters = functionCall._FunctionParameters.Select(p => Visit(p) as IValue).ToList();

            ClepsType functionType = null;
            if (targetClepsClass.StaticMemberMethods.ContainsKey(targetFunctionName))
            {
                functionType = targetClepsClass.StaticMemberMethods[targetFunctionName];
            }
            else if (isMemberFunctionsAccessible && targetClepsClass.MemberMethods.ContainsKey(targetFunctionName))
            {
                functionType = targetClepsClass.MemberMethods[targetFunctionName];
            }

            if (functionType == null)
            {
                string errorMessage = String.Format("Class {0} does not contain a function called {1}", targetClepsClass.FullyQualifiedName, targetFunctionName);
                Status.AddError(new CompilerError(FileName, functionCall.Start.Line, functionCall.Start.Column, errorMessage));
                //Just return something to avoid stalling compilation
                return CodeGenerator.CreateByte(0);
            }

            List<ClepsType> functionOverloads = new List<ClepsType> { functionType };

            int matchedPosition;
            string fnMatchErrorMessage;

            if (!FunctionOverloadManager.FindMatchingFunctionType(functionOverloads, parameters, out matchedPosition, out fnMatchErrorMessage))
            {
                Status.AddError(new CompilerError(FileName, functionCall.Start.Line, functionCall.Start.Column, fnMatchErrorMessage));
                //Just return something to avoid stalling compilation
                return CodeGenerator.CreateByte(0);
            }

            FunctionClepsType chosenFunctionType = functionOverloads[matchedPosition] as FunctionClepsType;

            if (!allowVoidReturn && chosenFunctionType.ReturnType == VoidType.GetVoidType())
            {
                string errorMessage = String.Format("Function {0} does not return a value", targetFunctionName);
                Status.AddError(new CompilerError(FileName, functionCall.Start.Line, functionCall.Start.Column, errorMessage));
                //Just return something to avoid stalling compilation
                return CodeGenerator.CreateByte(0);
            }

            IValue returnValue = CodeGenerator.GetFunctionCallReturnValue(FullyQualifiedClassName, targetFunctionName, chosenFunctionType, parameters);
            return returnValue;
        }

        #endregion Expression

        #region RegisterAssignments

        public override object VisitFieldOrClassAssignment([NotNull] ClepsParser.FieldOrClassAssignmentContext context)
        {
            List<string> namespaceClassAndFieldHierarchy = context._ClassHierarchy.Select(h => h.GetText()).ToList();
            bool classFound = false;
            int i;

            for (i = 1; i <= namespaceClassAndFieldHierarchy.Count; i++)
            {
                string classNameToTest = String.Join(".", namespaceClassAndFieldHierarchy.Take(i).ToList());
                if (ClassManager.IsClassBodySet(classNameToTest))
                {
                    classFound = true;
                    break;
                }
            }

            if(!classFound)
            {
                string errorMessage = String.Format("Could not find class or field {0}", String.Join(".", namespaceClassAndFieldHierarchy));
                Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                //just return something to avoid stalling
                return CodeGenerator.CreateByte(0);
            }

            string fullClassName = String.Join(".", namespaceClassAndFieldHierarchy.Take(i).ToList());
            ClepsClass currentClass = ClassManager.GetClass(fullClassName);
            BasicStaticClepsType currentType = new BasicStaticClepsType(fullClassName);
            var fieldAccesses = namespaceClassAndFieldHierarchy.Skip(i).ToList();

            if(fieldAccesses.Count != 0)
            {
                throw new NotImplementedException("Getting fields on static classes is not supported yet");
            }

            IValue ret = CodeGenerator.GetClassStaticInstance(currentType);
            return ret;
        }

        public override object VisitClassInstanceAssignment([NotNull] ClepsParser.ClassInstanceAssignmentContext context)
        {
            ClepsType typeName = Visit(context.typename()) as ClepsType;
            List<IValue> functionParams = context._FunctionParameters.Select(p => Visit(p) as IValue).ToList();

            IValue instance;
            if (
                (typeName == CompilerConstants.ClepsByteType || typeName == CompilerConstants.ClepsBoolType) &&
                (functionParams.Count == 1 && functionParams[0].ExpressionType == typeName)
            )
            {
                instance = functionParams[0];
            }
            else
            {
                instance = CodeGenerator.CreateClassInstance(typeName as BasicClepsType, functionParams);
            }

            return instance;
        }

        public override object VisitFunctionCallAssignment([NotNull] ClepsParser.FunctionCallAssignmentContext context)
        {
            if(!ClassManager.IsClassBodySet(FullyQualifiedClassName))
            {
                //This is probably due to some earlier error. Just return something to avoid stalling compilation
                return CodeGenerator.CreateByte(0);
            }

            ClepsType currentType = new BasicClepsType(FullyQualifiedClassName);
            return doFunctionCall(context.functionCall(), currentType, !CurrMemberIsStatic, false /* allowVoidReturn */);
        }

        public override object VisitArrayAssignment([NotNull] ClepsParser.ArrayAssignmentContext context)
        {
            var arrayElements = context._ArrayElements.Select(a => Visit(a) as IValue).ToList();
            var elementTypes = arrayElements.Select(a => a.ExpressionType).ToList();
            ClepsType arrayElementType = TypeManager.GetSuperType(elementTypes);
            ClepsType arrayType = new ArrayClepsType(arrayElementType, new long[] { arrayElements.Count });

            IValue arr = CodeGenerator.CreateArray(arrayType, arrayElements);
            return arr;
        }

        public override object VisitVariableAssignment([NotNull] ClepsParser.VariableAssignmentContext context)
        {
            IValueRegister register = GetVariableRegister(context);
            if (register == null)
            {
                return false;
            }

            IValue variableValue = CodeGenerator.GetRegisterValue(register);
            return variableValue;
        }

        public override object VisitNumericAssignments([NotNull] ClepsParser.NumericAssignmentsContext context)
        {
            IValue val;
            string numericValue = context.numeric().NumericValue.Text;
            string numericType = context.numeric().NumericType?.Text;

            if(numericType == "b")
            {
                byte byteVal;
                if(!byte.TryParse(numericValue, out byteVal))
                {
                    string errorMessage = String.Format("Value {0} is outside the range of values allowed for a byte. Allowed range is 0 to 255", numericValue);
                    Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                    //Use a different value to avoid stopping the compilation
                    byteVal = 0;
                }

                val = CodeGenerator.CreateByte(byteVal);
            }
            else
            {
                throw new NotImplementedException("Other numeric types not supported yet");
            }

            return val;
        }

        public override object VisitBooleanAssignments([NotNull] ClepsParser.BooleanAssignmentsContext context)
        {
            return CodeGenerator.CreateBoolean(context.TRUE() != null);
        }

        public override object VisitPlatformAssignment([NotNull] ClepsParser.PlatformAssignmentContext context)
        {
            IValue platformId = CodeGenerator.CreateByte(CodeGenerator.GetPlatform());
            return platformId;
        }

        #endregion RegisterAssignments

    }
}
