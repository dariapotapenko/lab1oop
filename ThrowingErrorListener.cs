using Antlr4.Runtime;

namespace Variant80Antlr;

internal sealed class ThrowingErrorListener : BaseErrorListener, IAntlrErrorListener<int>
{
    public override void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        IToken offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        int position = offendingSymbol?.StartIndex ?? charPositionInLine;
        int length = Math.Max(1, offendingSymbol?.Text?.Length ?? 1);
        throw new ExpressionEvaluationException(
            $"Синтаксична помилка: {TranslateMessage(msg)}",
            position,
            length);
    }

    public void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        int offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        throw new ExpressionEvaluationException(
            $"Синтаксична помилка: {TranslateMessage(msg)}",
            charPositionInLine);
    }

    private static string TranslateMessage(string message)
    {
        if (message.Contains("token recognition error", StringComparison.OrdinalIgnoreCase))
            return "невідомий символ";
        if (message.Contains("missing", StringComparison.OrdinalIgnoreCase))
            return "пропущено необхідний елемент";
        if (message.Contains("mismatched input", StringComparison.OrdinalIgnoreCase))
            return "неочікуваний елемент виразу";
        if (message.Contains("no viable alternative", StringComparison.OrdinalIgnoreCase))
            return "неправильна побудова виразу";

        return message;
    }
}
