using System.Numerics;
using Variant80Antlr;

var cells = new Dictionary<string, ExpressionValue>(StringComparer.OrdinalIgnoreCase)
{
    ["A1"] = ExpressionValue.FromInteger(new BigInteger(7)),
    ["B1"] = ExpressionValue.FromInteger(new BigInteger(3)),
    ["C1"] = ExpressionValue.FromInteger(new BigInteger(10))
};

ExpressionValue ResolveCell(string name)
{
    if (cells.TryGetValue(name, out ExpressionValue value))
        return value;

    throw new ExpressionEvaluationException(
        $"Клітинка {name} не має значення.", 0, name.Length);
}

var examples = new[]
{
    "=mmax(A1, B1, C1) > 9 and not (A1 = B1)",
    "mmin(100000000000000000000, 25, A1) = 7",
    "(A1 > B1) eqv (C1 > A1)",
    "A1 < B1 or C1 = 10",
    "not (mmax(A1, B1) < C1)"
};

Console.WriteLine("ANTLR — лабораторна робота №1, варіант 80\n");

foreach (string expression in examples)
{
    try
    {
        ExpressionValue result = Variant80ExpressionService.Evaluate(expression, ResolveCell);
        Console.WriteLine($"{expression}  =>  {result}");
    }
    catch (ExpressionEvaluationException error)
    {
        Console.WriteLine($"{expression}  =>  ПОМИЛКА: {error.Message} (позиція {error.Position})");
    }
}
