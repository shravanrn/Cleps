grammar Cleps;

COMMENT_SINGLELINE : '//' ~[\r\n]* -> channel(HIDDEN);
COMMENT_MULTILINE : '/*' .*? ('/*' .*? '*/')* .*? '*/' -> channel(HIDDEN);
WS : [ \t\r\n]+ -> channel(HIDDEN);
USING : 'using';
END : ';';
NAMESPACE : 'namespace';
NEW : 'new';
CLASS : 'class';
STATIC : 'static';
PUBLIC : 'public';
INTERNAL : 'internal';
VOID : 'void';
TRUE : 'true';
FALSE : 'false';
NULL : 'null';
THIS : 'this';
IF : 'if';
FOR : 'for';
DO : 'do';
WHILE : 'while';
RETURN : 'return';
CONST : 'const';
NATIVE : 'native';
ASSIGNMENT : 'assignment';
OPERATOR : 'operator';
ASSIGNMENT_OPERATOR : '=';
PASCALCASE_ID : [A-Z] [a-zA-Z0-9_]*;
ID : [a-zA-Z] [a-zA-Z0-9_]*;
NUMERIC_TOKEN : [0-9]+ ('.' [0-9]+)?;

//exclude '*' from the lexer as '*' is sometimes used in other contacts such as pointer declarations
//we have a parser version of operator Symbol as well below that includes '*' 
OPERATOR_SYMBOL_LEXER : ('+'|'-'|'/')+ 
	| '`' ('+'|'-'|'*'|'/'|[a-zA-Z0-9_])+ '`'
	| ('==' | '!=' | '<' | '>' | '<=' | '>=')
	;

///////////////////////////////////////////////////////

//token OPERATOR_SYMBOL_LEXER excludes '*' as '*' is sometimes used in other contacts such as pointer declarations
//below parser version of operatorSymbol includes '*'
operatorSymbol : OPERATOR_SYMBOL_LEXER | '*';

stringValue : StringType=ID? 
	(
		StringStyle='"' ('\\"'|.)*? '"'
		|	StringStyle='\'\'' ('\\\''|.)*? '\'\''
	);

variable : '@' VariableName=(ID|PASCALCASE_ID);
nestedIdentifier : PASCALCASE_ID ('.' PASCALCASE_ID)*;
numeric : NumericValue=NUMERIC_TOKEN NumericType=ID?;
classOrMemberName : Name=PASCALCASE_ID;
templateName : Name=PASCALCASE_ID;

visibilityModifier : PUBLIC | INTERNAL;
typename : 
	RawTypeName=nestedIdentifier # BasicOrTemplateType
	| BaseType=typename '*' # PointerType
	| BaseType=typename '[' ArrayDimensions+=numeric (',' ArrayDimensions+=numeric)* ']' # ArrayType
	| ('<' TemplateTypes+=templateName (',' FunctionTemplateTypes+=templateName)* '>')? '(' (FunctionParameterTypes+=typename (',' FunctionParameterTypes+=typename)*)? ')' '->' FunctionReturnType=typenameAndVoid #FunctionType;
typenameAndVoid : typename | VOID;

///////////////////////////////////////////////////////

compilationUnit : (namespaceBlockStatement | nativeGlobalStatement)+ EOF;

nativeGlobalStatement : nativeStatement;
namespaceBlockStatement : (NAMESPACE NamespaceName=nestedIdentifier '{' usingNamespaceStatements*)( namespaceBlockStatement | classDeclarationStatements)*('}');

usingNamespaceStatement : USING STATIC? nestedIdentifier END;
usingNamespaceStatements : usingNamespaceStatement+;

classDeclarationStatements : (visibilityModifier CLASS ClassName=classOrMemberName '{') classBodyStatements ('}');
classBodyStatements : 
(
		classDeclarationStatements
	|	memberDeclarationStatement
)*;

memberDeclarationStatement : visibilityModifier 
	(
		(STATIC? CONST? typename FieldName=classOrMemberName (ASSIGNMENT_OPERATOR rightHandExpression)?) |
		(STATIC CONST typename OPERATOR OperatorName=operatorSymbol rightHandExpression)
	)
	END;


///////////////////////////////////////////////////////

rightHandExpression : 
	'(' rightHandExpression ')' # BracketedExpression
	| rightHandExpressionSimple # SimpleExpression
	| rightHandExpression '.' functionCall # FunctionCallOnExpression
	| rightHandExpression '.' FieldName=classOrMemberName # FieldAccessOnExpression
	| ArrayExpression=rightHandExpression ('[' ArrayIndexExpression+=rightHandExpression ']')+ # ArrayAccessOnExpression
	| operatorSymbol rightHandExpression # PreOperatorOnExpression
	| LeftExpression=rightHandExpression operatorSymbol RightExpression=rightHandExpression # BinaryOperatorOnExpression
	| rightHandExpression operatorSymbol # PostOperatorOnExpression
	| '@(' addressOfOrValueAtTargetExpression ')' # AddressOfOnExpression
	| '$(' addressOfOrValueAtTargetExpression ')' # ValueAtOnExpression
;

rightHandExpressionSimple : stringAssignments | numericAssignments | nullAssignment | booleanAssignments | thisAssignment | arrayAssignment | functionCallAssignment | variableAssignment | fieldOrClassAssignment | classInstanceAssignment | functionAssignment;
numericAssignments : numeric;
nullAssignment : NULL;
booleanAssignments : TRUE|FALSE;
stringAssignments : stringValue;
thisAssignment : THIS;
arrayAssignment : '[' (ArrayElements+=rightHandExpression (',' ArrayElements+=rightHandExpression)*)? ']';
functionCallAssignment : functionCall;
variableAssignment : variable;
fieldOrClassAssignment : ClassHierarchy+=classOrMemberName ('.' ClassHierarchy+=classOrMemberName)*;
classInstanceAssignment : NEW typename '(' (FunctionParameters+=rightHandExpression (',' FunctionParameters+=rightHandExpression)*)? ')';
functionAssignment : ('<' FunctionTemplateTypes+=templateName (',' FunctionTemplateTypes+=templateName)* '>')? '(' (FunctionParameters+=variableDeclaration (',' FunctionParameters+=variableDeclaration)*)? ')' '->' FunctionReturnType=typenameAndVoid statementBlock;

functionCall : FunctionName=classOrMemberName '(' (FunctionParameters+=rightHandExpression (',' FunctionParameters+=rightHandExpression)*)? ')';
statementBlock : '{' functionStatement* '}';

addressOfOrValueAtTargetExpression : thisAssignment | variableAssignment | fieldOrClassAssignment;

/////////////////////////////////////////////////////////////

functionStatement : functionReturnStatement | functionVariableDeclarationStatement | functionFieldAssignmentStatement | functionVariableAssignmentStatement | functionArrayAssignmentStatement | functionCallStatement | ifStatement | doWhileStatement | nativeFunctionStatement;

functionReturnStatement : RETURN rightHandExpression? END;

functionVariableDeclarationStatement : variableDeclaration (ASSIGNMENT_OPERATOR rightHandExpression)? END;
variableDeclaration : CONST? typename variable;
functionVariableAssignmentStatement : variableAssignment ASSIGNMENT_OPERATOR rightHandExpression END;
functionFieldAssignmentStatement : (LeftExpression=rightHandExpression '.')? FieldName=classOrMemberName ASSIGNMENT_OPERATOR RightExpression=rightHandExpression END;
functionArrayAssignmentStatement : ArrayExpression=rightHandExpression ('[' ArrayIndexExpression+=rightHandExpression ']')+ ASSIGNMENT_OPERATOR RightExpression=rightHandExpression END;
functionCallStatement : (rightHandExpression '.')? functionCall END;

ifStatement : IF '(' rightHandExpression ')' statementBlock;
doWhileStatement : DO statementBlock WHILE '(' TerminalCondition=rightHandExpression ')' END;

nativeFunctionStatement : nativeStatement;
nativeStatement : NATIVE '(' PlatFormTarget=NUMERIC_TOKEN ')' NativeOpen='[{' nativeCode NativeClose='}]' END;
nativeCode : (~('}]'))*?;