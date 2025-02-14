grammar SearchPhrase;

options {
  language=CSharp;
}

searchPhrase          : DL* phrase (DL phrase)* DL*;
phrase                : keyword | filters;
keyword               : string;
filters               : negation? (attributeFilter | rangeFilter);
attributeFilter       : fieldName FD attributeFilterValue;
rangeFilter           : fieldName FD rangeFilterValue;
fieldName             : string;
attributeFilterValue  : string (VD string)*;
rangeFilterValue      : range (VD range)*;
range                 : rangeStart DL* lower? DL* RD DL* upper? DL* rangeEnd;
rangeStart            : RangeStart;
rangeEnd              : RangeEnd;
lower                 : string;
upper                 : string;

negation              : '!';
FD                    : ':'; // Filter delimiter
VD                    : ','; // Value delimiter
RD                    : 'TO' | 'to'; // Range delimiter
RangeStart            : '[' | '(';
RangeEnd              : ']' | ')';

string                : SimpleString | QuotedString;

SimpleString          : [\p{L}\p{N}_\-./]+;
QuotedString          : '"' (Esc | ~["\\\r\n\t])* '"';
Esc                   : '\\' (["\\rnt]);

DL                    : [ \t,!]+; // Delimiter
