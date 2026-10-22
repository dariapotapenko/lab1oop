using Antlr4.Runtime;
using System.Numerics;

namespace SpreadsheetLab.Ui.Parsing;

public sealed class EvaluatorVisitor : ExpressionBaseVisitor<object>
{
    private readonly Func<string, object> getCellValue;

    public EvaluatorVisitor(Func<string, object> getCellValue)
    {
        this.getCellValue = getCellValue;
    }

    public override object VisitCompileUnit(ExpressionParser.CompileUnitContext context)
    {
        return Visit(context.expression());
    }

    public override object VisitExpression(ExpressionParser.ExpressionContext context)
    {
        return Visit(context.equivalence());
    }

    public override object VisitEquivalence(ExpressionParser.EquivalenceContext context)
    {
        object result = Visit(context.disjunction(0));

        for (int i = 1; i < context.disjunction().Length; i++)
        {
            bool left = GetBoolean(result);
            bool right = GetBoolean(Visit(context.disjunction(i)));
            result = left == right;
        }

        return result;
    }

    public override object VisitDisjunction(ExpressionParser.DisjunctionContext context)
    {
        if (context.conjunction().Length == 1)
        {
            return Visit(context.conjunction(0));
        }

        bool result = GetBoolean(Visit(context.conjunction(0)));
        for (int i = 1; i < context.conjunction().Length; i++)
        {
            result = result || GetBoolean(Visit(context.conjunction(i)));
        }

        return result;
    }

    public override object VisitConjunction(ExpressionParser.ConjunctionContext context)
    {
        if (context.negation().Length == 1)
        {
            return Visit(context.negation(0));
        }

        bool result = GetBoolean(Visit(context.negation(0)));
        for (int i = 1; i < context.negation().Length; i++)
        {
            result = result && GetBoolean(Visit(context.negation(i)));
        }

        return result;
    }

    public override object VisitNegation(ExpressionParser.NegationContext context)
    {
        if (context.NOT() != null)
        {
            return !GetBoolean(Visit(context.negation()));
        }

        return Visit(context.comparison());
    }

    public override object VisitComparison(ExpressionParser.ComparisonContext context)
    {
        object left = Visit(context.primary(0));
        if (context.primary().Length == 1)
        {
            return left;
        }

        object right = Visit(context.primary(1));
        if (context.EQUAL() != null)
        {
            if (left.GetType() != right.GetType())
            {
                throw new Exception("Не можна порівнювати число з логічним значенням");
            }

            return left.Equals(right);
        }

        BigInteger first = GetNumber(left);
        BigInteger second = GetNumber(right);
        return context.LESS() != null ? first < second : first > second;
    }

    public override object VisitPrimary(ExpressionParser.PrimaryContext context)
    {
        if (context.INTEGER() != null)
        {
            return BigInteger.Parse(context.INTEGER().GetText());
        }

        if (context.CELL() != null)
        {
            return getCellValue(context.CELL().GetText().ToUpperInvariant());
        }

        if (context.functionCall() != null)
        {
            return Visit(context.functionCall());
        }

        return Visit(context.expression());
    }

    public override object VisitFunctionCall(ExpressionParser.FunctionCallContext context)
    {
        BigInteger result = GetNumber(Visit(context.expression(0)));

        for (int i = 1; i < context.expression().Length; i++)
        {
            BigInteger value = GetNumber(Visit(context.expression(i)));
            if (context.MMAX() != null && value > result)
            {
                result = value;
            }
            else if (context.MMIN() != null && value < result)
            {
                result = value;
            }
        }

        return result;
    }

    private static BigInteger GetNumber(object value)
    {
        return value is BigInteger number
            ? number
            : throw new Exception("Очікувалося числове значення");
    }

    private static bool GetBoolean(object value)
    {
        return value is bool boolean
            ? boolean
            : throw new Exception("Очікувалося логічне значення");
    }
}

public sealed class SyntaxErrorListener : BaseErrorListener
{
    public string? ErrorMessage { get; private set; }

    public override void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        IToken offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        ErrorMessage ??= $"Позиція {charPositionInLine + 1}: {msg}";
    }
}

public sealed class LexerErrorListener : IAntlrErrorListener<int>
{
    public string? ErrorMessage { get; private set; }

    public void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        int offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        ErrorMessage ??= $"Позиція {charPositionInLine + 1}: {msg}";
    }
}
