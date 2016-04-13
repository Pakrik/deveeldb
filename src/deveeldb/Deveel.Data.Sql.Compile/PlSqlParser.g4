parser grammar PlSqlParser;

options {
	tokenVocab=PlSqlLexer;
}

compilationUnit
    : unitStatement* EOF
    ;

unitStatement
    : createSchemaStatement
	| createTableStatement 
	| createViewStatement
	| createSequenceStatement
	| createTriggerStatement
	| createFunctionBody
	| createProcedureBody
	| createUserStatement

	| alterTableStatement
	| alterTriggerStatement
	
	| dropSchemaStatement
	| dropTableStatement
	| dropViewStatement
	| dropTriggerStatement
	| dropFunctionStatement
	| dropProcedureStatement
	| dropSequenceStatement
	| dropUserStatement

	| createUserStatement
	| alterUserStatement
	| dropUserStatement
	| createRoleStatement
	| dropRoleStatement
	| grantStatement
	| revokeStatement

	| assignmentStatement
	| cursorStatement
	| dmlStatement

	| showStatement
	| transactionControlStatement

	| block
	| declareStatement
    ;

dmlStatement
    : selectStatement
	| updateStatement
	| deleteStatement
	| insertStatement
    ;

// $<DML SQL PL/SQL Statements

invoker_rights_clause
    : AUTHID (CURRENT_USER|DEFINER)
    ;

call_spec
    : LANGUAGE (dotnet_spec)
    ;

dotnet_spec
    : DOTNET typeString=CHAR_STRING (ASSEMBLY assemblyString=CHAR_STRING)?
	;

// $<Function DDLs

dropFunctionStatement
    : DROP FUNCTION objectName ( ',' objectName )* SEMICOLON?
    ;

createSchemaStatement
    : CREATE SCHEMA id
	;

dropSchemaStatement
    : DROP SCHEMA id
	;

createFunctionBody
    : ( CREATE (OR REPLACE)? )? FUNCTION objectName ('(' parameter (',' parameter)* ')')?
      RETURN type_spec (DETERMINISTIC)*
      ( (IS | AS) (DECLARE? declaration* body | call_spec) ) SEMICOLON?
    ;

// $<Procedure DDLs

dropProcedureStatement
    : DROP PROCEDURE objectName ';'
    ;

createProcedureBody
    : (CREATE (OR REPLACE)?)? PROCEDURE objectName ('(' parameter (',' parameter)* ')')? 
      invoker_rights_clause? (IS | AS)
      (DECLARE? declaration* body | call_spec) SEMICOLON?
    ;

// $>

createTableStatement
    : CREATE TEMPORARY? TABLE (IF NOT EXISTS)? objectName '(' columnOrConstraintList ')' SEMICOLON?
	;

// $<Create Table - Specific Clauses

columnOrConstraintList
    : columnOrConstraint (',' columnOrConstraint )*
	;

columnOrConstraint
    : (tableColumn | tableConstraint )
	;

tableColumn
    : columnName type_spec (IDENTITY | (NOT? NULL)? columnConstraint* (defaultValuePart)? )?
	;

columnConstraint
    : ( PRIMARY KEY? | UNIQUE KEY? )
	;

tableConstraint
    : CONSTRAINT? id
	   ( primaryKeyConstraint | uniqueKeyConstraint | checkConstraint | foreignKeyConstraint)
	;

primaryKeyConstraint
    : PRIMARY KEY? '(' columnList ')'
	;

uniqueKeyConstraint
    : UNIQUE KEY? '(' columnList ')'
	;

checkConstraint
    : CHECK '(' expression ')'
	;

foreignKeyConstraint
    : FOREIGN KEY? '(' columns=columnList ')' REFERENCES objectName '(' refColumns=columnList ')'
	;

columnList
    : columnName ( ',' columnName )*
	;

// $>

alterTableStatement
    : ALTER TABLE objectName alterTableAction* SEMICOLON?
	;

// $<Alter Table - Specific Clauses

alterTableAction
     : addColumnAction
	 | alterColumnAction
	 | dropColumnAction
	 | addConstraintAction
	 | dropConstraintAction
	 | dropDefaultAction
	 | dropPrimaryKeyAction 
	 | setDefaultAction
	 ;

addColumnAction
    : ADD COLUMN? tableColumn
	;

alterColumnAction
    : ALTER COLUMN? tableColumn
	;

dropColumnAction
    : DROP COLUMN? id
	;

addConstraintAction
    : ADD CONSTRAINT tableConstraint
	;

dropConstraintAction
    : DROP CONSTRAINT regular_id
	;

dropDefaultAction
    : ALTER COLUMN? id DROP DEFAULT
	; 

dropPrimaryKeyAction
    : DROP PRIMARY KEY?
	;

setDefaultAction
    : ALTER COLUMN? id SET DEFAULT expression_wrapper
	;

// $>

dropTableStatement
    : DROP TABLE (IF EXISTS)? objectName (',' objectName)*
	;

createViewStatement
    : CREATE (OR REPLACE)? VIEW objectName ( '(' columnList ')' )? AS subquery SEMICOLON?
	;

dropViewStatement
    : DROP VIEW (IF EXISTS)? objectName ( ',' objectName )* SEMICOLON?
	;


createSequenceStatement
    : CREATE (OR REPLACE)? SEQUENCE objectName sequenceStartClause? sequenceSpec* SEMICOLON?
	;

dropSequenceStatement
    : DROP SEQUENCE (IF EXISTS)? objectName (',' objectName )* SEMICOLON?
	;

// $<Common Sequence

sequenceSpec
    : INCREMENT BY UNSIGNED_INTEGER
    | MAXVALUE UNSIGNED_INTEGER
    | NOMAXVALUE
    | MINVALUE UNSIGNED_INTEGER
    | NOMINVALUE
    | CYCLE
    | NOCYCLE
    | CACHE UNSIGNED_INTEGER
    | NOCACHE
    ;

sequenceStartClause
    : START WITH UNSIGNED_INTEGER
    ;

selectStatement
    : subquery 
	  ( order_by_clause? for_update_clause?
	  | for_update_clause? order_by_clause? )
	  queryLimitClause? SEMICOLON?
    ;

queryLimitClause
    : LIMIT (n1=numeric (',' n2=numeric)? )
	;

// $>

parameter
    : parameter_name ( IN | OUT | INOUT )* type_spec? defaultValuePart?
    ;

defaultValuePart
    : ( ':=' | DEFAULT) expression
    ;

// $<PL/SQL Elements Declarations

declaration
    : exceptionDeclaration
	| variableDeclaration
    | cursorDeclaration
    | pragmaDeclaration
    ;

declareStatement
    : DECLARE? declaration
	;

//incorporates constant_declaration
variableDeclaration
    : variable_name CONSTANT? type_spec (NOT NULL)? defaultValuePart? SEMICOLON?
    ;

//cursor_declaration incorportates curscursor_body and cursor_spec
cursorDeclaration
    : CURSOR cursor_name ('(' parameter_spec (',' parameter_spec)* ')' )? (IS subquery)? SEMICOLON?
    ;

parameter_spec
    : parameter_name (IN? type_spec)? defaultValuePart?
    ;

exceptionDeclaration 
    : id EXCEPTION SEMICOLON?
    ;

pragmaDeclaration
    : PRAGMA ( EXCEPTION_INIT '(' id ',' numeric ')' ) SEMICOLON?
    ;

updateStatement
    : UPDATE objectName (updateSetClause whereClause? | updateFromClause) ( updateLimitClause )? SEMICOLON?
    ;

// $<Update - Specific Clauses
updateSetClause
    : SET ( columnBasedUpdateClause (',' columnBasedUpdateClause)* 
	    |  VALUE '(' columnName ')' '=' expression )
    ;

columnBasedUpdateClause
    : columnName '=' expression
    // | '(' columnName (',' columnName)* ')' '=' subquery
    ;

updateFromClause
    : FROM '(' subquery ')'
	;

updateLimitClause
    : LIMIT numeric
	;

// $>

// $<PL/SQL Statements

seq_of_statements
    : (statement (';' | EOF) | labelDeclaration)+
    ;

labelDeclaration
    : '<' '<' id '>' '>'
    ;

statement
    : body
    | block
	| dmlStatement
    | assignmentStatement
    | continueStatement
    | exitStatement
    | gotoStatement
    | ifStatement
    | loopStatement
    | forallStatement
    | nullStatement
    | raiseStatement
    | returnStatement
    | caseStatement/*[true]*/
    | sql_statement
    | functionCall
    ;

createStatement
    : createTableStatement
	| createViewStatement
	| createTriggerStatement
	| createSequenceStatement
	;

alterStatement
    : alterTableStatement
	| alterTriggerStatement
	;

grantStatement
    : grantPrivilegeStatement
	| grantRoleStatement
	;

createUserStatement
    : CREATE USER userName IDENTIFIED (byPassword | externalId | globalId) SEMICOLON?
	;

alterUserStatement
    : ALTER USER userName alterUserAction* SEMICOLON?
	;

replacePassword
    : REPLACE oldPass=CHAR_STRING
	;

alterUserAction
    : alterUserIdAction
	| setAccountAction
	| setRoleAction
	;

alterUserIdAction
    : IDENTIFIED (byPassword replacePassword? | externalId | globalId)
	;

setAccountAction
    : ACCOUNT (LOCK | UNLOCK)
	;

setRoleAction
    : ROLE regular_id
	;

granteeName
    : (userName | roleName)
	;

userName
    : id ( '@' CHAR_STRING)?
	;

roleName
    : id
	;

createRoleStatement
    : CREATE ROLE regular_id SEMICOLON?
	;

dropUserStatement
    : DROP USER userName SEMICOLON?
	;

dropRoleStatement
    : DROP ROLE regular_id SEMICOLON?
	;

byPassword
    : BY PASSWORD CHAR_STRING
	;

externalId
    : EXTERNALLY (AS '(' CHAR_STRING ')' )?
	;

globalId
   : GLOBALLY ( AS '(' CHAR_STRING ')' )?
   ;

grantPrivilegeStatement
    : GRANT (ALL PRIVILEGES? | privilegeName (',' privilegeName)* ) ON objectName TO granteeName (WITH GRANT OPTION)? SEMICOLON?
	;

grantRoleStatement
    : GRANT roleName (',' roleName)* TO granteeName (WITH ADMIN)? SEMICOLON?
	;

revokeStatement
    : revokePrivilegeStatement
	| revokeRoleStatement
	;

revokePrivilegeStatement
    : REVOKE (GRANT OPTION OF)? ( ALL PRIVILEGES? | privilegeName ( ',' privilegeName )*) ON objectName FROM granteeName SEMICOLON?
	;

revokeRoleStatement
    : REVOKE roleName (',' roleName)* FROM granteeName SEMICOLON?
	;

privilegeName
    : SELECT 
	| CREATE
	| DELETE
	| INSERT
	| UPDATE
	| ALTER
	| DROP
	| EXECUTE
	| REFERENCE
	;

assignmentStatement
    : (general_element | bind_variable) ':=' expression
    ;

continueStatement
    : CONTINUE labelName? (WHEN condition)? SEMICOLON?
    ;

exitStatement
    : EXIT labelName? (WHEN condition)? SEMICOLON?
    ;

gotoStatement
    : GOTO labelName SEMICOLON?
    ;

ifStatement
    : IF condition THEN seq_of_statements elsifPart* elsePart? END IF
    ;

elsifPart
    : ELSIF condition THEN seq_of_statements
    ;

elsePart
    : ELSE seq_of_statements
    ;

loopStatement
    : labelName? (WHILE condition | FOR cursorLoopParam)? LOOP seq_of_statements END LOOP labelName?
    ;

// $<Loop - Specific Clause

cursorLoopParam
    : id IN REVERSE? lower_bound '..' upper_bound
    | record_name IN ( cursor_name expression_list? | '(' selectStatement ')')
    ;

// $>

forallStatement
    : FORALL id IN bounds_clause sql_statement
    ;

bounds_clause
    : lower_bound '..' upper_bound
    | VALUES OF id
    ;

between_bound
    : BETWEEN lower_bound AND upper_bound
    ;

lower_bound
    : concatenation
    ;

upper_bound
    : concatenation
    ;

nullStatement
    : NULL
    ;

raiseStatement
    : RAISE id? SEMICOLON?
    ;

returnStatement
    : RETURN condition? SEMICOLON?
    ;

functionCall
    : CALL? objectName function_argument?
    ;

body
    : BEGIN seq_of_statements exceptionClause? END labelName?
    ;

// $<Body - Specific Clause

exceptionClause
    : EXCEPTION exceptionHandler+
    ;

exceptionHandler
    : WHEN ( OTHERS | id (OR id)* ) THEN seq_of_statements
    ;

// $>

// $<Cursor Manipulation SQL PL/SQL Statements

cursorStatement
    : closeStatement
    | openStatement
    | fetchStatement
    | openForStatement
    ;

closeStatement
    : CLOSE cursor_name SEMICOLON?
    ;

openStatement
    : OPEN cursor_name expression_list? SEMICOLON?
    ;

fetchStatement
    : FETCH (fetchDirection FROM cursor_name | fetchDirection ) 
	   (INTO ( objectName | variable_name (',' variable_name )* ) )? SEMICOLON?
    ;

fetchDirection
    : (NEXT | PRIOR | FIRST | LAST | ABSOLUTE numeric | RELATIVE numeric)
	;

openForStatement
    : OPEN variable_name FOR (selectStatement | expression) SEMICOLON?
    ;

// $>

showStatement
    : SHOW (   SCHEMA
	         | TABLES 
	         | TABLE objectName
			 | OPEN SESSIONS
			 | CURRENT SESSION )
	;

// $<SQL PL/SQL Statements

sql_statement
    : executeImmediate
    | dmlStatement
    | cursorStatement
    | transactionControlStatement
    ;

executeImmediate
    : EXECUTE IMMEDIATE expression (into_clause | dynamic_returning_clause)?
    ;

// $<Execute Immediate - Specific Clause
dynamic_returning_clause
    : (RETURNING | RETURN) into_clause
    ;

// $>

// $<Transaction Control SQL PL/SQL Statements

transactionControlStatement
    : setTransactionCommand
    | set_constraint_command
    | commitStatement
    | rollbackStatement
    ;

setTransactionCommand
    : SET TRANSACTION 
      ( setTransactionAccess | setIsolationLevel | setIgnoreCase )
    ;

setTransactionAccess
   : READ ( ONLY | WRITE)
   ;

setIsolationLevel
   : ISOLATION LEVEL (SERIALIZABLE | READ COMMITTED | READ UNCOMMITTED )
   ;

setIgnoreCase
   : IGNORE IDENTIFIERS? CASE (ON | OFF)?
   ;

set_constraint_command
    : SET (CONSTRAINT | CONSTRAINTS) (ALL | constraint_name (',' constraint_name)*) (IMMEDIATE | DEFERRED)
    ;

commitStatement
    : COMMIT WORK? 
    ;

rollbackStatement
    : ROLLBACK WORK?
    ;


// $>

// $<Trigger DDLs

dropTriggerStatement
    : DROP CALLBACK? TRIGGER objectName (',' objectName)* SEMICOLON?
    ;

alterTriggerStatement
    : ALTER TRIGGER objectName alterTriggerAction SEMICOLON?
    ;

alterTriggerAction
    : triggerEnableAction 
	| triggerRenameAction
	;

triggerEnableAction
    : (ENABLE | DISABLE)
	;

triggerRenameAction
    : RENAME TO objectName
	;

createTriggerStatement
    : CREATE ( OR REPLACE )? CALLBACK? TRIGGER objectName
    (simple_dml_trigger | compound_dml_trigger | non_dml_trigger)
    (ENABLE | DISABLE)? trigger_when_clause? trigger_body SEMICOLON?
    ;

trigger_when_clause
    : WHEN '(' condition ')'
    ;

// $<Create Trigger- Specific Clauses
simple_dml_trigger
    : (BEFORE | AFTER | INSTEAD OF) dml_event_clause for_each_row?
    ;

for_each_row
    : FOR EACH ROW
    ;

compound_dml_trigger
    : FOR dml_event_clause
    ;

non_dml_trigger
    : (BEFORE | AFTER) non_dml_event (OR non_dml_event)* ON (DATABASE | (schema_name '.')? SCHEMA)
    ;

trigger_body
    : COMPOUND TRIGGER
    | CALL id
    | trigger_block
    ;

trigger_block
    : (DECLARE? declaration+)? body
    ;

routine_clause
    : objectName function_argument?
    ;

non_dml_event
    : ALTER
    | COMMENT
    | CREATE
    | DROP
    | GRANT
    | NOAUDIT
    | REVOKE
    | TRUNCATE
    | STARTUP
    | SHUTDOWN
    | SUSPEND
    | DATABASE
    | SCHEMA
    ;

dml_event_clause
    : dml_event_element (OR dml_event_element)* ON objectName
    ;

dml_event_element
    : (DELETE|INSERT|UPDATE) (OF id (',' id)*)?
    ;


// $>
// $>

deleteStatement
    : DELETE FROM? objectName whereClause? delete_limit?
    ;

delete_limit
    : LIMIT numeric
	;

insertStatement
    : INSERT (singleTableInsert | multiTableInsert) SEMICOLON?
    ;

// $<Insert - Specific Clauses

singleTableInsert
    : insertIntoClause (valuesClause | subquery)
	| insertSetClause
    ;

multiTableInsert
    : (ALL multiTableElement+ | conditionalInsertClause) selectStatement
    ;

multiTableElement
    : insertIntoClause valuesClause?
    ;

conditionalInsertClause
    : (ALL | FIRST)? conditionalInsertWhenPart+ conditional_insert_else_part?
    ;

conditionalInsertWhenPart
    : WHEN condition THEN multiTableElement+
    ;

conditional_insert_else_part
    : ELSE multiTableElement+
    ;

insertIntoClause
    : INTO objectName ('(' columnName (',' columnName)* ')')?
    ;

insertSetClause
    : INTO objectName SET insertAssignment ( ',' insertAssignment )*
	;

insertAssignment
    : columnName '=' expression
	;

valuesClause
    : VALUES expression_list ( ',' expression_list )*
    ;

// $>

// $<Common DDL Clauses

dml_table_expression_clause
    : ( '(' subquery ')' | objectName ) (AS? alias=regular_id)?
    ;

// $>


// $<PL/SQL Block


block
    : DECLARE? declaration* body
    ;


// $>

// $<Select - Specific Clauses

factoring_element
    : query_name ('(' id (',' id)* ')')? AS '(' subquery order_by_clause? ')'
    ;


subquery
    : subquery_basic_elements subquery_operation_part*
    ;

subquery_operation_part
    : (UNION ALL? | INTERSECT | ( MINUS | EXCEPT) ) subquery_basic_elements
    ;

subquery_basic_elements
    : query_block
    | '(' subquery ')'
    ;

query_block
    : SELECT (DISTINCT | UNIQUE | ALL)? (all='*' | selectedElement (',' selectedElement)*)
      into_clause? from_clause? whereClause? (group_by_clause | group_max_clause)?
    ;

selectedElement
    : (expression | selectedColumn ) column_alias?
    ;

from_clause
    : FROM table_ref_list
    ;

selectedColumn
    : objectName ('.' glob= '*')?
	;

table_ref_list
    : table_ref (',' table_ref)*
    ;

table_ref
    : dml_table_expression_clause join_clause*
    ;


join_clause
    : (CROSS | NATURAL)? (INNER | outer_join_type)? 
      JOIN dml_table_expression_clause join_on_part
    ;

join_on_part
    : ON condition
    ;

outer_join_type
    : (FULL | LEFT | RIGHT) OUTER?
    ;

start_part
    : START WITH condition
    ;

group_by_clause
    : GROUP BY group_by_elements (',' group_by_elements)* having_clause?
    | having_clause (GROUP BY group_by_elements (',' group_by_elements)*)?
    ;

group_max_clause
    : GROUP MAX objectName
	;

group_by_elements
    : expression
    ;

having_clause
    : HAVING condition
    ;

return_rows_clause
    : RETURN (UPDATED | ALL) ROWS
    ;

order_by_clause
    : ORDER BY order_by_elements (',' order_by_elements)*
    ;

order_by_elements
    : expression (ASC | DESC)? (NULLS (FIRST | LAST))?
    ;

for_update_clause
    : FOR UPDATE for_update_of_part? for_update_options?
    ;

for_update_of_part
    : OF id (',' id)*
    ;

for_update_options
    : SKIP LOCKED
    | NOWAIT
    | WAIT expression
    ;

// $>

// $<Expression & Condition
expression_unit
    : expression EOF
	;

cursor_expression
    : CURSOR '(' subquery ')'
    ;

expression_list
    : '(' expression? (',' expression)* ')'
    ;

condition
    : expression
    ;

condition_wrapper
    : expression
    ;

expression
    : cursor_expression
    | left=logical_and_expression ( op=OR right=logical_and_expression )*
    ;

expression_wrapper
    : expression
    ;

logical_and_expression
    : negated_expression ( AND negated_expression )*
    ;

negated_expression
    : NOT negated_expression
    | equality_expression
    ;

equality_expression
    : relational_expression (IS NOT? (NULL | NAN | PRESENT | A_LETTER SET | EMPTY | OF TYPE? '(' ONLY? type_spec (',' type_spec)* ')'))*
    ;

relational_expression
    : left=compound_expression
      (( op='=' | not_equal_op | op='<' | op='>' | less_than_or_equals_op | greater_than_or_equals_op ) right=compound_expression)*
    ;

compound_expression
    : exp=concatenation
      (NOT? (IN in_elements | BETWEEN min=concatenation AND max=concatenation | LIKE likeExp=concatenation like_escape_part?))?
    ;

like_escape_part
    : ESCAPE concatenation
    ;


in_elements
    : '(' subquery ')' #InSubquery
    | '(' concatenation_wrapper (',' concatenation_wrapper)* ')' #InArray
    | constant #InConstant
    | bind_variable #InVariable
    | general_element #InElement
    ;

concatenation
    : left=additive_expression (op=concatenation_op right=additive_expression)*
    ;

concatenation_wrapper
    : concatenation
    ;

additive_expression
    : left=multiply_expression ((op='+' | op='-') right=multiply_expression)*
    ;

multiply_expression
    : left=datetime_expression ((op='*' | op='/') right=datetime_expression)*
    ;

datetime_expression
    : unary_expression (AT (LOCAL | TIME ZONE concatenation_wrapper) | interval_expression)?
    ;

interval_expression
    : DAY ('(' concatenation_wrapper ')')? TO SECOND ('(' concatenation_wrapper ')')? #DayToSecondExpression
    | YEAR ('(' concatenation_wrapper ')')? TO MONTH #YearToMonthExpression
    ;

unary_expression
    : unaryplus_expression
    | unaryminus_expression
    | instantiate_expression
    | distinct_expression
    | all_expression
    | caseStatement/*[false]*/
    | quantifiedExpression
    | standard_function
    | atom
    ;

unaryplus_expression
    : '+' unary_expression
	;

unaryminus_expression
    : '-' unary_expression
	;

instantiate_expression
    : NEW unary_expression
	;

distinct_expression
    : DISTINCT unary_expression
	;

all_expression
    : ALL unary_expression
	;

caseStatement 
    : searchedCaseStatement
    | simpleCaseStatement
    ;

// $<CASE - Specific Clauses

simpleCaseStatement
    : labelName? CASE atom simpleCaseWhenPart+  caseElsePart? END CASE? labelName?
    ;

simpleCaseWhenPart
    : WHEN expression_wrapper THEN ( seq_of_statements  | expression_wrapper)
    ;

searchedCaseStatement
    : labelName? CASE searchedCaseWhenPart+ caseElsePart? END CASE? labelName?
    ;

searchedCaseWhenPart
    : WHEN condition_wrapper THEN ( seq_of_statements | expression_wrapper)
    ;

caseElsePart
    : ELSE ( seq_of_statements | expression_wrapper)
    ;
// $>

atom
    : objectName outer_join_sign
    | bind_variable
    | constant
    | general_element
    | '(' subquery ')' subquery_operation_part* 
	| subquery
	| group
    ;

group
    : '(' expression_or_vector ')'
	;

expression_or_vector
    : expression (vector_expr)?
    ;

vector_expr
    : ',' expression (',' expression)*
    ;

quantifiedExpression
    : (SOME | EXISTS | ALL | ANY) ('(' subquery ')' | expression_list )
    ;

standard_function
    : objectName '(' (argument (',' argument)*)? ')' #InvokedFunction
	| CURRENT_TIME #CurrentTimeFunction
	| CURRENT_TIMESTAMP #CurrentTimeStampFunction
	| CURRENT_DATE #CurrentDateFunction
	| NEXT VALUE FOR objectName #NextValueFunction
	| COUNT '(' (all='*' | ((DISTINCT | UNIQUE | ALL)? concatenation_wrapper)) ')' #CountFunction
    | CAST '(' (MULTISET '(' subquery ')' | concatenation_wrapper) AS type_spec ')' #CastFunction
    | EXTRACT '(' regular_id FROM concatenation_wrapper ')' #ExtractFunction
    | (FIRST_VALUE | LAST_VALUE) function_argument_analytic respect_or_ignore_nulls?
      '(' expression_wrapper (',' expression_wrapper)* ')' #FirstLastFunction
    | TREAT '(' expression_wrapper AS REF? type_spec ')' #TreatFunction
    | TRIM '(' ((LEADING | TRAILING | BOTH)? quoted_string? FROM)? concatenation_wrapper ')' #TrimFunction
    ;
   
// Common

column_alias
    : AS? (id | alias_quoted_string)
    | AS
    ;

table_alias
    : (id | alias_quoted_string)
    ;

alias_quoted_string
    : quoted_string
    ;

whereClause
    : WHERE (current_of_clause | condition_wrapper)
    ;

current_of_clause
    : CURRENT OF cursor_name
    ;

into_clause
    : INTO ( objectName | variable_name (',' variable_name)* )
    ;

// $>

// $<Common PL/SQL Specs

function_argument
    : '(' argument? (',' argument )* ')'
    ;

function_argument_analytic
    : '(' (argument respect_or_ignore_nulls?)? (',' argument respect_or_ignore_nulls?)* ')'
    ;

respect_or_ignore_nulls
    : (RESPECT | IGNORE) NULLS
    ;

argument
    : (id '=' '>')? expression_wrapper
    ;

type_spec
    : datatype #DataTypeSpec
    | REF? objectName (PERCENT_ROWTYPE | PERCENT_TYPE)?  #RefDataType
    ;

datatype
    : primitive_type #PrimitiveDataType
    | INTERVAL (top=YEAR | top=DAY) ('(' expression_wrapper ')')? TO (bottom=MONTH | bottom=SECOND) ('(' expression_wrapper ')')? #IntervalType
	| objectName type_argument? #UserDataType
    ;

type_argument
    : '(' type_argument_spec (',' type_argument_spec)* ')'
	;

type_argument_spec
    : ( id '=' '>' )? (numeric | quoted_string )
	;


primitive_type
    : (integer_type | numeric_type | boolean_type | string_type | binary_type | time_type)
	;

integer_type
    : (TINYINT | SMALLINT | BIGINT | INT | INTEGER) ('(' numeric ')')?
	;

numeric_type
    : (FLOAT | REAL | DOUBLE | NUMERIC | DECIMAL) ( '(' precision=numeric (',' scale=numeric)? ')' )?
	;

boolean_type
    : (BOOLEAN | BIT)
	;

binary_type
    : (BLOB | BINARY | VARBINARY | long_varbinary) ( '(' numeric ')' )?
	;

string_type
    : (CLOB | VARCHAR | CHAR | long_varchar | STRING) ( '(' numeric ')' )? 
	     (LOCALE locale=CHAR_STRING)? (ENCODING encoding=CHAR_STRING)? 
	;


long_varchar
    : LONG CHARACTER VARYING
	;

long_varbinary
    : LONG BINARY VARYING
	;

time_type
    : (DATE | TIME | TIMESTAMP) (WITH local=LOCAL? TIME ZONE)?
	;

bind_variable
    : (BINDVAR | ':' UNSIGNED_INTEGER)
    ;

general_element
    : objectName function_argument?
    ;

// $>

// $<Common PL/SQL Named Elements

attribute_name
    : id
    ;

savepoint_name
    : id
    ;

rollback_segment_name
    : id
    ;

table_var_name
    : id
    ;

schema_name
    : id
    ;

parameter_name
    : id
    ;

main_model_name
    : id
    ;

query_name
    : id
    ;

constraint_name
    : id ('.' id)* ('@' link_name)?
    ;

variable_name
    : id
    | bind_variable
    ;

cursor_name
    : id
    | bind_variable
    ;

record_name
    : id
    | bind_variable
    ;

collection_name
    : id ('.' id)?
    ;

link_name
    : id
    ;

objectName
   : id ('.' id)*
   ;

columnName
   : id
   ;

labelName
   : id
   ;

// $>

// $<Lexer Mappings

constant
    : TIMESTAMP (quoted_string | bind_variable) (AT TIME ZONE quoted_string)? #TimeStampConstant
    | INTERVAL (quoted_string | bind_variable | objectName)
      (DAY | HOUR | MINUTE | SECOND)
      ('(' (UNSIGNED_INTEGER | bind_variable) (',' (UNSIGNED_INTEGER | bind_variable) )? ')')?
      (TO ( DAY | HOUR | MINUTE | SECOND ('(' (UNSIGNED_INTEGER | bind_variable) ')')?))? #IntervalConstant
    | numeric #ConstantNumeric
    | DATE quoted_string #DateConstant
    | quoted_string #ConstantString
    | NULL #ConstantNull
    | TRUE #ConstantTrue
    | FALSE #ConstantFalse
    | DBTIMEZONE  #ConstantDBTimeZone
    | SESSIONTIMEZONE #ConstantSessionTimeZone
    | MINVALUE #ConstantMinValue
    | MAXVALUE #ConstantMaxValue
    ;

numeric
    : UNSIGNED_INTEGER
    | APPROXIMATE_NUM_LIT
    ;

quoted_string
    : CHAR_STRING
    //| CHAR_STRING_PERL
    | NATIONAL_CHAR_STRING_LIT
    ;

id
    : regular_id
    | DELIMITED_ID
    ;

not_equal_op
    : NOT_EQUAL_OP
    | '<' '>'
    | '!' '='
    | '^' '='
    ;

greater_than_or_equals_op
    : '>='
    | '>' '='
    ;

less_than_or_equals_op
    : '<='
    | '<' '='
    ;

concatenation_op
    : '||'
    | '|' '|'
    ;

outer_join_sign
    : '(' '+' ')'
    ;

regular_id
    : REGULAR_ID
    | A_LETTER
	| ABSOLUTE
    | ADD
	| ADMIN
    | AFTER
    | AGENT
    | AGGREGATE
    //| ALL
    //| ALTER
    //| AND
    //| ANY
    | ARRAY
    // | AS
    //| ASC
    | AT
    | ATTRIBUTE
    | AUTO
    | BEFORE
    //| BEGIN
    // | BETWEEN
    | BINARY
    | BLOB
    | BLOCK
    | BODY
    | BOOLEAN
    | BOTH
    // | BREADTH
    // | BY
    | BYTE
    | C_LETTER
    // | CACHE
    | CALL
	| CALLBACK
    | CASCADE
    //| CASE
    | CAST
    | CHAR
    | CHARACTER
    //| CHECK
    | CHR
    | CLOB
    | CLOSE
    | COLLECT
    | COLUMNS
    | COMMENT
    | COMMIT
    | COMMITTED
    | COMPOUND
    //| CONNECT
    //| CONNECT_BY_ROOT
    | CONSTANT
    | CONSTRAINT
    | CONSTRAINTS
    | CONSTRUCTOR
    | CONTENT
    | CONTINUE
    | CONVERT
    | COST
    | COUNT
    //| CREATE
    //| CURRENT
	| CURRENT_TIME
	| CURRENT_TIMESTAMP
    | CURRENT_USER
    | CURSOR
    | CYCLE
    | DATA
    | DATABASE
    | DATE
    | DAY
    | DBTIMEZONE
    | DEC
    | DECIMAL
    //| DECLARE
    | DECREMENT
    //| DEFAULT
    | DEFAULTS
    | DEFERRED
    // | DELETE
    // | DEPTH
    //| DESC
    | DETERMINISTIC
    | DISABLE
    //| DISTINCT
    | DOUBLE
    //| DROP
    | EACH
    //| ELSE
    //| ELSIF
    | EMPTY
    | ENABLE
    | ENCODING
    //| END
    | ESCAPE
    | EVALNAME
    | EXCEPTION
    | EXCEPTION_INIT
    | EXCEPTIONS
    | EXCLUDE
    //| EXCLUSIVE
    | EXECUTE
    //| EXISTS
    | EXIT
    | EXPLAIN
    | EXTERNAL
    | EXTRACT
    //| FALSE
    //| FETCH
    | FINAL
    | FIRST
    | FIRST_VALUE
    | FLOAT
    //| FOR
    | FORALL
    // | FROM
    | FULL
    | FUNCTION
    //| GOTO
    //| GRANT
    //| GROUP
    | GROUPING
    //| HAVING
    | HOUR
    //| IF
	| IDENTIFIED
	| IDENTIFIERS
	| IDENTITY
    | IGNORE
    | IMMEDIATE
    // | IN
    | INCREMENT
    //| INDEX
    | INNER
    | INOUT
    //| INSERT
    | INSTANTIABLE
    | INSTEAD
    | INT
    | INTEGER
    //| INTERSECT
    | INTERVAL
    // | INTO
    //| IS
    | ISOLATION
    | ITERATE
    | JOIN
    | KEEP
    | LANGUAGE
    | LAST
    | LAST_VALUE
    | LEADING
    | LEFT
    | LEVEL
    // | LIKE
    | LIMIT
    | LOCAL
	| LOCALE
    //| LOCK
    | LOCKED
    | LONG
    | LOOP
    | MAXVALUE
    | MEMBER
    | MERGE
    //| MINUS
    | MINUTE
    | MINVALUE
    //| MODE
    | MODIFY
    | MONTH
    | MULTISET
    | NAME
    | NAN
    | NATURAL
    | NATURALN
    | NAV
    | NESTED
    | NEW
	| NEXT
    | NO
    | NOAUDIT
    // | NOCACHE
    | NOCOPY
    | NOCYCLE
    //| NOMAXVALUE
    //| NOMINVALUE
    | NONE
    // | NOORDER
    //| NOT
    //| NOWAIT
    // | NULL
    | NULLS
    | NUMBER
    | NUMERIC
    | OBJECT
    //| OF
    | OFF
    | OID
    | OLD
    //| ON
    | ONLY
    | OPEN
    //| OPTION
    //| OR
    //| ORDER
	| OTHER
    | OUT
    | OUTER
    //| PERCENT_ROWTYPE
    //| PERCENT_TYPE
    //| PIVOT
    | PLAN
    | POSITIVE
    | POSITIVEN
    | PRAGMA
    | PRECEDING
    | PRECISION
    | PRESENT
    //| PRIOR
	| PRIVILEGES
    //| PROCEDURE
    | RAISE
    | RANGE
    | RAW
    | READ
    | REAL
    | RECORD
    | REF
    | REFERENCE
    | REFERENCING
	| RELATIVE
    | REPLACE
    | RESPECT
    | RESTRICT_REFERENCES
    | RESULT
    | RESULT_CACHE
    | RETURN
    | RETURNING
    | REUSE
    | REVERSE
    //| REVOKE
    | RIGHT
    | ROLLBACK
    | ROLLUP
    | ROW
    | ROWID
    | ROWS
    | RULES
    | SCHEMA
    // | SEARCH
    | SECOND
    | SEED
    // | SELECT
    | SELF
    // | SEQUENCE
    | SEQUENTIAL
    | SERIALIZABLE
	| SESSION
	| SESSIONS
    | SESSIONTIMEZONE
    | SET
    | SETS
    | SETTINGS
    //| SHARE
    | SHOW
    | SHUTDOWN
    | SINGLE
    //| SIZE
    | SKIP
    | SMALLINT
    | SNAPSHOT
    | SOME
    | STANDALONE
    //| START
    | STARTUP
    | STATEMENT
    | STATEMENT_ID
    | STATIC
    | STATISTICS
    | STRING
    | SUCCESS
    | SUSPEND
    //| TABLE
	| TABLES
    //| THE
    //| THEN
    | TIME
    | TIMESTAMP
    | TIMEZONE_ABBR
    | TIMEZONE_HOUR
    | TIMEZONE_MINUTE
    | TIMEZONE_REGION
    //| TO
    | TRAILING
    | TRANSACTION
    | TRANSLATE
    | TREAT
    | TRIGGER
    | TRIM
    //| TRUE
    | TRUNCATE
    | TYPE
    | UNBOUNDED
    | UNDER
    //| UNION
    //| UNIQUE
    | UNLIMITED
    //| UNPIVOT
    | UNTIL
    //| UPDATE
    | UPDATED
    | UPSERT
    | UROWID
    | USE
	| USER
    //| USING
    | VALUE
    //| VALUES
    | VARCHAR
    | VARIABLE
    | VARRAY
    | VARYING
    | VERSION
    | VERSIONS
    | WAIT
    | WARNING
    // | WHEN
    | WHENEVER
    // | WHERE
    | WHILE
    //| WITH
    | WORK
    | WRITE
    | YEAR
    | YES
    | ZONE
    | AVG
    | MAX
    | MIN
    | SUM
    ;
