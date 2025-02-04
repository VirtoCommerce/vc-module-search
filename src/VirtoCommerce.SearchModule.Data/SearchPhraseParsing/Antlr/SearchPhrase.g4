grammar SearchPhrase;

options {
  language=CSharp;
}

searchPhrase          : DL* phrase (DL phrase)* DL*;
phrase                : keyword | filters;
keyword               : String;
filters               : negation? (attributeFilter | rangeFilter);
attributeFilter       : fieldName FD attributeFilterValue;
rangeFilter           : fieldName FD rangeFilterValue;
fieldName             : String;
attributeFilterValue  : string (VD string)*;
rangeFilterValue      : range (VD range)*;
range                 : rangeStart DL* lower? DL* RD DL* upper? DL* rangeEnd;
rangeStart            : RangeStart;
rangeEnd              : RangeEnd;
lower                 : String;
upper                 : String;
string                : String;

negation              : '!';
FD                    : ':'; // Filter delimiter
VD                    : ','; // Value delimiter
RD                    : 'TO' | 'to'; // Range delimiter
RangeStart            : '[' | '(';
RangeEnd              : ']' | ')';

String                : SimpleString | QuotedString;

fragment SimpleString : [a-zA-Z0-9_-]+;
fragment QuotedString : ('"' (Esc | ~["\\\r\n\t])* '"');
fragment Esc          : '\\' (["\\rnt]);

DL                    : [ \t,]+; 
