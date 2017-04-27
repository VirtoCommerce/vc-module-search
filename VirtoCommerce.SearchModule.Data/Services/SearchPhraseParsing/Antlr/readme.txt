SearchPhrase.g4 file contains the grammar for the search phrase expression.

How to generate a parser

1. Download the ANTLR tool .jar file:
http://www.antlr.org/download.html

2. Add full path to the ANTLR tool .jar file to the CLASSPATH environment variable:
CLASSPATH=C:\Tools\antlr-4.7-complete.jar

3. Run the following command from the directory where the SearchPhrase.g4 is located:
java org.antlr.v4.Tool -package VirtoCommerce.SearchModule.Data.Services.SearchPhraseParsing.Antlr SearchPhrase.g4