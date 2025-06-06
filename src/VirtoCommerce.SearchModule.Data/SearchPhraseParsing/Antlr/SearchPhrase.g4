grammar SearchPhrase;

options {
  language = CSharp;
}

searchPhrase : (expression | keyword) (DL+ (expression | keyword))* EOF;

expression
    : '(' DL* expression DL* ')'               # ParenthesizedExpression
    | expression DL* OR DL* expression         # OrExpression
    | expression DL* AND DL* expression        # AndExpression
    | phrase (DL+ phrase)*                     # PhraseListExpression
    ;

phrase
    : filters
    | keyword
    ;

keyword
    : string
    ;

filters
    : negation? (attributeFilter | rangeFilter)
    ;

attributeFilter
    : fieldName FD attributeFilterValue
    ;

rangeFilter
    : fieldName FD rangeFilterValue
    ;

fieldName
    : string
    ;

attributeFilterValue
    : string (VD string)*
    ;

rangeFilterValue
    : range (VD range)*
    ;

range
    : rangeStart DL* lower=string? DL* RD DL* upper=string? DL* rangeEnd
    | rangeStart DL* lower=string? DL* rangeEnd  // Simplified form for single value
    ;

rangeStart
    : '[' | '('
    ;

rangeEnd
    : ']' | ')'
    ;

negation : '!';

FD : ':';             // Filter delimiter
VD : ',';             // Value delimiter
RD : 'TO' | 'to';     // Range delimiter

AND : 'AND';
OR  : 'OR';

string
    : SimpleString
    | QuotedString
    ;

SimpleString : [\p{L}\p{N}_\-./]+;
QuotedString : '"' (Esc | ~["\\\r\n\t])* '"';
Esc          : '\\' (["\\rnt]);

DL : [ \t,]+; // Delimiters (spaces, tabs, commas)
