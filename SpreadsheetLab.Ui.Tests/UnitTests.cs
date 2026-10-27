using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpreadsheetLab.Ui.Parsing;

namespace SpreadsheetLab.Ui.Tests;

[TestClass]
public class UnitTests
{
    // Тест 1: перевірка функцій пошуку максимального та мінімального значення.
    [TestMethod]
    public void CalculatesMmaxAndMmin()
    {
        var cells = new Dictionary<string, object>
        {
            ["A1"] = new BigInteger(10),
            ["A2"] = new BigInteger(20),
            ["A3"] = new BigInteger(5)
        };

        var maximum = ExpressionEvaluator.Evaluate("=mmax(A1,A2,A3)",
            address => cells[address]);
        var minimum = ExpressionEvaluator.Evaluate("=mmin(A1,A2,A3)",
            address => cells[address]);

        Assert.AreEqual(new BigInteger(20), maximum);
        Assert.AreEqual(new BigInteger(5), minimum);
    }

    // Тест 2: перевірка логічних операторів not, or, and і порівнянь клітинок.
    [TestMethod]
    public void CalculatesLogicalOperators()
    {
        var result = ExpressionEvaluator.Evaluate(
            "=not (A1 < A2) or A2 > A3 and A1 = A1",
            address => address == "A1" ? new BigInteger(10) : new BigInteger(20));

        Assert.AreEqual(true, result);
    }

    // Тест 3: перевірка повторного обчислення виразу після зміни значення клітинки.
    [TestMethod]
    public void RecalculatesExpressionAfterCellValueChanges()
    {
        var cells = new Dictionary<string, object>
        {
            ["A1"] = new BigInteger(10)
        };

        object Calculate() => ExpressionEvaluator.Evaluate("=A1 > 5", address => cells[address]);

        Assert.AreEqual(true, Calculate());

        cells["A1"] = new BigInteger(3);

        Assert.AreEqual(false, Calculate());
    }
}
