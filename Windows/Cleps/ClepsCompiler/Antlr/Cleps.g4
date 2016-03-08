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
FUNC : 'fn';
VOID : 'void';
TRUE : 'true';
FALSE : 'false';
NULL : 'null';
IF : 'if';
FOR : 'for';
DO : 'do';
WHILE : 'while';
RETURN : 'return';
RAWTYPEMAP : 'rawtypemap';
ASSIGNMENT : 'assignment';
OPERATOR : 'operator';
ASSIGNMENT_OPERATOR : '=';
PASCALCASE_ID : [A-Z] [a-zA-Z0-9_]*;
ID : [a-zA-Z] [a-zA-Z0-9_]*;
NUMERIC_TOKEN : [0-9]+ ('.' [0-9]+)?;
STRING : ID? '"' ('\\"'|.)*? '"'
	 |	ID? '\'\'' ('\\\''|.)*? '\'\''
	;
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

variable : '@' VariableName=(ID|PASCALCASE_ID);
nestedIdentifier : PASCALCASE_ID ('.' PASCALCASE_ID)*;
numeric : NumericValue=NUMERIC_TOKEN NumericType=ID?;
classOrMemberName : Name=PASCALCASE_ID;

visibilityModifier : PUBLIC | INTERNAL;
typename : RawTypeName=nestedIdentifier ('[' ArrayDimensions+=numeric ']')* (PtrIndirectionLevel+='*')* '!'?;
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
	|	memberVariableDeclarationStatement
	|	memberFunctionDeclarationStatement
	|	memberAssignmentFunctionDeclarationStatement
	|	memberOperatorFunctionDeclarationStatement
	|	rawTypeMapStatment
)*;

rawTypeMapStatment : RAWTYPEMAP typename END;

memberVariableDeclarationStatement : visibilityModifier STATIC? typename FieldName=classOrMemberName (ASSIGNMENT_OPERATOR rightHandExpression)? END;
memberFunctionDeclarationStatement : visibilityModifier STATIC? functionDeclarationStatement;
memberAssignmentFunctionDeclarationStatement : visibilityModifier STATIC? assignmentFunctionDeclarationStatement;
memberOperatorFunctionDeclarationStatement : visibilityModifier STATIC? operatorFunctionDeclarationStatment;

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

rightHandExpressionSimple : stringAssignments | numericAssignments | nullAssignment | booleanAssignments | functionCallAssignment | variableAssignment | fieldOrClassAssignment | classInstanceAssignment;
numericAssignments : numeric;
nullAssignment : NULL;
booleanAssignments : TRUE|FALSE;
stringAssignments : STRING;
functionCallAssignment : functionCall;
variableAssignment : variable;
fieldOrClassAssignment : classOrMemberName;
classInstanceAssignment : NEW typename '(' (FunctionParameters+=rightHandExpression (',' FunctionParameters+=rightHandExpression)*)? ')';

functionCall : FunctionName=classOrMemberName '(' (FunctionParameters+=rightHandExpression (',' FunctionParameters+=rightHandExpression)*)? ')';

/////////////////////////////////////////////////////////////

functionStatement : functionReturnStatement | functionVariableDeclarationStatement | functionFieldAssignmentStatement | functionVariableAssigmentStatement | functionArrayAssignmentStatement | functionDeclarationStatement | functionCallStatement | ifStatement | doWhileStatement;

functionReturnStatement : RETURN rightHandExpression? END;

functionVariableDeclarationStatement : variableDeclarationStatement;
variableDeclarationStatement : typename variable (ASSIGNMENT_OPERATOR rightHandExpression)? END;
functionVariableAssigmentStatement : variable ASSIGNMENT_OPERATOR rightHandExpression END;
functionFieldAssignmentStatement : (LeftExpression=rightHandExpression '.')? FieldName=classOrMemberName ASSIGNMENT_OPERATOR RightExpression=rightHandExpression END;
functionArrayAssignmentStatement : ArrayExpression=rightHandExpression ('[' ArrayIndexExpression+=rightHandExpression ']')+ ASSIGNMENT_OPERATOR RightExpression=rightHandExpression END;

functionDeclarationStatement : FUNC FunctionName=classOrMemberName (ASSIGNMENT_OPERATOR FUNC FunctionReturnType=typenameAndVoid '(' functionParametersList ')' statementBlock)? END;
assignmentFunctionDeclarationStatement : ASSIGNMENT FunctionName=ASSIGNMENT_OPERATOR FUNC FunctionReturnType=VOID '(' functionParametersList ')' statementBlock END;
operatorFunctionDeclarationStatment : OPERATOR FunctionName=operatorSymbol FUNC FunctionReturnType=typename '(' functionParametersList ')' statementBlock END;

functionParametersList : (FunctionParameterTypes+=typename FunctionParameterNames+=variable (',' FunctionParameterTypes+=typename FunctionParameterNames+=variable)*)?;
statementBlock : '{' functionStatement* '}';

functionCallStatement : (rightHandExpression '.')? functionCall END;

ifStatement : IF '(' rightHandExpression ')' statementBlock;

doWhileStatement : DO statementBlock WHILE '(' TerminalCondition=rightHandExpression ')' END;
