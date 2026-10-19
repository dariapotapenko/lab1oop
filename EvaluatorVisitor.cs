using System.Numerics;

namespace Lab1;

public class EvaluatorVisitor : ExpressionBaseVisitor<object>
{
    private readonly Dictionary<string, BigInteger> cells;

    public EvaluatorVisitor(Dictionary<string, BigInteger> cells)
    {
        this.cells = cells;
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
            bool left = (bool)result;
            bool right = (bool)Visit(context.disjunction(i));
            result = left == right;
        }

        return result;
    }

    public override object VisitDisjunction(ExpressionParser.DisjunctionContext context)
    {
        if (context.conjunction().Length == 1)
            return Visit(context.conjunction(0));

        bool result = (bool)Visit(context.conjunction(0));

        for (int i = 1; i < context.conjunction().Length; i++)
            result = result || (bool)Visit(context.conjunction(i));

        return result;
    }

    public override object VisitConjunction(ExpressionParser.ConjunctionContext context)
    {
        if (context.negation().Length == 1)
            return Visit(context.negation(0));

        bool result = (bool)Visit(context.negation(0));

        for (int i = 1; i < context.negation().Length; i++)
            result = result && (bool)Visit(context.negation(i));

        return result;
    }

    public override object VisitNegation(ExpressionParser.NegationContext context)
    {
        if (context.NOT() != null)
            return !(bool)Visit(context.negation());

        return Visit(context.comparison());
    }

    public override object VisitComparison(ExpressionParser.ComparisonContext context)
    {
        object left = Visit(context.primary(0));

        if (context.primary().Length == 1)
            return left;

        object right = Visit(context.primary(1));

        if (context.EQUAL() != null)
            return left.Equals(right);

        BigInteger first = (BigInteger)left;
        BigInteger second = (BigInteger)right;

        if (context.LESS() != null)
            return first < second;

        return first > second;
    }

    public override object VisitPrimary(ExpressionParser.PrimaryContext context)
    {
        if (context.INTEGER() != null)
            return BigInteger.Parse(context.INTEGER().GetText());

        if (context.CELL() != null)
        {
            string address = context.CELL().GetText().ToUpper();

            if (cells.ContainsKey(address))
                return cells[address];

            throw new Exception("Клітинка " + address + " не має значення");
        }

        if (context.functionCall() != null)
            return Visit(context.functionCall());

        return Visit(context.expression());
    }

    public override object VisitFunctionCall(ExpressionParser.FunctionCallContext context)
    {
        BigInteger result = (BigInteger)Visit(context.expression(0));

        for (int i = 1; i < context.expression().Length; i++)
        {
            BigInteger value = (BigInteger)Visit(context.expression(i));

            if (context.MMAX() != null && value > result)
                result = value;

            if (context.MMIN() != null && value < result)
                result = value;
        }

        return result;
    }
}
