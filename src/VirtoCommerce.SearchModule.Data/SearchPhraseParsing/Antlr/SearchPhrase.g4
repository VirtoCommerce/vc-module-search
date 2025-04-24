grammar SearchPhrase;

options {
  language = CSharp;
}

searchPhrase : DL* expression DL* EOF;

expression
    : expression DL* OR DL* expression      # OrExpression
    | expression DL* AND DL* expression     # AndExpression
    | phrase (DL+ phrase)*                  # PhraseListExpression
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
    : rangeStart DL* lower? DL* RD DL* upper? DL* rangeEnd
    ;

rangeStart
    : RangeStart
    ;

rangeEnd
    : RangeEnd
    ;

lower
    : string
    ;

upper
    : string
    ;

negation : '!';

FD : ':';             // Filter delimiter
VD : ',';             // Value delimiter
RD : 'TO' | 'to';     // Range delimiter

AND : 'AND';
OR  : 'OR';

RangeStart : '[' | '(';
RangeEnd   : ']' | ')';

string
    : SimpleString
    | QuotedString
    ;

SimpleString : [\p{L}\p{N}_\-./]+;
QuotedString : '"' (Esc | ~["\\\r\n\t])* '"';
Esc          : '\\' (["\\rnt]);

DL : [ \t,]+; // Delimiters (spaces, tabs, commas)
