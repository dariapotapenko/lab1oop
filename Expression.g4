grammar Expression;

compileUnit
    : EQUAL? expression EOF
    ;

expression
    : equivalence
    ;

equivalence
    : disjunction (EQV disjunction)*
    ;

disjunction
    : conjunction (OR conjunction)*
    ;

conjunction
    : negation (AND negation)*
    ;

negation
    : NOT negation
    | comparison
    ;

comparison
    : primary ((EQUAL | LESS | GREATER) primary)?
    ;

primary
    : INTEGER
    | CELL
    | functionCall
    | LPAREN expression RPAREN
    ;

functionCall
    : (MMAX | MMIN) LPAREN expression (COMMA expression)* RPAREN
    ;

MMAX    : 'mmax' | 'MMAX';
MMIN    : 'mmin' | 'MMIN';
NOT     : 'not' | 'NOT';
AND     : 'and' | 'AND';
OR      : 'or' | 'OR';
EQV     : 'eqv' | 'EQV';
EQUAL   : '=';
LESS    : '<';
GREATER : '>';
LPAREN  : '(';
RPAREN  : ')';
COMMA   : ',';
CELL    : [a-zA-Z]+ [1-9] [0-9]*;
INTEGER : [0-9]+;
WS      : [ \t\r\n]+ -> skip;
