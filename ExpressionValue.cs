using System.Numerics;

namespace Variant80Antlr;

public readonly record struct ExpressionValue
{
    public enum ValueKind
    {
        Integer,
        Boolean
    }

    private ExpressionValue(ValueKind kind, BigInteger integer, bool boolean)
    {
        Kind = kind;
        Integer = integer;
        Boolean = boolean;
    }

    public ValueKind Kind { get; }
    public BigInteger Integer { get; }
    public bool Boolean { get; }

    public static ExpressionValue FromInteger(BigInteger value) =>
        new(ValueKind.Integer, value, default);

    public static ExpressionValue FromBoolean(bool value) =>
        new(ValueKind.Boolean, default, value);

    public override string ToString() =>
        Kind == ValueKind.Integer
            ? Integer.ToString()
            : Boolean ? "ІСТИНА" : "ХИБНІСТЬ";
}
