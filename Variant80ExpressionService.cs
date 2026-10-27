using Antlr4.Runtime;
using Variant80Antlr.Generated;

namespace Variant80Antlr;

public static class Variant80ExpressionService
{
    public static ExpressionValue Evaluate(
        string expression,
        Func<string, ExpressionValue>? resolveCell = null)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new ExpressionEvaluationException("Вираз не може бути порожнім.", 0);

        var input = new AntlrInputStream(expression);
        var lexer = new Variant80ExpressionLexer(input);
        var errorListener = new ThrowingErrorListener();

        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(errorListener);

        var tokens = new CommonTokenStream(lexer);
        var parser = new Variant80ExpressionParser(tokens);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(errorListener);

        Variant80ExpressionParser.CompileUnitContext tree = parser.compileUnit();
        return new Variant80EvaluatorVisitor(resolveCell).Visit(tree);
    }
}
