﻿using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
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
    /// <summary>
    /// Abstract class that all cleps parsers inherit from
    /// </summary>
    /// <typeparam name="object"></typeparam>
    abstract class ClepsAbstractVisitor : ClepsBaseVisitor<object>
    {
        protected string FileName;
        protected CompileStatus Status;
        protected ClassManager ClassManager;
        protected ICodeGenerator CodeGenerator;
        protected TypeManager TypeManager;

        private List<string> CurrentNamespaceHierarchy;
        private List<string> CurrentClassHierarchy;
        protected string CurrentNamespace { get { return String.Join(".", CurrentNamespaceHierarchy); } }
        protected string FullyQualifiedClassName { get { return String.Join(".", CurrentNamespaceHierarchy.Union(CurrentClassHierarchy).ToList()); } }
        protected Dictionary<GenericClepsType, ClepsType> TemplateReplacementsToUse;

        public ClepsAbstractVisitor(CompileStatus status, ClassManager classManager, ICodeGenerator codeGenerator, TypeManager typeManager)
        {
            Status = status;
            ClassManager = classManager;
            CodeGenerator = codeGenerator;
            TypeManager = typeManager;
            TemplateReplacementsToUse = new Dictionary<GenericClepsType, ClepsType>();
        }

        public object ParseFile(string fileName, IParseTree tree)
        {
            FileName = fileName;
            object ret = Visit(tree);
            return ret;
        }

        #region CodeUsedInMultipleStagesOfCompilation
        public sealed override object VisitCompilationUnit([NotNull] ClepsParser.CompilationUnitContext context)
        {
            CurrentNamespaceHierarchy = new List<string>();
            CurrentClassHierarchy = new List<string>();
            VisitChildren(context);
            return true;
        }

        public sealed override object VisitNamespaceBlockStatement([NotNull] ClepsParser.NamespaceBlockStatementContext context)
        {
            CurrentNamespaceHierarchy.Add(context.NamespaceName.GetText());
            VisitChildren(context);
            CurrentNamespaceHierarchy.RemoveAt(CurrentNamespaceHierarchy.Count - 1);
            return true;
        }

        public virtual bool VisitClassDeclarationStatements_Ex([NotNull] ClepsParser.ClassDeclarationStatementsContext context)
        {
            VisitChildren(context);
            return true;
        }

        public sealed override object VisitClassDeclarationStatements([NotNull] ClepsParser.ClassDeclarationStatementsContext context)
        {
            if (context.ClassName == null)
            {
                //Some antlr parsing exception has occurred. Just exit.
                return false;
            }

            CurrentClassHierarchy.Add(context.ClassName.GetText());
            var ret = VisitClassDeclarationStatements_Ex(context);
            CurrentClassHierarchy.RemoveAt(CurrentClassHierarchy.Count - 1);

            return ret;
        }

        #endregion CodeUsedInMultipleStagesOfCompilation

        #region ClepsTypes

        public sealed override object VisitBasicType([NotNull] ClepsParser.BasicTypeContext context)
        {
            string rawTypeName = context.RawTypeName.GetText();
            ClepsType ret = new BasicClepsType(rawTypeName);
            return ret;
        }

        public sealed override object VisitTemplateType([NotNull] ClepsParser.TemplateTypeContext context)
        {
            string rawTypeName = context.templateName().Name.Text;
            GenericClepsType templateType = new GenericClepsType(TypeManager, rawTypeName);

            if(TemplateReplacementsToUse.ContainsKey(templateType))
            {
                return TemplateReplacementsToUse[templateType];
            }
            else
            {
                string errorMessage = String.Format("No substitution specified for template ${0}", rawTypeName);
                Status.AddError(new CompilerError(FileName, context.Start.Line, context.Start.Column, errorMessage));
                //in case of an error, try to gracefully continue by assuming any type 
                return CompilerConstants.ClepsByteType;
            }
        }

        public sealed override object VisitPointerType([NotNull] ClepsParser.PointerTypeContext context)
        {
            ClepsType basicType = Visit(context.BaseType) as ClepsType;
            Debug.Assert(basicType != null);
            var ret = new PointerClepsType(basicType);
            return ret;
        }

        public sealed override object VisitArrayType([NotNull] ClepsParser.ArrayTypeContext context)
        {
            ClepsType basicType = Visit(context.BaseType) as ClepsType;
            Debug.Assert(basicType != null);
            long[] dimensions = context._ArrayDimensions.Select(n => {
                long val;
                if (long.TryParse(n.GetText(), out val))
                {
                    return val;
                }
                else
                {
                    string errorMessage = String.Format("Expected a numeric value for array dimensions. However found {0} instead.", n.GetText());
                    Status.AddError(new CompilerError(FileName, n.Start.Line, n.Start.Column, errorMessage));
                    //in case of an error, try to gracefully continue by assuming the array dimensions is 1
                    return 1;
                }
            }).ToArray();

            var ret = new ArrayClepsType(basicType, dimensions);
            return ret;
        }

        public sealed override object VisitFunctionType([NotNull] ClepsParser.FunctionTypeContext context)
        {
            List<GenericClepsType> templateParameters = new List<GenericClepsType>();
            List<string> templateNamesAdded = new List<string>();

            if (context._FunctionTemplateTypes != null)
            {
                foreach (var t in context._FunctionTemplateTypes)
                {
                    var templateName = t.GetText();
                    templateParameters.Add(new GenericClepsType(TypeManager, templateName));

                    if (!templateNamesAdded.Contains(templateName))
                    {
                        templateNamesAdded.Add(templateName);
                    }
                    else
                    {
                        string errorMessage = String.Format("Template name {0} has already been defined.", templateName);
                        Status.AddError(new CompilerError(FileName, t.Start.Line, t.Start.Column, errorMessage));
                    }
                }
            }

            List<ClepsType> parameters = context._FunctionParameterTypes.Select(p => Visit(p) as ClepsType).ToList();
            ClepsType returnType = Visit(context.FunctionReturnType) as ClepsType;

            var ret = new FunctionClepsType(TypeManager, templateParameters, parameters, returnType);
            return ret;
        }

        public sealed override object VisitTypenameAndVoid([NotNull] ClepsParser.TypenameAndVoidContext context)
        {
            if (context.typename() != null)
            {
                return Visit(context.typename());
            }

            var ret = VoidClepsType.GetVoidType();
            return ret;
        }

        #endregion ClepsTypes
    }
}
