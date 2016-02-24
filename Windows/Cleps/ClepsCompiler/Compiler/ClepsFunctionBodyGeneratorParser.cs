using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using ClepsCompiler.CompilerHelpers;
using LLVMSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.Compiler
{
    /// <summary>
    /// This parser is used to generate LLVM bytecode for function and method bodies
    /// </summary>
    class ClepsFunctionBodyGeneratorParser : ClepsAbstractParser<LLVMRegister>
    {
        private ClassManager ClassManager;
        private CompileStatus Status;
        private LLVMContextRef Context;
        private LLVMModuleRef Module;
        private LLVMBuilderRef Builder;
        private ClepsLLVMTypeConvertor ClepsLLVMTypeConvertorInst;

        private List<string> CurrentNamespaceAndClass;
        private List<string> FunctionHierarchy;
        private VariableManager VariableManager;

        public ClepsFunctionBodyGeneratorParser(ClassManager classManager, CompileStatus status, LLVMContextRef context, LLVMModuleRef module, LLVMBuilderRef builder, ClepsLLVMTypeConvertor clepsLLVMTypeConvertor)
        {
            ClassManager = classManager;
            Status = status;
            Context = context;
            Module = module;
            Builder = builder;
            ClepsLLVMTypeConvertorInst = clepsLLVMTypeConvertor;
        }

        public override LLVMRegister VisitCompilationUnit([NotNull] ClepsParser.CompilationUnitContext context)
        {
            CurrentNamespaceAndClass = new List<string>();
            FunctionHierarchy = new List<string>();
            VariableManager = new VariableManager();

            var ret = VisitChildren(context);
            return ret;
        }

        public override LLVMRegister VisitNamespaceBlockStatement([NotNull] ClepsParser.NamespaceBlockStatementContext context)
        {
            CurrentNamespaceAndClass.Add(context.NamespaceName.GetText());
            var ret = VisitChildren(context);
            CurrentNamespaceAndClass.RemoveAt(CurrentNamespaceAndClass.Count - 1);
            return ret;
        }

        public override LLVMRegister VisitClassDeclarationStatements([NotNull] ClepsParser.ClassDeclarationStatementsContext context)
        {
            if (context.ClassName == null)
            {
                //Some antlr parsing exception has occurred. Just exit.
                return null;
            }

            CurrentNamespaceAndClass.Add(context.ClassName.GetText());

            string className = String.Join(".", CurrentNamespaceAndClass);
            ClepsClass classDetails;
            if (!ClassManager.LoadedClassesAndMembers.TryGetValue(className, out classDetails))
            {
                //if the class was not found in the loaded class stage, then this is probably due to an earlier parsing error, just stop processing this class
                return null;
            }

            var ret = VisitChildren(context);
            CurrentNamespaceAndClass.RemoveAt(CurrentNamespaceAndClass.Count - 1);
            return ret;
        }

        public override LLVMRegister VisitMemberFunctionDeclarationStatement([NotNull] ClepsParser.MemberFunctionDeclarationStatementContext context)
        {
            bool isStatic = context.STATIC() != null;
            ClepsParser.TypenameAndVoidContext returnTypeContext = context.functionDeclarationStatement().FunctionReturnType;
            ClepsType clepsReturnType = ClepsType.GetBasicOrVoidType(returnTypeContext);
            ClepsParser.FunctionParametersListContext parametersContext = context.functionDeclarationStatement().functionParametersList();
            string functionName = context.functionDeclarationStatement().FunctionName.GetText();
            return VisitFunctionDeclarationBody(context, clepsReturnType, parametersContext, functionName, isStatic);
        }

        public override LLVMRegister VisitMemberAssignmentFunctionDeclarationStatement([NotNull] ClepsParser.MemberAssignmentFunctionDeclarationStatementContext context)
        {
            bool isStatic = context.STATIC() != null;
            ClepsParser.FunctionParametersListContext parametersContext = context.assignmentFunctionDeclarationStatement().functionParametersList();
            string functionName = context.assignmentFunctionDeclarationStatement().FunctionName.Text;
            return VisitFunctionDeclarationBody(context, ClepsType.GetVoidType(), parametersContext, functionName, isStatic);
        }

        public override LLVMRegister VisitMemberOperatorFunctionDeclarationStatement([NotNull] ClepsParser.MemberOperatorFunctionDeclarationStatementContext context)
        {
            bool isStatic = context.STATIC() != null;
            ClepsParser.TypenameContext returnTypeContext = context.operatorFunctionDeclarationStatment().FunctionReturnType;
            ClepsType clepsReturnType = ClepsType.GetBasicType(returnTypeContext);
            ClepsParser.FunctionParametersListContext parametersContext = context.operatorFunctionDeclarationStatment().functionParametersList();
            string functionName = context.operatorFunctionDeclarationStatment().FunctionName.GetText();
            return VisitFunctionDeclarationBody(context, clepsReturnType, parametersContext, functionName, isStatic);
        }

        private LLVMRegister VisitFunctionDeclarationBody
        (
            Antlr4.Runtime.ParserRuleContext context,
            ClepsType clepsReturnType,
            ClepsParser.FunctionParametersListContext parametersContext,
            string functionName,
            bool isStatic
        )
        {
            string className = String.Join(".", CurrentNamespaceAndClass);

            if (!ClassManager.DoesClassContainMember(className, functionName, isStatic))
            {
                //if the member was not found in the loaded member stage, then this is probably due to an earlier parsing error, just stop processing this member
                return null;
            }

            FunctionHierarchy.Add(functionName);
            string fullyQualifiedName = String.Join(".", CurrentNamespaceAndClass.Union(FunctionHierarchy).ToList());

            LLVMValueRef currFunc = LLVM.GetNamedFunction(Module, fullyQualifiedName);
            LLVMBasicBlockRef basicBlock = LLVM.GetFirstBasicBlock(currFunc);
            LLVM.PositionBuilderAtEnd(Builder, basicBlock);

            VariableManager.AddBlock();

            List<string> paramNames = parametersContext._FunctionParameterNames.Select(p => p.VariableName.Text).ToList();
            List<ClepsType> clepsParameterTypes = parametersContext._FunctionParameterTypes.Select(t => ClepsType.GetBasicType(t)).ToList();

            if (!isStatic)
            {
                ClepsType thisClassType = ClepsType.GetBasicType(className, new List<uint>() /* array dims */, 1);
                paramNames.Insert(0, "this");
                clepsParameterTypes.Insert(0, thisClassType);
            }

            List<LLVMValueRef> paramValueRegisters = currFunc.GetParams().ToList();

            paramNames.Zip(clepsParameterTypes, (ParamName, ParamType) => new { ParamName, ParamType })
                .Zip(paramValueRegisters, (ParamNameAndType, ParamRegister) => new { ParamNameAndType.ParamName, ParamNameAndType.ParamType, ParamRegister })
                .ToList()
                .ForEach(p => {
                    LLVMValueRef functionParamPtr = LLVM.BuildAlloca(Builder, LLVM.TypeOf(p.ParamRegister), p.ParamName);
                    LLVM.BuildStore(Builder, p.ParamRegister, functionParamPtr);
                    VariableManager.AddLocalVariable(p.ParamName, p.ParamType, functionParamPtr);
                });

            var ret = VisitChildren(context);

            VariableManager.RemoveBlock();
            FunctionHierarchy.RemoveAt(FunctionHierarchy.Count - 1);
            return ret;
        }

        public override LLVMRegister VisitStatementBlock([NotNull] ClepsParser.StatementBlockContext context)
        {
            VariableManager.AddBlock();
            var ret = VisitChildren(context);
            VariableManager.RemoveBlock();

            return ret;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////// Function Statement Implementations ///////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Function Statement Implementations

        // Note - the return of statements are not really used. It is more used in the expression returns, which is in the next section

        public override LLVMRegister VisitFunctionReturnStatement([NotNull] ClepsParser.FunctionReturnStatementContext context)
        {
            LLVMValueRef returnValueRegister;
            ClepsType returnType;

            if (context.rightHandExpression() == null)
            {
                returnValueRegister = LLVM.BuildRetVoid(Builder);
                returnType = ClepsType.GetVoidType();
            }
            else
            {
                var returnValuePtr = Visit(context.rightHandExpression());
                returnValueRegister = LLVM.BuildLoad(Builder, returnValuePtr.LLVMPtrValueRef, "returnValue");
                LLVM.BuildRet(Builder, returnValueRegister);
                returnType = returnValuePtr.VariableType;
            }

            var ret = new LLVMRegister(returnType, returnValueRegister);
            return ret;
        }

        public override LLVMRegister VisitFunctionVariableDeclarationStatement([NotNull] ClepsParser.FunctionVariableDeclarationStatementContext context)
        {
            ClepsParser.VariableDeclarationStatementContext variableDeclarationStatement = context.variableDeclarationStatement();
            string variableName = variableDeclarationStatement.variable().VariableName.Text;

            if (VariableManager.IsVariableDefined(variableName))
            {
                string errorMessage = String.Format("Variable {0} is already defined", variableName);
                Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                return null;
            }

            ClepsType clepsVariableType = ClepsType.GetBasicType(variableDeclarationStatement.typename());
            LLVMTypeRef? primitiveTypeOrNull = ClepsLLVMTypeConvertorInst.GetPrimitiveLLVMTypeOrNull(clepsVariableType);
            LLVMValueRef variable;

            if (primitiveTypeOrNull != null)
            {
                variable = LLVM.BuildAlloca(Builder, primitiveTypeOrNull.Value, variableName + "Ptr");
            }
            else if (clepsVariableType.IsPointerType || clepsVariableType.IsArrayType)
            {
                LLVMTypeRef? pointerType = ClepsLLVMTypeConvertorInst.GetLLVMTypeOrNull(clepsVariableType);
                if (pointerType == null)
                {
                    string errorMessage = String.Format("Could not find type {0}", clepsVariableType.GetTypeName());
                    Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                    return null;
                }

                variable = LLVM.BuildAlloca(Builder, pointerType.Value, clepsVariableType.IsPointerType? (variableName + "Ptr") : (variableName + "Arr"));
            }
            else
            {
                LLVMValueRef? constructorReturn = CallConstructorAllocaForType(context, clepsVariableType, variableName);
                if (constructorReturn == null)
                {
                    return null;
                }

                variable = constructorReturn.Value;
            }

            VariableManager.AddLocalVariable(variableName, clepsVariableType, variable);
            LLVMRegister ret = new LLVMRegister(clepsVariableType, variable);
            return ret;
        }

        private LLVMValueRef? CallConstructorAllocaForType(ParserRuleContext context, ClepsType clepsVariableType, string variableName)
        {
            string constructorToCall = String.Format("{0}.new", clepsVariableType.RawTypeName);
            if (!ClassManager.IsClassLoaded(clepsVariableType.RawTypeName))
            {
                string errorMessage = String.Format("Could not find type {0}", clepsVariableType.GetTypeName());
                Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                return null;
            }

            LLVMValueRef llvmConstructor = LLVM.GetNamedFunction(Module, constructorToCall);
            LLVMValueRef variable = LLVM.BuildCall(Builder, llvmConstructor, new LLVMValueRef[0], variableName);
            LLVMValueRef variablePtr = LLVM.BuildAlloca(Builder, LLVM.TypeOf(variable), variableName + "Ptr");
            LLVM.BuildStore(Builder, variable, variablePtr);
            return variablePtr;
        }

        public override LLVMRegister VisitIfStatement([NotNull] ClepsParser.IfStatementContext context)
        {
            ClepsParser.RightHandExpressionContext condition = context.rightHandExpression();
            LLVMRegister expressionValue = Visit(condition);

            ClepsType nativeBooleanType = ClepsType.GetBasicType("System.LLVMTypes.I1", new List<uint>() /* array dims */, 0 /* ptr indirection level */);
            LLVMValueRef? conditionRegisterPtr = null;

            //handle native llvm boolean type
            if (expressionValue.VariableType == nativeBooleanType)
            {
                conditionRegisterPtr = expressionValue.LLVMPtrValueRef;
            }
            //handle cleps llvm boolean type
            else if (ClassManager.RawLLVMTypeMappingClasses.ContainsKey(nativeBooleanType))
            {
                ClepsClass mappedBooleanClass = ClassManager.RawLLVMTypeMappingClasses[nativeBooleanType];
                ClepsType mappedBooleanType = ClepsType.GetBasicType(mappedBooleanClass.FullyQualifiedName, new List<uint>() /* array dims */, 0 /* ptr indirection level */);

                if (expressionValue.VariableType == mappedBooleanType)
                {
                    //if the mapped type exists, then below can never be null, so call value automatically
                    LLVMTypeRef mappedBooleanTypeInLLVM = ClepsLLVMTypeConvertorInst.GetLLVMTypeOrNull(mappedBooleanType).Value;
                    //get the first field in the mapped type - see rawtypemap for more details
                    conditionRegisterPtr = LLVM.BuildStructGEP(Builder, expressionValue.LLVMPtrValueRef, 0, "ifCondBooleanFieldPtr");
                }
            }

            if (conditionRegisterPtr == null)
            {
                string errorMessage = String.Format("The condition expression in the if condition returns type {0} instead of a boolean expression. ", expressionValue.VariableType.GetTypeName());
                Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                //just assume this is condition is true to avoid stalling the compilation
                conditionRegisterPtr = LLVM.ConstInt(LLVM.Int1TypeInContext(Context), (ulong)1, false);
            }

            LLVMValueRef conditionRegister = LLVM.BuildLoad(Builder, conditionRegisterPtr.Value, "ifCondBooleanField");
            LLVMValueRef currentFunction = LLVM.GetInsertBlock(Builder).GetBasicBlockParent();
            LLVMBasicBlockRef ifThenBlock = LLVM.AppendBasicBlockInContext(Context, currentFunction, "ifthen");
            LLVMBasicBlockRef ifEndBlock = LLVM.AppendBasicBlockInContext(Context, currentFunction, "ifend");

            LLVM.BuildCondBr(Builder, conditionRegister, ifThenBlock, ifEndBlock);
            LLVM.PositionBuilderAtEnd(Builder, ifThenBlock);
            Visit(context.statementBlock());
            LLVM.BuildBr(Builder, ifEndBlock);
            LLVM.PositionBuilderAtEnd(Builder, ifEndBlock);

            return expressionValue;
        }

        public override LLVMRegister VisitFunctionCallStatement([NotNull] ClepsParser.FunctionCallStatementContext context)
        {
            string className = String.Join(".", CurrentNamespaceAndClass);
            string fullFunctionName = String.Join(".", FunctionHierarchy);
            bool isStatic = ClassManager.DoesClassContainMember(className, fullFunctionName, true /* search for static members */);

            string functionBeingCalled = context.functionCall().FunctionName.GetText();
            string fullyQualifiedNameOfFunctionBeingCalled;
            ClepsType functionType;
            List<LLVMValueRef> parameterPtrs;

            if(context.functionCall()._FunctionParameters.Count > 0)
            {
                throw new NotImplementedException("Passing parameters to functions is not yet supported");
            }

            if (ClassManager.DoesClassContainMember(className, functionBeingCalled, true /* check static members */))
            {
                fullyQualifiedNameOfFunctionBeingCalled = String.Format("{0}.{1}", className, functionBeingCalled);
                functionType = ClassManager.LoadedClassesAndMembers[className].StaticMemberMethods[functionBeingCalled];
                parameterPtrs = new List<LLVMValueRef>();
            }
            else if(!isStatic && ClassManager.DoesClassContainMember(className, functionBeingCalled, false /* check non static members */))
            {
                fullyQualifiedNameOfFunctionBeingCalled = String.Format("{0}.{1}", className, functionBeingCalled);
                functionType = ClassManager.LoadedClassesAndMembers[className].MemberMethods[functionBeingCalled];
                parameterPtrs = new List<LLVMValueRef>() { VariableManager.GetVariable("this").LLVMPtrValueRef };
            }
            else
            {
                string errorMessage = String.Format("The {0}function being called {1} was not found in class {2}", isStatic? "static" : String.Empty, functionBeingCalled, className);
                Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                //just assume this function returns a int to avoid stalling the compilation
                return GetConstantIntRegisterOfClepsType(context, LLVM.Int32TypeInContext(Context), 0, "int");
            }

            LLVMValueRef[] parameters = parameterPtrs.Select(pp => LLVM.BuildLoad(Builder, pp, "functionParam")).ToArray();
            LLVMValueRef llvmFunctionBeingCalled = LLVM.GetNamedFunction(Module, fullyQualifiedNameOfFunctionBeingCalled);
            LLVMValueRef retValue = LLVM.BuildCall(Builder, llvmFunctionBeingCalled, parameters, "retValue");
            LLVMValueRef retValuePtr = LLVM.BuildAlloca(Builder, LLVM.TypeOf(retValue), "retValuePtr");
            LLVM.BuildStore(Builder, retValue, retValuePtr);

            LLVMRegister ret = new LLVMRegister(functionType.FunctionReturnType, retValuePtr);
            return ret;
        }

        public override LLVMRegister VisitFunctionVariableAssigmentStatement([NotNull] ClepsParser.FunctionVariableAssigmentStatementContext context)
        {
            string variableName = context.variable().VariableName.Text;

            if (!VariableManager.IsVariableDefined(variableName))
            {
                string errorMessage = String.Format("The variable {0} was not defined", variableName);
                Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                return null;
            }

            LLVMRegister variablePtrRegister = VariableManager.GetVariable(variableName);
            LLVMRegister assignmentValuePtrRegister = Visit(context.rightHandExpression());
            string assignmentOperator = context.ASSIGNMENT_OPERATOR().GetText();
            return GenerateAssignmentToRegister(context, variablePtrRegister, assignmentValuePtrRegister, assignmentOperator);
        }

        private LLVMRegister GenerateAssignmentToRegister(ParserRuleContext context, LLVMRegister targetPtrRegister, LLVMRegister assignmentValuePtrRegister, string assignmentOperator)
        {            
            string targetClassName = targetPtrRegister.VariableType.RawTypeName;
            LLVMValueRef assignmentValue = LLVM.BuildLoad(Builder, assignmentValuePtrRegister.LLVMPtrValueRef, "assignmentValue");

            if (ClepsLLVMTypeConvertorInst.IsPrimitiveLLVMType(targetPtrRegister.VariableType))
            {
                //if it is a primitive llvmType, the assignment operators are hardcoded
                //only the = assignment operator is supported for primitive llvm types

                if(assignmentOperator != "=")
                {
                    string errorMessage = String.Format("Primitive class {0} does not have assignment operator {1} defined. = is the only assignment operator allowed", targetClassName, assignmentOperator);
                    Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                    return null;
                }

                if(targetPtrRegister.VariableType != assignmentValuePtrRegister.VariableType)
                {
                    string errorMessage = String.Format("Value of type {0} cannot be assigned to variable of type {1}", assignmentValuePtrRegister.VariableType, targetPtrRegister.VariableType);
                    Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                    return null;
                }

                LLVM.BuildStore(Builder, assignmentValue, targetPtrRegister.LLVMPtrValueRef);
                return null;
            }
            else
            {
                ClepsClass variableClass = ClassManager.LoadedClassesAndMembers[targetClassName];
                if (!variableClass.MemberMethods.ContainsKey(assignmentOperator))
                {
                    string errorMessage = String.Format("The class {0} does not have assignment operator {1} defined", targetClassName, assignmentOperator);
                    Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                    return null;
                }

                ClepsType assignmentFunctionToCall = variableClass.MemberMethods[assignmentOperator];
                if (assignmentFunctionToCall.FunctionParameters.Count != 2 && assignmentFunctionToCall.FunctionParameters[1] != assignmentValuePtrRegister.VariableType)
                {
                    string errorMessage = String.Format("The class {0} does not have an assignment operator {1} defined that takes parameter {2}", targetClassName, assignmentOperator, assignmentValuePtrRegister.VariableType);
                    Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                    return null;
                }

                string assignmentFunctionNameToCall = String.Format("{0}.{1}", targetClassName, assignmentOperator);
                LLVMValueRef llvmAssignmentFunction = LLVM.GetNamedFunction(Module, assignmentFunctionNameToCall);
                LLVMValueRef[] parameters = new LLVMValueRef[] { targetPtrRegister.LLVMPtrValueRef, assignmentValue };
                LLVM.BuildCall(Builder, llvmAssignmentFunction, parameters, "");
                return null;
            }
        }

        public override LLVMRegister VisitFunctionFieldAssignmentStatement([NotNull] ClepsParser.FunctionFieldAssignmentStatementContext context)
        {
            LLVMValueRef? instance = null;
            string className;
            string memberName = context.FieldName.Name.Text;

            if (context.LeftExpression != null)
            {
                //field access on an expression
                LLVMRegister leftExpressionRegister = Visit(context.LeftExpression);
                className = leftExpressionRegister.VariableType.RawTypeName;
                instance = leftExpressionRegister.LLVMPtrValueRef;
            }
            else
            {
                //field access on the same class of this member function
                className = String.Join(".", CurrentNamespaceAndClass);

                if (VariableManager.IsVariableDefined("this"))
                {
                    LLVMRegister instancePtr = VariableManager.GetVariable("this");
                    instance = LLVM.BuildLoad(Builder, instancePtr.LLVMPtrValueRef, "this");
                }
            }

            if (!ClassManager.DoesClassContainMember(className, memberName))
            {
                string errorMessage = String.Format("The class {0} does not have a member named {1}", className, memberName);
                Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                return null;
            }

            //some additional checks for field access on the same class of this member function
            if (context.LeftExpression == null)
            {
                string fullFunctionName = String.Join(".", FunctionHierarchy);
                bool iscurrFuncStatic = ClassManager.DoesClassContainMember(className, fullFunctionName, true /* search for static members */);
                bool isMemberStatic = ClassManager.DoesClassContainMember(className, memberName, true /* search for static members */);

                if (iscurrFuncStatic && !isMemberStatic)
                {
                    string errorMessage = String.Format("Static function {0} cannot access the non static member {1}", fullFunctionName, memberName);
                    Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                    return null;
                }
            }

            LLVMRegister memberPtrRegister = GetStaticOrMemberField(instance, className, memberName);
            LLVMRegister assignmentValuePtrRegister = Visit(context.RightExpression);
            string assignmentOperator = context.ASSIGNMENT_OPERATOR().GetText();
            return GenerateAssignmentToRegister(context, memberPtrRegister, assignmentValuePtrRegister, assignmentOperator);
        }

        private LLVMRegister GetStaticOrMemberField(LLVMValueRef? instance, string className, string memberName)
        {
            bool isMemberStatic = ClassManager.DoesClassContainMember(className, memberName, true /* search for static members */);

            LLVMRegister memberPtrRegister;
            if (isMemberStatic)
            {
                string fullyQualifiedMemberName = String.Join(".", className, memberName);
                LLVMValueRef memberPtr = LLVM.GetNamedGlobal(Module, fullyQualifiedMemberName);
                ClepsType memberType = ClassManager.LoadedClassesAndMembers[className].StaticMemberVariables[memberName];
                memberPtrRegister = new LLVMRegister(memberType, memberPtr);
            }
            else
            {
                uint fieldNumber = (uint)ClassManager.LoadedClassesAndMembers[className].MemberVariables.Keys.ToList().IndexOf(memberName);
                Debug.Assert(instance.HasValue);
                LLVMValueRef memberPtr = LLVM.BuildStructGEP(Builder, instance.Value, fieldNumber, memberName + "Field");
                ClepsType memberType = ClassManager.LoadedClassesAndMembers[className].MemberVariables[memberName];
                memberPtrRegister = new LLVMRegister(memberType, memberPtr);
            }

            return memberPtrRegister;
        }

        #endregion Function Statement Implementations

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////// Right Hand Expressions Implementations ///////////////////////////////////// 
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Right Hand Expressions Implementations

        public override LLVMRegister VisitVariableAssignment([NotNull] ClepsParser.VariableAssignmentContext context)
        {
            string variableName = context.variable().VariableName.Text;
            LLVMRegister ret = VariableManager.GetVariable(variableName);
            return ret;
        }

        public override LLVMRegister VisitFieldAccessOnExpression([NotNull] ClepsParser.FieldAccessOnExpressionContext context)
        {
            string memberName = context.FieldName.Name.Text;
            LLVMRegister targetRegister = Visit(context.rightHandExpression());
            string className = targetRegister.VariableType.RawTypeName;

            if (!ClassManager.DoesClassContainMember(className, memberName))
            {
                string errorMessage = String.Format("The class {0} does not have a member named {1}", className, memberName);
                Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                //just assume this is operation returns a constant int to avoid stalling the compilation
                LLVMRegister errRet = GetConstantIntRegisterOfClepsType(context, LLVM.Int32TypeInContext(Context), 5, "int32" /* friendly type name */);
                return errRet;
            }

            LLVMRegister ret = GetClassMember(className, memberName, targetRegister.LLVMPtrValueRef);
            return ret;
        }

        /// <summary>
        /// Retrieves a register pointing to the static or non-static member field of a class
        /// If retrieving a non static member, the object register ptr should be passed in targetRegister
        /// </summary>
        /// <param name="className"></param>
        /// <param name="memberName"></param>
        /// <param name="targetRegister"></param>
        /// <returns></returns>
        private LLVMRegister GetClassMember(string className, string memberName, LLVMValueRef targetRegister)
        {
            bool isMemberStatic = ClassManager.DoesClassContainMember(className, memberName, true /* search for static members */);

            LLVMRegister memberPtrRegister;
            if (isMemberStatic)
            {
                string fullyQualifiedMemberName = String.Join(".", className, memberName);
                LLVMValueRef memberPtr = LLVM.GetNamedGlobal(Module, fullyQualifiedMemberName);
                ClepsType memberType = ClassManager.LoadedClassesAndMembers[className].StaticMemberVariables[memberName];
                memberPtrRegister = new LLVMRegister(memberType, memberPtr);
            }
            else
            {
                uint fieldNumber = (uint)ClassManager.LoadedClassesAndMembers[className].MemberVariables.Keys.ToList().IndexOf(memberName);
                LLVMValueRef memberPtr = LLVM.BuildStructGEP(Builder, targetRegister, fieldNumber, memberName + "Field");
                ClepsType memberType = ClassManager.LoadedClassesAndMembers[className].MemberVariables[memberName];
                memberPtrRegister = new LLVMRegister(memberType, memberPtr);
            }

            return memberPtrRegister;
        }

        public override LLVMRegister VisitArrayAccessOnExpression([NotNull] ClepsParser.ArrayAccessOnExpressionContext context)
        {
            LLVMRegister arrayObject = Visit(context.ArrayExpression);

            if(!arrayObject.VariableType.IsArrayType)
            {
                string errorMessage = String.Format("The expression does not return an array object. Returned type is {0}.", arrayObject.VariableType.GetTypeName());
                Status.AddError(new CompilerError(FileName, context.ArrayExpression.Start.Line, context.ArrayExpression.Start.Column, errorMessage));
                //just assume this is operation returns a constant int to avoid stalling the compilation
                LLVMRegister errRet = GetConstantIntRegisterOfClepsType(context, LLVM.Int32TypeInContext(Context), 5, "int32" /* friendly type name */);
                return errRet;
            }

            ClepsType int32NativeType = ClepsType.GetBasicType("System.LLVMTypes.I32", new List<uint>() /* arrayDims */, 0 /* ptrIndirectionLevel */);
            ClepsClass int32MappedClass;
            ClepsType int32MappedType = null;

            if(ClassManager.RawLLVMTypeMappingClasses.TryGetValue(int32NativeType, out int32MappedClass))
            {
                int32MappedType = ClepsType.GetBasicType(int32MappedClass.FullyQualifiedName, new List<uint>() /* arrayDims */, 0 /* ptrIndirectionLevel */);
            }

            LLVMValueRef zero = LLVM.ConstInt(LLVM.Int32TypeInContext(Context), 0, false);
            List<LLVMValueRef> indexList = new List<LLVMValueRef>() { zero };

            foreach(var arrayIndexExpression in context._ArrayIndexExpression)
            {
                LLVMRegister index = Visit(arrayIndexExpression);
                LLVMValueRef llvmIndex;

                if (index.VariableType == int32NativeType)
                {
                    llvmIndex = index.LLVMPtrValueRef;
                }
                else if (int32MappedType != null && index.VariableType == int32MappedType)
                {
                    llvmIndex = LLVM.BuildStructGEP(Builder, index.LLVMPtrValueRef, 0, "rawField");
                }
                else
                {
                    string errorMessage = String.Format("The array index expression does not return an integer type. Returned type is {0}.", index.VariableType.GetTypeName());
                    Status.AddError(new CompilerError(FileName, arrayIndexExpression.Start.Line, arrayIndexExpression.Start.Column, errorMessage));
                    //just assume this is operation returns a constant int to avoid stalling the compilation
                    LLVMRegister errRet = GetConstantIntRegisterOfClepsType(context, LLVM.Int32TypeInContext(Context), 5, "int32" /* friendly type name */);
                    return errRet;
                }

                LLVMValueRef indexRegister = LLVM.BuildLoad(Builder, llvmIndex, "indexRegister");
                indexList.Add(indexRegister);
            }

            LLVMValueRef element = LLVM.BuildInBoundsGEP(Builder, arrayObject.LLVMPtrValueRef, indexList.ToArray(), "arrayElement");
            ClepsType returnType = ClepsType.GetArrayAccessOnType(arrayObject.VariableType, 1 /* number of dimensions accessed */);
            LLVMRegister ret = new LLVMRegister(returnType, element);
            return ret;
        }

        public override LLVMRegister VisitFieldOrClassAssignment([NotNull] ClepsParser.FieldOrClassAssignmentContext context)
        {
            string memberName = context.classOrMemberName().Name.Text;
            string className = String.Join(".", CurrentNamespaceAndClass);

            if (ClassManager.DoesClassContainMember(className, memberName))
            {
                LLVMRegister thisPtr = VariableManager.GetVariable("this");
                LLVMValueRef thisVar = LLVM.BuildLoad(Builder, thisPtr.LLVMPtrValueRef, "this");
                LLVMRegister ret = GetClassMember(className, memberName, thisVar);
                return ret;
            }
            else if(ClassManager.IsClassLoaded(className))
            {
                //get the class type
                throw new NotImplementedException("Class types are not yet supported");
            }
            else
            {
                string errorMessage = String.Format("No Class or Field named {0} exists", memberName);
                Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                //just assume this is operation returns a constant int to avoid stalling the compilation
                LLVMRegister ret = GetConstantIntRegisterOfClepsType(context, LLVM.Int32TypeInContext(Context), 11, "int32" /* friendly type name */);
                return ret;
            }            
        }

        public override LLVMRegister VisitBinaryOperatorOnExpression([NotNull] ClepsParser.BinaryOperatorOnExpressionContext context)
        {
            LLVMRegister leftExpression = Visit(context.LeftExpression);
            LLVMRegister rightExpression = Visit(context.RightExpression);

            if(leftExpression.VariableType != rightExpression.VariableType)
            {
                throw new NotImplementedException("Operators on different types are not yet supported");
            }

            string expressionClassName = leftExpression.VariableType.RawTypeName;
            string operatorName = context.operatorSymbol().GetText();

            if (ClepsLLVMTypeConvertorInst.IsPrimitiveLLVMType(leftExpression.VariableType))
            {
                LLVMValueRef leftValue = LLVM.BuildLoad(Builder, leftExpression.LLVMPtrValueRef, "leftValue");
                LLVMValueRef rightValue = LLVM.BuildLoad(Builder, rightExpression.LLVMPtrValueRef, "rightValue");
                LLVMRegister ret = null;

                //will return null if this is not an arithmetic operation
                ret = GenerateNativeArithmeticOperation(context, leftValue, rightValue, operatorName);

                if(ret == null)
                {
                    //will return null if this is not a comparison operation
                    ret = GenerateNativeComparison(context, leftValue, rightValue, operatorName);
                }

                if(ret == null)
                {
                    string errorMessage = String.Format("The native class {0} does not support operator {1}", expressionClassName, operatorName);
                    Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                    //just assume this is operation returns a constant int to avoid stalling the compilation
                    ret = GetConstantIntRegisterOfClepsType(context, LLVM.Int32TypeInContext(Context), 7, "int32" /* friendly type name */);
                }

                return ret;
            }
            else
            {
                if (!ClassManager.LoadedClassesAndMembers[expressionClassName].DoesClassContainMember(operatorName, false /* search member types only */))
                {
                    string errorMessage = String.Format("The class {0} does not have a operator {1} defined", expressionClassName, operatorName);
                    Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                    //just assume this is operation returns a constant int to avoid stalling the compilation
                    LLVMRegister ret = GetConstantIntRegisterOfClepsType(context, LLVM.Int32TypeInContext(Context), 7, "int32" /* friendly type name */);
                    return ret;
                }

                ClepsClass variableClass = ClassManager.LoadedClassesAndMembers[expressionClassName];
                if (!variableClass.MemberMethods.ContainsKey(operatorName))
                {
                    string errorMessage = String.Format("The class {0} does not have operator {1} defined", expressionClassName, operatorName);
                    Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                    //just assume this is operation returns a constant int to avoid stalling the compilation
                    LLVMRegister ret = GetConstantIntRegisterOfClepsType(context, LLVM.Int32TypeInContext(Context), 7, "int32" /* friendly type name */);
                    return ret;
                }

                ClepsType assignmentFunctionToCall = variableClass.MemberMethods[operatorName];
                if (assignmentFunctionToCall.FunctionParameters.Count != 3 && assignmentFunctionToCall.FunctionParameters[1] != leftExpression.VariableType && assignmentFunctionToCall.FunctionParameters[2] != rightExpression.VariableType)
                {
                    string errorMessage = String.Format("The class {0} does not have an operator {1} defined that takes parameter ({2}, {3})", expressionClassName, operatorName, leftExpression.VariableType, rightExpression.VariableType);
                    Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                    //just assume this is operation returns a constant int to avoid stalling the compilation
                    LLVMRegister ret = GetConstantIntRegisterOfClepsType(context, LLVM.Int32TypeInContext(Context), 7, "int32" /* friendly type name */);
                    return ret;
                }

                string assignmentFunctionNameToCall = String.Format("{0}.{1}", expressionClassName, operatorName);
                LLVMValueRef llvmAssignmentFunction = LLVM.GetNamedFunction(Module, assignmentFunctionNameToCall);
                LLVMValueRef operatorArg = LLVM.BuildLoad(Builder, rightExpression.LLVMPtrValueRef, "operatorArg");
                LLVMValueRef[] parameters = new LLVMValueRef[] { leftExpression.LLVMPtrValueRef, operatorArg };
                LLVMValueRef operatorReturnRegister = LLVM.BuildCall(Builder, llvmAssignmentFunction, parameters, "operator" + operatorName + "Call");
                LLVMValueRef operatorReturnPtr = LLVM.BuildAlloca(Builder, LLVM.TypeOf(operatorReturnRegister), "operatorReturnPtr");
                LLVM.BuildStore(Builder, operatorReturnRegister, operatorReturnPtr);
                ClepsType returnType = assignmentFunctionToCall.FunctionReturnType;
                LLVMRegister operatorReturn = new LLVMRegister(returnType, operatorReturnPtr);
                return operatorReturn;
            }
        }

        private LLVMRegister GenerateNativeArithmeticOperation(ParserRuleContext context, LLVMValueRef leftValue, LLVMValueRef rightValue, string operatorName)
        {
            LLVMValueRef result;
            string prefix;

            if (operatorName == "+")
            {
                prefix = "addition";
                result = LLVM.BuildAdd(Builder, leftValue, rightValue, prefix + "Result");
            }
            else if(operatorName == "-")
            {
                prefix = "subtraction";
                result = LLVM.BuildSub(Builder, leftValue, rightValue, prefix + "Result");
            }
            else if (operatorName == "*")
            {
                prefix = "multiply";
                result = LLVM.BuildMul(Builder, leftValue, rightValue, prefix + "Result");
            }
            else if (operatorName == "/")
            {
                prefix = "divide";
                result = LLVM.BuildSDiv(Builder, leftValue, rightValue, prefix + "Result");
            }
            else
            {
                return null;
            }

            LLVMTypeRef resultLLVMType = LLVM.TypeOf(result);
            LLVMValueRef resultPtr = LLVM.BuildAlloca(Builder, resultLLVMType, prefix + "ResultPtr");
            LLVM.BuildStore(Builder, result, resultPtr);
            ClepsType resultType = ClepsLLVMTypeConvertorInst.GetClepsNativeLLVMType(resultLLVMType);
            LLVMRegister ret = new LLVMRegister(resultType, resultPtr);
            return ret;            
        }

        private LLVMRegister GenerateNativeComparison(ParserRuleContext context, LLVMValueRef leftValue, LLVMValueRef rightValue, string operatorName)
        {
            LLVMIntPredicate nativeOperator;
            if (operatorName == "==")
            {
                nativeOperator = LLVMIntPredicate.LLVMIntEQ;
            }
            else if (operatorName == "!=")
            {
                nativeOperator = LLVMIntPredicate.LLVMIntNE;
            }
            else if (operatorName == "<=")
            {
                nativeOperator = LLVMIntPredicate.LLVMIntSLE;
            }
            else if (operatorName == ">=")
            {
                nativeOperator = LLVMIntPredicate.LLVMIntSGE;
            }
            else if (operatorName == "<")
            {
                nativeOperator = LLVMIntPredicate.LLVMIntSLT;
            }
            else if (operatorName == ">")
            {
                nativeOperator = LLVMIntPredicate.LLVMIntSGT;
            }
            else
            {
                return null;
            }

            LLVMValueRef isEqual = LLVM.BuildICmp(Builder, nativeOperator, leftValue, rightValue, "nativeIsEqual");
            LLVMRegister operatorRet = GetIntRegisterOfClepsType(context, LLVM.Int1TypeInContext(Context), isEqual, "bool");
            return operatorRet;
        }

        public override LLVMRegister VisitNumericAssignments([NotNull] ClepsParser.NumericAssignmentsContext context)
        {
            string valueString = context.numeric().NumericValue.Text;
            string numericType = context.numeric().NumericType != null ? context.numeric().NumericType.Text : "";

            ulong value;
            if(!ulong.TryParse(valueString, out value))
            {
                throw new Exception(String.Format("Numeric value {0} not a valid int", valueString));
            }


            LLVMTypeRef llvmType = LLVM.Int32TypeInContext(Context);

            //hardcoded identifiers for llvm i.e. native types
            if (numericType == "ni")
            {
                LLVMValueRef valueToRet = LLVM.ConstInt(llvmType, value, false);
                LLVMValueRef valuePtr = LLVM.BuildAlloca(Builder, LLVM.TypeOf(valueToRet), "nativeIntValuePtr");
                LLVM.BuildStore(Builder, valueToRet, valuePtr);
                LLVMRegister nativeRet = new LLVMRegister(ClepsLLVMTypeConvertorInst.GetClepsNativeLLVMType(llvmType), valuePtr);
                return nativeRet;
            }

            if (!String.IsNullOrEmpty(numericType))
            {
                //for now other numeric types are not supported
                string errorMessage = String.Format("Numerics of type {0} was not found. Make sure the class that defined {0} was imported.", numericType);
                Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                //just assume this is numericType is "" to avoid stalling the compilation
            }

            LLVMRegister ret = GetConstantIntRegisterOfClepsType(context, llvmType, value, "int32" /* friendly type name */);
            return ret;
        }

        public override LLVMRegister VisitBooleanAssignments([NotNull] ClepsParser.BooleanAssignmentsContext context)
        {
            bool boolValue = context.TRUE() != null;
            ulong value = boolValue ? 1u : 0u;
            LLVMTypeRef llvmType = LLVM.Int1TypeInContext(Context);
            LLVMRegister ret = GetConstantIntRegisterOfClepsType(context, llvmType, value, "bool" /* friendly type name */);
            return ret;
        }

        /// <summary>
        /// Return an LLVM variable defined on the stack. 
        /// The value of the initialized to this variable needs to be a constant bool, int or long.
        /// This native llvm type is then mapped to the appropriate cleps type (which is specified in code by the rawtypemap statement) and returned
        /// </summary>
        /// <param name="context"></param>
        /// <param name="llvmType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private LLVMRegister GetConstantIntRegisterOfClepsType(ParserRuleContext context, LLVMTypeRef llvmType, ulong value, string friendlyTypeName)
        {
            LLVMValueRef register = LLVM.ConstInt(llvmType, value, false);
            LLVMRegister ret = GetIntRegisterOfClepsType(context, llvmType, register, friendlyTypeName);
            return ret;
        }

        /// <summary>
        /// Return an LLVM variable defined on the stack. 
        /// The value of the initialized to this variable needs to be a stored in a register.
        /// This native llvm type is then mapped to the appropriate cleps type (which is specified in code by the rawtypemap statement) and returned
        /// </summary>
        /// <param name="context"></param>
        /// <param name="llvmType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private LLVMRegister GetIntRegisterOfClepsType(ParserRuleContext context, LLVMTypeRef llvmType, LLVMValueRef register, string friendlyTypeName)
        {
            ClepsType clepsType = ClepsLLVMTypeConvertorInst.GetClepsNativeLLVMType(llvmType);

            ClepsClass mappedClass;
            if (!ClassManager.RawLLVMTypeMappingClasses.TryGetValue(clepsType, out mappedClass))
            {
                string errorMessage = String.Format("Could not find a raw mapping for type {0}", clepsType.GetTypeName());
                Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                return null;
            }

            ClepsType mappedClassType = ClepsType.GetBasicType(mappedClass.FullyQualifiedName, new List<uint>() /* array dims */, 0);

            LLVMValueRef? instPtr = CallConstructorAllocaForType(context, mappedClassType, friendlyTypeName + "Inst");
            if (instPtr == null)
            {
                return null;
            }

            //the mapped type is always the first field
            LLVMValueRef instField = LLVM.BuildStructGEP(Builder, instPtr.Value, 0, friendlyTypeName + "InstField");
            LLVMTypeRef instFieldType = LLVM.TypeOf(instField);

            LLVM.BuildStore(Builder, register, instField);

            LLVMRegister ret = new LLVMRegister(mappedClassType, instPtr.Value);
            return ret;
        }

        #endregion Right Hand Expressions Implementations
    }
}
