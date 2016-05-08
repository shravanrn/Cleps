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
IF : 'if';
FOR : 'for';
DO : 'do';
WHILE : 'while';
RETURN : 'return';
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

visibilityModifier : PUBLIC | INTERNAL;
typename : 
	RawTypeName=nestedIdentifier # BasicType
	| BaseType=typename '*' # PointerType
	| BaseType=typename '[' ArrayDimensions+=numeric (',' ArrayDimensions+=numeric)* ']' # ArrayType
	| '(' (FunctionParameterTypes+=typename (',' FunctionParameterTypes+=typename)*)? ')' '->' FunctionReturnType=typename #FunctionType;
typenameAndVoid : typename | VOID;

///////////////////////////////////////////////////////

compilationUnit : namespaceBlockStatement;

namespaceBlockStatement : (NAMESPACE NamespaceName=nestedIdentifier '{' usingNamespaceStatements*)( namespaceBlockStatement | classDeclarationStatements)*('}');

usingNamespaceStatement : USING STATIC? nestedIdentifier END;
usingNamespaceStatements : usingNamespaceStatement+;

classDeclarationStatements : (visibilityModifier CLASS ClassName=classOrMemberName '{') classBodyStatements ('}');
classBodyStatements : 
(
		classDeclarationStatements
	|	memberDeclarationStatement
)*;

memberDeclarationStatement : visibilityModifier STATIC? typename FieldName=classOrMemberName (ASSIGNMENT_OPERATOR rightHandExpression)? END;

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
;

rightHandExpressionSimple : stringAssignments | numericAssignments | nullAssignment | booleanAssignments | arrayAssignment | functionCallAssignment | variableAssignment | fieldOrClassAssignment | classInstanceAssignment | functionAssignment;
numericAssignments : numeric;
nullAssignment : NULL;
booleanAssignments : TRUE|FALSE;
stringAssignments : stringValue;
arrayAssignment : '[' (ArrayElements+=rightHandExpression (',' ArrayElements+=rightHandExpression)*)? ']';
functionCallAssignment : functionCall;
variableAssignment : variable;
fieldOrClassAssignment : classOrMemberName;
classInstanceAssignment : NEW typename '(' (FunctionParameters+=rightHandExpression (',' FunctionParameters+=rightHandExpression)*)? ')';
functionAssignment : '(' (FunctionParameterTypes+=typename FormalParameters+=variable (',' FormalParameters+=variable)*)? ')' '->' FunctionReturnType=typename statementBlock;

functionCall : FunctionName=classOrMemberName '(' (FunctionParameters+=rightHandExpression (',' FunctionParameters+=rightHandExpression)*)? ')';
statementBlock : '{' functionStatement* '}';

/////////////////////////////////////////////////////////////

functionStatement : functionReturnStatement | functionVariableDeclarationStatement | functionFieldAssignmentStatement | functionVariableAssignmentStatement | functionArrayAssignmentStatement | functionCallStatement | ifStatement | doWhileStatement;

functionReturnStatement : RETURN rightHandExpression? END;

functionVariableDeclarationStatement : typename variable (ASSIGNMENT_OPERATOR rightHandExpression)? END;
functionVariableAssignmentStatement : variableAssignment ASSIGNMENT_OPERATOR rightHandExpression END;
functionFieldAssignmentStatement : (LeftExpression=rightHandExpression '.')? FieldName=classOrMemberName ASSIGNMENT_OPERATOR RightExpression=rightHandExpression END;
functionArrayAssignmentStatement : ArrayExpression=rightHandExpression ('[' ArrayIndexExpression+=rightHandExpression ']')+ ASSIGNMENT_OPERATOR RightExpression=rightHandExpression END;
functionCallStatement : (rightHandExpression '.')? functionCall END;

ifStatement : IF '(' rightHandExpression ')' statementBlock;
doWhileStatement : DO statementBlock WHILE '(' TerminalCondition=rightHandExpression ')' END;
