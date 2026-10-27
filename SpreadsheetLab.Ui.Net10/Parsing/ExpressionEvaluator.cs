using Antlr4.Runtime;

namespace SpreadsheetLab.Ui.Parsing;

public static class ExpressionEvaluator
{
    public static object Evaluate(string expression, Func<string, object> getCellValue)
    {
        var input = new AntlrInputStream(expression);
        var lexer = new ExpressionLexer(input);
        var lexerErrorListener = new LexerErrorListener();
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(lexerErrorListener);

        var tokens = new CommonTokenStream(lexer);
        var parser = new ExpressionParser(tokens);
        var parserErrorListener = new SyntaxErrorListener();
        parser.RemoveErrorListeners();
        parser.AddErrorListener(parserErrorListener);
        ExpressionParser.CompileUnitContext tree = parser.compileUnit();

        string? errorMessage = lexerErrorListener.ErrorMessage ?? parserErrorListener.ErrorMessage;
        if (errorMessage is not null)
        {
            throw new Exception(errorMessage);
        }

        return new EvaluatorVisitor(getCellValue).Visit(tree);
    }
}
