using Antlr4.Runtime;
using Lab1;
using System.Numerics;

var cells = new Dictionary<string, BigInteger>
{
    ["A1"] = 7,
    ["B1"] = 3,
    ["C1"] = 10
};

string[] expressions =
{
    "mmax(A1, B1, C1) > 9 and not (A1 = B1)",
    "mmin(100000000000000000000, 25, A1) = 7",
    "(A1 > B1) eqv (C1 > A1)",
    "A1 < B1 or C1 = 10"
};

foreach (string text in expressions)
{
    var input = new AntlrInputStream(text);
    var lexer = new ExpressionLexer(input);
    var tokens = new CommonTokenStream(lexer);
    var parser = new ExpressionParser(tokens);
    var tree = parser.compileUnit();

    if (parser.NumberOfSyntaxErrors > 0)
        throw new Exception("Некоректний вираз");

    var visitor = new EvaluatorVisitor(cells);
    object result = visitor.Visit(tree);
    Console.WriteLine(text + " = " + result);
}
