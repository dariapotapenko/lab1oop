namespace Variant80Antlr;

public sealed class ExpressionEvaluationException : Exception
{
    public ExpressionEvaluationException(string message, int position, int length = 1)
        : base(message)
    {
        Position = Math.Max(0, position);
        Length = Math.Max(1, length);
    }

    public int Position { get; }
    public int Length { get; }
}
