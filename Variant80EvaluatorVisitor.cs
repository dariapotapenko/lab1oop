using System.Numerics;
using Antlr4.Runtime;
using Variant80Antlr.Generated;

namespace Variant80Antlr;

public sealed class Variant80EvaluatorVisitor : Variant80ExpressionBaseVisitor<ExpressionValue>
{
    private readonly Func<string, ExpressionValue> _resolveCell;

    public Variant80EvaluatorVisitor(Func<string, ExpressionValue>? resolveCell = null)
    {
        _resolveCell = resolveCell ?? (name => throw Error(
            $"Клітинка {name} не має значення.",
            0,
            name.Length));
    }

    public override ExpressionValue VisitCompileUnit(
        Variant80ExpressionParser.CompileUnitContext context) =>
        Visit(context.expression());

    public override ExpressionValue VisitExpression(
        Variant80ExpressionParser.ExpressionContext context) =>
        Visit(context.equivalence());

    public override ExpressionValue VisitEquivalence(
        Variant80ExpressionParser.EquivalenceContext context)
    {
        var operands = context.disjunction();
        ExpressionValue result = Visit(operands[0]);

        for (int i = 1; i < operands.Length; i++)
        {
            bool left = RequireBoolean(result, context.EQV(i - 1).Symbol);
            bool right = RequireBoolean(Visit(operands[i]), context.EQV(i - 1).Symbol);
            result = ExpressionValue.FromBoolean(left == right);
        }

        return result;
    }

    public override ExpressionValue VisitDisjunction(
        Variant80ExpressionParser.DisjunctionContext context)
    {
        var operands = context.conjunction();
        if (operands.Length == 1)
            return Visit(operands[0]);

        bool result = RequireBoolean(Visit(operands[0]), operands[0].Start);

        for (int i = 1; i < operands.Length; i++)
        {
            bool right = RequireBoolean(Visit(operands[i]), context.OR(i - 1).Symbol);
            result = result || right;
        }

        return ExpressionValue.FromBoolean(result);
    }

    public override ExpressionValue VisitConjunction(
        Variant80ExpressionParser.ConjunctionContext context)
    {
        var operands = context.negation();
        if (operands.Length == 1)
            return Visit(operands[0]);

        bool result = RequireBoolean(Visit(operands[0]), operands[0].Start);

        for (int i = 1; i < operands.Length; i++)
        {
            bool right = RequireBoolean(Visit(operands[i]), context.AND(i - 1).Symbol);
            result = result && right;
        }

        return ExpressionValue.FromBoolean(result);
    }

    public override ExpressionValue VisitNegation(
        Variant80ExpressionParser.NegationContext context)
    {
        if (context.NOT() is null)
            return Visit(context.comparison());

        bool operand = RequireBoolean(Visit(context.negation()), context.NOT().Symbol);
        return ExpressionValue.FromBoolean(!operand);
    }

    public override ExpressionValue VisitComparison(
        Variant80ExpressionParser.ComparisonContext context)
    {
        var operands = context.primary();
        ExpressionValue left = Visit(operands[0]);

        if (operands.Length == 1)
            return left;

        ExpressionValue right = Visit(operands[1]);
        IToken operation = context.GetChild(1).Payload as IToken
            ?? throw Error("Не вдалося визначити операцію порівняння.", context.Start);

        bool result = operation.Type switch
        {
            Variant80ExpressionParser.EQUAL => AreEqual(left, right, operation),
            Variant80ExpressionParser.LESS =>
                RequireInteger(left, operation) < RequireInteger(right, operation),
            Variant80ExpressionParser.GREATER =>
                RequireInteger(left, operation) > RequireInteger(right, operation),
            _ => throw Error("Невідома операція порівняння.", operation)
        };

        return ExpressionValue.FromBoolean(result);
    }

    public override ExpressionValue VisitPrimary(
        Variant80ExpressionParser.PrimaryContext context)
    {
        if (context.INTEGER() is { } integerNode)
        {
            return ExpressionValue.FromInteger(
                BigInteger.Parse(integerNode.GetText()));
        }

        if (context.CELL() is { } cellNode)
        {
            string name = cellNode.GetText().ToUpperInvariant();
            try
            {
                return _resolveCell(name);
            }
            catch (ExpressionEvaluationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ExpressionEvaluationException(
                    $"Не вдалося отримати значення клітинки {name}: {ex.Message}",
                    cellNode.Symbol.StartIndex,
                    name.Length);
            }
        }

        if (context.functionCall() is { } functionCall)
            return Visit(functionCall);

        return Visit(context.expression());
    }

    public override ExpressionValue VisitFunctionCall(
        Variant80ExpressionParser.FunctionCallContext context)
    {
        var argumentContexts = context.expression();
        var numbers = new BigInteger[argumentContexts.Length];

        for (int i = 0; i < argumentContexts.Length; i++)
        {
            numbers[i] = RequireInteger(
                Visit(argumentContexts[i]),
                argumentContexts[i].Start);
        }

        BigInteger result = numbers[0];
        bool isMaximum = context.MMAX() is not null;

        for (int i = 1; i < numbers.Length; i++)
        {
            if (isMaximum && numbers[i] > result || !isMaximum && numbers[i] < result)
                result = numbers[i];
        }

        return ExpressionValue.FromInteger(result);
    }

    private static bool AreEqual(
        ExpressionValue left,
        ExpressionValue right,
        IToken token)
    {
        if (left.Kind != right.Kind)
            throw Error("Не можна порівнювати число з логічним значенням.", token);

        return left.Kind == ExpressionValue.ValueKind.Integer
            ? left.Integer == right.Integer
            : left.Boolean == right.Boolean;
    }

    private static BigInteger RequireInteger(ExpressionValue value, IToken token)
    {
        if (value.Kind != ExpressionValue.ValueKind.Integer)
            throw Error("Для цієї операції потрібне ціле число.", token);

        return value.Integer;
    }

    private static bool RequireBoolean(ExpressionValue value, IToken token)
    {
        if (value.Kind != ExpressionValue.ValueKind.Boolean)
            throw Error("Для логічної операції потрібен результат порівняння.", token);

        return value.Boolean;
    }

    private static ExpressionEvaluationException Error(string message, IToken token) =>
        Error(message, token.StartIndex, token.Text?.Length ?? 1);

    private static ExpressionEvaluationException Error(
        string message,
        int position,
        int length = 1) =>
        new(message, position, length);
}
