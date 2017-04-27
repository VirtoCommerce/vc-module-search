grammar SearchPhrase;

options {
  language=CSharp;
}

searchPhrase         : phrase (WS phrase)*;
phrase               : filter | keyword;
keyword              : String;
filter               : fieldName FD (attributeFilterValue | rangeFilterValue);
fieldName            : String;
attributeFilterValue : String;
rangeFilterValue     : Range;

FD: ':'; // Filter delimiter
Range  : RangeStart (RangeValue WS)? RD (WS RangeValue)? RangeEnd;
fragment RD: 'TO'; // Range delimiter
fragment RangeStart: '[' | '(';
fragment RangeEnd: ']' | ')';
fragment RangeValue: String;

String   : (~[: \t])+;
WS     : [ \t]+;
